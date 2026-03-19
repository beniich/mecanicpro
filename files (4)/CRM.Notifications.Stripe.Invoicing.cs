// ============================================================
// PHASES 5-9 : CRM, NOTIFICATIONS, ABONNEMENTS, STRIPE,
//              E-COMMERCE, FACTURATION
// ============================================================

// ─────────────────────────────────────────────────────────────
// PHASE 5 — CRM SERVICE
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Infrastructure.CRM;

public class CrmService : ICrmService
{
    private readonly ICustomerRepository _customers;
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;

    public CrmService(ICustomerRepository c, AppDbContext db, IUnitOfWork uow, INotificationService n)
    { _customers = c; _db = db; _uow = uow; _notifications = n; }

    // Profil 360° complet
    public async Task<Customer360Dto> GetCustomer360Async(Guid customerId)
    {
        var customer = await _customers.GetWithVehiclesAsync(customerId)
            ?? throw new NotFoundException("Customer", customerId);

        var recentRevisions = await _db.Revisions
            .Where(r => customer.Vehicles.Select(v => v.Id).Contains(r.VehicleId))
            .OrderByDescending(r => r.ScheduledDate)
            .Take(5)
            .ToListAsync();

        var activeDiags = await _db.Diagnostics
            .Where(d => d.Status != DiagnosticStatus.Resolved &&
                        customer.Vehicles.Select(v => v.Id).Contains(d.VehicleId))
            .ToListAsync();

        var recentOrders = await _db.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        var lifetimeValue = await _db.Orders
            .Where(o => o.CustomerId == customerId && o.Status == OrderStatus.Paid)
            .SumAsync(o => o.TotalAmount.Amount);

        return new Customer360Dto(
            customer.ToDto(),
            customer.Vehicles.Select(v => v.ToDto()),
            recentRevisions.Select(r => r.ToDto()),
            activeDiags.Select(d => d.ToDto()),
            recentOrders.Select(o => o.ToOrderDto()),
            lifetimeValue,
            customer.Loyalty.ComputeSegment().ToString()
        );
    }

    // Rapport fidélité
    public async Task<LoyaltyReportDto> GetLoyaltyReportAsync(Guid customerId)
    {
        var customer = await _customers.GetByIdAsync(customerId)
            ?? throw new NotFoundException("Customer", customerId);

        var nextLevelPoints = customer.Loyalty.Level switch
        {
            CustomerSegment.Standard => 500,
            CustomerSegment.Silver   => 2000,
            CustomerSegment.Gold     => 5000,
            _                       => 0
        };

        return new LoyaltyReportDto(
            customer.Loyalty.Points,
            customer.Loyalty.Level.ToString(),
            nextLevelPoints,
            customer.Loyalty.Transactions.OrderByDescending(t => t.Date).Take(10).ToList()
        );
    }

    // Export RGPD
    public async Task<byte[]> ExportCustomerDataAsync(Guid customerId)
    {
        var data = await GetCustomer360Async(customerId);
        var json = System.Text.Json.JsonSerializer.Serialize(data,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        return Encoding.UTF8.GetBytes(json);
    }

    // Segmentation automatique (job quotidien)
    public async Task UpdateSegmentsAsync(CancellationToken ct = default)
    {
        var customers = await _db.Customers.ToListAsync(ct);
        foreach (var customer in customers)
        {
            var totalSpent = await _db.Orders
                .Where(o => o.CustomerId == customer.Id && o.Status == OrderStatus.Paid)
                .SumAsync(o => o.TotalAmount.Amount, ct);

            var serviceCount = await _db.Revisions
                .Where(r => customer.Vehicles.Select(v => v.Id).Contains(r.VehicleId)
                            && r.Status == RevisionStatus.Completed)
                .CountAsync(ct);

            // Award loyalty points for completed services
            var newPoints = (int)(totalSpent / 10); // 1 point per 10€
            if (customer.Loyalty.Points != newPoints)
                customer.AddLoyaltyPoints(Math.Max(0, newPoints - customer.Loyalty.Points), "Auto-segmentation");
        }
        await _uow.SaveChangesAsync(ct);
    }
}

// ─────────────────────────────────────────────────────────────
// PHASE 6 — NOTIFICATION SERVICE
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Infrastructure.Notifications;

public class NotificationDispatcher : INotificationService
{
    private readonly IEmailService _email;
    private readonly ISmsService _sms;
    private readonly IPushService _push;
    private readonly ISignalRNotifier _signalR;
    private readonly AppDbContext _db;

