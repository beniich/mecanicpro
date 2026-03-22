using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Customers;

namespace MecaPro.API.Endpoints.Modules;

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
