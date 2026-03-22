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

// Handlers
public class InvoicingHandlers(IInvoiceRepository invoices) : 
    IRequestHandler<GetInvoicesQuery, Result<List<InvoiceDto>>>
{
    public async Task<Result<List<InvoiceDto>>> Handle(GetInvoicesQuery query, CancellationToken ct)
    {
        var items = await invoices.GetByCustomerIdAsync(query.CustomerId, ct);
        return Result<List<InvoiceDto>>.Success(items.Select(i => i.ToDto()).ToList());
    }
}