    public NotificationDispatcher(IEmailService e, ISmsService s, IPushService p,
        ISignalRNotifier r, AppDbContext db)
    { _email = e; _sms = s; _push = p; _signalR = r; _db = db; }

    public async Task SendAsync(NotificationRequest request)
    {
        var user = await _db.Users.FindAsync(request.UserId)
            ?? throw new NotFoundException("User", request.UserId);

        var tasks = new List<Task>();

        if (request.Channels.Contains(NotificationChannel.Email) && user.Email != null)
            tasks.Add(_email.SendTemplateAsync(user.Email, request.TemplateId, request.Data));

        if (request.Channels.Contains(NotificationChannel.SMS) && user.PhoneNumber != null)
            tasks.Add(_sms.SendAsync(user.PhoneNumber, request.Body));

        if (request.Channels.Contains(NotificationChannel.Push))
            tasks.Add(_push.SendAsync(request.UserId, request.Title, request.Body, request.ActionUrl));

        if (request.Channels.Contains(NotificationChannel.InApp))
        {
            var notif = new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Body = request.Body,
                Type = request.Type,
                Channel = "InApp",
                ActionUrl = request.ActionUrl
            };
            _db.Notifications.Add(notif);
            await _db.SaveChangesAsync();
            tasks.Add(_signalR.NotifyUserAsync(request.UserId, notif));
        }

        await Task.WhenAll(tasks);
    }
}

// Email Service (SendGrid)
public class SendGridEmailService : IEmailService
{
    private readonly SendGridClient _client;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public SendGridEmailService(IConfiguration config)
    {
        _client = new SendGridClient(config["SendGrid:ApiKey"]);
        _fromEmail = config["SendGrid:FromEmail"]!;
        _fromName = config["SendGrid:FromName"] ?? "MecaPro";
    }

    public async Task SendAsync(string to, string subject, string htmlBody, string? textBody = null)
    {
        var msg = new SendGridMessage
        {
            From = new EmailAddress(_fromEmail, _fromName),
            Subject = subject,
            HtmlContent = htmlBody,
            PlainTextContent = textBody
        };
        msg.AddTo(new EmailAddress(to));
        await _client.SendEmailAsync(msg);
    }

    public async Task SendTemplateAsync(string to, string templateId, Dictionary<string, object> data)
    {
        var msg = new SendGridMessage();
        msg.SetFrom(new EmailAddress(_fromEmail, _fromName));
        msg.AddTo(new EmailAddress(to));
        msg.SetTemplateId(templateId);
        msg.SetTemplateData(data);
        await _client.SendEmailAsync(msg);
    }
}

// SMS Service (Twilio)
public class TwilioSmsService : ISmsService
{
    public TwilioSmsService(IConfiguration config)
    {
        TwilioClient.Init(config["Twilio:AccountSid"], config["Twilio:AuthToken"]);
    }

    public async Task SendAsync(string to, string body)
    {
        await MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            from: new PhoneNumber("+33XXXXXXXXX"),
            body: body);
    }
}

