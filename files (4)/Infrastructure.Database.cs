// ============================================================
// PHASE 2 — INFRASTRUCTURE / DATABASE LAYER
// EF Core 8 — DbContext, Configurations, Repositories, Seeder
// ============================================================

// ─────────────────────────────────────────────────────────────
// APP DB CONTEXT
// ─────────────────────────────────────────────────────────────

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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
    public string TokenHash { get; set; } = null!;  // SHA-256 du token
    public string Family { get; set; } = null!;     // pour détecter réutilisation
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

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Domain entities
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Diagnostic> Diagnostics => Set<Diagnostic>();
    public DbSet<Revision> Revisions => Set<Revision>();
    public DbSet<ServiceTask> ServiceTasks => Set<ServiceTask>();
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();

    // Infrastructure
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Rename Identity tables
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");

        // Global query filter: soft delete
        builder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);
        builder.Entity<Vehicle>().HasQueryFilter(v => !v.IsDeleted);
        builder.Entity<Part>().HasQueryFilter(p => !p.IsDeleted);
    }

    // Auto audit
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(ct);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);
        foreach (var entry in entries)
        {
            if (entry.Entity is AggregateRoot<Guid> agg && entry.State == EntityState.Modified)
                agg.MarkUpdated();
        }
    }
}

// ─────────────────────────────────────────────────────────────
// ENTITY CONFIGURATIONS (Fluent API)
// ─────────────────────────────────────────────────────────────

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        // Value Objects as owned
        builder.OwnsOne(c => c.Name, n =>
        {
            n.Property(x => x.FirstName).HasMaxLength(100).HasColumnName("FirstName").IsRequired();
            n.Property(x => x.LastName).HasMaxLength(100).HasColumnName("LastName").IsRequired();
        });

        builder.OwnsOne(c => c.Email, e =>
        {
            e.Property(x => x.Value).HasMaxLength(255).HasColumnName("Email").IsRequired();
            e.HasIndex(x => x.Value).IsUnique();
        });

        builder.OwnsOne(c => c.Phone, p =>
            p.Property(x => x.Value).HasMaxLength(30).HasColumnName("Phone"));

        builder.OwnsOne(c => c.Address, a =>
        {
            a.Property(x => x.Street).HasMaxLength(200).HasColumnName("Street");
            a.Property(x => x.City).HasMaxLength(100).HasColumnName("City");
            a.Property(x => x.PostalCode).HasMaxLength(20).HasColumnName("PostalCode");
            a.Property(x => x.Country).HasMaxLength(3).HasColumnName("Country");
        });

        builder.OwnsOne(c => c.Loyalty, l =>
        {
            l.Property(x => x.Points).HasColumnName("LoyaltyPoints").HasDefaultValue(0);
            l.Property(x => x.Level).HasColumnName("LoyaltyLevel").HasDefaultValue(CustomerSegment.Standard);
            l.Ignore(x => x.Transactions); // not persisted here
        });

        builder.Property(c => c.Segment).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.StripeCustomerId).HasMaxLength(100);
        builder.Property(c => c.Notes).HasMaxLength(2000);

        builder.HasMany(c => c.Vehicles)
               .WithOne()
               .HasForeignKey(v => v.CustomerId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Customers");
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.OwnsOne(v => v.LicensePlate, p =>
        {
            p.Property(x => x.Value).HasMaxLength(20).HasColumnName("LicensePlate").IsRequired();
            p.HasIndex(x => x.Value).IsUnique();
        });

        builder.OwnsOne(v => v.VIN, vin =>
        {
            vin.Property(x => x.Value).HasMaxLength(17).HasColumnName("VIN");
            vin.HasIndex(x => x.Value);
        });

        builder.Property(v => v.Make).HasMaxLength(100).IsRequired();
        builder.Property(v => v.Model).HasMaxLength(100).IsRequired();
        builder.Property(v => v.FuelType).HasMaxLength(30);
        builder.Property(v => v.Color).HasMaxLength(50);
        builder.Property(v => v.QrCodeToken).HasMaxLength(100).IsRequired();
        builder.HasIndex(v => v.QrCodeToken).IsUnique();
        builder.Property(v => v.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasMany(v => v.Diagnostics)
               .WithOne()
               .HasForeignKey(d => d.VehicleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Revisions)
               .WithOne()
               .HasForeignKey(r => r.VehicleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Images)
               .WithOne()
               .HasForeignKey("VehicleId")
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Vehicles");
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.StripeSubscriptionId).HasMaxLength(100).IsRequired();
        builder.HasIndex(s => s.StripeSubscriptionId).IsUnique();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(s => s.PlanTier).HasMaxLength(20);
        builder.ToTable("Subscriptions");
    }
}

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Reference).HasMaxLength(50).IsRequired();
        builder.HasIndex(p => p.Reference).IsUnique();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Brand).HasMaxLength(100);

        builder.OwnsOne(p => p.UnitPrice, m =>
        {
            m.Property(x => x.Amount).HasColumnName("Price").HasColumnType("decimal(10,2)");
            m.Property(x => x.Currency).HasColumnName("Currency").HasMaxLength(3).HasDefaultValue("EUR");
        });

        builder.Property(p => p.CompatibleVehicles)
               .HasConversion(
                   v => string.Join(',', v),
                   v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList())
               .HasMaxLength(2000);

        builder.ToTable("Parts");
    }
}

