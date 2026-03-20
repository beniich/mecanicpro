// ============================================================
// PHASE 1 — DOMAIN LAYER
// Entities, Value Objects, Enums, Interfaces, Exceptions
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace MecaPro.Domain.Common;

public abstract class BaseEntity<TId>
{
    public TId Id { get; protected set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public abstract class AggregateRoot<TId> : BaseEntity<TId>
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(object domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
    public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────
// VALUE OBJECTS
// ─────────────────────────────────────────────────────────────

public record FullName
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string Full => $"{FirstName} {LastName}";

    private FullName() { }

    private FullName(string first, string last)
    {
        FirstName = first;
        LastName = last;
    }

    public static FullName Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first)) throw new ArgumentException("Prénom requis.");
        if (string.IsNullOrWhiteSpace(last)) throw new ArgumentException("Nom requis.");
        return new FullName(first.Trim(), last.Trim());
    }
}

public record Email
{
    public string Value { get; private set; } = null!;
    private Email() { }
    private Email(string value) => Value = value;
    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains("@"))
            throw new ArgumentException("Email invalide.");
        return new Email(value.ToLowerInvariant().Trim());
    }
}

public record Money
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;
    private Money() { }
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    public static Money Create(decimal amount, string currency = "EUR") => new(amount, currency);
    public int InCents() => (int)(Amount * 100);
}

public record LicensePlate
{
    public string Value { get; private set; } = null!;
    private LicensePlate() { }
    private LicensePlate(string value) => Value = value;
    public static LicensePlate Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Immatriculation requise.");
        return new LicensePlate(value.ToUpperInvariant().Trim());
    }
}

public record VIN
{
    public string Value { get; private set; } = null!;
    private VIN() { }
    private VIN(string value) => Value = value;
    public static VIN Create(string value)
    {
        if (value?.Length != 17) throw new ArgumentException("Le VIN doit comporter 17 caractères.");
        return new VIN(value.ToUpperInvariant());
    }
}

public record Address
{
    public string Street { get; private set; } = null!;
    public string City { get; private set; } = null!;
    public string PostalCode { get; private set; } = null!;
    public string Country { get; private set; } = null!;

    private Address() { }

    private Address(string street, string city, string postalCode, string country)
    {
        Street = street; City = city; PostalCode = postalCode; Country = country;
    }

    public static Address Create(string street, string city, string postalCode, string country = "FR")
        => new(street, city, postalCode, country);
}

public record Phone
{
    public string Value { get; private set; } = null!;
    private Phone() { }
    private Phone(string value) => Value = value;
    public static Phone Create(string value) => new(value.Replace(" ", ""));
}

// ─────────────────────────────────────────────────────────────
// ENUMS
// ─────────────────────────────────────────────────────────────

public enum VehicleStatus { Active, InRepair, Idle, Retired }
public enum DiagnosticSeverity { Info = 1, Minor = 2, Major = 3, Critical = 4 }
public enum DiagnosticStatus { Detected, InAnalysis, Resolved, Ignored }
public enum RevisionStatus { Scheduled, InProgress, Completed, Cancelled }
public enum OrderStatus { Draft, Pending, Paid, Shipped, Cancelled }
public enum CustomerSegment { Standard, Silver, Gold, Platinum, VIP }
public enum SubscriptionStatus { Active, Trialing, PastDue, Cancelled }
public enum ContactChannel { Email, SMS, Phone, WhatsApp }

// ─────────────────────────────────────────────────────────────
// ENTITIES
// ─────────────────────────────────────────────────────────────