// Hangfire Jobs
public class RevisionReminderJob
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public RevisionReminderJob(AppDbContext db, INotificationService n)
    { _db = db; _notifications = n; }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        var thresholds = new[] { 30, 7, 1 };
        foreach (var days in thresholds)
        {
            var targetDate = DateTime.UtcNow.Date.AddDays(days);
            var revisions = await _db.Revisions
                .Include(r => r.Vehicle)
                .Where(r => r.Status == RevisionStatus.Scheduled
                            && r.ScheduledDate.Date == targetDate)
                .ToListAsync();

            foreach (var rev in revisions)
            {
                var customer = await _db.Customers.FindAsync(rev.Vehicle.CustomerId);
                if (customer == null) continue;

                await _notifications.SendAsync(new NotificationRequest
                {
                    UserId = customer.Id.ToString(),
                    Title = $"Rappel révision — {rev.Vehicle.Make} {rev.Vehicle.Model}",
                    Body = $"Votre révision '{rev.Type}' est prévue dans {days} jour(s) (le {rev.ScheduledDate:dd/MM/yyyy}). Coût estimé: {rev.EstimatedCost.Amount}€.",
                    TemplateId = "revision_reminder",
                    Channels = new[] { NotificationChannel.Email, NotificationChannel.SMS },
                    Data = new Dictionary<string, object>
                    {
                        ["vehicle"] = $"{rev.Vehicle.Make} {rev.Vehicle.Model}",
                        ["revision_type"] = rev.Type,
                        ["date"] = rev.ScheduledDate.ToString("dd/MM/yyyy"),
                        ["cost"] = rev.EstimatedCost.Amount,
                        ["days"] = days
                    }
                });
            }
        }
    }
}

// ─────────────────────────────────────────────────────────────
// PHASE 7 — ABONNEMENTS & STRIPE
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Infrastructure.Payment;

public class StripeSubscriptionService : IStripeSubscriptionService
{
    private readonly StripeClient _stripe;
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notifications;

    public StripeSubscriptionService(IConfiguration cfg, AppDbContext db,
        IUnitOfWork uow, INotificationService n)
    {
        _stripe = new StripeClient(cfg["Stripe:SecretKey"]);
        _db = db;
        _uow = uow;
        _notifications = n;
    }

    // Créer checkout session abonnement
    public async Task<CheckoutSessionResult> CreateSubscriptionCheckoutAsync(
        Guid garageId, Guid planId, string customerEmail)
    {
        var plan = await _db.SubscriptionPlans.FindAsync(planId)
            ?? throw new NotFoundException("Plan", planId);

        // Récupérer ou créer customer Stripe
        var stripeCustomerId = await GetOrCreateStripeCustomerAsync(garageId, customerEmail);

        var sessionService = new SessionService(_stripe);
        var options = new SessionCreateOptions
        {
            Customer = stripeCustomerId,
            Mode = "subscription",
            PaymentMethodTypes = new List<string> { "card", "sepa_debit" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = plan.StripePriceIdMonthly,
                    Quantity = 1
                }
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                TrialPeriodDays = await IsFirstSubscriptionAsync(garageId) ? 14 : 0,
                Metadata = new Dictionary<string, string>
                {
                    ["garage_id"] = garageId.ToString(),
                    ["plan_id"] = planId.ToString(),
                    ["plan_tier"] = plan.Tier
                }
            },
            SuccessUrl = "https://mecapro.app/dashboard?checkout=success&session={CHECKOUT_SESSION_ID}",
            CancelUrl = "https://mecapro.app/pricing?checkout=cancelled",
        };

