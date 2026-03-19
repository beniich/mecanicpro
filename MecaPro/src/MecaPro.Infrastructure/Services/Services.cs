using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MecaPro.Domain.Common;
using MecaPro.Infrastructure.Persistence;
using SendGrid;
using SendGrid.Helpers.Mail;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using Stripe;
using Stripe.Checkout;
using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using MecaPro.Application.Common;

namespace MecaPro.Infrastructure.CRM
{
    public record Customer360Dto(CustomerDto Customer, IEnumerable<VehicleDto> Vehicles, IEnumerable<RevisionDto> RecentRevisions, IEnumerable<DiagnosticDto> ActiveDiagnostics, IEnumerable<OrderDto> RecentOrders, decimal LifetimeValue, string LoyaltyLevel);
    public record RevisionDto(); // Placeholder
    public record OrderDto(); // Placeholder
    public record CustomerDto(Guid Id, string FirstName, string LastName, string Email);

    public interface ICrmService { Task<Customer360Dto> GetCustomer360Async(Guid customerId); }
    public class CrmService(ICustomerRepository customers, AppDbContext db, IUnitOfWork uow) : ICrmService
    {
        public async Task<Customer360Dto> GetCustomer360Async(Guid customerId)
        {
            var customer = await customers.GetWithVehiclesAsync(customerId) ?? throw new NotFoundException("Customer", customerId);
            return new Customer360Dto(new CustomerDto(customer.Id, customer.Name.FirstName, customer.Name.LastName, customer.Email.Value), customer.Vehicles.Select(v => v.ToDto()), new List<RevisionDto>(), new List<DiagnosticDto>(), new List<OrderDto>(), 0, "Standard");
        }
    }
}

namespace MecaPro.Infrastructure.Notifications
{
    public enum NotificationChannel { Email, SMS, Push, InApp }
    public record NotificationRequest { public string UserId { get; set; } = null!; public string Title { get; set; } = null!; public string Body { get; set; } = null!; public string? TemplateId { get; set; } public NotificationChannel[] Channels { get; set; } = []; public Dictionary<string, object> Data { get; set; } = []; public string? Type { get; set; } public string? ActionUrl { get; set; } }

    public interface INotificationService { Task SendAsync(NotificationRequest request); }
    public interface IEmailService { Task SendAsync(string to, string subject, string html, string? text); Task SendTemplateAsync(string to, string templateId, Dictionary<string, object> data); }
    public interface ISmsService { Task SendAsync(string to, string body); }
    public interface IPushService { Task SendAsync(string userId, string title, string body, string? url); }
    public interface ISignalRNotifier { Task NotifyUserAsync(string userId, Notification n); }

    public class NotificationDispatcher(IEmailService email, ISmsService sms, AppDbContext db, ISignalRNotifier signalR) : INotificationService
    {
        public async Task SendAsync(NotificationRequest req)
        {
            var user = await db.Users.FindAsync(req.UserId);
            if (user == null) return;
            if (req.Channels.Contains(NotificationChannel.Email) && user.Email != null) await email.SendTemplateAsync(user.Email, req.TemplateId ?? "", req.Data);
            if (req.Channels.Contains(NotificationChannel.SMS) && user.PhoneNumber != null) await sms.SendAsync(user.PhoneNumber, req.Body);
            if (req.Channels.Contains(NotificationChannel.InApp))
            {
                var n = new Notification { UserId = req.UserId, Title = req.Title, Body = req.Body, Type = req.Type, ActionUrl = req.ActionUrl };
                db.Notifications.Add(n); await db.SaveChangesAsync();
                await signalR.NotifyUserAsync(req.UserId, n);
            }
        }
    }

    public class SendGridEmailService(IConfiguration config) : IEmailService
    {
        private readonly SendGridClient _client = new(config["SendGrid:ApiKey"]);
        public async Task SendAsync(string to, string subject, string html, string? text)
        {
            var msg = new SendGridMessage { From = new EmailAddress(config["SendGrid:FromEmail"]), Subject = subject, HtmlContent = html, PlainTextContent = text };
            msg.AddTo(new EmailAddress(to));
            await _client.SendEmailAsync(msg);
        }
        public async Task SendTemplateAsync(string to, string templateId, Dictionary<string, object> data)
        {
            var msg = new SendGridMessage(); msg.SetFrom(new EmailAddress(config["SendGrid:FromEmail"])); msg.AddTo(new EmailAddress(to)); msg.SetTemplateId(templateId); msg.SetTemplateData(data);
            await _client.SendEmailAsync(msg);
        }
    }

