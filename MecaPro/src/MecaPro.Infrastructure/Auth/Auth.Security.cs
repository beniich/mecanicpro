using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using QRCoder;
using StackExchange.Redis;
using MecaPro.Infrastructure.Persistence;
using MecaPro.Application.Common;
using System.Net;
using Carter;
using Microsoft.AspNetCore.Routing;
namespace MecaPro.Infrastructure.Identity;

public class JwtSettings
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string PrivateKeyPem { get; set; } = null!;
    public string PublicKeyPem { get; set; } = null!;
    public int AccessTokenExpiryMinutes { get; set; } = 15;
}

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry);

public interface IJwtTokenService
{
    TokenPair GenerateTokenPair(AppUser user, IList<string> roles);
    string HashRefreshToken(string token);
    ClaimsPrincipal? ValidateAccessToken(string token);
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

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        var key = RSA.Create();
        key.ImportFromPem(_settings.PublicKeyPem);
        var parameters = new TokenValidationParameters { ValidateIssuer = true, ValidIssuer = _settings.Issuer, ValidateAudience = true, ValidAudience = _settings.Audience, ValidateIssuerSigningKey = true, IssuerSigningKey = new RsaSecurityKey(key) };
        return new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);
    }
}

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto);
    Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto);
    Task<Result<AuthResponseDto>> RefreshAsync(string refreshToken);
}

public class AuthService(UserManager<AppUser> userManager, IJwtTokenService jwt, AppDbContext db, ICurrentUserService currentUser) : IAuthService
{
    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var user = new AppUser { UserName = dto.Email, Email = dto.Email, FirstName = dto.FirstName, LastName = dto.LastName, GarageId = dto.GarageId ?? Guid.NewGuid() };
        var res = await userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded) return Result<AuthResponseDto>.Failure(res.Errors.First().Description);
        await userManager.AddToRoleAsync(user, "GarageOwner");
        var tokens = jwt.GenerateTokenPair(user, new[] { "GarageOwner" });
        return Result<AuthResponseDto>.Success(new AuthResponseDto(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry, user.Id, user.Email!, user.FirstName, user.LastName, new List<string> { "GarageOwner" }));
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password)) return Result<LoginResponseDto>.Failure("Invalid credentials");
        var roles = await userManager.GetRolesAsync(user);
        var tokens = jwt.GenerateTokenPair(user, roles);
        return Result<LoginResponseDto>.Success(new LoginResponseDto(false, null, tokens.AccessToken, tokens.RefreshToken));
    }

    public async Task<Result<AuthResponseDto>> RefreshAsync(string refreshToken) => Result<AuthResponseDto>.Failure("Not implemented");
}

public record RegisterDto(string FirstName, string LastName, string Email, string Password, Guid? GarageId = null);
public record LoginDto(string Email, string Password);
public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt, string UserId, string Email, string FirstName, string LastName, List<string> Roles);
public record LoginResponseDto(bool Requires2Fa, string? TempToken, string? AccessToken, string? RefreshToken);

public class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");
        
        group.MapPost("/register", async (RegisterDto dto, IAuthService auth) =>
        {
            var result = await auth.RegisterAsync(dto);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        });

        group.MapPost("/login", async (LoginDto dto, IAuthService auth) =>
        {
            var result = await auth.LoginAsync(dto);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error ?? "Invalid credentials");
        });
    }
}
