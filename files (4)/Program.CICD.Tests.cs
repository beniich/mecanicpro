// ============================================================
// PHASES 10-15 : PROGRAM.CS COMPLET, API ENDPOINTS, BLAZOR,
//               CI/CD GITHUB ACTIONS, MONITORING, GO-LIVE
// ============================================================

// ─────────────────────────────────────────────────────────────
// PHASE 10 — PROGRAM.CS COMPLET (ASP.NET Core 8)
// ─────────────────────────────────────────────────────────────

// Program.cs
using Carter;
using Hangfire;
using MassTransit;
using Microsoft.AspNetCore.RateLimiting;
using NetEscapades.AspNetCore.SecurityHeaders;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ───────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MecaPro.API")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Seq(builder.Configuration["Seq:Url"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ── Database ──────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default"),
        sql =>
        {
            sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
            sql.CommandTimeout(30);
            sql.MigrationsAssembly("MecaPro.Infrastructure");
        })
    .AddInterceptors(new AuditSaveChangesInterceptor(
        builder.Services.BuildServiceProvider().GetRequiredService<ICurrentUserService>()))
);

// ── Identity & Auth ───────────────────────────────────────────
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

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        var publicRsa = RSA.Create();
        publicRsa.ImportFromPem(jwtSettings.PublicKeyPem);
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true, ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(publicRsa),
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero
        };
        opt.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
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

// ── Authorization ─────────────────────────────────────────────
builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("RequireMechanic",
        p => p.RequireRole("Mechanic", "GarageOwner", "SuperAdmin"));
    opt.AddPolicy("RequireGarageOwner",
        p => p.RequireRole("GarageOwner", "SuperAdmin"));
    opt.AddPolicy("RequireActiveSubscription",
        p => p.RequireClaim("subscription", "pro", "enterprise"));
    opt.AddPolicy("RequireAdmin",
        p => p.RequireRole("SuperAdmin"));
});

// ── MediatR + Behaviors ───────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateVehicleCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(CreateVehicleCommandValidator).Assembly);

// ── Cache (Redis) ─────────────────────────────────────────────
builder.Services.AddStackExchangeRedisCache(opt =>
    opt.Configuration = builder.Configuration["Redis:ConnectionString"]);

var redis = ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// ── Stripe ────────────────────────────────────────────────────
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// ── MassTransit + RabbitMQ ────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderPaidConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"], "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"]!);
            h.Password(builder.Configuration["RabbitMQ:Password"]!);
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

// ── Hangfire ──────────────────────────────────────────────────
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddHangfireServer();

// ── SignalR ───────────────────────────────────────────────────
builder.Services.AddSignalR(opt => { opt.EnableDetailedErrors = builder.Environment.IsDevelopment(); })
    .AddStackExchangeRedis(builder.Configuration["Redis:ConnectionString"]!);

// ── Carter (Minimal API modules) ─────────────────────────────
builder.Services.AddCarter();

// ── Rate Limiting ─────────────────────────────────────────────
builder.Services.AddRateLimiter(opt =>
{
    opt.AddSlidingWindowLimiter("global", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.PermitLimit = 300;
        o.QueueLimit = 20;
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    opt.AddSlidingWindowLimiter("auth", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.PermitLimit = 5;
    });
    opt.AddSlidingWindowLimiter("payment", o =>
    {
        o.Window = TimeSpan.FromMinutes(1);
        o.SegmentsPerWindow = 6;
        o.PermitLimit = 10;
    });
    opt.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = 429;
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            error = "Trop de requêtes. Réessayez dans quelques instants.",
            retryAfter = 60
        }, ct);
    };
});

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(opt => opt.AddPolicy("MecaProCors", policy =>
    policy.WithOrigins(
        builder.Configuration["AllowedOrigins:Blazor"]!,
        builder.Configuration["AllowedOrigins:Mobile"]!)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials()));

// ── Health Checks ─────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default")!, name: "sql-server")
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!, name: "redis")
    .AddUrlGroup(new Uri("https://api.stripe.com"), name: "stripe")
    .AddHangfire(opt => { opt.MinimumAvailableServers = 1; }, name: "hangfire");

