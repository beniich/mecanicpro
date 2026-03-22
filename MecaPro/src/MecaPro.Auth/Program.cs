using Carter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MecaPro.Auth.Domain;
using MecaPro.Auth.Infrastructure;
using MecaPro.Auth.Application;
using MecaPro.Domain.Common;
using System.Threading.RateLimiting;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
//  LOGGING (Serilog + Seq)
// ═══════════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MecaPro.Auth")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ═══════════════════════════════════════════════════════════════════════════
//  DATABASE
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddDbContext<AuthDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ═══════════════════════════════════════════════════════════════════════════
//  IDENTITY
// ═══════════════════════════════════════════════════════════════════════════
builder.Services
    .AddIdentity<AppUser, IdentityRole>(opt =>
    {
        opt.Password.RequiredLength         = 10;
        opt.Password.RequireDigit           = true;
        opt.Password.RequireNonAlphanumeric = true;
        opt.Password.RequireUppercase       = true;
        opt.Password.RequireLowercase       = true;
        opt.Lockout.MaxFailedAccessAttempts = 5;
        opt.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

// ═══════════════════════════════════════════════════════════════════════════
//  JWT & AUTHENTICATION
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

var publicRsa = System.Security.Cryptography.RSA.Create();
publicRsa.ImportFromPem(jwtSettings.PublicKeyPem);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,  ValidIssuer          = jwtSettings.Issuer,
            ValidateAudience         = true,  ValidAudience        = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,  IssuerSigningKey     = new RsaSecurityKey(publicRsa),
            ValidateLifetime         = true,  ClockSkew            = TimeSpan.Zero,
            RequireExpirationTime    = true
        };
    });

builder.Services.AddAuthorization();

// ═══════════════════════════════════════════════════════════════════════════
//  SERVICES
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddCarter();

builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = 429;
    opt.AddPolicy(RateLimitPolicies.Auth, ctx => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }
    ));
});

var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();

// Seed super admin dynamically
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

    dbContext.Database.EnsureCreated(); // Normally migrations are used

    foreach (var role in Roles.All)
    {
        if (!roleManager.RoleExistsAsync(role).Result)
            roleManager.CreateAsync(new IdentityRole(role)).Wait();
    }

    if (userManager.FindByEmailAsync("superadmin@mecapro.com").Result == null)
    {
        var sa = new AppUser { UserName = "superadmin@mecapro.com", Email = "superadmin@mecapro.com", FirstName = "Super", LastName = "Admin", GarageId = Guid.Empty };
        userManager.CreateAsync(sa, "Admin@123456!").Wait();
        userManager.AddToRoleAsync(sa, Roles.SuperAdmin).Wait();
    }
}

app.Run();
