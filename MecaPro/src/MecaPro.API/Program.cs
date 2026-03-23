using Carter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using MassTransit;
using MecaPro.Domain.Common.Events;
using MecaPro.Infrastructure.Persistence;
// Auth/Identity removed due to microservices migration
using MecaPro.Infrastructure.Persistence.Repositories;
using MecaPro.Infrastructure.Security;
using MecaPro.Infrastructure.Modules.CRM;
using MecaPro.Infrastructure.Modules.Payment;
using MecaPro.Infrastructure.Modules.Invoicing;
using MecaPro.Infrastructure.Services.Payments;
using MecaPro.Application.Common;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Operations;
using MecaPro.Domain.Modules.Inventory;
using MecaPro.Domain.Modules.Invoicing;
using MecaPro.Domain.Modules.Feedback;
using MecaPro.Domain.Modules.HR;
using MediatR;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
//  SERILOG — Structured Logging
// ═══════════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MecaPro.API")
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// ═══════════════════════════════════════════════════════════════════════════
//  DATABASE
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOpt => sqlOpt.EnableRetryOnFailure(3)));


// ═══════════════════════════════════════════════════════════════════════════
//  JWT AUTHENTICATION — RS256
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;

var publicRsa = System.Security.Cryptography.RSA.Create();
try 
{
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

            opt.Events = new JwtBearerEvents
            {
                OnChallenge = ctx => { ctx.HandleResponse(); ctx.Response.StatusCode = 401; ctx.Response.ContentType = "application/json"; return ctx.Response.WriteAsync("{\"error\":\"Unauthorized\"}"); },
                OnForbidden = ctx => { ctx.Response.StatusCode = 403; ctx.Response.ContentType = "application/json"; return ctx.Response.WriteAsync("{\"error\":\"Forbidden\"}"); }
            };
        });
}
catch (System.Security.Cryptography.CryptographicException) 
{
    // Ignore RSA configuration if PEM is corrupted, primarily for EF Core tooling where runtime auth isn't needed.
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
}

// ═══════════════════════════════════════════════════════════════════════════
//  AUTHORIZATION — Role-based + Resource-based policies
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddAuthorization(opt =>
{
    // Default: every endpoint requires authenticated user unless .AllowAnonymous()
    opt.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    opt.AddPolicy(Policies.SuperAdmin,
        p => p.RequireRole(Roles.SuperAdmin));

    opt.AddPolicy(Policies.GarageOwner,
        p => p.RequireRole(Roles.GarageOwner, Roles.SuperAdmin));

    opt.AddPolicy(Policies.Mechanic,
        p => p.RequireRole(Roles.Mechanic, Roles.GarageOwner, Roles.SuperAdmin));

    opt.AddPolicy(Policies.AnyAuthenticated,
        p => p.RequireAuthenticatedUser());

    // Tenant (multi-garage) isolation is enforced via SameGarageHandler
    opt.AddPolicy(Policies.SameGarage,
        p => p.Requirements.Add(new SameGarageRequirement(Guid.Empty)));
});

// Register resource-based auth handlers
builder.Services.AddScoped<IAuthorizationHandler, SameGarageHandler>();
builder.Services.AddScoped<IAuthorizationHandler, SameUserOrAdminHandler>();

// ═══════════════════════════════════════════════════════════════════════════
//  RATE LIMITING — Token bucket per IP
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = 429;

    // Auth endpoints: 5 requests/minute per IP
    opt.AddPolicy(RateLimitPolicies.Auth, ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit       = 5,
                Window            = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit        = 0
            }));

    // General API: 120 requests/minute per user
    opt.AddPolicy(RateLimitPolicies.Api, ctx =>
    {
        var userId = ctx.User?.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: userId,
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit            = 120,
                ReplenishmentPeriod   = TimeSpan.FromMinutes(1),
                TokensPerPeriod       = 120,
                AutoReplenishment     = true
            });
    });

    // Sensitive ops (invoice gen, payment): 10 requests/minute
    opt.AddPolicy(RateLimitPolicies.Strict, ctx =>
    {
        var userId = ctx.User?.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window      = TimeSpan.FromMinutes(1)
            });
    });
});

// ═══════════════════════════════════════════════════════════════════════════
//  MEDIATR — CQRS Pipeline
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Result<>).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});