// ── App Services ──────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IPartRepository, PartRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICrmService, CrmService>();
builder.Services.AddScoped<INotificationService, NotificationDispatcher>();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
builder.Services.AddScoped<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<IStripeSubscriptionService, StripeSubscriptionService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoiceSequencer, InvoiceSequencer>();

// ── OpenTelemetry ─────────────────────────────────────────────
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddEntityFrameworkCoreInstrumentation()
        .AddSource("MecaPro.*"));

// ── API Versioning ────────────────────────────────────────────
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
});

// ── Scalar API Docs ───────────────────────────────────────────
builder.Services.AddOpenApi();

// ─────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Security Headers ──────────────────────────────────────────
app.UseSecurityHeaders(policies => policies
    .AddDefaultSecurityHeaders()
    .AddStrictTransportSecurity(maxAge: 63072000, includeSubDomains: true)
    .AddContentSecurityPolicy(csp => csp.AddDefaultSrc().Self())
    .AddXContentTypeOptions()
    .AddXFrameOptions().Deny()
);

app.UseHttpsRedirection();
app.UseHsts();
app.UseCors("MecaProCors");
app.UseRateLimiter();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TokenBlacklistMiddleware>();
app.UseAuthorization();
app.UseMiddleware<SubscriptionFeatureMiddleware>();
app.UseMiddleware<AuditMiddleware>();

// ── Exception Handler ─────────────────────────────────────────
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var ex = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var statusCode = ex switch
    {
        NotFoundException         => 404,
        BusinessRuleViolationException => 422,
        ValidationException       => 400,
        SubscriptionExpiredException => 402,
        DomainException           => 400,
        _                         => 500
    };
    ctx.Response.StatusCode = statusCode;
    await ctx.Response.WriteAsJsonAsync(new
    {
        type = ex?.GetType().Name,
        title = ex?.Message ?? "Une erreur est survenue.",
        status = statusCode,
        traceId = ctx.TraceIdentifier
    });
}));

// ── Routes ────────────────────────────────────────────────────
app.MapCarter();

// ── SignalR Hubs ──────────────────────────────────────────────
app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

// ── Health Checks ─────────────────────────────────────────────
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// ── Hangfire Dashboard ────────────────────────────────────────
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireAdminFilter()]
});

// ── Scalar Docs ───────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapScalarApiReference();

// ── Register Hangfire Jobs ────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    RecurringJob.AddOrUpdate<RevisionReminderJob>(
        "revision-reminders", x => x.ExecuteAsync(), Cron.Daily(8, 0));
    RecurringJob.AddOrUpdate<SubscriptionExpiryJob>(
        "subscription-expiry", x => x.ExecuteAsync(), Cron.Daily(7, 0));
    RecurringJob.AddOrUpdate<StockAlertJob>(
        "stock-alerts", x => x.ExecuteAsync(), Cron.Hourly());
    RecurringJob.AddOrUpdate<CrmSegmentationJob>(
        "crm-segmentation", x => x.ExecuteAsync(), Cron.Daily(3, 0));

    // Seed database
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();

// ─────────────────────────────────────────────────────────────
// PHASE 11 — BLAZOR COMPONENTS (exemples clés)
// ─────────────────────────────────────────────────────────────

/*
// Pages/Dashboard.razor
@page "/dashboard"
@attribute [Authorize]
@inject IHttpClientFactory HttpFactory
@inject NavigationManager Nav

<PageTitle>Tableau de Bord — MecaPro</PageTitle>

<div class="dashboard-grid">
    @if (_loading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudGrid>
            <MudItem xs="12" sm="6" md="3">
                <MudCard>
                    <MudCardContent>
                        <MudText Typo="Typo.h4">@_stats?.VehiclesInProgress</MudText>
                        <MudText>Véhicules en cours</MudText>
                    </MudCardContent>
                </MudCard>
            </MudItem>
        </MudGrid>
    }
</div>

@code {
    private DashboardStats? _stats;
    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        var client = HttpFactory.CreateClient("MecaProApi");
        _stats = await client.GetFromJsonAsync<DashboardStats>("api/v1/dashboard/stats");
        _loading = false;
    }
}
*/

