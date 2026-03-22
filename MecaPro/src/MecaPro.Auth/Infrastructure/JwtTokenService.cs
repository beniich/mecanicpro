using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MecaPro.Auth.Domain;

namespace MecaPro.Auth.Infrastructure;

public interface IJwtTokenService
{
    TokenPair GenerateTokenPair(AppUser user, IList<string> roles);
    string HashRefreshToken(string token);
    ClaimsPrincipal? ValidateAccessToken(string token);
    string GenerateTempToken(string userId);
}

public class JwtTokenService(IOptions<JwtSettings> settings) : IJwtTokenService
{
    private readonly JwtSettings _settings = settings.Value;

    public TokenPair GenerateTokenPair(AppUser user, IList<string> roles)
    {
        var expiry = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("email", user.Email!),
            new("garage_id", user.GarageId.ToString())
        };
        foreach (var r in roles) claims.Add(new Claim(ClaimTypes.Role, r));

        var key = RSA.Create();
        key.ImportFromPem(_settings.PrivateKeyPem);
        var creds = new SigningCredentials(new RsaSecurityKey(key), SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(_settings.Issuer, _settings.Audience, claims, expires: expiry, signingCredentials: creds);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        return new TokenPair(accessToken, refreshToken, expiry);
    }

    public string HashRefreshToken(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public string GenerateTempToken(string userId)
    {
        var expiry  = DateTime.UtcNow.AddMinutes(5);
        var claims  = new[] { new Claim("sub", userId), new Claim("purpose", "2fa") };
        var key     = RSA.Create();
        key.ImportFromPem(_settings.PrivateKeyPem);
        var creds   = new SigningCredentials(new RsaSecurityKey(key), SecurityAlgorithms.RsaSha256);
        var token   = new JwtSecurityToken(_settings.Issuer, _settings.Audience, claims, expires: expiry, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var key = RSA.Create();
        key.ImportFromPem(_settings.PublicKeyPem);
        var parameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = _settings.Issuer, ValidateAudience = true, ValidAudience = _settings.Audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new RsaSecurityKey(key) };
        return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
    }
}
