using System;

namespace MecaPro.Domain.Common;

/// <summary>
/// Defines all roles in the system — single source of truth for role names.
/// Prevents magic strings scattered across the codebase.
/// </summary>
public static class Roles
{
    public const string SuperAdmin   = "SuperAdmin";
    public const string GarageOwner = "GarageOwner";
    public const string Mechanic    = "Mechanic";
    public const string Client      = "Client";

    public static readonly string[] All = [SuperAdmin, GarageOwner, Mechanic, Client];
}

/// <summary>
/// Named policy constants for Authorization middleware.
/// Use these in [Authorize(Policy = Policies.Mechanic)] or RequireAuthorization(Policies.Mechanic).
/// </summary>
public static class Policies
{
    // Role-based
    public const string SuperAdmin       = "RequireSuperAdmin";
    public const string GarageOwner     = "RequireGarageOwner";
    public const string Mechanic        = "RequireMechanic";
    public const string AnyAuthenticated = "RequireAuthenticated";

    // Resource-based (enforced in handlers)
    public const string SameGarage      = "SameGarage";   // user.GarageId == resource.GarageId
    public const string SameUserOrAdmin = "SameUserOrAdmin";
}

/// <summary>
/// Claim type constants used in JWT tokens.
/// </summary>
public static class Claims
{
    public const string UserId    = "sub";
    public const string GarageId = "garage_id";
    public const string Role      = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    public const string Email     = "email";
    public const string TenantId  = "tenant_id";
}

/// <summary>
/// Rate limiting policy names.
/// </summary>
public static class RateLimitPolicies
{
    public const string Auth     = "auth_rate_limit";     // Login / Register  → 5 req/min
    public const string Api      = "api_rate_limit";      // General API       → 120 req/min
    public const string Strict   = "strict_rate_limit";   // Sensitive ops     → 10 req/min
}

/// <summary>
/// Multi-tenant isolation helper.
/// Ensures users can only access resources within their GarageId scope.
/// </summary>
public static class TenantGuard
{
    /// <summary>Throws if the requesting user's garage doesn't match the resource's garage.</summary>
    public static void Enforce(Guid? userGarageId, Guid resourceGarageId, bool isSuperAdmin = false)
    {
        if (isSuperAdmin) return; // SuperAdmin bypasses tenant guard
        if (userGarageId == null || userGarageId != resourceGarageId)
            throw new AccessDeniedException($"Access denied: resource belongs to a different garage.");
    }
}

/// <summary>
/// Thrown when a user tries to access a resource they don't own.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public class AccessDeniedException(string message) : DomainException(message);

/// <summary>
/// Thrown when request exceeds rate limits.
/// Maps to HTTP 429 Too Many Requests.
/// </summary>
public class RateLimitExceededException(string message) : DomainException(message);
