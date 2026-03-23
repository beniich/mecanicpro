using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MecaPro.Application.Modules.HR;
using MecaPro.Application.Common;

namespace MecaPro.API.Endpoints.Modules;

public class HrEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var grp = app.MapGroup("/api/v1/hr").RequireAuthorization().WithTags("Human Resources");

        // Absences
        grp.MapPost("/absences", async (RequestAbsenceCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/v1/hr/absences/{result.Value!.Id}", result.Value) : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic"); // Simplified role check

        grp.MapGet("/absences/employee/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetEmployeeAbsencesQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        grp.MapPost("/absences/{id:guid}/approve", async (Guid id, ICurrentUserService user, IMediator mediator) =>
        {
            var result = await mediator.Send(new ApproveAbsenceCommand(id, user.UserId ?? "SYSTEM"));
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Error);
        }).RequireAuthorization("RequireGarageOwner");

        // Skills
        grp.MapPost("/skills", async (AddSkillCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created("", result.Value) : Results.BadRequest(result.Errors);
        }).RequireAuthorization("RequireMechanic");

        grp.MapGet("/skills/employee/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetEmployeeSkillsQuery(id));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
    }
}
