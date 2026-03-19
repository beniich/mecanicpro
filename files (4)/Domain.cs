// ============================================================
// PHASE 1 — DOMAIN LAYER
// MecaPro.Domain — Entités, Value Objects, Events, Interfaces
// ============================================================

// ─────────────────────────────────────────────────────────────
// BASE CLASSES
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Domain.Common;

public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    protected void RaiseDomainEvent(IDomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
    public override bool Equals(object? obj) => obj is Entity<TId> e && Id!.Equals(e.Id);
    public override int GetHashCode() => Id!.GetHashCode();
}

public abstract class AggregateRoot<TId> : Entity<TId>
{
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public string? CreatedBy { get; protected set; }
    public bool IsDeleted { get; protected set; }
    public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
    public void SoftDelete() => IsDeleted = true;
}

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
}

public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────────────────────
// VALUE OBJECTS
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Domain.ValueObjects;

public record Email
{
    public string Value { get; }
    private Email(string value) => Value = value;
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email invalide.");
        var trimmed = email.Trim().ToLowerInvariant();
        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            throw new DomainException($"'{email}' n'est pas un email valide.");
        return new Email(trimmed);
    }
    public override string ToString() => Value;
    public static implicit operator string(Email e) => e.Value;
}

public record FullName
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Full => $"{FirstName} {LastName}";
    private FullName(string first, string last) { FirstName = first; LastName = last; }
    public static FullName Create(string first, string last)
    {
        if (string.IsNullOrWhiteSpace(first)) throw new DomainException("Prénom requis.");
        if (string.IsNullOrWhiteSpace(last)) throw new DomainException("Nom requis.");
        return new FullName(first.Trim(), last.Trim());
    }
    public override string ToString() => Full;
}

public record Phone
{
    public string Value { get; }
    private Phone(string value) => Value = value;
    public static Phone Create(string phone)
    {
        var cleaned = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());
        if (cleaned.Length < 8) throw new DomainException("Numéro de téléphone invalide.");
        return new Phone(cleaned);
    }
    public override string ToString() => Value;
}

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    public static Money Create(decimal amount, string currency = "EUR")
    {
        if (amount < 0) throw new DomainException("Le montant ne peut pas être négatif.");
        return new Money(Math.Round(amount, 2), currency.ToUpper());
    }
    public static Money Zero(string currency = "EUR") => new(0, currency);
    public Money Add(Money other)
    {
        if (Currency != other.Currency) throw new DomainException("Devises différentes.");
        return new Money(Amount + other.Amount, Currency);
    }
    public Money Subtract(Money other) => new Money(Math.Max(0, Amount - other.Amount), Currency);
    public long InCents() => (long)(Amount * 100);
    public override string ToString() => $"{Amount:F2} {Currency}";
}

public record LicensePlate
{
    public string Value { get; }
    private LicensePlate(string value) => Value = value;
    public static LicensePlate Create(string plate)
    {
        if (string.IsNullOrWhiteSpace(plate)) throw new DomainException("Immatriculation requise.");
        return new LicensePlate(plate.Trim().ToUpperInvariant());
    }
    public override string ToString() => Value;
}

public record VIN
{
    public string Value { get; }
    private VIN(string value) => Value = value;
    public static VIN Create(string vin)
    {
        if (string.IsNullOrWhiteSpace(vin)) throw new DomainException("VIN requis.");
        var cleaned = vin.Trim().ToUpperInvariant();
        if (cleaned.Length != 17) throw new DomainException("Le VIN doit contenir 17 caractères.");
        return new VIN(cleaned);
    }
    public override string ToString() => Value;
}

public record Address
{
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }
    private Address(string street, string city, string postalCode, string country)
    { Street = street; City = city; PostalCode = postalCode; Country = country; }
    public static Address Create(string street, string city, string postalCode, string country = "FR")
        => new(street.Trim(), city.Trim(), postalCode.Trim(), country.Trim().ToUpperInvariant());
    public override string ToString() => $"{Street}, {PostalCode} {City}, {Country}";
}

// ─────────────────────────────────────────────────────────────
// ENUMS
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Domain.Enums;

