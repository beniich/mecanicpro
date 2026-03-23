using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Invoicing;
using MecaPro.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;

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

        grp.MapPost("/checkout/{invoiceId}", async (Guid invoiceId, IMediator mediator) =>
        {
            var result = await mediator.Send(new CreateCheckoutCommand(invoiceId));
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        grp.MapPost("/webhook", async (HttpRequest request, IConfiguration config, AppDbContext db) =>
        {
            var json = await new StreamReader(request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = Stripe.EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], config["Stripe:WebhookSecret"]);

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null && session.Metadata.TryGetValue("invoice_id", out var idStr) && Guid.TryParse(idStr, out var invId))
                    {
                        var inv = await db.Invoices.FindAsync(invId);
                        if (inv != null)
                        {
                            inv.Status = "Paid";
                            await db.SaveChangesAsync();
                        }
                    }
                }
                return Results.Ok();
            }
            catch (Exception) { return Results.BadRequest(); }
        }).AllowAnonymous();
    }
}
