using System;
using System.Threading.Tasks;
using Stripe;
using Stripe.Checkout;
using Microsoft.Extensions.Configuration;
using MecaPro.Infrastructure.Persistence;
using MecaPro.Domain.Common;
using MassTransit;
using MecaPro.Domain.Common.Events;

namespace MecaPro.Infrastructure.Modules.Payment;

public record CheckoutSessionResult(string SessionId, string Url);

public interface IStripeSubscriptionService { Task<CheckoutSessionResult> CreateSubscriptionCheckoutAsync(Guid garageId, Guid planId, string email); Task HandleWebhookAsync(string json, string sig, string secret); }

public class StripeSubscriptionService(IConfiguration cfg, AppDbContext db, IUnitOfWork uow, IPublishEndpoint publishEndpoint) : IStripeSubscriptionService
{
    private readonly StripeClient _stripe = new(cfg["Stripe:SecretKey"]);
    public async Task<CheckoutSessionResult> CreateSubscriptionCheckoutAsync(Guid garageId, Guid planId, string email)
    {
        var plan = await db.SubscriptionPlans.FindAsync(planId) ?? throw new Domain.Common.NotFoundException("Plan", planId);
        var options = new SessionCreateOptions { Mode = "subscription", SuccessUrl = "https://mecapro.app/success", CancelUrl = "https://mecapro.app/cancel", LineItems = new List<SessionLineItemOptions> { new() { Price = plan.StripePriceIdMonthly, Quantity = 1 } } };
        var service = new SessionService(_stripe);
        var session = await service.CreateAsync(options);
        
        // Example of async notification instead of direct call
        await publishEndpoint.Publish(new SendNotificationEvent { 
            Email = email, 
            Title = "Checkout Initiated", 
            Body = "Your checkout session is ready.",
            Channels = ["Email"]
        });

        return new CheckoutSessionResult(session.Id, session.Url);
    }
    public async Task HandleWebhookAsync(string json, string sig, string secret) { /* webhook logic */ }
}
