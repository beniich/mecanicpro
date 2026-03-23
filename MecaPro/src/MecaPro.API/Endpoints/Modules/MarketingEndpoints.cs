using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using MecaPro.Application.Modules.Feedback;

namespace MecaPro.API.Endpoints.Modules;

public class MarketingEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/marketing").RequireAuthorization().WithTags("Marketing & Feedback");

        // Surveys
        grp.MapPost("/surveys/send", async (SendSurveyCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/v1/marketing/surveys/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        // Response submission (Allowed anonymous for customers)
        app.MapPost("/api/v1/marketing/surveys/submit", async ([FromBody] SubmitSurveyResponseCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok(new { success = true, message = "Thank you for your feedback!" }) : Results.BadRequest(result.Error);
        }).AllowAnonymous();

        // Stats
        grp.MapGet("/nps", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetNpsStatsQuery());
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });
    }
}
