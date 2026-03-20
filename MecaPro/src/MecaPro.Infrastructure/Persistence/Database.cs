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

public class RevisionRepository(AppDbContext db) : Repository<Revision, Guid>(db), IRevisionRepository
{
    public async Task<Revision?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _set.Include(r => r.Tasks).Include(r => r.Parts).FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IEnumerable<Revision>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default)
        => await _set.Where(r => r.VehicleId == vehicleId).OrderByDescending(r => r.ScheduledDate).ToListAsync(ct);

    public async Task<(IEnumerable<Revision> Items, int Total)> GetPagedByVehicleAsync(Guid vehicleId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = _set.Where(v => v.VehicleId == vehicleId).OrderByDescending(r => r.ScheduledDate);
        return (await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), await q.CountAsync(ct));
    }

    public async Task<(IEnumerable<Revision> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var q = _set.AsQueryable();
        if (!string.IsNullOrEmpty(search)) q = q.Where(r => r.Type.Contains(search));
        q = q.OrderByDescending(r => r.ScheduledDate);
        return (await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), await q.CountAsync(ct));
    }
}

public class InvoiceRepository(AppDbContext db) : IInvoiceRepository
{
    public async Task<IEnumerable<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) => await db.Invoices.Where(i => i.CustomerId == customerId).ToListAsync(ct);
    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default) => await db.Invoices.FindAsync(new object[] { id }, ct);
}

public class PartRepository(AppDbContext db) : Repository<Part, Guid>(db), IPartRepository
{
    public async Task<IEnumerable<Part>> GetByCategoryAsync(string category, CancellationToken ct = default) =>
        await _set.Where(p => p.Category == category).ToListAsync(ct);
    public async Task<Part?> GetByReferenceAsync(string reference, CancellationToken ct = default) =>
        await _set.FirstOrDefaultAsync(p => p.Reference == reference, ct);
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
        builder.OwnsOne(c => c.Loyalty, l => 
        {
            l.ToJson();
            l.OwnsMany(la => la.Transactions);
        });
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

        // ── Rôles ─────────────────────────────────────────────
        foreach (var r in new[] { "SuperAdmin", "GarageOwner", "Mechanic", "Client" })
            if (!await roleManager.RoleExistsAsync(r)) await roleManager.CreateAsync(new IdentityRole(r));

        // ── Admin par défaut ──────────────────────────────────
        if (await userManager.FindByEmailAsync("admin@mecapro.com") == null)
        {
            var admin = new AppUser { UserName = "admin@mecapro.com", Email = "admin@mecapro.com", FirstName = "Admin", LastName = "MecaPro", GarageId = Guid.Parse("11111111-1111-1111-1111-111111111111"), EmailConfirmed = true, IsActive = true };
            var res = await userManager.CreateAsync(admin, "Admin@MecaPro123!");
            if (res.Succeeded) await userManager.AddToRoleAsync(admin, "SuperAdmin");
        }

        // ── Mécanicien de démonstration ───────────────────────
        if (await userManager.FindByEmailAsync("mechanic@mecapro.com") == null)
        {
            var mech = new AppUser { UserName = "mechanic@mecapro.com", Email = "mechanic@mecapro.com", FirstName = "Jean-Marc", LastName = "Lefebvre", GarageId = Guid.Parse("11111111-1111-1111-1111-111111111111"), EmailConfirmed = true, IsActive = true };
            var res = await userManager.CreateAsync(mech, "Mech@MecaPro123!");
            if (res.Succeeded) await userManager.AddToRoleAsync(mech, "Mechanic");
        }

        // ── Données de démonstration ──────────────────────────
        if (await db.Customers.AnyAsync()) return;

        // Clients
        var client1 = Customer.Create(FullName.Create("Marc", "Dupont"), Email.Create("marc.dupont@email.com"), Phone.Create("+33612345678"));
        var client2 = Customer.Create(FullName.Create("Sophie", "Martin"), Email.Create("sophie.martin@email.com"), Phone.Create("+33698765432"));
        client1.AddLoyaltyPoints(1250, "Fidélité programme - bienvenue");
        client2.AddLoyaltyPoints(2800, "Fidélité programme - bienvenue");
        db.Customers.AddRange(client1, client2);

        // Véhicules
        var v1 = Vehicle.Create(client1.Id, LicensePlate.Create("AB-123-CD"), "Peugeot", "308", 2021, 42000);
        var v2 = Vehicle.Create(client1.Id, LicensePlate.Create("EF-456-GH"), "Renault", "Clio", 2019, 78000);
        var v3 = Vehicle.Create(client2.Id, LicensePlate.Create("IJ-789-KL"), "Volkswagen", "Golf", 2022, 15000);
        var v4 = Vehicle.Create(client2.Id, LicensePlate.Create("MN-012-OP"), "Toyota", "Yaris", 2020, 55000);
        v2.SetStatus(VehicleStatus.InRepair);
        v1.SetStatus(VehicleStatus.Active);
        db.Vehicles.AddRange(v1, v2, v3, v4);

        // Diagnostics
        var mechUserId = (await userManager.FindByEmailAsync("mechanic@mecapro.com"))?.Id;
        var mechGuid = Guid.TryParse(mechUserId, out var mg) ? mg : Guid.Empty;