public class Customer : AggregateRoot<Guid>
{
    public FullName Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Phone? Phone { get; private set; }
    public Address? Address { get; private set; }
    public CustomerSegment Segment { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? Notes { get; private set; }
    public string? Tags { get; private set; }
    public ContactChannel PreferredContact { get; private set; } = ContactChannel.Email;
    public LoyaltyAccount Loyalty { get; private set; } = new();

    private readonly List<Vehicle> _vehicles = new();
    public IReadOnlyCollection<Vehicle> Vehicles => _vehicles.AsReadOnly();
    
    public bool IsBusiness { get; private set; }
    public string? CompanyName { get; private set; }
    public string? TaxId { get; private set; }

    private Customer() { } // EF Core

    public static Customer Create(FullName name, Email email, Phone? phone = null)
    {
        var c = new Customer { Id = Guid.NewGuid(), Name = name, Email = email, Phone = phone, Segment = CustomerSegment.Standard };
        c.AddDomainEvent(new CustomerCreatedEvent(c.Id, email.Value));
        return c;
    }

    public static Customer CreateBusiness(string companyName, string taxId, Email email, Phone? phone = null)
    {
        var c = new Customer { 
            Id = Guid.NewGuid(), 
            Name = FullName.Create("Company", companyName), 
            Email = email, 
            Phone = phone, 
            Segment = CustomerSegment.Gold,
            IsBusiness = true,
            CompanyName = companyName,
            TaxId = taxId
        };
        c.AddDomainEvent(new CustomerCreatedEvent(c.Id, email.Value));
        return c;
    }

    public void UpdateContact(FullName name, Email email, Phone? phone, Address? address, string? notes, string? tags, ContactChannel preferredContact, string? companyName = null, string? taxId = null)
    {
        Name = name; Email = email; Phone = phone; Address = address;
        Notes = notes; Tags = tags; PreferredContact = preferredContact;
        CompanyName = companyName; TaxId = taxId;
        MarkUpdated();
    }

    public void AddLoyaltyPoints(int points, string reason)
    {
        Loyalty.AddPoints(points, reason);
        AddDomainEvent(new LoyaltyPointsAwardedEvent(Id, points, Loyalty.Points));
    }

    public void SetStripeId(string id) => StripeCustomerId = id;
}

public record CustomerCreatedEvent(Guid Id, string Email);
public record LoyaltyPointsAwardedEvent(Guid CustomerId, int PointsAwarded, int NewTotal);

public class LoyaltyAccount
{
    public int Points { get; set; }
    public CustomerSegment Level { get; set; }
    public List<LoyaltyTransaction> Transactions { get; set; } = new();

    public void AddPoints(int p, string reason)
    {
        Points += p;
        Transactions.Add(new LoyaltyTransaction(p, reason, DateTime.UtcNow));
        Level = ComputeSegment();
    }

    public CustomerSegment ComputeSegment() => Points switch
    {
        > 5000 => CustomerSegment.Platinum,
        > 2000 => CustomerSegment.Gold,
        > 500  => CustomerSegment.Silver,
        _      => CustomerSegment.Standard
    };
}

public record LoyaltyTransaction(int Points, string Reason, DateTime Date);

public class Vehicle : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public LicensePlate LicensePlate { get; private set; } = null!;
    public VIN? VIN { get; private set; }
    public string Make { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public int Year { get; private set; }
    public int Mileage { get; private set; }
    public string? FuelType { get; private set; }
    public string? Color { get; private set; }
    public VehicleStatus Status { get; private set; }
    public string QrCodeToken { get; private set; } = null!;

    private readonly List<Diagnostic> _diagnostics = new();
    public IReadOnlyCollection<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();

    private readonly List<Revision> _revisions = new();
    public IReadOnlyCollection<Revision> Revisions => _revisions.AsReadOnly();

    private readonly List<VehicleImage> _images = new();
    public IReadOnlyCollection<VehicleImage> Images => _images.AsReadOnly();

    private Vehicle() { }

    public static Vehicle Create(Guid customerId, LicensePlate plate, string make, string model, int year, int mileage)
    {
        return new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            LicensePlate = plate,
            Make = make,
            Model = model,
            Year = year,
            Mileage = mileage,
            Status = VehicleStatus.Idle,
            QrCodeToken = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()
        };
    }

    public void UpdateMileage(int m) 
    { 
        if (m < Mileage) throw new BusinessRuleViolationException("New mileage must be greater than current.");
        Mileage = m; 
        MarkUpdated(); 
    }
    public void AddDiagnostic(Diagnostic d) => _diagnostics.Add(d);
    public void AddRevision(Revision r) => _revisions.Add(r);
    public void AddImage(VehicleImage i) => _images.Add(i);
    public void SetStatus(VehicleStatus s) => Status = s;
    public void RegenerateQrToken() => QrCodeToken = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
}

