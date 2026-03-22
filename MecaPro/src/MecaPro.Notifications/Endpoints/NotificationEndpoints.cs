using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MecaPro.Notifications.Application;

namespace MecaPro.Notifications.Endpoints;

public class NotificationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapPost("/", async (NotificationRequest req, INotificationService notifications) =>
        {
            await notifications.SendAsync(req);
            return Results.Accepted();
        });
    }
}