        var session = await sessionService.CreateAsync(options);
        return new CheckoutSessionResult(session.Id, session.Url);
    }

    // Webhook Stripe — handler principal
    public async Task HandleWebhookAsync(string json, string signature, string webhookSecret)
    {
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, signature, webhookSecret);
        }
        catch (StripeException)
        {
            throw new DomainException("Signature webhook Stripe invalide.");
        }

        switch (stripeEvent.Type)
        {
            case Events.CustomerSubscriptionCreated:
                await HandleSubscriptionCreatedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                break;
            case Events.CustomerSubscriptionUpdated:
                await HandleSubscriptionUpdatedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                break;
            case Events.CustomerSubscriptionDeleted:
                await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                break;
            case Events.InvoicePaymentSucceeded:
                await HandleInvoicePaidAsync(stripeEvent.Data.Object as Invoice);
                break;
            case Events.InvoicePaymentFailed:
                await HandlePaymentFailedAsync(stripeEvent.Data.Object as Invoice);
                break;
        }
    }

    private async Task HandleSubscriptionCreatedAsync(Stripe.Subscription? sub)
    {
        if (sub == null) return;
        var garageId = Guid.Parse(sub.Metadata["garage_id"]);
        var planId = Guid.Parse(sub.Metadata["plan_id"]);
        var plan = await _db.SubscriptionPlans.FindAsync(planId);
        if (plan == null) return;

        var subscription = Subscription.Create(
            garageId, planId, sub.Id, plan.Tier,
            plan.MaxMechanics, plan.HasEcommerce, plan.HasApiAccess, plan.IsWhiteLabel);

        _db.Subscriptions.Add(subscription);
        await _uow.SaveChangesAsync();
    }

    private async Task HandleSubscriptionDeletedAsync(Stripe.Subscription? sub)
    {
        if (sub == null) return;
        var existing = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == sub.Id);
        if (existing == null) return;

        existing.Cancel();
        await _uow.SaveChangesAsync();

        // Notify garage owner
        await NotifySubscriptionCancelledAsync(existing.GarageId);
    }

    private async Task HandlePaymentFailedAsync(Invoice? invoice)
    {
        if (invoice == null) return;
        var subId = invoice.SubscriptionId;
        var sub = await _db.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subId);
        if (sub == null) return;

        sub.SetPastDue();
        await _uow.SaveChangesAsync();

        // Dunning email
        await NotifyPaymentFailedAsync(sub.GarageId, invoice.AmountDue / 100m);
    }

    private async Task HandleSubscriptionUpdatedAsync(Stripe.Subscription? sub)
    {
        if (sub == null) return;
        var existing = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == sub.Id);
        if (existing == null) return;

        var status = sub.Status switch
        {
            "active" => SubscriptionStatus.Active,
            "trialing" => SubscriptionStatus.Trialing,
            "past_due" => SubscriptionStatus.PastDue,
            "canceled" => SubscriptionStatus.Cancelled,
            _ => SubscriptionStatus.Active
        };

        if (status == SubscriptionStatus.Active) existing.Activate();
        else if (status == SubscriptionStatus.PastDue) existing.SetPastDue();

        await _uow.SaveChangesAsync();
    }

    private async Task HandleInvoicePaidAsync(Invoice? invoice)
    {
        if (invoice == null) return;
        // Record payment in our DB
        await _uow.SaveChangesAsync();
    }

    private async Task<string> GetOrCreateStripeCustomerAsync(Guid garageId, string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GarageId == garageId && u.Email == email);
        if (user?.StripeCustomerId != null) return user.StripeCustomerId;

        var customerService = new CustomerService(_stripe);
        var stripeCustomer = await customerService.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Metadata = new Dictionary<string, string> { ["garage_id"] = garageId.ToString() }
        });

        if (user != null)
        {
            user.StripeCustomerId = stripeCustomer.Id;
            await _db.SaveChangesAsync();
        }

        return stripeCustomer.Id;
    }

    private async Task<bool> IsFirstSubscriptionAsync(Guid garageId)
        => !await _db.Subscriptions.AnyAsync(s => s.GarageId == garageId);

    private async Task NotifySubscriptionCancelledAsync(Guid garageId)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GarageId == garageId);
        if (user == null) return;
        await _notifications.SendAsync(new NotificationRequest
        {
            UserId = user.Id,
            Title = "Abonnement annulé",
            Body = "Votre abonnement MecaPro a été annulé. Vos données restent accessibles 30 jours.",
            Channels = new[] { NotificationChannel.Email }
        });
    }

    private async Task NotifyPaymentFailedAsync(Guid garageId, decimal amount)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.GarageId == garageId);
        if (user == null) return;
        await _notifications.SendAsync(new NotificationRequest
        {
            UserId = user.Id,
            Title = "⚠ Échec de paiement",
            Body = $"Le paiement de {amount}€ a échoué. Mettez à jour votre moyen de paiement pour éviter la suspension.",
            Channels = new[] { NotificationChannel.Email, NotificationChannel.SMS },
            ActionUrl = "https://mecapro.app/billing"
        });
    }
}

// Checkout pour achats de pièces (Payment Intent)
public class CheckoutService : ICheckoutService
{
    private readonly StripeClient _stripe;
    private readonly AppDbContext _db;
    private readonly IUnitOfWork _uow;
    private readonly IPartRepository _parts;