public class Diagnostic : BaseEntity<Guid>
{
    public Guid VehicleId { get; set; }
    public Guid MechanicId { get; set; }
    public string FaultCode { get; set; } = null!; // P0301, etc.
    public string Description { get; set; } = null!;
    public DiagnosticSeverity Severity { get; set; }
    public DiagnosticStatus Status { get; set; }
    public string? DiagnosticTool { get; set; }
    public string? ProbableCauses { get; set; }
    public string? Resolution { get; set; }
    public DateTime DiagnosedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public static Diagnostic Create(Guid vId, Guid mId, string code, string desc, DiagnosticSeverity sev, string? tool = null, string? causes = null)
    {
        return new Diagnostic { Id = Guid.NewGuid(), VehicleId = vId, MechanicId = mId, FaultCode = code, Description = desc, Severity = sev, Status = DiagnosticStatus.Detected, DiagnosticTool = tool, ProbableCauses = causes };
    }

    public void Resolve(string res) { Resolution = res; Status = DiagnosticStatus.Resolved; ResolvedAt = DateTime.UtcNow; }
}

public class Revision : AggregateRoot<Guid>
{
    public Guid VehicleId { get; set; }
    public string Type { get; set; } = null!; // "Vidange", "Freins", "Distribution"
    public DateTime ScheduledDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public int? ActualDurationMinutes { get; set; }
    public Money EstimatedCost { get; set; } = null!;
    public Money? ActualCost { get; set; }
    public RevisionStatus Status { get; set; }
    public string? Notes { get; set; }
    public Guid? AssignedMechanicId { get; private set; }

    private readonly List<RevisionTask> _tasks = new();
    public IReadOnlyCollection<RevisionTask> Tasks => _tasks.AsReadOnly();

    private readonly List<RevisionPart> _parts = new();
    public IReadOnlyCollection<RevisionPart> Parts => _parts.AsReadOnly();

    public static Revision Create(Guid vId, string type, DateTime date, int estMin, Money estCost, int mileage)
    {
        return new Revision { Id = Guid.NewGuid(), VehicleId = vId, Type = type, ScheduledDate = date, EstimatedDurationMinutes = estMin, EstimatedCost = estCost, Status = RevisionStatus.Scheduled };
    }

    public void Start(Guid mechanicId) { AssignedMechanicId = mechanicId; Status = RevisionStatus.InProgress; MarkUpdated(); }
    public void Complete(int actualMin, Money actualCost, string? notes = null)
    {
        ActualDurationMinutes = actualMin; ActualCost = actualCost; Notes = notes;
        Status = RevisionStatus.Completed; CompletedDate = DateTime.UtcNow;
        MarkUpdated();
    }
    public void SetStatus(RevisionStatus s) { Status = s; MarkUpdated(); }
    public void AddTask(string desc, int estMin) => _tasks.Add(new RevisionTask(Id, desc, estMin));
    public void AddPart(Guid partId, string name, int qty, Money unitPrice) => _parts.Add(new RevisionPart(Id, partId, name, qty, unitPrice));
}

public class RevisionTask(Guid revisionId, string description, int estimatedMinutes) : BaseEntity<Guid>
{
    public Guid RevisionId { get; set; } = revisionId;
    public string Description { get; set; } = description;
    public int EstimatedMinutes { get; set; } = estimatedMinutes;
    public int? ActualMinutes { get; set; }
    public bool IsCompleted { get; set; }
    public void Complete(int minutes) { IsCompleted = true; ActualMinutes = minutes; }
}

public class RevisionPart(Guid revisionId, Guid partId, string partName, int quantity, Money unitPrice) : BaseEntity<Guid>
{
    public Guid RevisionId { get; set; } = revisionId;
    public Guid PartId { get; set; } = partId;
    public string PartName { get; set; } = partName;
    public int Quantity { get; set; } = quantity;
    public Money UnitPrice { get; set; } = unitPrice;
    public decimal Total => UnitPrice.Amount * Quantity;
}

