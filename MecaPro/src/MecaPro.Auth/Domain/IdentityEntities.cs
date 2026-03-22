using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace MecaPro.Auth.Domain;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public Guid GarageId { get; set; }
    public Guid? CustomerId { get; set; }
    public string SubscriptionTier { get; set; } = "starter";
    public string? TotpSecretEncrypted { get; set; }
    public bool TotpEnabled { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public string Family { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }
}
