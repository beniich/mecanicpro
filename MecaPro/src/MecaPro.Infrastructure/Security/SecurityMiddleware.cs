using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MecaPro.Infrastructure.Persistence;

namespace MecaPro.Infrastructure.Security;

/// <summary>
/// Adds security headers to every HTTP response:
/// HSTS, CSP, anti-clickjacking, referrer policy, etc.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        var h = ctx.Response.Headers;

        // Prevent clickjacking
        h["X-Frame-Options"]           = "DENY";
        // Block MIME-sniffing
        h["X-Content-Type-Options"]    = "nosniff";
        // Modern XSS protection (CSP is preferred)
        h["X-XSS-Protection"]          = "1; mode=block";
        // Only send origin on cross-origin requests
        h["Referrer-Policy"]           = "strict-origin-when-cross-origin";
        // Only allow HTTPS for 1 year (including sub-domains)
        h["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        // Disable FLoC/Google Topics tracking
        h["Permissions-Policy"]        = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";

        // Content Security Policy — tighten further per-page as needed
        h["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +   // 'unsafe-inline' required by Blazor; replace with nonces in prod
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com; " +
            "img-src 'self' data: https:; " +
            "connect-src 'self' https://api.stripe.com; " +
            "frame-ancestors 'none';";

        await next(ctx);
    }
}

/// <summary>
/// Logs every inbound request with timing, user, and result.
/// Writes to the AuditLog table for sensitive operation tracking.
/// </summary>
public class AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
{
    private static readonly string[] _sensitiveVerbs = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        var sw    = Stopwatch.StartNew();
        var path  = ctx.Request.Path.Value ?? "";
        var verb  = ctx.Request.Method;

        await next(ctx);

        sw.Stop();

        var userId     = ctx.User?.FindFirst("sub")?.Value;
        var statusCode = ctx.Response.StatusCode;
        var ip         = ctx.Connection.RemoteIpAddress?.ToString();

        // Only persist audit records for authenticated users on mutating operations
        if (userId != null && _sensitiveVerbs.Contains(verb))
        {
            db.AuditLogs.Add(new AuditLog
            {
                UserId     = userId,
                Action     = $"{verb} {path}",
                EntityType = "HttpRequest",
                EntityId   = ctx.TraceIdentifier,
                NewValues  = statusCode.ToString(),
                IpAddress  = ip,
                Timestamp  = DateTime.UtcNow
            });

            try { await db.SaveChangesAsync(); }
            catch { /* Non-blocking: audit failure must not break the request */ }
        }

        logger.LogInformation(
            "[AUDIT] {Method} {Path} → {StatusCode} ({Duration}ms) User={UserId}",
            verb, path, statusCode, sw.ElapsedMilliseconds, userId ?? "anonymous");
    }
}

/// <summary>
/// Global exception handler — maps domain exceptions to HTTP status codes.
/// Prevents stack traces from leaking to clients.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (MecaPro.Domain.Common.AccessDeniedException ex)
        {
            logger.LogWarning("Access denied: {Message}", ex.Message);
            ctx.Response.StatusCode = 403;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (MecaPro.Domain.Common.NotFoundException ex)
        {
            logger.LogWarning("Not found: {Message}", ex.Message);
            ctx.Response.StatusCode = 404;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (MecaPro.Domain.Common.BusinessRuleViolationException ex)
        {
            logger.LogWarning("Business rule: {Message}", ex.Message);
            ctx.Response.StatusCode = 422;
            await ctx.Response.WriteAsJsonAsync(new { error = ex.Message });
        }
        catch (FluentValidation.ValidationException ex)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.WriteAsJsonAsync(new
            {
                error  = "Validation failed",
                errors = ex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Path}", ctx.Request.Path);
            ctx.Response.StatusCode = 500;
            await ctx.Response.WriteAsJsonAsync(new { error = "An internal server error occurred." });
        }
    }
}