public class Part : AggregateRoot<Guid>
{
    public string Reference { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string? Brand { get; private set; }
    public string? Description { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public int MinStockAlert { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsAvailable => !IsDeleted && StockQuantity > 0;
    public bool IsLowStock => StockQuantity <= MinStockAlert;
    public List<string> CompatibleVehicles { get; private set; } = new();

    public static Part Create(string refCode, string name, string cat, Money price, int stock, string? brand = null)
    {
        return new Part { Id = Guid.NewGuid(), Reference = refCode, Name = name, Category = cat, UnitPrice = price, StockQuantity = stock, MinStockAlert = 5, Brand = brand };
    }

    public void AdjustStock(int delta) 
    { 
        if (StockQuantity + delta < 0) throw new BusinessRuleViolationException("Insufficient stock.");
        StockQuantity += delta; 
        MarkUpdated(); 
    }
    public void UpdatePrice(Money newPrice) { UnitPrice = newPrice; MarkUpdated(); }
}

public class Order : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid GarageId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = null!;
    public decimal TaxRate { get; private set; } = 0.20m;
    public Money TaxAmount => Money.Create(TotalAmount.Amount * TaxRate);
    public string IdempotencyKey { get; private set; } = Guid.NewGuid().ToString();
    public string? StripePaymentIntentId { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(Guid customerId, Guid garageId, List<(Part Part, int Qty)> items)
    {
        var order = new Order { Id = Guid.NewGuid(), CustomerId = customerId, GarageId = garageId, Status = OrderStatus.Pending };
        decimal total = 0;
        foreach (var (part, qty) in items)
        {
            var item = new OrderItem(order.Id, part.Id, part.Name, part.UnitPrice, qty);
            order._items.Add(item);
            total += item.LineTotal;
        }
        order.TotalAmount = Money.Create(total);
        return order;
    }

    public void MarkPaid(string paymentIntentId) { StripePaymentIntentId = paymentIntentId; Status = OrderStatus.Paid; MarkUpdated(); }
}

public class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid PartId { get; private set; }
    public string PartName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice.Amount * Quantity;

    public OrderItem(Guid orderId, Guid partId, string name, Money price, int qty)
    { OrderId = orderId; PartId = partId; PartName = name; UnitPrice = price; Quantity = qty; }

    private OrderItem() { PartName = null!; UnitPrice = null!; }
}

public class Subscription : AggregateRoot<Guid>
{
    public Guid GarageId { get; private set; }
    public Guid PlanId { get; private set; }
    public string StripeSubscriptionId { get; private set; } = null!;
    public string PlanTier { get; private set; } = null!;
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public bool CancelAtPeriodEnd { get; private set; }

    // Features config
    public int MaxMechanics { get; private set; }
    public bool HasEcommerce { get; private set; }
    public bool HasApiAccess { get; private set; }
    public bool IsWhiteLabel { get; private set; }

    public static Subscription Create(Guid garageId, Guid planId, string stripeId, string tier, int maxMech, bool ecommerce, bool api, bool whitelabel)
    {
        return new Subscription { Id = Guid.NewGuid(), GarageId = garageId, PlanId = planId, StripeSubscriptionId = stripeId, PlanTier = tier, Status = SubscriptionStatus.Active, MaxMechanics = maxMech, HasEcommerce = ecommerce, HasApiAccess = api, IsWhiteLabel = whitelabel, CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1) };
    }

    public void Cancel() { CancelAtPeriodEnd = true; Status = SubscriptionStatus.Cancelled; MarkUpdated(); }
    public void Activate() { Status = SubscriptionStatus.Active; MarkUpdated(); }
    public void SetPastDue() { Status = SubscriptionStatus.PastDue; MarkUpdated(); }
}

public class VehicleImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FileName { get; private set; } = null!;
    public string BlobUrl { get; private set; } = null!;
    public long FileSize { get; private set; }
    public string? Description { get; private set; }
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;

    public static VehicleImage Create(string name, string url, long size) => new() { FileName = name, BlobUrl = url, FileSize = size };
}

// ─────────────────────────────────────────────────────────────
// REPOSITORY INTERFACES
// ─────────────────────────────────────────────────────────────

public interface IRepository<T, TId> where T : AggregateRoot<TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface ICustomerRepository : IRepository<Customer, Guid>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Customer?> GetWithVehiclesAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
}

public interface IVehicleRepository : IRepository<Vehicle, Guid>
{
    Task<Vehicle?> GetByQrTokenAsync(string token, CancellationToken ct = default);
    Task<Vehicle?> GetByLicensePlateAsync(string plate, CancellationToken ct = default);
    Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<bool> UserHasAccessAsync(Guid userId, Guid vehicleId, CancellationToken ct = default);
}

