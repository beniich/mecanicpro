// ============================================================
// PHASE 3 — APPLICATION LAYER : CQRS
// Commands, Queries, Handlers, Validators, MediatR Behaviors
// ============================================================

// ─────────────────────────────────────────────────────────────
// BASE RESULT TYPE
// ─────────────────────────────────────────────────────────────

namespace MecaPro.Application.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public List<string> Errors { get; } = new();

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(string error) { IsSuccess = false; Error = error; Errors.Add(error); }
    private Result(List<string> errors) { IsSuccess = false; Errors = errors; Error = errors.FirstOrDefault(); }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);
    public static Result<T> ValidationFailure(List<string> errors) => new(errors);
    public static implicit operator Result<T>(T value) => Success(value);
}

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrev => Page > 1;
}

// ─────────────────────────────────────────────────────────────
// MEDIATR PIPELINE BEHAVIORS
// ─────────────────────────────────────────────────────────────

// 1. Validation Behavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}

// 2. Logging Behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        _logger.LogInformation("[START] {RequestName} {@Request}", name, request);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            _logger.LogInformation("[END] {RequestName} ({ElapsedMs}ms)", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "[ERROR] {RequestName} ({ElapsedMs}ms)", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

// 3. Caching Behavior
public interface ICacheableQuery { string CacheKey { get; } TimeSpan CacheDuration { get; } }

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    { _cache = cache; _logger = logger; }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable) return await next();

        var cached = await _cache.GetStringAsync(cacheable.CacheKey, ct);
        if (cached != null)
        {
            _logger.LogDebug("[CACHE HIT] {Key}", cacheable.CacheKey);
            return System.Text.Json.JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        var response = await next();
        var json = System.Text.Json.JsonSerializer.Serialize(response);
        await _cache.SetStringAsync(cacheable.CacheKey, json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheable.CacheDuration }, ct);

        return response;
    }
}

// 4. Transaction Behavior
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly AppDbContext _db;

    public TransactionBehavior(AppDbContext db) => _db = db;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is IQuery) return await next(); // No transaction for queries

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
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

public interface IQuery { }

// ─────────────────────────────────────────────────────────────
// MODULE VÉHICULES — COMMANDS & QUERIES
// ─────────────────────────────────────────────────────────────

// --- Commands ---
public record CreateVehicleCommand(
    Guid CustomerId, string LicensePlate, string? VIN,
    string Make, string Model, int Year, int Mileage,
    string? FuelType, string? Color) : IRequest<Result<VehicleDto>>;

public record UpdateVehicleCommand(
    Guid VehicleId, string Make, string Model,
    int Mileage, string? FuelType, string? Color) : IRequest<Result<VehicleDto>>;

public record GenerateQrCodeCommand(Guid VehicleId) : IRequest<Result<QrCodeDto>>;

public record DeleteVehicleCommand(Guid VehicleId) : IRequest<Result<bool>>;

// --- Queries ---
public record GetVehicleByIdQuery(Guid VehicleId) : IRequest<Result<VehicleDetailDto>>;
public record GetVehicleByQrQuery(string QrToken) : IRequest<Result<VehicleDetailDto>>;
public record GetVehiclesByCustomerQuery(Guid CustomerId) : IRequest<Result<IEnumerable<VehicleDto>>>;

public record GetVehiclesPagedQuery(
    int Page, int PageSize, string? Search, Guid? GarageId)
    : IRequest<Result<PagedResult<VehicleDto>>>, ICacheableQuery
{
    public string CacheKey => $"vehicles:paged:{Page}:{PageSize}:{Search}:{GarageId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(2);
}

// --- Handlers ---
public class CreateVehicleHandler : IRequestHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;

    public CreateVehicleHandler(IVehicleRepository vehicles, ICustomerRepository customers, IUnitOfWork uow)
    { _vehicles = vehicles; _customers = customers; _uow = uow; }

    public async Task<Result<VehicleDto>> Handle(CreateVehicleCommand cmd, CancellationToken ct)
    {
        // Check customer exists
        var customer = await _customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer == null) return Result<VehicleDto>.Failure($"Client {cmd.CustomerId} introuvable.");

        // Check duplicate plate
        var existing = await _vehicles.GetByLicensePlateAsync(cmd.LicensePlate, ct);
        if (existing != null) return Result<VehicleDto>.Failure($"Immatriculation '{cmd.LicensePlate}' déjà enregistrée.");

        var plate = LicensePlate.Create(cmd.LicensePlate);
        var vehicle = Vehicle.Create(cmd.CustomerId, plate, cmd.Make, cmd.Model, cmd.Year, cmd.Mileage);

        if (!string.IsNullOrEmpty(cmd.VIN))
            vehicle.GetType().GetProperty("VIN")!.SetValue(vehicle, VIN.Create(cmd.VIN));

        await _vehicles.AddAsync(vehicle, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<VehicleDto>.Success(vehicle.ToDto());
    }
}

