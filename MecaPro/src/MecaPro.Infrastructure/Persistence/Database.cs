using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MecaPro.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MecaPro.Infrastructure.Persistence;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public Guid GarageId { get; set; }
    public Guid? CustomerId { get; set; }
    public string SubscriptionTier { get; set; } = "starter";
    public string? TotpSecretEncrypted { get; set; }
    public bool TotpEnabled { get; set; }
    public string? StripeCustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<RefreshToken> RefreshTokens { get; set; } = new();
}

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public string Family { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }
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

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();
    public DbSet<Revision> Revisions => Set<Revision>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
    }
}

// ─────────────────────────────────────────────────────────────
// REPOSITORIES
// ─────────────────────────────────────────────────────────────

public class Repository<T, TId>(AppDbContext db) : IRepository<T, TId> where T : AggregateRoot<TId>
{
    protected readonly DbSet<T> _set = db.Set<T>();
    public async Task<T?> GetByIdAsync(TId id, CancellationToken ct = default) => await _set.FindAsync(new object[] { id! }, ct);
    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) => await _set.ToListAsync(ct);
    public async Task AddAsync(T entity, CancellationToken ct = default) => await _set.AddAsync(entity, ct);
    public void Update(T entity) => _set.Update(entity);
    public void Remove(T entity) => _set.Remove(entity);
}

public class CustomerRepository(AppDbContext db) : Repository<Customer, Guid>(db), ICustomerRepository
{
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default) => await _set.FirstOrDefaultAsync(c => c.Email.Value == email.ToLower(), ct);
    public async Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken ct = default) => await _set.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == id, ct);
    public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var q = _set.AsQueryable();
        if (!string.IsNullOrEmpty(search)) q = q.Where(c => c.Name.FirstName.Contains(search) || c.Email.Value.Contains(search));
        return (await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), await q.CountAsync(ct));
    }
}

public class VehicleRepository(AppDbContext db) : Repository<Vehicle, Guid>(db), IVehicleRepository
{
    public async Task<Vehicle?> GetByQrTokenAsync(string token, CancellationToken ct = default) => await _set.FirstOrDefaultAsync(v => v.QrCodeToken == token, ct);
    public async Task<Vehicle?> GetByLicensePlateAsync(string plate, CancellationToken ct = default) => await _set.FirstOrDefaultAsync(v => v.LicensePlate.Value == plate.ToUpper(), ct);
    public async Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) => await _set.Where(v => v.CustomerId == customerId).ToListAsync(ct);
    public async Task<bool> UserHasAccessAsync(Guid userId, Guid vehicleId, CancellationToken ct = default) => true; // simplified
}

public class UnitOfWork(AppDbContext db, IMediator mediator) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var aggregates = db.ChangeTracker.Entries<AggregateRoot<Guid>>().Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();
        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());
        int res = await db.SaveChangesAsync(ct);
        foreach (var e in events) await mediator.Publish(e, ct);
        return res;
    }
}

// ─────────────────────────────────────────────────────────────
// CONFIGURATIONS
// ─────────────────────────────────────────────────────────────

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.OwnsOne(c => c.Name);
        builder.OwnsOne(c => c.Email);
        builder.OwnsOne(c => c.Phone);
        builder.OwnsOne(c => c.Address);
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);
        builder.OwnsOne(v => v.LicensePlate);
        builder.OwnsOne(v => v.VIN);
    }
}

// ─────────────────────────────────────────────────────────────
// SEEDER & OUTBOX
// ─────────────────────────────────────────────────────────────

public class DatabaseSeeder(AppDbContext db, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
{
    public async Task SeedAsync()
    {
        await db.Database.MigrateAsync();
        foreach (var r in new[] { "SuperAdmin", "Mechanic", "Client" })
        {
            if (!await roleManager.RoleExistsAsync(r)) await roleManager.CreateAsync(new IdentityRole(r));
        }
    }
}

public class OutboxMessage { public Guid Id { get; set; } public string Type { get; set; } = null!; public string Payload { get; set; } = null!; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? ProcessedAt { get; set; } }

// DTOs for Services
public class SubscriptionPlan { public Guid Id { get; set; } public string Name { get; set; } = null!; public string Tier { get; set; } = null!; public decimal PriceMonthly { get; set; } public int MaxMechanics { get; set; } public bool HasEcommerce { get; set; } public bool HasApiAccess { get; set; } public bool IsWhiteLabel { get; set; } public string? StripePriceIdMonthly { get; set; } }
public class Invoice { public Guid Id { get; set; } public string Number { get; set; } = null!; public Guid CustomerId { get; set; } public Guid GarageId { get; set; } public decimal TotalTTC { get; set; } public string? Status { get; set; } public string? PdfBlobUrl { get; set; } public DateTime IssuedAt { get; set; } }
public class ChatMessage { public Guid Id { get; set; } public Guid GarageId { get; set; } public string SenderId { get; set; } = null!; public string RecipientId { get; set; } = null!; public string Content { get; set; } = null!; public bool IsRead { get; set; } public DateTime SentAt { get; set; } = DateTime.UtcNow; public Guid? VehicleId { get; set; } public DateTime? ReadAt { get; set; } }
public class Notification { public Guid Id { get; set; } public string UserId { get; set; } = null!; public string Title { get; set; } = null!; public string Body { get; set; } = null!; public string? ActionUrl { get; set; } public bool IsRead { get; set; } public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public string? Type { get; set; } public string? Channel { get; set; } public DateTime? ReadAt { get; set; } }



public class TransactionBehavior<TRequest, TResponse>(AppDbContext db) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request.GetType().Name.EndsWith("Query")) return await next();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var response = await next();
            await tx.CommitAsync(ct);
            return response;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
