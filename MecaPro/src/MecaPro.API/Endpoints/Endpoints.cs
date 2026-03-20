using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MecaPro.Application.Common;
using MecaPro.Infrastructure.Persistence;
using MecaPro.Infrastructure.Invoicing;
using MecaPro.Domain.Common;
using MecaPro.Infrastructure.Identity;

using MecaPro.Infrastructure.Identity;

namespace MecaPro.API.Endpoints;

// ─────────────────────────────────────────────────────────────
// CRM / CLIENTS (PHASE 1)
// ─────────────────────────────────────────────────────────────

public class CrmModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/customers").RequireAuthorization().WithTags("CRM");

        grp.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, IMediator mediator = null!) =>
        {
            var result = await mediator.Send(new GetCustomersPagedQuery(page, pageSize, search));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapPost("/", async (CreateCustomerCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/v1/customers/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        grp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetCustomerByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        grp.MapPut("/{id:guid}", async (Guid id, UpdateCustomerCommand cmd, IMediator mediator) =>
        {
            if (id != cmd.Id) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");

        grp.MapPost("/{id:guid}/loyalty", async (Guid id, AddLoyaltyPointsCommand cmd, IMediator mediator) =>
        {
            if (id != cmd.CustomerId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");
    }
}

// ─────────────────────────────────────────────────────────────
// VÉHICULES
// ─────────────────────────────────────────────────────────────

public class VehicleModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/vehicles").RequireAuthorization().WithTags("Vehicles");

        grp.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, IMediator mediator = null!) =>
        {
            var result = await mediator.Send(new GetVehiclesPagedQuery(page, pageSize, search, null));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        grp.MapPost("/", async (CreateVehicleCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicles/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        grp.MapGet("/qr/{token}", async (string token, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByQrQuery(token));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        grp.MapPost("/{id:guid}/qr", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GenerateQrCodeCommand(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");
    }
}

// ─────────────────────────────────────────────────────────────
// DIAGNOSTICS
// ─────────────────────────────────────────────────────────────

public class DiagnosticModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/diagnostics").RequireAuthorization("RequireMechanic").WithTags("Diagnostics");

        grp.MapPost("/", async (AddDiagnosticCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created("", result.Value) : Results.BadRequest(result.Errors);
        });
    }
}

// ─────────────────────────────────────────────────────────────
// DASHBOARD
// ─────────────────────────────────────────────────────────────

public class DashboardModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/dashboard").RequireAuthorization().WithTags("Dashboard");

        grp.MapGet("/stats", async (AppDbContext db) =>
        {
            var now = DateTime.UtcNow;
            return Results.Ok(new DashboardStatsDto(
                VehiclesInProgress: await db.Revisions.CountAsync(r => r.Status == RevisionStatus.InProgress),
                ActiveDiagnostics:  await db.Diagnostics.CountAsync(d => d.Status != DiagnosticStatus.Resolved),
                TotalClients:       await db.Customers.CountAsync(),
                TodayRevisions:     await db.Revisions.CountAsync(r => r.ScheduledDate.Date == now.Date)
            ));
        });
    }
}

// ─────────────────────────────────────────────────────────────
// PLANNING / RÉVISIONS
// ─────────────────────────────────────────────────────────────

public class PlanningModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/revisions").RequireAuthorization().WithTags("Planning");

        grp.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, IMediator mediator = null!) =>
        {
            var result = await mediator.Send(new GetRevisionsQuery(page, pageSize, search));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapGet("/schedule", async ([FromQuery] DateTime start, [FromQuery] DateTime end, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWorkshopScheduleQuery(start, end));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetRevisionDetailQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        grp.MapPost("/", async (CreateRevisionCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/revisions/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        grp.MapPost("/{id:guid}/status", async (Guid id, UpdateRevisionStatusCommand cmd, IMediator mediator) =>
        {
            if (id != cmd.Id) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");
    }
}

// ─────────────────────────────────────────────────────────────
// FACTURATION
// ─────────────────────────────────────────────────────────────

public class BillingModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/billing").RequireAuthorization().WithTags("Billing");

        grp.MapGet("/invoices", async (ICurrentUserService user, IMediator mediator) =>
        {
            if (!Guid.TryParse(user.UserId, out var userId))
                return Results.Unauthorized();
            var result = await mediator.Send(new GetInvoicesQuery(userId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}

// ─────────────────────────────────────────────────────────────
// PROFIL UTILISATEUR
// ─────────────────────────────────────────────────────────────

public class ProfileModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/profile").RequireAuthorization().WithTags("Profile");

        grp.MapGet("/", async (ICurrentUserService user, IMediator mediator) =>
        {
            if (string.IsNullOrEmpty(user.UserId)) return Results.Unauthorized();
            var result = await mediator.Send(new GetUserProfileQuery(user.UserId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}

// ─────────────────────────────────────────────────────────────
// STOCK / PIÈCES
// ─────────────────────────────────────────────────────────────

public class StockModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/parts").RequireAuthorization().WithTags("Stock");

        grp.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] string? category = null, IMediator mediator = null!) =>
        {
            var result = await mediator.Send(new GetPartsPagedQuery(page, pageSize, search, category));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapGet("/categories", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetPartCategoriesQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapPost("/{id:guid}/stock", async (Guid id, AdjustStockCommand cmd, IMediator mediator) =>
        {
            if (id != cmd.Id) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        });
    }
}
