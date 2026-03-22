using System;
using System.Collections.Generic;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.Operations;

public enum VehicleStatus { Active, InRepair, Idle, Retired }
public enum DiagnosticSeverity { Info = 1, Minor = 2, Major = 3, Critical = 4 }
public enum DiagnosticStatus { Detected, InAnalysis, Resolved, Ignored }
public enum RevisionStatus { Scheduled, InProgress, Completed, Cancelled }

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
    private RevisionTask() : this(Guid.Empty, "", 0) { }
}

public class RevisionPart(Guid revisionId, Guid partId, string partName, int quantity, Money unitPrice) : BaseEntity<Guid>
{
    public Guid RevisionId { get; set; } = revisionId;
    public Guid PartId { get; set; } = partId;
    public string PartName { get; set; } = partName;
    public int Quantity { get; set; } = quantity;
    public Money UnitPrice { get; set; } = unitPrice;
    public decimal Total => UnitPrice.Amount * Quantity;
    private RevisionPart() : this(Guid.Empty, Guid.Empty, "", 0, null!) { }
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