// ═══════════════════════════════════════════════════════════════════════════
//  CARTER — Minimal API module routing
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddCarter();

// ═══════════════════════════════════════════════════════════════════════════
//  SWAGGER — API documentation with JWT support
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MecaPro API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter JWT token"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        { new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

// ═══════════════════════════════════════════════════════════════════════════
//  EVENT BUS (MassTransit)
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ") ?? "localhost");
        cfg.ConfigureEndpoints(ctx);
    });
});

// ═══════════════════════════════════════════════════════════════════════════
//  APPLICATION SERVICES
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUnitOfWork,         UnitOfWork>();

// Repositories
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IVehicleRepository,  VehicleRepository>();
builder.Services.AddScoped<IRevisionRepository, RevisionRepository>();
builder.Services.AddScoped<IPartRepository,     PartRepository>();
builder.Services.AddScoped<IOrderRepository,    OrderRepository>();
builder.Services.AddScoped<IInvoiceRepository,  InvoiceRepository>();
builder.Services.AddScoped<IAbsenceRepository,  AbsenceRepository>();
builder.Services.AddScoped<ISkillRepository,    SkillRepository>();
builder.Services.AddScoped<ISurveyRepository,   SurveyRepository>();

// Domain services
builder.Services.AddScoped<ICrmService,                  CrmService>();
builder.Services.AddScoped<IInvoiceService,              InvoiceService>();
builder.Services.AddScoped<IInvoiceSequencer,            InvoiceSequencer>();
builder.Services.AddScoped<IBlobStorageService,          MockBlobStorageService>();
builder.Services.AddScoped<ISignalRNotifier,             MockSignalRNotifier>();
builder.Services.AddScoped<IStripeSubscriptionService,   StripeSubscriptionService>();
builder.Services.AddScoped<IPaymentService,              StripePaymentService>();
builder.Services.AddScoped<DatabaseSeeder>();

// ═══════════════════════════════════════════════════════════════════════════
//  CORS
// ═══════════════════════════════════════════════════════════════════════════
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5200", "https://localhost:5201"];

builder.Services.AddCors(opt => opt.AddPolicy("AllowBlazor", policy =>
    policy.WithOrigins(allowedOrigins)
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials()));

// ═══════════════════════════════════════════════════════════════════════════
//  REDIS — Distributed cache
// ═══════════════════════════════════════════════════════════════════════════
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrEmpty(redisConn))
    builder.Services.AddStackExchangeRedisCache(opt => opt.Configuration = redisConn);
else
    builder.Services.AddDistributedMemoryCache(); // Fallback for dev

// ═══════════════════════════════════════════════════════════════════════════
//  HEALTH CHECKS
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")!);

// ─────────────────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────────────────

// ═══════════════════════════════════════════════════════════════════════════
//  MIDDLEWARE PIPELINE — ORDER MATTERS
// ═══════════════════════════════════════════════════════════════════════════

// 1. Global exception handler (catches everything below)
app.UseMiddleware<GlobalExceptionMiddleware>();

// 2. Security headers (on every response)
app.UseMiddleware<SecurityHeadersMiddleware>();

// 3. Audit logging
app.UseMiddleware<AuditLogMiddleware>();

// 4. HTTPS redirect
app.UseHttpsRedirection();

// 5. Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MecaPro API v1");
        c.RoutePrefix = "swagger";
    });
}

// 6. Serilog request logging
app.UseSerilogRequestLogging();

// 7. Rate limiting
app.UseRateLimiter();

// 8. CORS
app.UseCors("AllowBlazor");

// 9. Auth
app.UseAuthentication();
app.UseAuthorization();

// 10. Carter routes
app.MapCarter();

// 11. Health check endpoint
app.MapHealthChecks("/health").AllowAnonymous();

// ═══════════════════════════════════════════════════════════════════════════
//  DATABASE SEEDING
// ═══════════════════════════════════════════════════════════════════════════
if (app.Environment.IsDevelopment())
{
    using var scope  = app.Services.CreateScope();
    var seeder       = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}


app.Run();

// Classes used by builder must be at the end if using top-level statements
public class JwtSettings { public string Issuer {get;set;}=null!; public string Audience {get;set;}=null!; public string PublicKeyPem {get;set;}=null!; }
