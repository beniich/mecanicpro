using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MecaPro.Application.Modules.Operations;

namespace MecaPro.API.Endpoints.Modules;

public class VehicleEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/vehicles").RequireAuthorization().WithTags("Vehicles");

        grp.MapPost("/", async (CreateVehicleCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/v1/vehicles/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        grp.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetVehicleByIdQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        // Diagnostics attached to vehicle
        grp.MapPost("/{id:guid}/diagnostics", async (Guid id, AddDiagnosticCommand cmd, IMediator mediator) =>
        {
            if (id != cmd.VehicleId) return Results.BadRequest("ID mismatch");
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");
    }
}