public class GenerateQrCodeHandler : IRequestHandler<GenerateQrCodeCommand, Result<QrCodeDto>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly IUnitOfWork _uow;

    public GenerateQrCodeHandler(IVehicleRepository vehicles, IUnitOfWork uow)
    { _vehicles = vehicles; _uow = uow; }

    public async Task<Result<QrCodeDto>> Handle(GenerateQrCodeCommand cmd, CancellationToken ct)
    {
        var vehicle = await _vehicles.GetByIdAsync(cmd.VehicleId, ct);
        if (vehicle == null) return Result<QrCodeDto>.Failure("Véhicule introuvable.");

        vehicle.RegenerateQrToken();
        _vehicles.Update(vehicle);
        await _uow.SaveChangesAsync(ct);

        // Generate QR image
        using var qrGenerator = new QRCodeGenerator();
        var url = $"https://mecapro.app/v/{vehicle.QrCodeToken}";
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(10);
        var base64 = Convert.ToBase64String(pngBytes);

        return Result<QrCodeDto>.Success(new QrCodeDto(
            vehicle.QrCodeToken, url,
            $"data:image/png;base64,{base64}",
            vehicle.LicensePlate.Value));
    }
}

// ─────────────────────────────────────────────────────────────
// MODULE DIAGNOSTICS — COMMANDS & QUERIES
// ─────────────────────────────────────────────────────────────

public record AddDiagnosticCommand(
    Guid VehicleId, string FaultCode, string Description,
    int Severity, string? DiagnosticTool, string? ProbableCauses) : IRequest<Result<DiagnosticDto>>;

public record ResolveDiagnosticCommand(
    Guid DiagnosticId, string Resolution) : IRequest<Result<DiagnosticDto>>;

public record GetDiagnosticsByVehicleQuery(Guid VehicleId) : IRequest<Result<IEnumerable<DiagnosticDto>>>;

public class AddDiagnosticHandler : IRequestHandler<AddDiagnosticCommand, Result<DiagnosticDto>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly ICurrentUserService _user;
    private readonly IUnitOfWork _uow;

    public AddDiagnosticHandler(IVehicleRepository v, ICurrentUserService u, IUnitOfWork uow)
    { _vehicles = v; _user = u; _uow = uow; }

    public async Task<Result<DiagnosticDto>> Handle(AddDiagnosticCommand cmd, CancellationToken ct)
    {
        var vehicle = await _vehicles.GetByIdAsync(cmd.VehicleId, ct);
        if (vehicle == null) return Result<DiagnosticDto>.Failure("Véhicule introuvable.");

        var mechanicId = Guid.Parse(_user.UserId!);
        var severity = (DiagnosticSeverity)cmd.Severity;
        var diag = Diagnostic.Create(vehicle.Id, mechanicId, cmd.FaultCode,
            cmd.Description, severity, cmd.DiagnosticTool, cmd.ProbableCauses);

        vehicle.AddDiagnostic(diag);
        _vehicles.Update(vehicle);
        await _uow.SaveChangesAsync(ct);

        return Result<DiagnosticDto>.Success(diag.ToDto());
    }
}

// ─────────────────────────────────────────────────────────────
// MODULE RÉVISIONS
// ─────────────────────────────────────────────────────────────

public record ScheduleRevisionCommand(
    Guid VehicleId, string Type, DateTime ScheduledDate,
    int EstimatedMinutes, decimal EstimatedCost, int CurrentMileage) : IRequest<Result<RevisionDto>>;

public record CompleteRevisionCommand(
    Guid RevisionId, int ActualMinutes, decimal ActualCost, string? Notes) : IRequest<Result<RevisionDto>>;

public record GetRevisionTimelineQuery(Guid VehicleId) : IRequest<Result<IEnumerable<RevisionDto>>>;

public record ComputeRevisionCostQuery(
    Guid RevisionId, List<Guid> PartIds) : IRequest<Result<RevisionCostDto>>;

public class ScheduleRevisionHandler : IRequestHandler<ScheduleRevisionCommand, Result<RevisionDto>>
{
    private readonly IVehicleRepository _vehicles;
    private readonly IUnitOfWork _uow;

    public ScheduleRevisionHandler(IVehicleRepository v, IUnitOfWork uow) { _vehicles = v; _uow = uow; }

