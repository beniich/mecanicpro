using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Invoicing;

namespace MecaPro.API.Endpoints.Modules;

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