public enum CustomerSegment { Standard, Silver, Gold, Platinum, VIP }
public enum VehicleStatus { Active, Sold, Scrapped }
public enum DiagnosticSeverity { Info = 1, Minor = 2, Major = 3, Critical = 4 }
public enum DiagnosticStatus { Open, InProgress, Resolved, Cancelled }
public enum RevisionStatus { Scheduled, Confirmed, InProgress, Completed, Cancelled }
public enum TaskStatus { Pending, InProgress, Completed, Blocked }
public enum SubscriptionStatus { Trialing, Active, PastDue, Cancelled, Expired }
public enum OrderStatus { Pending, Paid, Processing, Shipped, Delivered, Cancelled, Refunded }
public enum PaymentStatus { Pending, Succeeded, Failed, Refunded, Cancelled }
public enum NotificationChannel { Email, SMS, Push, InApp }
public enum UserRole { SuperAdmin, GarageOwner, Mechanic, Client, ReadOnly }

// ─────────────────────────────────────────────────────────────
// DOMAIN EXCEPTIONS
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string entity, object id) : base($"{entity} avec l'id '{id}' introuvable.") { }
}

public class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string rule) : base(rule) { }
}

public class SubscriptionExpiredException : DomainException
{
    public SubscriptionExpiredException() : base("Abonnement expiré. Veuillez renouveler pour accéder à cette fonctionnalité.") { }
}

public class UnauthorizedAccessException : DomainException
{
    public UnauthorizedAccessException(string action) : base($"Accès non autorisé pour l'action: {action}") { }
}

// ─────────────────────────────────────────────────────────────
// CUSTOMER AGGREGATE
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Domain.Aggregates;

using MecaPro.Domain.Common;
using MecaPro.Domain.Enums;
using MecaPro.Domain.Events;
using MecaPro.Domain.Exceptions;
using MecaPro.Domain.ValueObjects;

public class Customer : AggregateRoot<Guid>
{
    private readonly List<Vehicle> _vehicles = new();
    private readonly List<ServiceRecord> _serviceHistory = new();

    public FullName Name { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public Phone? Phone { get; private set; }
    public Address? Address { get; private set; }
    public CustomerSegment Segment { get; private set; } = CustomerSegment.Standard;
    public LoyaltyAccount Loyalty { get; private set; } = new();
    public string? StripeCustomerId { get; private set; }
    public bool GdprConsentGiven { get; private set; }
    public DateTime? GdprConsentDate { get; private set; }
    public string? Notes { get; private set; }
    public IReadOnlyList<Vehicle> Vehicles => _vehicles.AsReadOnly();
    public IReadOnlyList<ServiceRecord> ServiceHistory => _serviceHistory.AsReadOnly();

    private Customer() { }

    public static Customer Create(FullName name, Email email, Phone? phone = null)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Phone = phone
        };
        customer.RaiseDomainEvent(new CustomerCreatedEvent(customer.Id, email.Value));
        return customer;
    }

    public void UpdateContact(FullName name, Email email, Phone? phone, Address? address)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Address = address;
        MarkUpdated();
        RaiseDomainEvent(new CustomerUpdatedEvent(Id));
    }

    public void AddVehicle(Vehicle vehicle)
    {
        if (_vehicles.Any(v => v.LicensePlate.Value == vehicle.LicensePlate.Value))
            throw new BusinessRuleViolationException("Ce véhicule est déjà associé au client.");
        _vehicles.Add(vehicle);
        MarkUpdated();
        RaiseDomainEvent(new VehicleAddedToCustomerEvent(Id, vehicle.Id));
    }

    public void AddLoyaltyPoints(int points, string reason)
    {
        Loyalty.Credit(points, reason);
        var newSegment = Loyalty.ComputeSegment();
        if (newSegment != Segment)
        {
            Segment = newSegment;
            RaiseDomainEvent(new CustomerSegmentChangedEvent(Id, Segment));
        }
        MarkUpdated();
    }

    public void SetStripeCustomerId(string stripeId)
    {
        StripeCustomerId = stripeId;
        MarkUpdated();
    }

    public void GiveGdprConsent()
    {
        GdprConsentGiven = true;
        GdprConsentDate = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Anonymize()
    {
        var anonymId = Id.ToString()[..8];
        Name = FullName.Create($"Anonyme", anonymId);
        Email = Email.Create($"anonyme_{anonymId}@deleted.local");
        Phone = null;
        Address = null;
        Notes = null;
        SoftDelete();
        RaiseDomainEvent(new CustomerAnonymizedEvent(Id));
    }
}

