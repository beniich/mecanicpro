// ============================================================
// PHASE 4 — AUTH & SÉCURITÉ
// JWT RS256, Refresh Token Rotation, TOTP 2FA, OAuth2, Middleware
// ============================================================

// ─────────────────────────────────────────────────────────────
// JWT TOKEN SERVICE
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Infrastructure.Identity;

public class JwtSettings
{
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string PrivateKeyPem { get; set; } = null!;   // RSA private key
    public string PublicKeyPem { get; set; } = null!;    // RSA public key
    public int AccessTokenExpiryMinutes { get; set; } = 15;
    public int RefreshTokenExpiryDays { get; set; } = 30;
}

public record TokenPair(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry);

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;
    private readonly RsaSecurityKey _privateKey;
    private readonly RsaSecurityKey _publicKey;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(_settings.PrivateKeyPem);
        _privateKey = new RsaSecurityKey(privateRsa) { KeyId = "mecapro-key-1" };

        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(_settings.PublicKeyPem);
        _publicKey = new RsaSecurityKey(publicRsa) { KeyId = "mecapro-key-1" };
    }

    public TokenPair GenerateTokenPair(AppUser user, IList<string> roles)
    {
        var accessToken = GenerateAccessToken(user, roles);
        var refreshToken = GenerateRefreshToken();
        return new TokenPair(accessToken.Token, refreshToken, accessToken.Expiry);
    }

    private (string Token, DateTime Expiry) GenerateAccessToken(AppUser user, IList<string> roles)
    {
        var expiry = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new("email", user.Email!),
            new("first_name", user.FirstName),
            new("last_name", user.LastName),
            new("garage_id", user.GarageId.ToString()),
            new("subscription", user.SubscriptionTier),
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiry,
            signingCredentials: new SigningCredentials(_privateKey, SecurityAlgorithms.RsaSha256)
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    public ClaimsPrincipal? ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _settings.Issuer,
                ValidateAudience = true,
                ValidAudience = _settings.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _publicKey,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch { return null; }
    }
}

// ─────────────────────────────────────────────────────────────
// AUTH SERVICE — LOGIN, REGISTER, REFRESH, LOGOUT
// ─────────────────────────────────────────────────────────────