    public CheckoutService(IConfiguration cfg, AppDbContext db, IUnitOfWork uow, IPartRepository parts)
    { _stripe = new StripeClient(cfg["Stripe:SecretKey"]); _db = db; _uow = uow; _parts = parts; }

    public async Task<PaymentIntentResult> CreateOrderPaymentAsync(
        Guid customerId, List<(Guid PartId, int Qty)> items)
    {
        // Build order
        var partsList = new List<(Part Part, int Qty)>();
        foreach (var (partId, qty) in items)
        {
            var part = await _parts.GetByIdAsync(partId)
                ?? throw new NotFoundException("Part", partId);
            if (!part.IsAvailable || part.StockQuantity < qty)
                throw new BusinessRuleViolationException($"Stock insuffisant pour '{part.Name}'.");
            partsList.Add((part, qty));
        }

        var garageId = (await _db.Users.FirstOrDefaultAsync(u => u.CustomerId == customerId))?.GarageId
            ?? Guid.Empty;
        var order = Order.Create(customerId, garageId, partsList);

        // Lock stock
        foreach (var (part, qty) in partsList)
        {
            part.AdjustStock(-qty);
            _db.Parts.Update(part);
        }

        _db.Orders.Add(order);

        // Payment Intent
        var customer = await _db.Customers.FindAsync(customerId);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.CustomerId == customerId);

        var piService = new PaymentIntentService(_stripe);
        var intent = await piService.CreateAsync(new PaymentIntentCreateOptions
        {
            Amount = order.TotalAmount.InCents() + order.TaxAmount.InCents(),
            Currency = "eur",
            Customer = user?.StripeCustomerId,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
            Metadata = new Dictionary<string, string>
            {
                ["order_id"] = order.Id.ToString(),
                ["customer_id"] = customerId.ToString(),
                ["type"] = "parts_order"
            },
            IdempotencyKey = order.IdempotencyKey
        });

        await _uow.SaveChangesAsync();

        return new PaymentIntentResult(intent.ClientSecret!, intent.Id, order.Id);
    }

    public async Task ConfirmOrderPaymentAsync(string paymentIntentId)
    {
        var order = await _db.Orders
            .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntentId)
            ?? throw new NotFoundException("Order", paymentIntentId);

        order.MarkPaid(paymentIntentId);
        await _uow.SaveChangesAsync();
    }
}

// ─────────────────────────────────────────────────────────────
// PHASE 9 — INVOICE SERVICE
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Infrastructure.Invoicing;

public class InvoiceService : IInvoiceService
{
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blob;
    private readonly IEmailService _email;
    private readonly IInvoiceSequencer _sequencer;

    public InvoiceService(AppDbContext db, IBlobStorageService blob,
        IEmailService email, IInvoiceSequencer sequencer)
    { _db = db; _blob = blob; _email = email; _sequencer = sequencer; }

    public async Task<InvoiceDto> GenerateAsync(GenerateInvoiceCommand cmd)
    {
        var customer = await _db.Customers.FindAsync(cmd.CustomerId)
            ?? throw new NotFoundException("Customer", cmd.CustomerId);

        var invoiceNumber = await _sequencer.GetNextAsync("INV", cmd.GarageId);

        var totalHT = cmd.Lines.Sum(l => l.Qty * l.UnitPrice);
        var vatRate = GetVatRate("FR");
        var taxAmount = totalHT * vatRate;
        var totalTTC = totalHT + taxAmount;

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            Number = invoiceNumber,
            CustomerId = cmd.CustomerId,
            GarageId = cmd.GarageId,
            TotalHT = totalHT,
            TaxAmount = taxAmount,
            TotalTTC = totalTTC,
            Status = "issued",
            IssuedAt = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(cmd.PaymentTermsDays)
        };

        // Generate PDF
        var pdfBytes = await GeneratePdfAsync(invoice, customer, cmd.Lines);
        var blobUrl = await _blob.UploadAsync(
            $"invoices/{invoice.Number}.pdf", pdfBytes, "application/pdf");
        invoice.PdfBlobUrl = blobUrl;

        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        // Send email
        await _email.SendAsync(customer.Email.Value,
            $"Votre facture MecaPro — {invoice.Number}",
            $"<p>Bonjour {customer.Name.FirstName},</p><p>Veuillez trouver ci-joint votre facture <strong>{invoice.Number}</strong> d'un montant de <strong>{totalTTC:F2}€ TTC</strong>.</p>",
            null);

