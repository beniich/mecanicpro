using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MecaPro.Infrastructure.Persistence;
using MecaPro.Domain.Modules.Invoicing;

namespace MecaPro.Infrastructure.Modules.Invoicing;

public record InvoiceResultDto(Guid Id, string Number, decimal TotalTTC, DateTime IssuedAt, string PdfBlobUrl);
public record GenerateInvoiceCommand(Guid CustomerId, Guid GarageId, List<InvoiceLine> Lines);
public record InvoiceLine(string Description, int Qty, decimal UnitPrice);

public interface IInvoiceService { Task<InvoiceResultDto> GenerateAsync(GenerateInvoiceCommand cmd); }
public interface IBlobStorageService { Task<string> UploadAsync(string path, byte[] content, string contentType); }
public interface IInvoiceSequencer { Task<string> GetNextAsync(string prefix, Guid garageId); }

public class InvoiceService(AppDbContext db, IBlobStorageService blob, IInvoiceSequencer sequencer) : IInvoiceService
{
    public async Task<InvoiceResultDto> GenerateAsync(GenerateInvoiceCommand cmd)
    {
        var num = await sequencer.GetNextAsync("INV", cmd.GarageId);
        var inv = new Invoice { Id = Guid.NewGuid(), Number = num, CustomerId = cmd.CustomerId, GarageId = cmd.GarageId, TotalTTC = cmd.Lines.Sum(l => l.Qty * l.UnitPrice) * 1.2m, IssuedAt = DateTime.UtcNow, Status = "Issued" };
        var pdf = System.Text.Encoding.UTF8.GetBytes($"Invoice {num}");
        inv.PdfBlobUrl = await blob.UploadAsync($"invoices/{num}.pdf", pdf, "application/pdf");
        db.Invoices.Add(inv); await db.SaveChangesAsync();
        return new InvoiceResultDto(inv.Id, inv.Number, inv.TotalTTC, inv.IssuedAt, inv.PdfBlobUrl ?? "");
    }
}

public class InvoiceSequencer(AppDbContext db) : IInvoiceSequencer
{
    public async Task<string> GetNextAsync(string prefix, Guid gid) => $"{prefix}-{DateTime.UtcNow.Year}-{(await db.Invoices.CountAsync() + 1):D4}";
}

public class MockBlobStorageService : IBlobStorageService
{
    public Task<string> UploadAsync(string path, byte[] content, string contentType) => Task.FromResult($"https://mockstorage.local/{path}");
}