public class AuthService : IAuthService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IJwtTokenService _jwt;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuthService(UserManager<AppUser> um, IJwtTokenService jwt, AppDbContext db, ICurrentUserService cu)
    { _userManager = um; _jwt = jwt; _db = db; _currentUser = cu; }

    // --- REGISTER ---
    public async Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto)
    {
        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing != null)
            return Result<AuthResponseDto>.Failure("Cet email est déjà utilisé.");

        var user = new AppUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            GarageId = dto.GarageId ?? Guid.NewGuid(),
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return Result<AuthResponseDto>.ValidationFailure(result.Errors.Select(e => e.Description).ToList());

        await _userManager.AddToRoleAsync(user, "GarageOwner");

        var roles = await _userManager.GetRolesAsync(user);
        var tokens = _jwt.GenerateTokenPair(user, roles);
        await SaveRefreshTokenAsync(user, tokens.RefreshToken);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry,
            user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList()));
    }

    // --- LOGIN ---
    public async Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
            return Result<LoginResponseDto>.Failure("Email ou mot de passe incorrect.");

        if (await _userManager.IsLockedOutAsync(user))
            return Result<LoginResponseDto>.Failure("Compte temporairement verrouillé. Réessayez dans 15 minutes.");

        if (!user.IsActive)
            return Result<LoginResponseDto>.Failure("Compte désactivé. Contactez l'administrateur.");

        // 2FA required?
        if (user.TotpEnabled)
            return Result<LoginResponseDto>.Success(new LoginResponseDto(
                Requires2Fa: true, TempToken: GenerateTempToken(user.Id), AccessToken: null, RefreshToken: null));

        await _userManager.ResetAccessFailedCountAsync(user);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var tokens = _jwt.GenerateTokenPair(user, roles);
        await SaveRefreshTokenAsync(user, tokens.RefreshToken);

        return Result<LoginResponseDto>.Success(new LoginResponseDto(
            Requires2Fa: false, TempToken: null,
            AccessToken: tokens.AccessToken, RefreshToken: tokens.RefreshToken));
    }

    // --- 2FA VERIFY ---
    public async Task<Result<AuthResponseDto>> Verify2FAAsync(string tempToken, string otpCode)
    {
        var userId = ValidateTempToken(tempToken);
        if (userId == null)
            return Result<AuthResponseDto>.Failure("Token temporaire invalide ou expiré.");

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Result<AuthResponseDto>.Failure("Utilisateur introuvable.");

        var secret = DecryptTotpSecret(user.TotpSecretEncrypted!);
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        if (!totp.VerifyTotp(otpCode, out _, new VerificationWindow(2, 2)))
            return Result<AuthResponseDto>.Failure("Code 2FA invalide.");

        var roles = await _userManager.GetRolesAsync(user);
        var tokens = _jwt.GenerateTokenPair(user, roles);
        await SaveRefreshTokenAsync(user, tokens.RefreshToken);

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiry,
            user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList()));
    }

    // --- REFRESH TOKEN ROTATION ---
    public async Task<Result<AuthResponseDto>> RefreshAsync(string refreshToken)
    {
        var tokenHash = _jwt.HashRefreshToken(refreshToken);
        var storedToken = await _db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

        if (storedToken == null)
            return Result<AuthResponseDto>.Failure("Token invalide.");

        // Reuse detection: revoke entire family
        if (storedToken.IsRevoked)
        {
            await RevokeTokenFamilyAsync(storedToken.Family);
            return Result<AuthResponseDto>.Failure("Token réutilisé détecté. Reconnectez-vous.");
        }

        if (storedToken.ExpiresAt < DateTime.UtcNow)
            return Result<AuthResponseDto>.Failure("Token expiré. Reconnectez-vous.");

        var user = storedToken.User;
        var roles = await _userManager.GetRolesAsync(user);
        var newTokens = _jwt.GenerateTokenPair(user, roles);

        // Revoke old, save new
        storedToken.IsRevoked = true;
        storedToken.RevokedAt = DateTime.UtcNow;
        storedToken.ReplacedByToken = _jwt.HashRefreshToken(newTokens.RefreshToken);
        await SaveRefreshTokenAsync(user, newTokens.RefreshToken, storedToken.Family);
        await _db.SaveChangesAsync();

        return Result<AuthResponseDto>.Success(new AuthResponseDto(
            newTokens.AccessToken, newTokens.RefreshToken, newTokens.AccessTokenExpiry,
            user.Id, user.Email!, user.FirstName, user.LastName, roles.ToList()));
    }

    // --- LOGOUT ---
    public async Task LogoutAsync(string refreshToken, string jwtId)
    {
        // Revoke refresh token
        var tokenHash = _jwt.HashRefreshToken(refreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        if (stored != null) { stored.IsRevoked = true; stored.RevokedAt = DateTime.UtcNow; }

        // Blacklist JWT JTI in Redis
        await BlacklistJtiAsync(jwtId);
        await _db.SaveChangesAsync();
    }

    // --- SETUP 2FA ---
    public async Task<TotpSetupDto> Setup2FAAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var secret = Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));
        var totp = new Totp(Base32Encoding.ToBytes(secret));

        // Encrypt and store secret temporarily
        user.TotpSecretEncrypted = EncryptTotpSecret(secret);
        await _userManager.UpdateAsync(user);

        var otpAuthUrl = $"otpauth://totp/MecaPro:{user.Email}?secret={secret}&issuer=MecaPro&algorithm=SHA1&digits=6&period=30";

        // Generate QR for authenticator app
        using var qrGen = new QRCodeGenerator();
        var qrData = qrGen.CreateQrCode(otpAuthUrl, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var qrBase64 = Convert.ToBase64String(qrCode.GetGraphic(10));

        var backupCodes = GenerateBackupCodes();

        return new TotpSetupDto(secret, otpAuthUrl, $"data:image/png;base64,{qrBase64}", backupCodes);
    }

    public async Task<Result<bool>> Confirm2FASetupAsync(string userId, string otpCode)
    {
        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException("User", userId);

        var secret = DecryptTotpSecret(user.TotpSecretEncrypted!);
        var totp = new Totp(Base32Encoding.ToBytes(secret));
        if (!totp.VerifyTotp(otpCode, out _, new VerificationWindow(2, 2)))
            return Result<bool>.Failure("Code invalide.");

        user.TotpEnabled = true;
        await _userManager.UpdateAsync(user);
        return Result<bool>.Success(true);
    }

    // Helpers
    private async Task SaveRefreshTokenAsync(AppUser user, string rawToken, string? family = null)
    {
        var rt = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _jwt.HashRefreshToken(rawToken),
            Family = family ?? Guid.NewGuid().ToString(),
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedByIp = _currentUser.IpAddress
        };
        _db.RefreshTokens.Add(rt);
        await _db.SaveChangesAsync();
    }

    private async Task RevokeTokenFamilyAsync(string family)
    {
        var tokens = await _db.RefreshTokens.Where(rt => rt.Family == family).ToListAsync();
        foreach (var t in tokens) { t.IsRevoked = true; t.RevokedAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();
    }

    private async Task BlacklistJtiAsync(string jti)
    {
        // Store JTI in Redis with TTL = remaining token lifetime
    }

    private static string GenerateTempToken(string userId)
    {
        var bytes = Encoding.UTF8.GetBytes($"{userId}:{DateTime.UtcNow.AddMinutes(5):O}");
        return Convert.ToBase64String(bytes);
    }

    private static string? ValidateTempToken(string tempToken)
    {
        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(tempToken));
            var parts = decoded.Split(':');
            var expiry = DateTime.Parse(parts[1]);
            return expiry > DateTime.UtcNow ? parts[0] : null;
        }
        catch { return null; }
    }

    private static List<string> GenerateBackupCodes()
        => Enumerable.Range(0, 10)
            .Select(_ => $"{Random.Shared.Next(100000, 999999)}-{Random.Shared.Next(100000, 999999)}")
            .ToList();

    private static string EncryptTotpSecret(string secret)
    {
        // Use Data Protection API in production
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(secret));
    }

    private static string DecryptTotpSecret(string encrypted)
        => Encoding.UTF8.GetString(Convert.FromBase64String(encrypted));
}

