using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Identity;

namespace MecaPro.API.Endpoints.Modules;

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
