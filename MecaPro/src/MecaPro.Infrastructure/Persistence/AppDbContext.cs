using Microsoft.EntityFrameworkCore;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Operations;
using MecaPro.Domain.Modules.Inventory;
using MecaPro.Domain.Modules.Invoicing;

namespace MecaPro.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();
    public DbSet<Revision> Revisions => Set<Revision>();
    public DbSet<RevisionTask> RevisionTasks => Set<RevisionTask>();
    public DbSet<RevisionPart> RevisionParts => Set<RevisionPart>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

public class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class OutboxMessage { public Guid Id { get; set; } public string Type { get; set; } = null!; public string Payload { get; set; } = null!; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? ProcessedAt { get; set; } }
public class SubscriptionPlan { public Guid Id { get; set; } public string Name { get; set; } = null!; public string Tier { get; set; } = null!; public decimal PriceMonthly { get; set; } public int MaxMechanics { get; set; } public bool HasEcommerce { get; set; } public bool HasApiAccess { get; set; } public bool IsWhiteLabel { get; set; } public string? StripePriceIdMonthly { get; set; } }
public class ChatMessage { public Guid Id { get; set; } public Guid GarageId { get; set; } public string SenderId { get; set; } = null!; public string RecipientId { get; set; } = null!; public string Content { get; set; } = null!; public bool IsRead { get; set; } public DateTime SentAt { get; set; } = DateTime.UtcNow; public Guid? VehicleId { get; set; } public DateTime? ReadAt { get; set; } }
public class Notification { public Guid Id { get; set; } public string UserId { get; set; } = null!; public string Title { get; set; } = null!; public string Body { get; set; } = null!; public string? ActionUrl { get; set; } public bool IsRead { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public string? Type { get; set; } public string? Channel { get; set; } public DateTime? ReadAt { get; set; } }