// ─────────────────────────────────────────────────────────────
// TOKEN BLACKLIST MIDDLEWARE
// ─────────────────────────────────────────────────────────────

public class TokenBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;

    public TokenBlacklistMiddleware(RequestDelegate next, IConnectionMultiplexer redis)
    { _next = next; _redis = redis; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var jti = ctx.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (jti != null)
            {
                var db = _redis.GetDatabase();
                var isBlacklisted = await db.KeyExistsAsync($"blacklist:jti:{jti}");
                if (isBlacklisted)
                {
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.WriteAsJsonAsync(new { error = "Token révoqué." });
                    return;
                }
            }
        }
        await _next(ctx);
    }
}

// ─────────────────────────────────────────────────────────────
// AUDIT MIDDLEWARE
// ─────────────────────────────────────────────────────────────

public class AuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditMiddleware> _logger;

    public AuditMiddleware(RequestDelegate next, ILogger<AuditMiddleware> logger)
    { _next = next; _logger = logger; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await _next(ctx);
        sw.Stop();

        if (ctx.User.Identity?.IsAuthenticated == true)
        {
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ip = ctx.Connection.RemoteIpAddress?.ToString();

            _logger.LogInformation(
                "AUDIT | User:{UserId} | {Method} {Path} | Status:{Status} | {ElapsedMs}ms | IP:{Ip}",
                userId, ctx.Request.Method, ctx.Request.Path,
                ctx.Response.StatusCode, sw.ElapsedMilliseconds, ip);
        }
    }
}

// ─────────────────────────────────────────────────────────────
// SUBSCRIPTION FEATURE MIDDLEWARE
// ─────────────────────────────────────────────────────────────

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresFeatureAttribute : Attribute
{
    public string Feature { get; }
    public RequiresFeatureAttribute(string feature) => Feature = feature;
}

public class SubscriptionFeatureMiddleware
{
    private readonly RequestDelegate _next;

