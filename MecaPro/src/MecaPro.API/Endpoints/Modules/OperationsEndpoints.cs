using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Operations;
using MecaPro.Infrastructure.Persistence;
using MecaPro.Domain.Modules.Operations;

namespace MecaPro.API.Endpoints.Modules;

public class OperationsModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // ─────────────────────────────────────────────────────────────
        // VÉHICULES
        // ─────────────────────────────────────────────────────────────
        var vGrp = app.MapGroup("/api/v1/vehicles").RequireAuthorization().WithTags("Vehicles");

        vGrp.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, IMediator mediator = null!) =>
        {
            var result = await mediator.Send(new GetVehiclesPagedQuery(page, pageSize, search, null));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        vGrp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        vGrp.MapPost("/", async (CreateVehicleCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/vehicles/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        vGrp.MapGet("/qr/{token}", async (string token, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByQrQuery(token));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        vGrp.MapPost("/{id:guid}/qr", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GenerateQrCodeCommand(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");

        // ─────────────────────────────────────────────────────────────
        // DIAGNOSTICS
        // ─────────────────────────────────────────────────────────────
        var dGrp = app.MapGroup("/api/v1/diagnostics").RequireAuthorization("RequireMechanic").WithTags("Diagnostics");

        dGrp.MapPost("/", async (AddDiagnosticCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created("", result.Value) : Results.BadRequest(result.Errors);
        });

        // ─────────────────────────────────────────────────────────────
        // PLANNING / RÉVISIONS
        // ─────────────────────────────────────────────────────────────
        var rGrp = app.MapGroup("/api/v1/revisions").RequireAuthorization().WithTags("Planning");

        rGrp.MapGet("/", async ([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, IMediator mediator = null!) =>
        {
            var result = await mediator.Send(new GetRevisionsQuery(page, pageSize, search));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        rGrp.MapGet("/schedule", async ([FromQuery] DateTime start, [FromQuery] DateTime end, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetWorkshopScheduleQuery(start, end));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        rGrp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetRevisionDetailQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        rGrp.MapPost("/", async (CreateRevisionCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess
                ? Results.Created($"/api/v1/revisions/{result.Value!.Id}", result.Value)
                : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        rGrp.MapPost("/{id:guid}/status", async (Guid id, UpdateRevisionStatusCommand cmd, IMediator mediator) =>
        {
            if (id != cmd.Id) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireMechanic");

        // ─────────────────────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────────────────────
        var dashGrp = app.MapGroup("/api/v1/dashboard").RequireAuthorization().WithTags("Dashboard");

        dashGrp.MapGet("/stats", async (AppDbContext db) =>
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
