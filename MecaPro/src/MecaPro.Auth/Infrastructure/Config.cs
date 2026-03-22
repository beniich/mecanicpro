using System;

namespace MecaPro.Auth.Infrastructure;

public class JwtSettings
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpiryMinutes { get; set; }
    public string PrivateKeyPem { get; set; } = null!;
    public string PublicKeyPem { get; set; } = null!;
}

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry);