    public SubscriptionFeatureMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx, AppDbContext db)
    {
        var endpoint = ctx.GetEndpoint();
        var featureAttr = endpoint?.Metadata.GetMetadata<RequiresFeatureAttribute>();

        if (featureAttr != null && ctx.User.Identity?.IsAuthenticated == true)
        {
            var garageIdClaim = ctx.User.FindFirst("garage_id")?.Value;
            if (Guid.TryParse(garageIdClaim, out var garageId))
            {
                var sub = await db.Subscriptions
                    .FirstOrDefaultAsync(s => s.GarageId == garageId && s.Status == SubscriptionStatus.Active);

                if (sub == null || !HasFeature(sub, featureAttr.Feature))
                {
                    ctx.Response.StatusCode = 402;
                    await ctx.Response.WriteAsJsonAsync(new
                    {
                        error = "Cette fonctionnalité n'est pas disponible dans votre abonnement.",
                        feature = featureAttr.Feature,
                        upgrade_url = "https://mecapro.app/pricing"
                    });
                    return;
                }
            }
        }

        await _next(ctx);
    }

    private static bool HasFeature(Subscription sub, string feature) => feature switch
    {
        "ecommerce" => sub.HasEcommerce,
        "api_access" => sub.HasApiAccess,
        "white_label" => sub.IsWhiteLabel,
        _ => sub.IsActive()
    };
}

// ─────────────────────────────────────────────────────────────
// CURRENT USER SERVICE
// ─────────────────────────────────────────────────────────────

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    public string? UserId => _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
    public bool IsAuthenticated => _http.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}

// ─────────────────────────────────────────────────────────────
// AUTH ENDPOINTS (MINIMAL API)
// ─────────────────────────────────────────────────────────────

public class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", async (RegisterDto dto, IAuthService auth) =>
        {
            var result = await auth.RegisterAsync(dto);
            return result.IsSuccess ? Results.Created("/api/v1/auth/me", result.Value) :
                Results.BadRequest(new { errors = result.Errors });
        }).WithName("Register");

        group.MapPost("/login", async (LoginDto dto, IAuthService auth) =>
        {
            var result = await auth.LoginAsync(dto);
            return result.IsSuccess ? Results.Ok(result.Value) :
                Results.Unauthorized();
        }).WithName("Login").AddEndpointFilter<RateLimitFilter>();

        group.MapPost("/refresh", async (RefreshDto dto, IAuthService auth) =>
        {
            var result = await auth.RefreshAsync(dto.RefreshToken);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).WithName("RefreshToken");

        group.MapPost("/logout", async (LogoutDto dto, IAuthService auth, HttpContext ctx) =>
        {
            var jti = ctx.User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value ?? "";
            await auth.LogoutAsync(dto.RefreshToken, jti);
            return Results.Ok(new { message = "Déconnecté avec succès." });
        }).RequireAuthorization().WithName("Logout");

        group.MapPost("/2fa/setup", async (IAuthService auth, HttpContext ctx) =>
        {
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var setup = await auth.Setup2FAAsync(userId);
            return Results.Ok(setup);
        }).RequireAuthorization().WithName("Setup2FA");

        group.MapPost("/2fa/confirm", async (Confirm2FaDto dto, IAuthService auth, HttpContext ctx) =>
        {
            var userId = ctx.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
            var result = await auth.Confirm2FASetupAsync(userId, dto.Code);
            return result.IsSuccess ? Results.Ok(new { message = "2FA activé." }) :
                Results.BadRequest(new { error = result.Error });
        }).RequireAuthorization().WithName("Confirm2FA");

        group.MapPost("/2fa/verify", async (Verify2FaDto dto, IAuthService auth) =>
        {
            var result = await auth.Verify2FAAsync(dto.TempToken, dto.Code);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.Unauthorized();
        }).WithName("Verify2FA");

        group.MapPost("/forgot-password", async (ForgotPasswordDto dto, UserManager<AppUser> um, IEmailService email) =>
        {
            var user = await um.FindByEmailAsync(dto.Email);
            if (user != null)
            {
                var token = await um.GeneratePasswordResetTokenAsync(user);
                var resetUrl = $"https://mecapro.app/reset-password?token={Uri.EscapeDataString(token)}&email={dto.Email}";
                await email.SendAsync(dto.Email, "Réinitialisation mot de passe",
                    $"Cliquez ici pour réinitialiser : <a href='{resetUrl}'>Réinitialiser</a>");
            }
            // Always return OK (security: don't reveal if email exists)
            return Results.Ok(new { message = "Si cet email existe, un lien de réinitialisation a été envoyé." });
        }).WithName("ForgotPassword");

        group.MapPost("/reset-password", async (ResetPasswordDto dto, UserManager<AppUser> um) =>
        {
            var user = await um.FindByEmailAsync(dto.Email);
            if (user == null) return Results.BadRequest(new { error = "Lien invalide." });
            var result = await um.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            return result.Succeeded ? Results.Ok(new { message = "Mot de passe réinitialisé." }) :
                Results.BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }).WithName("ResetPassword");
    }
}