/*
// Components/QrCodeScanner.razor — .NET MAUI
@using ZXing.Net.Maui

<ZXingBarcodeReader
    Options="@_options"
    IsDetecting="@_isDetecting"
    BarcodesDetected="@OnBarcodeDetected" />

@code {
    private readonly BarcodeReaderOptions _options = new()
    {
        Formats = BarcodeFormats.QrCode,
        AutoRotate = true
    };
    private bool _isDetecting = true;
    [Parameter] public EventCallback<string> OnScanned { get; set; }

    private async Task OnBarcodeDetected(BarcodeDetectionEventArgs args)
    {
        _isDetecting = false;
        var token = args.Results.FirstOrDefault()?.Value;
        if (token != null) await OnScanned.InvokeAsync(token);
    }
}
*/

// ─────────────────────────────────────────────────────────────
// PHASE 13 — TESTS
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Tests.Unit;

public class CustomerTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateCustomer()
    {
        var name = FullName.Create("Jean", "Dupont");
        var email = Email.Create("jean@test.com");
        var customer = Customer.Create(name, email);

        Assert.NotNull(customer);
        Assert.Equal("jean@test.com", customer.Email.Value);
        Assert.Equal(CustomerSegment.Standard, customer.Segment);
        Assert.Single(customer.DomainEvents.OfType<CustomerCreatedEvent>());
    }

    [Fact]
    public void AddLoyaltyPoints_WhenExceedsThreshold_ShouldUpgradeSegment()
    {
        var customer = Customer.Create(FullName.Create("A", "B"), Email.Create("a@b.com"));
        customer.AddLoyaltyPoints(600, "Test");

        Assert.Equal(CustomerSegment.Silver, customer.Segment);
        Assert.Contains(customer.DomainEvents, e => e is CustomerSegmentChangedEvent);
    }

    [Fact]
    public void Email_WithInvalidFormat_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Email.Create("not-an-email"));
    }

    [Fact]
    public void Money_WithNegativeAmount_ShouldThrow()
    {
        Assert.Throws<DomainException>(() => Money.Create(-1));
    }
}

public class VehicleTests
{
    [Fact]
    public void Create_WithValidData_ShouldGenerateQrToken()
    {
        var plate = LicensePlate.Create("AB-123-CD");
        var vehicle = Vehicle.Create(Guid.NewGuid(), plate, "Peugeot", "308", 2021, 50000);

        Assert.NotNull(vehicle.QrCodeToken);
        Assert.Contains("ab123cd", vehicle.QrCodeToken);
    }

    [Fact]
    public void UpdateMileage_WithLowerValue_ShouldThrow()
    {
        var vehicle = Vehicle.Create(Guid.NewGuid(), LicensePlate.Create("AA-000-AA"),
            "Renault", "Clio", 2020, 100000);
        Assert.Throws<DomainException>(() => vehicle.UpdateMileage(50000));
    }
}

public class CreateVehicleHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateVehicle()
    {
        var vehicleRepo = Substitute.For<IVehicleRepository>();
        var customerRepo = Substitute.For<ICustomerRepository>();
        var uow = Substitute.For<IUnitOfWork>();

        var customer = Customer.Create(FullName.Create("J", "D"), Email.Create("j@d.com"));
        customerRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(customer);
        vehicleRepo.GetByLicensePlateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((Vehicle?)null);

        var handler = new CreateVehicleHandler(vehicleRepo, customerRepo, uow);
        var cmd = new CreateVehicleCommand(customer.Id, "AB-123-CD", null, "Peugeot", "308", 2021, 0, null, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("AB-123-CD", result.Value!.LicensePlate);
        await vehicleRepo.Received(1).AddAsync(Arg.Any<Vehicle>(), Arg.Any<CancellationToken>());
    }
}

// ─────────────────────────────────────────────────────────────
// PHASE 14 — MONITORING
// ─────────────────────────────────────────────────────────────

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ctx.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
            correlationId = Guid.NewGuid().ToString();

        ctx.Response.Headers[CorrelationIdHeader] = correlationId;

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId.ToString()))
        {
            await _next(ctx);
        }
    }
}