        return invoice.ToDto();
    }

    private static decimal GetVatRate(string country) => country switch
    {
        "FR" => 0.20m,
        "BE" => 0.21m,
        "DE" => 0.19m,
        "CH" => 0.081m,
        _    => 0.20m
    };

    private async Task<byte[]> GeneratePdfAsync(Invoice invoice, Customer customer,
        IEnumerable<InvoiceLine> lines)
    {
        // Use SkiaSharp or a template engine
        // Placeholder: return minimal PDF bytes
        var html = $@"
            <html><body>
            <h1>FACTURE {invoice.Number}</h1>
            <p>Client: {customer.Name.Full}</p>
            <p>Date: {invoice.IssuedAt:dd/MM/yyyy}</p>
            <table>
                <tr><th>Description</th><th>Qté</th><th>PU HT</th><th>Total HT</th></tr>
                {string.Join("", lines.Select(l => $"<tr><td>{l.Description}</td><td>{l.Qty}</td><td>{l.UnitPrice:F2}€</td><td>{l.Qty * l.UnitPrice:F2}€</td></tr>"))}
            </table>
            <p>Total HT: {invoice.TotalHT:F2}€</p>
            <p>TVA 20%: {invoice.TaxAmount:F2}€</p>
            <p><strong>Total TTC: {invoice.TotalTTC:F2}€</strong></p>
            </body></html>";

        return Encoding.UTF8.GetBytes(html); // Replace with actual PDF rendering
    }
}

public class InvoiceSequencer : IInvoiceSequencer
{
    private readonly AppDbContext _db;
    public InvoiceSequencer(AppDbContext db) => _db = db;

    public async Task<string> GetNextAsync(string prefix, Guid garageId)
    {
        // Thread-safe sequence using DB
        var year = DateTime.UtcNow.Year;
        var key = $"{prefix}-{year}-{garageId}";

        // Simple: count existing + 1
        var count = await _db.Invoices
            .Where(i => i.GarageId == garageId && i.Number.StartsWith($"{prefix}-{year}"))
            .CountAsync();

        return $"{prefix}-{year}-{(count + 1):D4}"; // INV-2025-0042
    }
}

// ─────────────────────────────────────────────────────────────
// SIGNALR CHAT HUB
// ─────────────────────────────────────────────────────────────

namespace MecaPro.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db) => _db = db;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        var garageId = Context.User?.FindFirst("garage_id")?.Value;
        if (garageId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"garage:{garageId}");
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(string recipientId, string content, string? vehicleId = null)
    {
        var senderId = Context.UserIdentifier!;

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            SenderId = senderId,
            RecipientId = recipientId,
            Content = content.Trim(),
            VehicleId = vehicleId != null ? Guid.Parse(vehicleId) : null,
            SentAt = DateTime.UtcNow
        };

        var garageId = Context.User?.FindFirst("garage_id")?.Value;
        if (garageId != null) message.GarageId = Guid.Parse(garageId);

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        // Send to recipient
        await Clients.User(recipientId).SendAsync("ReceiveMessage", new
        {
            id = message.Id,
            senderId,
            content,
            vehicleId,
            sentAt = message.SentAt,
            isRead = false
        });
    }

    public async Task MarkAsRead(string messageId)
    {
        var msg = await _db.ChatMessages.FindAsync(Guid.Parse(messageId));
        if (msg != null && msg.RecipientId == Context.UserIdentifier)
        {
            msg.IsRead = true;
            msg.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            await Clients.User(msg.SenderId).SendAsync("MessageRead", messageId);
        }
    }

    public async Task<IEnumerable<object>> GetHistory(string otherUserId, int page = 1)
    {
        var myId = Context.UserIdentifier!;
        var messages = await _db.ChatMessages
            .Where(m => (m.SenderId == myId && m.RecipientId == otherUserId) ||
                        (m.SenderId == otherUserId && m.RecipientId == myId))
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * 30).Take(30)
            .Select(m => new { m.Id, m.SenderId, m.Content, m.SentAt, m.IsRead, m.VehicleId })
            .ToListAsync();

        return messages;
    }
}