public class LoyaltyAccount
{
    public int Points { get; private set; }
    public CustomerSegment Level { get; private set; } = CustomerSegment.Standard;
    private readonly List<LoyaltyTransaction> _transactions = new();
    public IReadOnlyList<LoyaltyTransaction> Transactions => _transactions.AsReadOnly();

    public void Credit(int points, string reason)
    {
        Points += points;
        _transactions.Add(new LoyaltyTransaction(points, reason, DateTime.UtcNow));
    }

    public bool Debit(int points, string reason)
    {
        if (Points < points) return false;
        Points -= points;
        _transactions.Add(new LoyaltyTransaction(-points, reason, DateTime.UtcNow));
        return true;
    }

    public CustomerSegment ComputeSegment() => Points switch
    {
        >= 5000 => CustomerSegment.Platinum,
        >= 2000 => CustomerSegment.Gold,
        >= 500  => CustomerSegment.Silver,
        _       => CustomerSegment.Standard
    };
}

public record LoyaltyTransaction(int Points, string Reason, DateTime Date);

// ─────────────────────────────────────────────────────────────
// VEHICLE AGGREGATE
// ─────────────────────────────────────────────────────────────

public class Vehicle : AggregateRoot<Guid>
{
    private readonly List<Diagnostic> _diagnostics = new();
    private readonly List<Revision> _revisions = new();
    private readonly List<VehicleImage> _images = new();

    public Guid CustomerId { get; private set; }
    public LicensePlate LicensePlate { get; private set; } = null!;
    public VIN? VIN { get; private set; }
    public string Make { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public int Year { get; private set; }
    public int Mileage { get; private set; }
    public string? FuelType { get; private set; }
    public string? Color { get; private set; }
    public string QrCodeToken { get; private set; } = null!;
    public VehicleStatus Status { get; private set; } = VehicleStatus.Active;

    public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics.AsReadOnly();
    public IReadOnlyList<Revision> Revisions => _revisions.AsReadOnly();
    public IReadOnlyList<VehicleImage> Images => _images.AsReadOnly();

    private Vehicle() { }

    public static Vehicle Create(Guid customerId, LicensePlate plate, string make, string model, int year, int mileage)
    {
        if (year < 1900 || year > DateTime.UtcNow.Year + 1)
            throw new DomainException("Année de véhicule invalide.");
        var vehicle = new Vehicle
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            LicensePlate = plate,
            Make = make.Trim(),
            Model = model.Trim(),
            Year = year,
            Mileage = mileage,
            QrCodeToken = GenerateQrToken(plate.Value)
        };
        vehicle.RaiseDomainEvent(new VehicleCreatedEvent(vehicle.Id, customerId, plate.Value));
        return vehicle;
    }

    public void UpdateMileage(int newMileage)
    {
        if (newMileage < Mileage) throw new DomainException("Le kilométrage ne peut pas diminuer.");
        Mileage = newMileage;
        MarkUpdated();
    }

    public void AddDiagnostic(Diagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
        RaiseDomainEvent(new DiagnosticAddedEvent(Id, diagnostic.Id, diagnostic.Severity));
    }

    public void AddRevision(Revision revision) => _revisions.Add(revision);
    public void AddImage(VehicleImage image) => _images.Add(image);

    public void RegenerateQrToken()
    {
        QrCodeToken = GenerateQrToken(LicensePlate.Value);
        MarkUpdated();
    }

    private static string GenerateQrToken(string plate)
        => $"{plate.ToLowerInvariant().Replace("-", "")}-{Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..8]}";
}

public class VehicleImage
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FileName { get; private set; } = null!;
    public string BlobUrl { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public DateTime UploadedAt { get; private set; } = DateTime.UtcNow;
    public string? Description { get; private set; }

    public static VehicleImage Create(string fileName, string blobUrl, long size, string? description = null)
        => new() { FileName = fileName, BlobUrl = blobUrl, FileSizeBytes = size, Description = description };
}

