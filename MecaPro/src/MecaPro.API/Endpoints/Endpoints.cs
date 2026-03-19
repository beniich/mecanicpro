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

namespace MecaPro.API.Endpoints;

public class VehicleModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/vehicles").RequireAuthorization().WithTags("Vehicles");

        grp.MapGet("/", async ([FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? search, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehiclesPagedQuery(page, pageSize, search, null));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        grp.MapPost("/", async (CreateVehicleCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/v1/vehicles/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");
    }
}

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

public class DashboardModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/dashboard").RequireAuthorization().WithTags("Dashboard");

        grp.MapGet("/stats", async (AppDbContext db, ICurrentUserService user) =>
        {
            var now = DateTime.UtcNow;
            return Results.Ok(new
            {
                vehiclesInProgress = await db.Revisions.CountAsync(r => r.Status == RevisionStatus.InProgress),
                activeDiagnostics = await db.Diagnostics.CountAsync(d => d.Status != DiagnosticStatus.Resolved),
                totalClients = await db.Customers.CountAsync(),
                todayRevisions = await db.Revisions.CountAsync(r => r.ScheduledDate.Date == now.Date)
            });
        });
    }
}
