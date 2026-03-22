using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.Invoicing;

public class Invoice : AggregateRoot<Guid>
{
    public string Number { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Guid GarageId { get; set; }
    public decimal TotalTTC { get; set; }
    public string? Status { get; set; }
    public string? PdfBlobUrl { get; set; }
    public DateTime IssuedAt { get; set; }
}

public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