// ─────────────────────────────────────────────────────────────
// GENERIC REPOSITORY
// ─────────────────────────────────────────────────────────────

public class Repository<T, TId> : IRepository<T, TId>
    where T : AggregateRoot<TId>
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(AppDbContext db) { _db = db; _set = db.Set<T>(); }

    public async Task<T?> GetByIdAsync(TId id, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(e => EF.Property<TId>(e, "Id")!.Equals(id), ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _set.ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
        => await _set.AddAsync(entity, ct);

    public void Update(T entity) => _set.Update(entity);
    public void Remove(T entity) => _set.Remove(entity);
}

// ─────────────────────────────────────────────────────────────
// UNIT OF WORK
// ─────────────────────────────────────────────────────────────

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    private readonly IMediator _mediator;

    public UnitOfWork(AppDbContext db, IMediator mediator) { _db = db; _mediator = mediator; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Dispatch domain events before saving
        var aggregates = _db.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();
        aggregates.ForEach(a => a.ClearDomainEvents());

        var result = await _db.SaveChangesAsync(ct);

        foreach (var evt in domainEvents)
            await _mediator.Publish(evt, ct);

        return result;
    }
}

// ─────────────────────────────────────────────────────────────
// CUSTOMER REPOSITORY
// ─────────────────────────────────────────────────────────────

public class CustomerRepository : Repository<Customer, Guid>, ICustomerRepository
{
    public CustomerRepository(AppDbContext db) : base(db) { }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(c => c.Email.Value == email.ToLowerInvariant(), ct);

    public async Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken ct = default)
        => await _set.Include(c => c.Vehicles).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(
        int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _set.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.Name.FirstName.Contains(search) ||
                c.Name.LastName.Contains(search) ||
                c.Email.Value.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}

// ─────────────────────────────────────────────────────────────
// VEHICLE REPOSITORY
// ─────────────────────────────────────────────────────────────

public class VehicleRepository : Repository<Vehicle, Guid>, IVehicleRepository
{
    public VehicleRepository(AppDbContext db) : base(db) { }

    public async Task<Vehicle?> GetByQrTokenAsync(string token, CancellationToken ct = default)
        => await _set.Include(v => v.Diagnostics).Include(v => v.Revisions)
                     .FirstOrDefaultAsync(v => v.QrCodeToken == token, ct);

    public async Task<Vehicle?> GetByLicensePlateAsync(string plate, CancellationToken ct = default)
        => await _set.FirstOrDefaultAsync(v => v.LicensePlate.Value == plate.ToUpperInvariant(), ct);

    public async Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await _set.Where(v => v.CustomerId == customerId).ToListAsync(ct);

    public async Task<bool> UserHasAccessAsync(Guid userId, Guid vehicleId, CancellationToken ct = default)
    {
        // Mechanic/Owner: access all vehicles of their garage
        // Client: only their own vehicles
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId.ToString(), ct);
        if (user == null) return false;

        var roles = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.UserId == userId.ToString())
            .Select(x => x.Name)
            .ToListAsync(ct);

        if (roles.Any(r => r is "Mechanic" or "GarageOwner" or "SuperAdmin"))
            return true;

        // Client: check vehicle belongs to their customer record
        var vehicle = await _set.FirstOrDefaultAsync(v => v.Id == vehicleId, ct);
        if (vehicle == null) return false;

        return user.CustomerId.HasValue && vehicle.CustomerId == user.CustomerId;
    }
}