    public async Task<Result<RevisionDto>> Handle(ScheduleRevisionCommand cmd, CancellationToken ct)
    {
        var vehicle = await _vehicles.GetByIdAsync(cmd.VehicleId, ct);
        if (vehicle == null) return Result<RevisionDto>.Failure("Véhicule introuvable.");

        if (cmd.ScheduledDate < DateTime.UtcNow.Date)
            return Result<RevisionDto>.Failure("La date de révision ne peut pas être dans le passé.");

        var revision = Revision.Create(cmd.VehicleId, cmd.Type, cmd.ScheduledDate,
            cmd.EstimatedMinutes, Money.Create(cmd.EstimatedCost), cmd.CurrentMileage);

        vehicle.AddRevision(revision);
        _vehicles.Update(vehicle);
        await _uow.SaveChangesAsync(ct);

        return Result<RevisionDto>.Success(revision.ToDto());
    }
}

// ─────────────────────────────────────────────────────────────
// MODULE CLIENTS — CRM
// ─────────────────────────────────────────────────────────────

public record CreateCustomerCommand(
    string FirstName, string LastName, string Email,
    string? Phone, string? Street, string? City,
    string? PostalCode, string? Country) : IRequest<Result<CustomerDto>>;

public record UpdateCustomerCommand(
    Guid CustomerId, string FirstName, string LastName,
    string Email, string? Phone, string? Notes) : IRequest<Result<CustomerDto>>;

public record GetCustomerByIdQuery(Guid CustomerId) : IRequest<Result<CustomerDetailDto>>;

public record GetCustomersPagedQuery(int Page, int PageSize, string? Search)
    : IRequest<Result<PagedResult<CustomerDto>>>;

public record GetCustomer360Query(Guid CustomerId) : IRequest<Result<Customer360Dto>>;

public record AnonymizeCustomerCommand(Guid CustomerId) : IRequest<Result<bool>>;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;

    public CreateCustomerHandler(ICustomerRepository c, IUnitOfWork uow) { _customers = c; _uow = uow; }

    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var email = Email.Create(cmd.Email);
        var existing = await _customers.GetByEmailAsync(email.Value, ct);
        if (existing != null)
            return Result<CustomerDto>.Failure("Un client avec cet email existe déjà.");

        var name = FullName.Create(cmd.FirstName, cmd.LastName);
        var phone = !string.IsNullOrEmpty(cmd.Phone) ? Phone.Create(cmd.Phone) : null;
        var customer = Customer.Create(name, email, phone);

        if (!string.IsNullOrEmpty(cmd.Street))
            customer.UpdateContact(name, email, phone,
                Address.Create(cmd.Street, cmd.City!, cmd.PostalCode!, cmd.Country ?? "FR"));

        await _customers.AddAsync(customer, ct);
        await _uow.SaveChangesAsync(ct);

        return Result<CustomerDto>.Success(customer.ToDto());
    }
}

// ─────────────────────────────────────────────────────────────
// MODULE PIÈCES
// ─────────────────────────────────────────────────────────────

public record CreatePartCommand(
    string Reference, string Name, string Category,
    decimal Price, int Stock, string? Brand,
    string? Description, List<string>? CompatibleVehicles) : IRequest<Result<PartDto>>;

public record UpdateStockCommand(
    Guid PartId, int Delta, string Reason) : IRequest<Result<PartDto>>;

public record SearchPartsQuery(
    string? Query, string? Category, int Page = 1, int PageSize = 20)
    : IRequest<Result<PagedResult<PartDto>>>, ICacheableQuery
{
    public string CacheKey => $"parts:search:{Query}:{Category}:{Page}:{PageSize}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

public record GetLowStockPartsQuery() : IRequest<Result<IEnumerable<PartDto>>>;

// ─────────────────────────────────────────────────────────────
// VALIDATORS
// ─────────────────────────────────────────────────────────────

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty().WithMessage("Client requis.");
        RuleFor(x => x.LicensePlate).NotEmpty().MaximumLength(20).WithMessage("Immatriculation invalide.");
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100).WithMessage("Marque requise.");
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100).WithMessage("Modèle requis.");
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1).WithMessage("Année invalide.");
        RuleFor(x => x.Mileage).GreaterThanOrEqualTo(0).WithMessage("Kilométrage invalide.");
        RuleFor(x => x.VIN).Length(17).When(x => !string.IsNullOrEmpty(x.VIN)).WithMessage("Le VIN doit avoir 17 caractères.");
    }
}

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email invalide.");
        RuleFor(x => x.Phone).Matches(@"^[\d\+\s\-\(\)]{8,20}$")
            .When(x => !string.IsNullOrEmpty(x.Phone))
            .WithMessage("Numéro de téléphone invalide.");
    }
}

public class AddDiagnosticCommandValidator : AbstractValidator<AddDiagnosticCommand>
{
    public AddDiagnosticCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.FaultCode).NotEmpty().MaximumLength(10).Matches(@"^[A-Z][0-9]{4}$")
            .WithMessage("Code panne invalide (ex: P0301).");
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Severity).InclusiveBetween(1, 4).WithMessage("Sévérité entre 1 (Info) et 4 (Critique).");
    }
}

