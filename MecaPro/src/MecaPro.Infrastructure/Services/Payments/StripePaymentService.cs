using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MecaPro.Domain.Modules.Invoicing;
using Stripe;
using Stripe.Checkout;

namespace MecaPro.Infrastructure.Services.Payments;

public class StripePaymentService : IPaymentService
{
    private readonly string _secretKey;
    private readonly string _successUrl;
    private readonly string _cancelUrl;

    public StripePaymentService(IConfiguration config)
    {
        _secretKey = config["Stripe:SecretKey"] ?? throw new ArgumentNullException("Stripe:SecretKey is missing");
        StripeConfiguration.ApiKey = _secretKey;
        
        // In a real app, these would come from config (frontend URLs)
        _successUrl = "http://localhost:5000/billing/success?session_id={CHECKOUT_SESSION_ID}";
        _cancelUrl = "http://localhost:5000/billing/cancel";
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Guid invoiceId, decimal amount, string currency = "eur", CancellationToken ct = default)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(amount * 100), // Stripe uses cents
                        Currency = currency,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Facture MECAPRO #{invoiceId}",
                            Description = "Maintenance Automobile Experte",
                        },
                    },
                    Quantity = 1,
                },
            },
            Mode = "payment",
            SuccessUrl = _successUrl,
            CancelUrl = _cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { "invoice_id", invoiceId.ToString() }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options, cancellationToken: ct);

        return new CheckoutSessionResult(session.Id, session.Url);
    }
}
