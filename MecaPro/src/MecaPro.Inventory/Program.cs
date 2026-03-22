using Carter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using MassTransit;
using System.Security.Cryptography;
using MecaPro.Inventory.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
//  LOGGING (Serilog + Seq)
// ═══════════════════════════════════════════════════════════════════════════
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "MecaPro.Inventory")
    .WriteTo.Console()
    .WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341")
    .CreateLogger();

builder.Host.UseSerilog();

// ═══════════════════════════════════════════════════════════════════════════
//  DATABASE
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddDbContext<InventoryDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ═══════════════════════════════════════════════════════════════════════════
//  SECURITY CONFIG (JWT RS256)
// ═══════════════════════════════════════════════════════════════════════════
var issuer = builder.Configuration["Jwt:Issuer"]!;
var audience = builder.Configuration["Jwt:Audience"]!;
var publicKeyPem = builder.Configuration["Jwt:PublicKeyPem"]!;

var publicRsa = RSA.Create();
publicRsa.ImportFromPem(publicKeyPem);

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
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ") ?? "localhost");
        cfg.ConfigureEndpoints(ctx);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCarter();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapCarter();

app.Run();
