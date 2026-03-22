using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Inventory;

namespace MecaPro.API.Endpoints.Modules;

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