// ─────────────────────────────────────────────────────────────
// AUDIT INTERCEPTOR (EF Core)
// ─────────────────────────────────────────────────────────────

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditSaveChangesInterceptor(ICurrentUserService currentUser)
        => _currentUser = currentUser;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        if (eventData.Context is AppDbContext db)
            AuditChanges(db);
        return await base.SavingChangesAsync(eventData, result, ct);
    }

    private void AuditChanges(AppDbContext db)
    {
        var auditEntries = db.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted
                        && e.Entity is not AuditLog and not OutboxMessage)
            .Select(e => new AuditLog
            {
                UserId = _currentUser.UserId,
                Action = e.State.ToString(),
                EntityType = e.Entity.GetType().Name,
                EntityId = e.Property("Id").CurrentValue?.ToString() ?? "",
                OldValues = e.State == EntityState.Modified
                    ? System.Text.Json.JsonSerializer.Serialize(
                        e.OriginalValues.Properties.ToDictionary(p => p.Name, p => e.OriginalValues[p]))
                    : null,
                NewValues = e.State != EntityState.Deleted
                    ? System.Text.Json.JsonSerializer.Serialize(
                        e.CurrentValues.Properties.ToDictionary(p => p.Name, p => e.CurrentValues[p]))
                    : null,
                IpAddress = _currentUser.IpAddress,
                Timestamp = DateTime.UtcNow
            })
            .ToList();

        db.AuditLogs.AddRange(auditEntries);
    }
}

// ─────────────────────────────────────────────────────────────
// DB SEEDER
// ─────────────────────────────────────────────────────────────

public class DatabaseSeeder
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public DatabaseSeeder(AppDbContext db, UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
    { _db = db; _userManager = userManager; _roleManager = roleManager; }

    public async Task SeedAsync()
    {
        await _db.Database.MigrateAsync();
        await SeedRolesAsync();
        await SeedSubscriptionPlansAsync();
        await SeedAdminUserAsync();
        await SeedSamplePartsAsync();
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = ["SuperAdmin", "GarageOwner", "Mechanic", "Client", "ReadOnly"];
        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private async Task SeedSubscriptionPlansAsync()
    {
        if (!_db.SubscriptionPlans.Any())
        {
            _db.SubscriptionPlans.AddRange(
                new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Starter", Tier = "starter", PriceMonthly = 29m, PriceYearly = 290m, MaxMechanics = 1, HasEcommerce = false, HasApiAccess = false },
                new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Pro", Tier = "pro", PriceMonthly = 79m, PriceYearly = 790m, MaxMechanics = 5, HasEcommerce = true, HasApiAccess = false },
                new SubscriptionPlan { Id = Guid.NewGuid(), Name = "Enterprise", Tier = "enterprise", PriceMonthly = 0m, PriceYearly = 0m, MaxMechanics = int.MaxValue, HasEcommerce = true, HasApiAccess = true, IsWhiteLabel = true }
            );
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedAdminUserAsync()
    {
        const string adminEmail = "admin@mecapro.app";
        if (await _userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "MecaPro",
                GarageId = Guid.NewGuid(),
                SubscriptionTier = "enterprise",
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(admin, "Admin@MecaPro2025!");
            await _userManager.AddToRoleAsync(admin, "SuperAdmin");
        }
    }

    private async Task SeedSamplePartsAsync()
    {
        if (!_db.Parts.Any())
        {
            var parts = new[]
            {
                Part.Create("BRK-PLA-001", "Plaquettes frein avant", "Freinage", Money.Create(48m), 12, "Bosch"),
                Part.Create("OIL-FLT-220", "Filtre à huile", "Moteur", Money.Create(18m), 38, "Mann"),
                Part.Create("AIR-FLT-033", "Filtre à air", "Moteur", Money.Create(22m), 25, "K&N"),
                Part.Create("BAT-STD-770", "Batterie 77Ah AGM", "Électrique", Money.Create(189m), 6, "Varta"),
                Part.Create("IGN-SPK-114", "Bougie d'allumage NGK", "Allumage", Money.Create(12m), 50, "NGK"),
            };
            await _db.Parts.AddRangeAsync(parts);
            await _db.SaveChangesAsync();
        }
    }
}

// ─────────────────────────────────────────────────────────────
// OUTBOX PATTERN (pour fiabilité events)
// ─────────────────────────────────────────────────────────────

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

// Placeholders for entities referenced above
public class SubscriptionPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Tier { get; set; } = null!;
    public decimal PriceMonthly { get; set; }
    public decimal PriceYearly { get; set; }
    public int MaxMechanics { get; set; }
    public bool HasEcommerce { get; set; }
    public bool HasApiAccess { get; set; }
    public bool IsWhiteLabel { get; set; }
    public string? StripeProductId { get; set; }
    public string? StripePriceIdMonthly { get; set; }
    public string? StripePriceIdYearly { get; set; }
}

public class Invoice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Number { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Guid GarageId { get; set; }
    public decimal TotalHT { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalTTC { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = "draft";
    public string? PdfBlobUrl { get; set; }
    public string? StripeInvoiceId { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
}

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GarageId { get; set; }
    public string SenderId { get; set; } = null!;
    public string RecipientId { get; set; } = null!;
    public string Content { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public Guid? VehicleId { get; set; }
}

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Body { get; set; } = null!;
    public string Type { get; set; } = null!;
    public string Channel { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
    public string? ActionUrl { get; set; }
    public string? Metadata { get; set; }
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}