public interface IPartRepository : IRepository<Part, Guid>
{
    Task<IEnumerable<Part>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<Part?> GetByReferenceAsync(string reference, CancellationToken ct = default);
}
public interface IOrderRepository : IRepository<Order, Guid> { }

public interface ISubscriptionRepository : IRepository<Subscription, Guid> { }
public interface IRevisionRepository : IRepository<Revision, Guid>
{
    Task<Revision?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Revision>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default);
    Task<(IEnumerable<Revision> Items, int Total)> GetPagedByVehicleAsync(Guid vehicleId, int page, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<Revision> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
}

public class Invoice
{
    public Guid Id { get; set; }
    public string Number { get; set; } = null!;
    public Guid CustomerId { get; set; }
    public Guid GarageId { get; set; }
    public decimal TotalTTC { get; set; }
    public string? Status { get; set; }
    public string? PdfBlobUrl { get; set; }
    public DateTime IssuedAt { get; set; }
}
public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<Invoice?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────────
// SATISFACTION ENTITIES
// ─────────────────────────────────────────────────────────────

public class SurveyCampaign : BaseEntity<Guid>
{
    public Guid RevisionId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime SentAt { get; private set; }
    public string Token { get; private set; } = null!;
    public int? NpsScore { get; private set; } // 0 to 10
    public string? Comment { get; private set; }
    public string Channel { get; private set; } = "Email";

    private SurveyCampaign() { }

    public static SurveyCampaign Create(Guid revisionId, Guid customerId, string channel = "Email")
    {
        return new SurveyCampaign
        {
            Id = Guid.NewGuid(),
            RevisionId = revisionId,
            CustomerId = customerId,
            SentAt = DateTime.UtcNow,
            Token = Guid.NewGuid().ToString("N"),
            Channel = channel
        };
    }

    public void RegisterResponse(int score, string? comment)
    {
        if (score < 0 || score > 10) throw new BusinessRuleViolationException("NPS Score must be between 0 and 10.");
        NpsScore = score;
        Comment = comment;
    }
}

public interface ISurveyRepository
{
    Task<IEnumerable<SurveyCampaign>> GetAllAsync(Guid garageId, CancellationToken ct = default);
    Task<SurveyCampaign?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(SurveyCampaign survey, CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────────
// DOMAIN SERVICES
// ─────────────────────────────────────────────────────────────

public static class SatisfactionService
{
    public static double CalculateNps(IEnumerable<int> scores)
    {
        var list = scores.ToList();
        if (!list.Any()) return 0;

        var promoters = list.Count(s => s >= 9);
        var detractors = list.Count(s => s <= 6);
        
        return Math.Round(((double)promoters - detractors) / list.Count * 100, 1);
    }
}

public static class MaintenanceService
{
    private static readonly Dictionary<string, int> Thresholds = new()
    {
        { "Huile Moteur", 15000 },
        { "Filtre à Air", 30000 },
        { "Plaquettes Freins", 40000 },
        { "Courroie Distribution", 120000 },
        { "Bougies", 60000 }
    };

    public static int CalculateUrgency(string partType, int currentMileage, int lastMaintenanceMileage)
    {
        if (!Thresholds.TryGetValue(partType, out var threshold)) return 0;
        
        var milesSinceLast = currentMileage - lastMaintenanceMileage;
        if (milesSinceLast < 0) return 0;
        
        var urgency = (double)milesSinceLast / threshold * 100;
        return (int)Math.Min(urgency, 100);
    }
}

public static class LoyaltyService
{
    public static int CalculatePoints(decimal amount, string? serviceType = null, bool isBusiness = false)
    {
        var points = (int)(amount / 10);
        
        if (serviceType == "Diagnostic_IA") points += 50;
        if (isBusiness) points += 20;

        return points;
    }
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

// ─────────────────────────────────────────────────────────────
// DOMAIN EVENTS & EXCEPTIONS
// ─────────────────────────────────────────────────────────────

// CustomerCreatedEvent already declared above

public class DomainException(string message) : Exception(message);
public class NotFoundException(string name, object key) : DomainException($"{name} ({key}) introuvable.");
public class BusinessRuleViolationException(string message) : DomainException(message);
