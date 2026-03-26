using Carter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MecaPro.Notifications.Application;
using System.Security.Cryptography;
using MassTransit;
using MecaPro.Notifications.Consumers;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
//  LOGGING (Serilog + Seq)
// ═══════════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MecaPro.Notifications")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ═══════════════════════════════════════════════════════════════════════════
//  SECURITY CONFIG
// ═══════════════════════════════════════════════════════════════════════════
var issuer = builder.Configuration["Jwt:Issuer"]!;
var audience = builder.Configuration["Jwt:Audience"]!;
var publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"]!;

var publicRsa = RSA.Create();
var pkPem = publicKeyPem.Trim();
if (pkPem.Contains("BEGIN RSA PUBLIC KEY"))
{
    var base64 = pkPem
        .Replace("-----BEGIN RSA PUBLIC KEY-----", "")
        .Replace("-----END RSA PUBLIC KEY-----", "")
        .Replace("\n", "").Replace("\r", "").Trim();
    publicRsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(base64), out _);
}
else
{
    publicRsa.ImportFromPem(pkPem);
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = issuer,
            ValidateAudience = true, ValidAudience = audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new RsaSecurityKey(publicRsa),
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ═══════════════════════════════════════════════════════════════════════════
//  EVENT BUS (MassTransit)
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<SendNotificationConsumer>();
    
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ") ?? "localhost");
        cfg.ConfigureEndpoints(ctx);
    });
});

// ═══════════════════════════════════════════════════════════════════════════
//  APPLICATION SERVICES
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddSingleton<IEmailService, SendGridEmailService>();
builder.Services.AddSingleton<ISmsService, TwilioSmsService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddCarter();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();

app.Run();
