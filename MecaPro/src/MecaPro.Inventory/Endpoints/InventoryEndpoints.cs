using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MecaPro.Inventory.Infrastructure;
using MecaPro.Domain.Modules.Inventory;

namespace MecaPro.Inventory.Endpoints;

public class InventoryModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inventory").WithTags("Inventory").RequireAuthorization();

        group.MapGet("/", async (InventoryDbContext db) => 
            await db.Parts.ToListAsync());

        group.MapGet("/{id:guid}", async (Guid id, InventoryDbContext db) =>
            await db.Parts.FindAsync(id) is Part p ? Results.Ok(p) : Results.NotFound());

        group.MapPost("/", async (Part part, InventoryDbContext db) =>
        {
            db.Parts.Add(part);
            await db.SaveChangesAsync();
            return Results.Created($"/api/v1/inventory/{part.Id}", part);
        });

        group.MapPatch("/{id:guid}/stock", async (Guid id, int delta, InventoryDbContext db) =>
        {
            var part = await db.Parts.FindAsync(id);
            if (part == null) return Results.NotFound();
            part.AdjustStock(delta);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