// ─────────────────────────────────────────────────────────────
// DIAGNOSTIC ENTITY
// ─────────────────────────────────────────────────────────────

public class Diagnostic : Entity<Guid>
{
    public Guid VehicleId { get; private set; }
    public Guid MechanicId { get; private set; }
    public string FaultCode { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public DiagnosticSeverity Severity { get; private set; }
    public DiagnosticStatus Status { get; private set; } = DiagnosticStatus.Open;
    public string? DiagnosticTool { get; private set; }
    public string? ProbableCauses { get; private set; }
    public string? Resolution { get; private set; }
    public DateTime DiagnosedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; private set; }

    private Diagnostic() { }

    public static Diagnostic Create(Guid vehicleId, Guid mechanicId, string faultCode,
        string description, DiagnosticSeverity severity, string? tool = null, string? causes = null)
        => new()
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            MechanicId = mechanicId,
            FaultCode = faultCode.Trim().ToUpperInvariant(),
            Description = description.Trim(),
            Severity = severity,
            DiagnosticTool = tool,
            ProbableCauses = causes
        };

    public void Resolve(string resolution)
    {
        Status = DiagnosticStatus.Resolved;
        Resolution = resolution;
        ResolvedAt = DateTime.UtcNow;
    }

    public void StartInvestigation() => Status = DiagnosticStatus.InProgress;
}

// ─────────────────────────────────────────────────────────────
// REVISION ENTITY
// ─────────────────────────────────────────────────────────────

public class Revision : Entity<Guid>
{
    private readonly List<ServiceTask> _tasks = new();

    public Guid VehicleId { get; private set; }
    public Guid? AssignedMechanicId { get; private set; }
    public string Type { get; private set; } = null!;
    public DateTime ScheduledDate { get; private set; }
    public DateTime? CompletedDate { get; private set; }
    public int EstimatedDurationMinutes { get; private set; }
    public int? ActualDurationMinutes { get; private set; }
    public Money EstimatedCost { get; private set; } = Money.Zero();
    public Money? ActualCost { get; private set; }
    public RevisionStatus Status { get; private set; } = RevisionStatus.Scheduled;
    public string? Notes { get; private set; }
    public int MileageAtRevision { get; private set; }
    public IReadOnlyList<ServiceTask> Tasks => _tasks.AsReadOnly();

    private Revision() { }

    public static Revision Create(Guid vehicleId, string type, DateTime scheduledDate,
        int estimatedMinutes, Money estimatedCost, int currentMileage)
        => new()
        {
            Id = Guid.NewGuid(),
            VehicleId = vehicleId,
            Type = type.Trim(),
            ScheduledDate = scheduledDate,
            EstimatedDurationMinutes = estimatedMinutes,
            EstimatedCost = estimatedCost,
            MileageAtRevision = currentMileage
        };

    public void Assign(Guid mechanicId)
    {
        AssignedMechanicId = mechanicId;
        Status = RevisionStatus.Confirmed;
    }

    public void Start() => Status = RevisionStatus.InProgress;

    public void Complete(int actualMinutes, Money actualCost, string? notes = null)
    {
        Status = RevisionStatus.Completed;
        CompletedDate = DateTime.UtcNow;
        ActualDurationMinutes = actualMinutes;
        ActualCost = actualCost;
        Notes = notes;
    }

    public void AddTask(ServiceTask task) => _tasks.Add(task);
}

// ─────────────────────────────────────────────────────────────
// SERVICE TASK ENTITY
// ─────────────────────────────────────────────────────────────

public class ServiceTask : Entity<Guid>
{
    public Guid RevisionId { get; private set; }
    public Guid MechanicId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public TaskStatus Status { get; private set; } = TaskStatus.Pending;
    public int EstimatedMinutes { get; private set; }
    public int? ActualMinutes { get; private set; }
    public Money LaborCost { get; private set; } = Money.Zero();
    public Money PartsCost { get; private set; } = Money.Zero();
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? PartsUsed { get; private set; }

    private ServiceTask() { }

    public static ServiceTask Create(Guid revisionId, Guid mechanicId, string title,
        int estimatedMinutes, Money laborCost)
        => new()
        {
            Id = Guid.NewGuid(),
            RevisionId = revisionId,
            MechanicId = mechanicId,
            Title = title.Trim(),
            EstimatedMinutes = estimatedMinutes,
            LaborCost = laborCost,
            StartedAt = DateTime.UtcNow
        };

