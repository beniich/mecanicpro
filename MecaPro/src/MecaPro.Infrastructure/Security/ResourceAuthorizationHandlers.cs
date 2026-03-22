using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using MecaPro.Domain.Common;

namespace MecaPro.Infrastructure.Security;

// ── Requirement: same garage ─────────────────────────────────────────────────
public class SameGarageRequirement(Guid resourceGarageId) : IAuthorizationRequirement
{
    public Guid ResourceGarageId { get; } = resourceGarageId;
}

public class SameGarageHandler : AuthorizationHandler<SameGarageRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameGarageRequirement requirement)
    {
        var garageIdClaim = context.User.FindFirst(Claims.GarageId)?.Value;
        if (garageIdClaim == null) return Task.CompletedTask;

        // SuperAdmin can access any garage
        if (context.User.IsInRole(Roles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (Guid.TryParse(garageIdClaim, out var userGarageId) &&
            userGarageId == requirement.ResourceGarageId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

// ── Requirement: same user or admin ─────────────────────────────────────────
public class SameUserOrAdminRequirement(string? resourceUserId) : IAuthorizationRequirement
{
    public string? ResourceUserId { get; } = resourceUserId;
}

public class SameUserOrAdminHandler : AuthorizationHandler<SameUserOrAdminRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameUserOrAdminRequirement requirement)
    {
        var userId = context.User.FindFirst(Claims.UserId)?.Value;

        if (context.User.IsInRole(Roles.SuperAdmin) || context.User.IsInRole(Roles.GarageOwner))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (userId != null && userId == requirement.ResourceUserId)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

// ── Extension: ClaimsPrincipal helpers ───────────────────────────────────────
public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
        => user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public static Guid? GetGarageId(this ClaimsPrincipal user)
    {
        var val = user.FindFirst(Claims.GarageId)?.Value;
        return Guid.TryParse(val, out var id) ? id : null;
    }

    public static bool IsSuperAdmin(this ClaimsPrincipal user)
        => user.IsInRole(Roles.SuperAdmin);

    public static bool IsGarageOwner(this ClaimsPrincipal user)
        => user.IsInRole(Roles.GarageOwner) || user.IsSuperAdmin();

    public static bool IsMechanic(this ClaimsPrincipal user)
        => user.IsInRole(Roles.Mechanic) || user.IsGarageOwner();

    /// <summary>
    /// Returns true when the user belongs to the specified garage
    /// OR is a SuperAdmin (no tenant scope restriction).
    /// </summary>
    public static bool CanAccessGarage(this ClaimsPrincipal user, Guid garageId)
        => user.IsSuperAdmin() || user.GetGarageId() == garageId;
}