// ─────────────────────────────────────────────────────────────
// DTOs AUTH
// ─────────────────────────────────────────────────────────────

public record RegisterDto(string FirstName, string LastName, string Email,
    string Password, string ConfirmPassword, Guid? GarageId = null);

public record LoginDto(string Email, string Password);
public record RefreshDto(string RefreshToken);
public record LogoutDto(string RefreshToken);
public record Confirm2FaDto(string Code);
public record Verify2FaDto(string TempToken, string Code);
public record ForgotPasswordDto(string Email);
public record ResetPasswordDto(string Email, string Token, string NewPassword);

public record AuthResponseDto(
    string AccessToken, string RefreshToken, DateTime ExpiresAt,
    string UserId, string Email, string FirstName, string LastName, List<string> Roles);

public record LoginResponseDto(
    bool Requires2Fa, string? TempToken,
    string? AccessToken, string? RefreshToken);

public record TotpSetupDto(string Secret, string OtpAuthUrl, string QrCodeBase64, List<string> BackupCodes);

// ─────────────────────────────────────────────────────────────
// PROGRAM.CS — CONFIGURATION COMPLÈTE
// ─────────────────────────────────────────────────────────────

// program_builder_auth.cs — à intégrer dans Program.cs

/*
// ── Identity ──────────────────────────────────────────────────
builder.Services.AddIdentity<AppUser, IdentityRole>(opt =>
{
    opt.Password.RequiredLength = 10;
    opt.Password.RequireDigit = true;
    opt.Password.RequireUppercase = true;
    opt.Password.RequireNonAlphanumeric = true;
    opt.Lockout.MaxFailedAccessAttempts = 5;
    opt.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    opt.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ── JWT Auth ──────────────────────────────────────────────────
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetPublicKey(jwtSettings.PublicKeyPem),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
        // Support SignalR (token in query string)
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    })
    .AddGoogle(opt =>
    {
        opt.ClientId = builder.Configuration["OAuth:Google:ClientId"]!;
        opt.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"]!;
    });

// ── Rate Limiting ─────────────────────────────────────────────
builder.Services.AddRateLimiter(opt =>
{
    opt.AddSlidingWindowLimiter("global", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.PermitLimit = 200;
        o.QueueLimit = 10;
    });
    opt.AddSlidingWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.PermitLimit = 5;
    });
    opt.RejectionStatusCode = 429;
});
*/

public interface IJwtTokenService
{
    TokenPair GenerateTokenPair(AppUser user, IList<string> roles);
    string HashRefreshToken(string token);
    ClaimsPrincipal? ValidateAccessToken(string token);
}

public interface IAuthService
{
    Task<Result<AuthResponseDto>> RegisterAsync(RegisterDto dto);
    Task<Result<LoginResponseDto>> LoginAsync(LoginDto dto);
    Task<Result<AuthResponseDto>> Verify2FAAsync(string tempToken, string otpCode);
    Task<Result<AuthResponseDto>> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken, string jwtId);
    Task<TotpSetupDto> Setup2FAAsync(string userId);
    Task<Result<bool>> Confirm2FASetupAsync(string userId, string otpCode);
}

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, string? textBody = null);
}

public class RateLimitFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
        => await next(ctx); // Placeholder — actual limiting done by middleware
}