public class HangfireAdminFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpCtx = context.GetHttpContext();
        return httpCtx.User.IsInRole("SuperAdmin");
    }
}

// Placeholder jobs
public class SubscriptionExpiryJob
{
    public async Task ExecuteAsync() { /* Notify garages with expiring subscriptions */ await Task.CompletedTask; }
}

public class StockAlertJob
{
    private readonly IPartRepository _parts;
    private readonly INotificationService _notifications;
    public StockAlertJob(IPartRepository p, INotificationService n) { _parts = p; _notifications = n; }
    public async Task ExecuteAsync()
    {
        var lowStockParts = await _parts.GetLowStockAsync();
        foreach (var part in lowStockParts)
        {
            // Notify garage owners
        }
    }
}

public class CrmSegmentationJob
{
    private readonly ICrmService _crm;
    public CrmSegmentationJob(ICrmService crm) => _crm = crm;
    public async Task ExecuteAsync() => await _crm.UpdateSegmentsAsync();
}

public class OrderPaidConsumer : IConsumer<OrderPaidEvent>
{
    private readonly IInvoiceService _invoices;
    public OrderPaidConsumer(IInvoiceService inv) => _invoices = inv;

    public async Task Consume(ConsumeContext<OrderPaidEvent> ctx)
    {
        // Auto-generate invoice when order is paid
        await _invoices.GenerateAsync(new GenerateInvoiceCommand(
            ctx.Message.CustomerId, Guid.Empty, new List<InvoiceLine>()));
    }
}

// ─────────────────────────────────────────────────────────────
// PHASE 15 — GITHUB ACTIONS CI/CD PIPELINE
// ─────────────────────────────────────────────────────────────

/*
# .github/workflows/deploy.yml

name: MecaPro CI/CD

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

env:
  DOTNET_VERSION: '8.0.x'
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}/mecapro-api

jobs:
  # ── JOB 1 : Tests ────────────────────────────────────────────
  test:
    name: Build & Test
    runs-on: ubuntu-latest
    
    services:
      sql-server:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          SA_PASSWORD: TestPass@123!
          ACCEPT_EULA: Y
        ports:
          - 1433:1433
      redis:
        image: redis:7-alpine
        ports:
          - 6379:6379

    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Restore packages
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore -c Release

      - name: Run Unit Tests
        run: dotnet test tests/MecaPro.Tests.Unit --no-build -c Release --collect:"XPlat Code Coverage"

      - name: Run Integration Tests
        run: dotnet test tests/MecaPro.Tests.Integration --no-build -c Release
        env:
          ConnectionStrings__Default: "Server=localhost,1433;Database=MecaProTest;User Id=SA;Password=TestPass@123!;TrustServerCertificate=True"
          Redis__ConnectionString: "localhost:6379"

      - name: Upload Coverage
        uses: codecov/codecov-action@v4
        with:
          token: ${{ secrets.CODECOV_TOKEN }}

      - name: SonarQube Scan
        uses: SonarSource/sonarcloud-github-action@master
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}

  # ── JOB 2 : Docker Build ─────────────────────────────────────
  docker:
    name: Build Docker Image
    runs-on: ubuntu-latest
    needs: test
    if: github.ref == 'refs/heads/main'
    
    steps:
      - uses: actions/checkout@v4

      - name: Login to GitHub Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and Push
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./src/MecaPro.API/Dockerfile
          push: true
          tags: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:latest,${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:${{ github.sha }}

  # ── JOB 3 : Deploy to Server ─────────────────────────────────
  deploy:
    name: Deploy to Production
    runs-on: ubuntu-latest
    needs: docker
    if: github.ref == 'refs/heads/main'
    environment: production

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET (for publish)
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ env.DOTNET_VERSION }}

      - name: Publish App
        run: dotnet publish src/MecaPro.API/MecaPro.API.csproj -c Release -o ./publish

      - name: Deploy via SSH
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          port: ${{ secrets.SERVER_PORT }}
          script: |
            # Backup current version
            cp -r /var/www/mecapro/api /var/backups/mecapro/api-$(date +%Y%m%d-%H%M%S) || true
            
            # Stop service
            sudo systemctl stop mecapro
            
            # Clear old files
            rm -rf /var/www/mecapro/api/*

      - name: Copy Files via SCP
        uses: appleboy/scp-action@master
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          port: ${{ secrets.SERVER_PORT }}
          source: "./publish/*"
          target: "/var/www/mecapro/api/"

      - name: Run Migrations & Restart
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.SERVER_HOST }}
          username: ${{ secrets.SERVER_USER }}
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          port: ${{ secrets.SERVER_PORT }}
          script: |
            # Run DB migrations
            cd /var/www/mecapro/api
            dotnet ef database update --no-build || true
            
            # Restart service
            sudo systemctl start mecapro
            sudo systemctl status mecapro
            
            # Wait and health check
            sleep 10
            curl -f http://localhost:5000/health || (sudo journalctl -u mecapro -n 50; exit 1)
            
            echo "✅ Déploiement réussi!"

      - name: Notify Slack on Success
        if: success()
        uses: rtCamp/action-slack-notify@v2
        env:
          SLACK_WEBHOOK: ${{ secrets.SLACK_WEBHOOK }}
          SLACK_MESSAGE: "✅ MecaPro deployed to production - ${{ github.sha }}"

      - name: Notify Slack on Failure
        if: failure()
        uses: rtCamp/action-slack-notify@v2
        env:
          SLACK_WEBHOOK: ${{ secrets.SLACK_WEBHOOK }}
          SLACK_MESSAGE: "🔴 MecaPro deployment FAILED - ${{ github.sha }}"
*/

