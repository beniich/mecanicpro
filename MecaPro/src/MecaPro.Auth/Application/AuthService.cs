using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MecaPro.Application.Common;
using MecaPro.Domain.Common;
using MecaPro.Auth.Domain;
using MecaPro.Auth.Infrastructure;

namespace MecaPro.Auth.Application;

// ── DTOs ────────────────────────────────────────────────────────────────────
public record RegisterDto(string FirstName, string LastName, string Email, string Password, string Role = Roles.GarageOwner, Guid? GarageId = null);
public record LoginDto(string Email, string Password, string? IpAddress = null);
public record AuthResponseDto(string AccessToken, string RefreshToken, DateTime ExpiresAt, string UserId, string Email, string FirstName, string LastName, List<string> Roles, Guid GarageId);
public record RefreshRequestDto(string RefreshToken, string? IpAddress = null);
public record LoginResponseDto(bool Requires2Fa, string? TempToken, string? AccessToken, string? RefreshToken, string? UserId, Guid? GarageId, List<string>? Roles);

// ── Interface ────────────────────────────────────────────────────────────────
public interface IAuthService
{
    Task<Result<AuthResponseDto>>  RegisterAsync(RegisterDto dto);
    Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto);
    Task<Result<AuthResponseDto>>  RefreshAsync(RefreshRequestDto req);
    Task<Result<bool>>             RevokeAsync(string refreshToken, string? ipAddress = null);
    Task<Result<bool>>             RevokeAllForUserAsync(string userId);
}

// ── Implementation ───────────────────────────────────────────────────────────
public class AuthService(
    UserManager<AppUser>      userManager,
    RoleManager<IdentityRole> roleManager,
    IJwtTokenService          jwt,
    AuthDbContext             db) : IAuthService
{
    private const int RefreshTokenDays = 30;

    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        if (await userManager.FindByEmailAsync(dto.Email) != null)
            return Result<AuthResponseDto>.Failure("Email already in use.");

        var safeRole = dto.Role == Roles.GarageOwner ? Roles.GarageOwner : Roles.GarageOwner;

        var user = new AppUser
        {
            UserName  = dto.Email,
            Email     = dto.Email,
            FirstName = dto.FirstName,
            LastName  = dto.LastName,
            GarageId  = dto.GarageId ?? Guid.NewGuid(),
            IsActive  = true,
            EmailConfirmed = false
        };

        var res = await userManager.CreateAsync(user, dto.Password);
        if (!res.Succeeded) return Result<AuthResponseDto>.Failure(string.Join("; ", res.Errors.Select(e => e.Description)));

        if (!await roleManager.RoleExistsAsync(safeRole))
            await roleManager.CreateAsync(new IdentityRole(safeRole));

        await userManager.AddToRoleAsync(user, safeRole);

        var tokens = await IssueTokenPairAsync(user, new[] { safeRole }, null);
        return Result<AuthResponseDto>.Success(BuildAuthResponse(user, tokens, new[] { safeRole }));
    }

    public async Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await userManager.CheckPasswordAsync(user, dto.Password))
            return Result<LoginResponseDto>.Failure("Invalid credentials.");

        if (!user.IsActive) return Result<LoginResponseDto>.Failure("Account suspended. Contact support.");

        if (user.TotpEnabled)
        {
            var tempToken = jwt.GenerateTempToken(user.Id);
            return Result<LoginResponseDto>.Success(new LoginResponseDto(true, tempToken, null, null, user.Id, user.GarageId, null));
        }

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        var roles  = await userManager.GetRolesAsync(user);
        var tokens = await IssueTokenPairAsync(user, roles, dto.IpAddress);

        return Result<LoginResponseDto>.Success(new LoginResponseDto(false, null, tokens.AccessToken, tokens.RefreshToken, user.Id, user.GarageId, roles.ToList()));
    }

    public async Task<Result<AuthResponseDto>> RefreshAsync(RefreshRequestDto req)
    {
        var tokenHash = jwt.HashRefreshToken(req.RefreshToken);
        var stored = await db.RefreshTokens.Include(t => t.User).FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored == null) return Result<AuthResponseDto>.Failure("Invalid refresh token.");
        if (stored.IsRevoked)
        {
            await RevokeFamily(stored.Family, "Reuse detected");
            return Result<AuthResponseDto>.Failure("Token revoked. Please log in again.");
        }
        if (stored.ExpiresAt < DateTime.UtcNow) return Result<AuthResponseDto>.Failure("Refresh token expired.");

        var user  = stored.User;
        var roles = await userManager.GetRolesAsync(user);

        stored.IsRevoked  = true;
        stored.RevokedAt  = DateTime.UtcNow;
        db.RefreshTokens.Update(stored);

        var tokens = await IssueTokenPairAsync(user, roles, req.IpAddress, stored.Family);
        await db.SaveChangesAsync();

        return Result<AuthResponseDto>.Success(BuildAuthResponse(user, tokens, roles));
    }

    public async Task<Result<bool>> RevokeAsync(string refreshToken, string? ipAddress = null)
    {
        var tokenHash = jwt.HashRefreshToken(refreshToken);
        var stored    = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
        if (stored == null) return Result<bool>.Failure("Token not found.");

        stored.IsRevoked = true;
        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RevokeAllForUserAsync(string userId)
    {
        var tokens = await db.RefreshTokens.Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
        foreach (var t in tokens) { t.IsRevoked = true; t.RevokedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private async Task<TokenPair> IssueTokenPairAsync(AppUser user, IList<string> roles, string? ipAddress, string? existingFamily = null)
    {
        var pair   = jwt.GenerateTokenPair(user, roles);
        var family = existingFamily ?? Guid.NewGuid().ToString();

        db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = jwt.HashRefreshToken(pair.RefreshToken), Family = family, ExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays), CreatedByIp = ipAddress });
        await db.SaveChangesAsync();
        return pair;
    }

    private async Task RevokeFamily(string family, string reason)
    {
        var tokens = await db.RefreshTokens.Where(t => t.Family == family && !t.IsRevoked).ToListAsync();
        foreach (var t in tokens) { t.IsRevoked = true; t.RevokedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync();
    }

    private static AuthResponseDto BuildAuthResponse(AppUser user, TokenPair tokens, IList<string> roles) =>
        new(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry, user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList(), user.GarageId);
}
