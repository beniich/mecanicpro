using Carter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.RateLimiting;
using MecaPro.Auth.Application;
using MecaPro.Domain.Common;

namespace MecaPro.Auth.Endpoints;

public class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting(RateLimitPolicies.Auth);

        group.MapPost("/register", async (RegisterDto dto, IAuthService auth) =>
        {
            var result = await auth.RegisterAsync(dto);
            return result.IsSuccess ? Results.Created("/api/v1/auth/me", result.Value) : Results.BadRequest(new { error = result.Error });
        }).AllowAnonymous();

        group.MapPost("/login", async (LoginDto dto, IAuthService auth, HttpContext ctx) =>
        {
            var ip     = ctx.Connection.RemoteIpAddress?.ToString();
            var result = await auth.LoginAsync(dto with { IpAddress = ip });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequestDto req, IAuthService auth, HttpContext ctx) =>
        {
            var ip     = ctx.Connection.RemoteIpAddress?.ToString();
            var result = await auth.RefreshAsync(req with { IpAddress = ip });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).AllowAnonymous();

        group.MapPost("/revoke", async (RefreshRequestDto req, IAuthService auth, HttpContext ctx) =>
        {
            var ip     = ctx.Connection.RemoteIpAddress?.ToString();
            var result = await auth.RevokeAsync(req.RefreshToken, ip);
            return result.IsSuccess ? Results.NoContent() : Results.BadRequest(result.Error);
        }).RequireAuthorization();

        group.MapPost("/logout-all", async (IAuthService auth, HttpContext ctx) =>
        {
            var userId = ctx.User.FindFirst("sub")?.Value;
            if (userId == null) return Results.Unauthorized();
            var result = await auth.RevokeAllForUserAsync(userId);
            return result.IsSuccess ? Results.NoContent() : Results.Problem();
        }).RequireAuthorization();
    }
}