// ─────────────────────────────────────────────────────────────
// DOCKERFILE
// ─────────────────────────────────────────────────────────────

/*
# src/MecaPro.API/Dockerfile

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/MecaPro.API/MecaPro.API.csproj", "src/MecaPro.API/"]
COPY ["src/MecaPro.Application/MecaPro.Application.csproj", "src/MecaPro.Application/"]
COPY ["src/MecaPro.Infrastructure/MecaPro.Infrastructure.csproj", "src/MecaPro.Infrastructure/"]
COPY ["src/MecaPro.Domain/MecaPro.Domain.csproj", "src/MecaPro.Domain/"]
RUN dotnet restore "src/MecaPro.API/MecaPro.API.csproj"
COPY . .
WORKDIR "/src/src/MecaPro.API"
RUN dotnet build "MecaPro.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MecaPro.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s CMD curl -f http://localhost:8080/health || exit 1
ENTRYPOINT ["dotnet", "MecaPro.API.dll"]
*/

// ─────────────────────────────────────────────────────────────
// APPSETTINGS.JSON TEMPLATE
// ─────────────────────────────────────────────────────────────

/*
// appsettings.Production.json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=MecaProDb;User Id=mecapro_user;Password=__FROM_ENV__;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "Jwt": {
    "Issuer": "https://api.mecapro.app",
    "Audience": "mecapro-clients",
    "PrivateKeyPem": "__FROM_KEY_VAULT__",
    "PublicKeyPem": "__FROM_KEY_VAULT__",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 30
  },
  "Redis": {
    "ConnectionString": "localhost:6379,password=__FROM_ENV__"
  },
  "Stripe": {
    "SecretKey": "__FROM_ENV__",
    "WebhookSecret": "__FROM_ENV__"
  },
  "SendGrid": {
    "ApiKey": "__FROM_ENV__",
    "FromEmail": "noreply@mecapro.app",
    "FromName": "MecaPro"
  },
  "Twilio": {
    "AccountSid": "__FROM_ENV__",
    "AuthToken": "__FROM_ENV__"
  },
  "OAuth": {
    "Google": {
      "ClientId": "__FROM_ENV__",
      "ClientSecret": "__FROM_ENV__"
    }
  },
  "AllowedOrigins": {
    "Blazor": "https://app.mecapro.app",
    "Mobile": "https://mobile.mecapro.app"
  },
  "Seq": {
    "Url": "http://localhost:5341"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Username": "mecapro",
    "Password": "__FROM_ENV__"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
*/