    public class TwilioSmsService(IConfiguration config) : ISmsService
    {
        public async Task SendAsync(string to, string body)
        {
            TwilioClient.Init(config["Twilio:AccountSid"], config["Twilio:AuthToken"]);
            await MessageResource.CreateAsync(to: new PhoneNumber(to), from: new PhoneNumber(config["Twilio:FromNumber"]), body: body);
        }
    }
}

namespace MecaPro.Infrastructure.Payment
{
    using MecaPro.Infrastructure.Notifications;

    public interface IStripeSubscriptionService { Task<CheckoutSessionResult> CreateSubscriptionCheckoutAsync(Guid garageId, Guid planId, string email); Task HandleWebhookAsync(string json, string sig, string secret); }
    public class StripeSubscriptionService(IConfiguration cfg, AppDbContext db, IUnitOfWork uow, INotificationService notifs) : IStripeSubscriptionService
    {
        private readonly StripeClient _stripe = new(cfg["Stripe:SecretKey"]);
        public async Task<CheckoutSessionResult> CreateSubscriptionCheckoutAsync(Guid garageId, Guid planId, string email)
        {
            var plan = await db.SubscriptionPlans.FindAsync(planId) ?? throw new NotFoundException("Plan", planId);
            var options = new SessionCreateOptions { Mode = "subscription", SuccessUrl = "https://mecapro.app/success", CancelUrl = "https://mecapro.app/cancel", LineItems = new List<SessionLineItemOptions> { new() { Price = plan.StripePriceIdMonthly, Quantity = 1 } } };
            var service = new SessionService(_stripe);
            var session = await service.CreateAsync(options);
            return new CheckoutSessionResult(session.Id, session.Url);
        }
        public async Task HandleWebhookAsync(string json, string sig, string secret) { /* webhook logic */ }
    }

    public record CheckoutSessionResult(string SessionId, string Url);
}

namespace MecaPro.Infrastructure.Invoicing
{
    using MecaPro.Infrastructure.Notifications;

    public interface IInvoiceService { Task<InvoiceDto> GenerateAsync(GenerateInvoiceCommand cmd); }
    public interface IBlobStorageService { Task<string> UploadAsync(string path, byte[] content, string contentType); }
    public interface IInvoiceSequencer { Task<string> GetNextAsync(string prefix, Guid garageId); }

    public class InvoiceService(AppDbContext db, IBlobStorageService blob, IEmailService email, IInvoiceSequencer sequencer) : IInvoiceService
    {
        public async Task<InvoiceDto> GenerateAsync(GenerateInvoiceCommand cmd)
        {
            var num = await sequencer.GetNextAsync("INV", cmd.GarageId);
            var inv = new MecaPro.Infrastructure.Persistence.Invoice { Id = Guid.NewGuid(), Number = num, CustomerId = cmd.CustomerId, GarageId = cmd.GarageId, TotalTTC = cmd.Lines.Sum(l => l.Qty * l.UnitPrice) * 1.2m, IssuedAt = DateTime.UtcNow, Status = "Issued" };
            var pdf = Encoding.UTF8.GetBytes($"Invoice {num}");
            inv.PdfBlobUrl = await blob.UploadAsync($"invoices/{num}.pdf", pdf, "application/pdf");
            db.Invoices.Add(inv); await db.SaveChangesAsync();
            return new InvoiceDto(inv.Id, inv.Number, inv.TotalTTC, inv.IssuedAt, inv.PdfBlobUrl);
        }
    }

    public class InvoiceSequencer(AppDbContext db) : IInvoiceSequencer
    {
        public async Task<string> GetNextAsync(string prefix, Guid gid) => $"{prefix}-{DateTime.UtcNow.Year}-{(await db.Invoices.CountAsync() + 1):D4}";
    }

    public record InvoiceDto(Guid Id, string Number, decimal TotalTTC, DateTime IssuedAt, string PdfBlobUrl);
    public record GenerateInvoiceCommand(Guid CustomerId, Guid GarageId, List<InvoiceLine> Lines);
    public record InvoiceLine(string Description, int Qty, decimal UnitPrice);

    public class MockBlobStorageService : IBlobStorageService
    {
        public Task<string> UploadAsync(string path, byte[] content, string contentType) => Task.FromResult($"https://mockstorage.local/{path}");
    }

    public class MockSignalRNotifier : ISignalRNotifier
    {
        public Task NotifyUserAsync(string userId, Notification n) => Task.CompletedTask;
    }
}