public class ScheduleRevisionCommandValidator : AbstractValidator<ScheduleRevisionCommand>
{
    public ScheduleRevisionCommandValidator()
    {
        RuleFor(x => x.VehicleId).NotEmpty();
        RuleFor(x => x.Type).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ScheduledDate).GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("La date doit être dans le futur.");
        RuleFor(x => x.EstimatedMinutes).GreaterThan(0);
        RuleFor(x => x.EstimatedCost).GreaterThanOrEqualTo(0);
    }
}

// ─────────────────────────────────────────────────────────────
// DTOs
// ─────────────────────────────────────────────────────────────

public record VehicleDto(
    Guid Id, string LicensePlate, string? VIN, string Make, string Model,
    int Year, int Mileage, string? FuelType, string? Color, string Status,
    string QrCodeToken, DateTime CreatedAt);

public record VehicleDetailDto(
    VehicleDto Vehicle,
    IEnumerable<DiagnosticDto> ActiveDiagnostics,
    RevisionDto? NextRevision,
    IEnumerable<RevisionDto> RevisionHistory,
    IEnumerable<VehicleImageDto> Images,
    CustomerDto Customer);

public record DiagnosticDto(
    Guid Id, string FaultCode, string Description, string Severity,
    string Status, string? Tool, string? ProbableCauses,
    string? Resolution, DateTime DiagnosedAt, DateTime? ResolvedAt);

public record RevisionDto(
    Guid Id, string Type, DateTime ScheduledDate, DateTime? CompletedDate,
    int EstimatedMinutes, int? ActualMinutes, decimal EstimatedCost,
    decimal? ActualCost, string Status, string? Notes);

public record RevisionCostDto(
    decimal LaborCost, decimal PartsCost, decimal TotalHT,
    decimal TVA, decimal TotalTTC, List<string> PartDetails);

public record CustomerDto(
    Guid Id, string FirstName, string LastName, string Email,
    string? Phone, string Segment, int LoyaltyPoints, DateTime CreatedAt);

public record CustomerDetailDto(
    CustomerDto Customer, string? Address,
    int VehicleCount, int ServiceCount, decimal TotalSpent);

public record Customer360Dto(
    CustomerDto Customer,
    IEnumerable<VehicleDto> Vehicles,
    IEnumerable<RevisionDto> RecentRevisions,
    IEnumerable<DiagnosticDto> ActiveDiagnostics,
    IEnumerable<OrderDto> RecentOrders,
    decimal LifetimeValue, string LoyaltyLevel);

public record PartDto(
    Guid Id, string Reference, string Name, string Category,
    string? Brand, decimal Price, int Stock, bool IsLowStock, string? ImageUrl);

public record OrderDto(
    Guid Id, string Status, decimal TotalAmount, DateTime CreatedAt,
    DateTime? PaidAt, IEnumerable<OrderItemDto> Items);

public record OrderItemDto(Guid PartId, string PartName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record QrCodeDto(string Token, string Url, string ImageBase64, string LicensePlate);

public record VehicleImageDto(Guid Id, string FileName, string BlobUrl, string? Description, DateTime UploadedAt);

// Extension methods for mapping
public static class MappingExtensions
{
    public static VehicleDto ToDto(this Vehicle v)
        => new(v.Id, v.LicensePlate.Value, v.VIN?.Value, v.Make, v.Model,
               v.Year, v.Mileage, v.FuelType, v.Color, v.Status.ToString(),
               v.QrCodeToken, v.CreatedAt);

    public static DiagnosticDto ToDto(this Diagnostic d)
        => new(d.Id, d.FaultCode, d.Description, d.Severity.ToString(),
               d.Status.ToString(), d.DiagnosticTool, d.ProbableCauses,
               d.Resolution, d.DiagnosedAt, d.ResolvedAt);

    public static RevisionDto ToDto(this Revision r)
        => new(r.Id, r.Type, r.ScheduledDate, r.CompletedDate,
               r.EstimatedDurationMinutes, r.ActualDurationMinutes,
               r.EstimatedCost.Amount, r.ActualCost?.Amount, r.Status.ToString(), r.Notes);

    public static CustomerDto ToDto(this Customer c)
        => new(c.Id, c.Name.FirstName, c.Name.LastName, c.Email.Value,
               c.Phone?.Value, c.Segment.ToString(), c.Loyalty.Points, c.CreatedAt);

    public static PartDto ToDto(this Part p)
        => new(p.Id, p.Reference, p.Name, p.Category, p.Brand,
               p.UnitPrice.Amount, p.StockQuantity, p.IsLowStock, p.ImageUrl);
}
