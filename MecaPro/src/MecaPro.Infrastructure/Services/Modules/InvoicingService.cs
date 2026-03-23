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
        var totalHT = cmd.Lines.Sum(l => l.Qty * l.UnitPrice);
        var totalTTC = totalHT * 1.2m;
        
        var inv = new Invoice { 
            Id = Guid.NewGuid(), 
            Number = num, 
            CustomerId = cmd.CustomerId, 
            GarageId = cmd.GarageId, 
            TotalTTC = totalTTC, 
            IssuedAt = DateTime.UtcNow, 
            Status = "Issued" 
        };

        // Render HTML Invoice
        var templatePath = System.IO.Path.Combine(AppContext.BaseDirectory, "Templates", "invoice.html");
        // Fallback if not found in bin
        if (!System.IO.File.Exists(templatePath)) templatePath = "c:\\Users\\pc gold\\projet dash\\mecanicpro\\MecaPro\\src\\MecaPro.Infrastructure\\Templates\\invoice.html";
        
        var templateSource = await System.IO.File.ReadAllTextAsync(templatePath);
        var template = Scriban.Template.Parse(templateSource);
        
        var model = new {
            number = num,
            issued_at = inv.IssuedAt,
            customer_name = "Client MecaPro", // To be fetched from DB in prod
            customer_id = cmd.CustomerId,
            items = cmd.Lines.Select(l => new { description = l.Description, quantity = l.Qty, unit_price = l.UnitPrice, total_ht = l.Qty * l.UnitPrice }),
            total_ht = totalHT,
            total_vat = totalTTC - totalHT,
            total_ttc = totalTTC
        };

        var html = await template.RenderAsync(model);
        var pdfBytes = System.Text.Encoding.UTF8.GetBytes(html); // In production, use SkiaSharp/Puppeteer to convert to PDF

        inv.PdfBlobUrl = await blob.UploadAsync($"invoices/{num}.html", pdfBytes, "text/html");
        
        db.Invoices.Add(inv); 
        await db.SaveChangesAsync();
        
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