        // Révisions
        var rev1 = Revision.Create(v1.Id, "Vidange + Filtres", DateTime.UtcNow.AddDays(3), 90, Money.Create(180m), 42000);
        rev1.AddTask("Vidange huile moteur 5W30", 30);
        rev1.AddTask("Remplacement filtre à huile", 15);
        rev1.AddTask("Contrôle niveaux et freins", 45);
        rev1.AddPart(Guid.NewGuid(), "Huile Moteur 5W30", 5, Money.Create(15.5m));
        rev1.AddPart(Guid.NewGuid(), "Filtre à huile Peugeot", 1, Money.Create(12.9m));

        var rev2 = Revision.Create(v2.Id, "Freins avant", DateTime.UtcNow.AddDays(-1), 120, Money.Create(350m), 78000);
        rev2.AddTask("Dépose des plaquettes usées", 30);
        rev2.AddTask("Nettoyage étriers", 20);
        rev2.AddTask("Pose plaquettes neuves", 40);
        rev2.AddTask("Purge liquide de frein", 30);
        rev2.AddPart(Guid.NewGuid(), "Plaquettes Brembo Front", 1, Money.Create(85m));
        rev2.AddPart(Guid.NewGuid(), "Liquide de frein DOT4", 1, Money.Create(12m));
        
        rev2.Start(mechGuid);
        var rev3 = Revision.Create(v3.Id, "Distribution", DateTime.UtcNow.AddDays(7), 240, Money.Create(650m), 15000);
        db.Revisions.AddRange(rev1, rev2, rev3);

        var diag1 = Diagnostic.Create(v2.Id, mechGuid, "P0301", "Raté d'allumage cylindre 1", DiagnosticSeverity.Major, "OBD-III Pro", "Bougie défectueuse ou bobine d'allumage");
        var diag2 = Diagnostic.Create(v4.Id, mechGuid, "P0420", "Efficacité catalyseur insuffisante", DiagnosticSeverity.Minor, "OBD-III Pro", "Sonde lambda ou catalyseur usé");
        db.Diagnostics.AddRange(diag1, diag2);

        // Pièces en stock
        db.Parts.AddRange(
            Part.Create("FIL-001", "Filtre à huile Peugeot", "Filtres", Money.Create(12.90m), 45, "Bosch"),
            Part.Create("PLQ-001", "Plaquettes de frein avant", "Freinage", Money.Create(48.50m), 15, "Brembo"),
            Part.Create("BOU-001", "Bougie d'allumage NGK", "Allumage", Money.Create(8.90m), 60, "NGK"),
            Part.Create("BAT-001", "Batterie 70Ah", "Électrique", Money.Create(129.00m), 3, "Varta"),
            Part.Create("AMR-002", "Amortisseurs Avant", "Suspension", Money.Create(185.00m), 12, "Monroe"),
            Part.Create("EMB-003", "Kit d'embrayage", "Transmission", Money.Create(340.00m), 1, "Sachs"),
            Part.Create("PNE-004", "Pneu Pilot Sport 5", "Pneumatiques", Money.Create(155.00m), 24, "Michelin")
        );

        // Factures
        db.Invoices.AddRange(
            new Invoice { Id = Guid.NewGuid(), Number = "INV-2025-0001", CustomerId = client1.Id, GarageId = Guid.Parse("11111111-1111-1111-1111-111111111111"), TotalTTC = 216.00m, Status = "Paid", IssuedAt = DateTime.UtcNow.AddDays(-30) },
            new Invoice { Id = Guid.NewGuid(), Number = "INV-2025-0002", CustomerId = client2.Id, GarageId = Guid.Parse("11111111-1111-1111-1111-111111111111"), TotalTTC = 420.00m, Status = "Issued", IssuedAt = DateTime.UtcNow.AddDays(-5) }
        );

        await db.SaveChangesAsync();
    }
}

public class OutboxMessage { public Guid Id { get; set; } public string Type { get; set; } = null!; public string Payload { get; set; } = null!; public DateTime CreatedAt { get; set; } = DateTime.UtcNow; public DateTime? ProcessedAt { get; set; } }

// DTOs for Services
public class SubscriptionPlan { public Guid Id { get; set; } public string Name { get; set; } = null!; public string Tier { get; set; } = null!; public decimal PriceMonthly { get; set; } public int MaxMechanics { get; set; } public bool HasEcommerce { get; set; } public bool HasApiAccess { get; set; } public bool IsWhiteLabel { get; set; } public string? StripePriceIdMonthly { get; set; } }
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

public class RevisionConfiguration : IEntityTypeConfiguration<Revision>
{
    public void Configure(EntityTypeBuilder<Revision> builder)
    {
        builder.HasKey(r => r.Id);
        builder.OwnsOne(r => r.EstimatedCost);
        builder.OwnsOne(r => r.ActualCost);
        builder.HasMany(r => r.Tasks).WithOne().HasForeignKey(t => t.RevisionId);
        builder.HasMany(r => r.Parts).WithOne().HasForeignKey(p => p.RevisionId);
    }
}

public class RevisionPartConfiguration : IEntityTypeConfiguration<RevisionPart>
{
    public void Configure(EntityTypeBuilder<RevisionPart> builder)
    {
        builder.HasKey(rp => rp.Id);
        builder.OwnsOne(rp => rp.UnitPrice);
    }
}

public class PartConfiguration : IEntityTypeConfiguration<Part>
{
    public void Configure(EntityTypeBuilder<Part> builder)
    {
        builder.OwnsOne(p => p.UnitPrice);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.OwnsOne(o => o.TotalAmount);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.OwnsOne(oi => oi.UnitPrice);
    }
}