    public void Complete(int actualMinutes, Money partsCost, string? partsUsed = null)
    {
        Status = TaskStatus.Completed;
        ActualMinutes = actualMinutes;
        PartsCost = partsCost;
        PartsUsed = partsUsed;
        CompletedAt = DateTime.UtcNow;
    }
}

// ─────────────────────────────────────────────────────────────
// SUBSCRIPTION AGGREGATE
// ─────────────────────────────────────────────────────────────

public class Subscription : AggregateRoot<Guid>
{
    public Guid GarageId { get; private set; }
    public Guid PlanId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public string StripeSubscriptionId { get; private set; } = null!;
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public DateTime? CancelAtPeriodEnd { get; private set; }
    public string PlanTier { get; private set; } = "starter";
    public int MaxMechanics { get; private set; }
    public bool HasEcommerce { get; private set; }
    public bool HasApiAccess { get; private set; }
    public bool IsWhiteLabel { get; private set; }

    private Subscription() { }

    public static Subscription Create(Guid garageId, Guid planId, string stripeSubId,
        string planTier, int maxMechanics, bool ecommerce, bool apiAccess, bool whiteLabel)
        => new()
        {
            Id = Guid.NewGuid(),
            GarageId = garageId,
            PlanId = planId,
            StripeSubscriptionId = stripeSubId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            PlanTier = planTier,
            MaxMechanics = maxMechanics,
            HasEcommerce = ecommerce,
            HasApiAccess = apiAccess,
            IsWhiteLabel = whiteLabel
        };

    public void Activate() { Status = SubscriptionStatus.Active; MarkUpdated(); }
    public void SetPastDue() { Status = SubscriptionStatus.PastDue; MarkUpdated(); }

    public void Cancel(DateTime? cancelAt = null)
    {
        if (cancelAt.HasValue)
            CancelAtPeriodEnd = cancelAt;
        else
        {
            Status = SubscriptionStatus.Cancelled;
            EndDate = DateTime.UtcNow;
        }
        MarkUpdated();
        RaiseDomainEvent(new SubscriptionCancelledEvent(Id, GarageId));
    }

    public bool IsActive() => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trialing;
    public bool CanAddMechanic(int currentCount) => currentCount < MaxMechanics;
}

// ─────────────────────────────────────────────────────────────
// PART / CATALOG ENTITY
// ─────────────────────────────────────────────────────────────

public class Part : AggregateRoot<Guid>
{
    public string Reference { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string? Brand { get; private set; }
    public string? Description { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int StockQuantity { get; private set; }
    public int MinStockAlert { get; private set; } = 5;
    public string? ImageUrl { get; private set; }
    public List<string> CompatibleVehicles { get; private set; } = new();
    public bool IsAvailable => StockQuantity > 0;
    public bool IsLowStock => StockQuantity <= MinStockAlert;

    private Part() { }

    public static Part Create(string reference, string name, string category,
        Money price, int stock, string? brand = null)
        => new()
        {
            Id = Guid.NewGuid(),
            Reference = reference.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Category = category.Trim(),
            UnitPrice = price,
            StockQuantity = stock,
            Brand = brand
        };

    public void AdjustStock(int delta)
    {
        var newQty = StockQuantity + delta;
        if (newQty < 0) throw new DomainException($"Stock insuffisant pour '{Name}'.");
        StockQuantity = newQty;
        if (IsLowStock) RaiseDomainEvent(new LowStockAlertEvent(Id, Name, StockQuantity));
        MarkUpdated();
    }

    public void UpdatePrice(Money newPrice) { UnitPrice = newPrice; MarkUpdated(); }
}

// ─────────────────────────────────────────────────────────────
// ORDER AGGREGATE
// ─────────────────────────────────────────────────────────────

public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = new();

    public Guid CustomerId { get; private set; }
    public Guid GarageId { get; private set; }
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public Money TotalAmount { get; private set; } = Money.Zero();
    public Money TaxAmount { get; private set; } = Money.Zero();
    public string? StripePaymentIntentId { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public string? TrackingNumber { get; private set; }
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(Guid customerId, Guid garageId, List<(Part Part, int Qty)> lines)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            GarageId = garageId,
            IdempotencyKey = $"order-{customerId}-{Guid.NewGuid():N}"
        };
        foreach (var (part, qty) in lines)
        {
            order._items.Add(OrderItem.Create(order.Id, part.Id, part.Name, part.UnitPrice, qty));
        }
        order.TotalAmount = Money.Create(order._items.Sum(i => i.LineTotal.Amount));
        order.TaxAmount = Money.Create(order.TotalAmount.Amount * 0.20m); // TVA 20%
        order.RaiseDomainEvent(new OrderCreatedEvent(order.Id, customerId));
        return order;
    }

