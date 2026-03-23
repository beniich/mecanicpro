using System;
using System.Threading;
using System.Threading.Tasks;

namespace MecaPro.Domain.Modules.Invoicing;

public record CheckoutSessionResult(string SessionId, string CheckoutUrl);

public interface IPaymentService
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Guid invoiceId, decimal amount, string currency = "eur", CancellationToken ct = default);
}
