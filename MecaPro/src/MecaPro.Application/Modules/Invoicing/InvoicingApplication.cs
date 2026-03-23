using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Application.Common;
using MecaPro.Domain.Modules.Invoicing;
using MediatR;

namespace MecaPro.Application.Modules.Invoicing;

// DTOs
public record InvoiceDto(Guid Id, string Number, decimal Amount, DateTime Date, string Status, string? PdfUrl);

// Mapping Extensions
public static class InvoicingMappingExtensions
{
    public static InvoiceDto ToDto(this Invoice i) => new(i.Id, i.Number, i.TotalTTC, i.IssuedAt, i.Status ?? "Issued", i.PdfBlobUrl);
}

// Queries
public record GetInvoicesQuery(Guid CustomerId) : IRequest<Result<List<InvoiceDto>>>;
public record CreateCheckoutCommand(Guid InvoiceId) : IRequest<Result<CheckoutSessionResult>>;

// Handlers
public class InvoicingHandlers(IInvoiceRepository invoices, IPaymentService payments) : 
    IRequestHandler<GetInvoicesQuery, Result<List<InvoiceDto>>>,
    IRequestHandler<CreateCheckoutCommand, Result<CheckoutSessionResult>>
{
    public async Task<Result<List<InvoiceDto>>> Handle(GetInvoicesQuery query, CancellationToken ct)
    {
        var items = await invoices.GetByCustomerIdAsync(query.CustomerId, ct);
        return Result<List<InvoiceDto>>.Success(items.Select(i => i.ToDto()).ToList());
    }

    public async Task<Result<CheckoutSessionResult>> Handle(CreateCheckoutCommand command, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(command.InvoiceId, ct);
        if (invoice == null) return Result<CheckoutSessionResult>.Failure("Facture introuvable.");

        var result = await payments.CreateCheckoutSessionAsync(invoice.Id, invoice.TotalTTC, "eur", ct);
        return Result<CheckoutSessionResult>.Success(result);
    }
}