    public void MarkPaid(string paymentIntentId)
    {
        Status = OrderStatus.Paid;
        StripePaymentIntentId = paymentIntentId;
        PaidAt = DateTime.UtcNow;
        RaiseDomainEvent(new OrderPaidEvent(Id, CustomerId, TotalAmount));
    }

    public void Ship(string trackingNumber)
    {
        Status = OrderStatus.Shipped;
        TrackingNumber = trackingNumber;
        ShippedAt = DateTime.UtcNow;
    }
}

public class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid PartId { get; private set; }
    public string PartName { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = Money.Zero();
    public int Quantity { get; private set; }
    public Money LineTotal => Money.Create(UnitPrice.Amount * Quantity);

    private OrderItem() { }

    public static OrderItem Create(Guid orderId, Guid partId, string partName, Money unitPrice, int qty)
        => new() { Id = Guid.NewGuid(), OrderId = orderId, PartId = partId, PartName = partName, UnitPrice = unitPrice, Quantity = qty };
}

// ─────────────────────────────────────────────────────────────
// SERVICE RECORD (read model)
// ─────────────────────────────────────────────────────────────

public record ServiceRecord(
    Guid VehicleId, string VehicleLabel, string ServiceType,
    DateTime Date, Money Cost, string MechanicName, string Status);

// ─────────────────────────────────────────────────────────────
// DOMAIN EVENTS
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Domain.Events;

public record CustomerCreatedEvent(Guid CustomerId, string Email) : DomainEvent;
public record CustomerUpdatedEvent(Guid CustomerId) : DomainEvent;
public record CustomerSegmentChangedEvent(Guid CustomerId, CustomerSegment NewSegment) : DomainEvent;
public record CustomerAnonymizedEvent(Guid CustomerId) : DomainEvent;
public record VehicleAddedToCustomerEvent(Guid CustomerId, Guid VehicleId) : DomainEvent;
public record VehicleCreatedEvent(Guid VehicleId, Guid CustomerId, string LicensePlate) : DomainEvent;
public record DiagnosticAddedEvent(Guid VehicleId, Guid DiagnosticId, DiagnosticSeverity Severity) : DomainEvent;
public record OrderCreatedEvent(Guid OrderId, Guid CustomerId) : DomainEvent;
public record OrderPaidEvent(Guid OrderId, Guid CustomerId, Money TotalAmount) : DomainEvent;
public record SubscriptionCancelledEvent(Guid SubscriptionId, Guid GarageId) : DomainEvent;
public record LowStockAlertEvent(Guid PartId, string PartName, int CurrentStock) : DomainEvent;

// ─────────────────────────────────────────────────────────────
// REPOSITORY INTERFACES
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Domain.Interfaces;

public interface IRepository<T, TId> where T : AggregateRoot<TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
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
    Task<(IEnumerable<Part> Items, int Total)> SearchAsync(string? query, string? category, int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Part>> GetLowStockAsync(CancellationToken ct = default);
}

public interface ISubscriptionRepository : IRepository<Subscription, Guid>
{
    Task<Subscription?> GetActiveByGarageIdAsync(Guid garageId, CancellationToken ct = default);
    Task<Subscription?> GetByStripeIdAsync(string stripeSubId, CancellationToken ct = default);
}

public interface IOrderRepository : IRepository<Order, Guid>
{
    Task<Order?> GetByPaymentIntentAsync(string paymentIntentId, CancellationToken ct = default);
    Task<IEnumerable<Order>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
}