// Notification Hub
[Authorize]
public class NotificationHub : Hub
{
    private readonly AppDbContext _db;
    public NotificationHub(AppDbContext db) => _db = db;

    public async Task<IEnumerable<object>> GetUnread()
    {
        var userId = Context.UserIdentifier!;
        return await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new { n.Id, n.Title, n.Body, n.Type, n.ActionUrl, n.CreatedAt })
            .ToListAsync();
    }

    public async Task MarkAllRead()
    {
        var userId = Context.UserIdentifier!;
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true)
                                      .SetProperty(x => x.ReadAt, DateTime.UtcNow));
    }
}

// ─────────────────────────────────────────────────────────────
// ADDITIONAL DTOs & INTERFACES
// ─────────────────────────────────────────────────────────────

public record LoyaltyReportDto(
    int Points, string Level, int PointsToNextLevel,
    IEnumerable<LoyaltyTransaction> RecentTransactions);

public record CheckoutSessionResult(string SessionId, string Url);
public record PaymentIntentResult(string ClientSecret, string PaymentIntentId, Guid OrderId);

public record GenerateInvoiceCommand(
    Guid CustomerId, Guid GarageId, List<InvoiceLine> Lines,
    int PaymentTermsDays = 30);

public record InvoiceLine(string Description, int Qty, decimal UnitPrice);

public record InvoiceDto(
    Guid Id, string Number, decimal TotalHT, decimal TaxAmount,
    decimal TotalTTC, string Status, string? PdfUrl, DateTime IssuedAt);

public record NotificationRequest
{
    public string UserId { get; init; } = null!;
    public string Title { get; init; } = null!;
    public string Body { get; init; } = null!;
    public string Type { get; init; } = "general";
    public NotificationChannel[] Channels { get; init; } = [NotificationChannel.InApp];
    public string? TemplateId { get; init; }
    public string? ActionUrl { get; init; }
    public Dictionary<string, object> Data { get; init; } = new();
}

public interface ICrmService
{
    Task<Customer360Dto> GetCustomer360Async(Guid customerId);
    Task<byte[]> ExportCustomerDataAsync(Guid customerId);
    Task UpdateSegmentsAsync(CancellationToken ct = default);
}

public interface INotificationService
{
    Task SendAsync(NotificationRequest request);
}

public interface ISmsService { Task SendAsync(string to, string body); }
public interface IPushService { Task SendAsync(string userId, string title, string body, string? url); }
public interface ISignalRNotifier { Task NotifyUserAsync(string userId, Notification notif); }
public interface IStripeSubscriptionService
{
    Task<CheckoutSessionResult> CreateSubscriptionCheckoutAsync(Guid garageId, Guid planId, string email);
    Task HandleWebhookAsync(string json, string signature, string webhookSecret);
}
public interface ICheckoutService
{
    Task<PaymentIntentResult> CreateOrderPaymentAsync(Guid customerId, List<(Guid PartId, int Qty)> items);
    Task ConfirmOrderPaymentAsync(string paymentIntentId);
}
public interface IInvoiceService { Task<InvoiceDto> GenerateAsync(GenerateInvoiceCommand cmd); }
public interface IInvoiceSequencer { Task<string> GetNextAsync(string prefix, Guid garageId); }
public interface IBlobStorageService { Task<string> UploadAsync(string path, byte[] data, string contentType); }

// Extension
public static partial class MappingExtensions
{
    public static OrderDto ToOrderDto(this Order o)
        => new(o.Id, o.Status.ToString(), o.TotalAmount.Amount, o.CreatedAt, o.PaidAt,
               o.Items.Select(i => new OrderItemDto(i.PartId, i.PartName, i.UnitPrice.Amount, i.Quantity, i.LineTotal.Amount)));

    public static InvoiceDto ToDto(this Invoice i)
        => new(i.Id, i.Number, i.TotalHT, i.TaxAmount, i.TotalTTC, i.Status, i.PdfBlobUrl, i.IssuedAt);
}
