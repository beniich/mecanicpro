// ============================================================
// PHASE 1 — APPLICATION LAYER (CQRS + PIPELINES)
// ============================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using MecaPro.Domain.Common;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace MecaPro.Application;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string[]? Errors { get; }

    protected Result(bool success, T? value, string? error, string[]? errors = null)
    {
        IsSuccess = success;
        Value = value;
        Error = error;
        Errors = errors;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(string[] errors) => new(false, default, null, errors);
}

public record PagedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

// ─────────────────────────────────────────────────────────────
// PIPELINES
// ─────────────────────────────────────────────────────────────

public class LoggingBehavior<TRequest, TResponse>(ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        logger.LogInformation("[PROJET DASH] Handling {Name}", typeof(TRequest).Name);
        var response = await next();
        logger.LogInformation("[PROJET DASH] Handled {Name}", typeof(TRequest).Name);
        return response;
    }
}

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = validators.Select(v => v.Validate(context)).SelectMany(r => r.Errors).Where(f => f != null).ToList();
        if (failures.Count != 0) throw new ValidationException(failures);
        return await next();
    }
}

public interface ICacheableRequest
{
    string CacheKey { get; }
    TimeSpan CacheDuration { get; }
}

public class CachingBehavior<TRequest, TResponse>(IDistributedCache cache, ILogger<TRequest> logger) : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableRequest cacheable) return await next();
        var cached = await cache.GetStringAsync(cacheable.CacheKey, ct);
        if (cached != null)
        {
            logger.LogDebug("[CACHE HIT] {Key}", cacheable.CacheKey);
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }
        var response = await next();
        var json = JsonSerializer.Serialize(response);
        await cache.SetStringAsync(cacheable.CacheKey, json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheable.CacheDuration }, ct);
        return response;
    }
}

// ─────────────────────────────────────────────────────────────
// DTOs (SOURCE DE VÉRITÉ UNIQUE)
// ─────────────────────────────────────────────────────────────

public record VehicleDto(Guid Id, string LicensePlate, string Make, string Model, int Year, string Status, int Mileage = 0);
public record VehicleDetailDto(Guid Id, string LicensePlate, string? VIN, string Make, string Model, int Year, int Mileage, string? FuelType, string? Color, string Status, string QrCodeToken, DateTime CreatedAt, string? CustomerName);
public record QrCodeDto(string Token, string Url, string Image, string LicensePlate);
public record DiagnosticDto(Guid Id, Guid VehicleId, string FaultCode, string Description, string Severity, string Status, DateTime CreatedAt);
public record RevisionDto(Guid Id, Guid VehicleId, string Type, DateTime ScheduledDate, string Status, decimal EstimatedCost, int EstimatedDuration);
public record InvoiceDto(Guid Id, string Number, decimal Amount, DateTime Date, string Status, string? PdfUrl);
public record DashboardStatsDto(int VehiclesInProgress, int ActiveDiagnostics, int TotalClients, int TodayRevisions);
public record UserProfileDto(string Id, string Name, string Email, string Role, string? Avatar, string? GarageId);
public record PartDto(Guid Id, string Reference, string Name, string Category, string? Brand, decimal UnitPrice, int StockQuantity, bool IsLowStock);

public record CustomerDto(Guid Id, string FirstName, string LastName, string Email, string? Phone, string Segment, int LoyaltyPoints, DateTime CreatedAt, bool IsBusiness = false, string? CompanyName = null);

public record CustomerDetailDto(
    Guid Id, string FirstName, string LastName, string Email, string? Phone, string? Street, string? City, string? PostalCode,
    string Segment, int LoyaltyPoints, string? Notes, string? Tags, string PreferredContact, DateTime CreatedAt,
    List<VehicleDto> Vehicles, List<LoyaltyTransactionDto> LoyaltyHistory, List<RevisionDto> Revisions,
    bool IsBusiness = false, string? CompanyName = null, string? TaxId = null
);
public record LoyaltyTransactionDto(int Points, string Reason, DateTime Date);

public record RevisionTaskDto(Guid Id, string Description, int EstimatedMinutes, int? ActualMinutes, bool IsCompleted);
public record RevisionPartDto(Guid Id, string PartName, int Quantity, decimal UnitPrice, decimal Total);

public record RevisionDetailDto(
    Guid Id, Guid VehicleId, string Type, DateTime ScheduledDate, DateTime? CompletedDate,
    string Status, decimal EstimatedCost, decimal? ActualCost, string? Notes,
    List<RevisionTaskDto> Tasks, List<RevisionPartDto> Parts
);

public record WorkshopScheduleDto(DateTime Date, List<AppointmentDto> Appointments);
public record AppointmentDto(Guid Id, string Title, string Description, string Status, DateTime Start, int DurationMinutes, string? ResourceName);

// ─────────────────────────────────────────────────────────────
// MAPPING EXTENSIONS
// ─────────────────────────────────────────────────────────────

public static class MappingExtensions
{
    public static VehicleDto ToDto(this Vehicle v) => new(v.Id, v.LicensePlate.Value, v.Make, v.Model, v.Year, v.Status.ToString(), v.Mileage);
    public static VehicleDetailDto ToDetailDto(this Vehicle v, string? customerName = null) => new(v.Id, v.LicensePlate.Value, v.VIN?.Value, v.Make, v.Model, v.Year, v.Mileage, v.FuelType, v.Color, v.Status.ToString(), v.QrCodeToken, v.CreatedAt, customerName);
    public static DiagnosticDto ToDto(this Diagnostic d) => new(d.Id, d.VehicleId, d.FaultCode, d.Description, d.Severity.ToString(), d.Status.ToString(), d.CreatedAt);
    public static RevisionDto ToDto(this Revision r) => new(r.Id, r.VehicleId, r.Type, r.ScheduledDate, r.Status.ToString(), r.EstimatedCost.Amount, r.EstimatedDurationMinutes);
    public static InvoiceDto ToDto(this Invoice i) => new(i.Id, i.Number, i.TotalTTC, i.IssuedAt, i.Status ?? "Issued", i.PdfBlobUrl);
    public static PartDto ToDto(this Part p) => new(p.Id, p.Reference, p.Name, p.Category, p.Brand, p.UnitPrice.Amount, p.StockQuantity, p.IsLowStock);
    public static CustomerDto ToDto(this Customer c) => new(c.Id, c.Name.FirstName, c.Name.LastName, c.Email.Value, c.Phone?.Value, c.Segment.ToString(), c.Loyalty.Points, c.CreatedAt, c.IsBusiness, c.CompanyName);

    public static CustomerDetailDto ToDetailDto(this Customer c, List<Revision> revs) => new(
        c.Id, c.Name.FirstName, c.Name.LastName, c.Email.Value, c.Phone?.Value, c.Address?.Street, c.Address?.City, c.Address?.PostalCode,
        c.Segment.ToString(), c.Loyalty.Points, c.Notes, c.Tags, c.PreferredContact.ToString(), c.CreatedAt,
        c.Vehicles.Select(v => v.ToDto()).ToList(),
        c.Loyalty.Transactions.Select(t => new LoyaltyTransactionDto(t.Points, t.Reason, t.Date)).ToList(),
        revs.Select(r => r.ToDto()).ToList(),
        c.IsBusiness, c.CompanyName, c.TaxId
    );

    public static RevisionDetailDto ToDetailDto(this Revision r) => new(
        r.Id, r.VehicleId, r.Type, r.ScheduledDate, r.CompletedDate,
        r.Status.ToString(), r.EstimatedCost.Amount, r.ActualCost?.Amount, r.Notes,
        r.Tasks.Select(t => new RevisionTaskDto(t.Id, t.Description, t.EstimatedMinutes, t.ActualMinutes, t.IsCompleted)).ToList(),
        r.Parts.Select(p => new RevisionPartDto(p.Id, p.PartName, p.Quantity, p.UnitPrice.Amount, p.Total)).ToList()
    );
}

// ─────────────────────────────────────────────────────────────
// COMMANDES & QUERIES
// ─────────────────────────────────────────────────────────────

public record CreateCustomerCommand(string FirstName, string LastName, string Email, string? Phone, bool IsBusiness = false, string? CompanyName = null, string? TaxId = null) : IRequest<Result<CustomerDto>>;
public record UpdateCustomerCommand(Guid Id, string FirstName, string LastName, string Email, string? Phone, string? Street, string? City, string? PostalCode, string? Notes, string? Tags, string PreferredContact, string? CompanyName = null, string? TaxId = null) : IRequest<Result<CustomerDto>>;
public record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDetailDto>>;
public record GetCustomersPagedQuery(int Page, int PageSize, string? Search) : IRequest<Result<PagedResult<CustomerDto>>>;
public record AddLoyaltyPointsCommand(Guid CustomerId, int Points, string Reason) : IRequest<Result<bool>>;

public record GetRevisionDetailQuery(Guid Id) : IRequest<Result<RevisionDetailDto>>;
public record UpdateRevisionStatusCommand(Guid Id, string Status) : IRequest<Result<bool>>;
public record GetWorkshopScheduleQuery(DateTime Start, DateTime End) : IRequest<Result<List<WorkshopScheduleDto>>>;

public record CreateVehicleCommand(Guid CustomerId, string LicensePlate, string Make, string Model, int Year, int Mileage) : IRequest<Result<VehicleDto>>;
public record GetVehiclesByCustomerQuery(Guid CustomerId) : IRequest<Result<List<VehicleDto>>>;

public record GetPartsPagedQuery(int Page, int PageSize, string? Search = null, string? Category = null) : IRequest<Result<PagedResult<PartDto>>>;
public record GetPartByReferenceQuery(string Reference) : IRequest<Result<PartDto>>;
public record AdjustStockCommand(Guid Id, int Delta) : IRequest<Result<bool>>;
public record GetPartCategoriesQuery() : IRequest<Result<List<string>>>;

// ─────────────────────────────────────────────────────────────
// HANDLERS
// ─────────────────────────────────────────────────────────────

public class CreateCustomerHandler(ICustomerRepository customers, IUnitOfWork uow) : IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(CreateCustomerCommand cmd, CancellationToken ct)
    {
        var existing = await customers.GetByEmailAsync(cmd.Email, ct);
        if (existing != null) return Result<CustomerDto>.Failure("Un client avec cet email existe déjà.");

        var customer = cmd.IsBusiness 
            ? Customer.CreateBusiness(cmd.CompanyName ?? "Entité Professionnelle", cmd.TaxId ?? "", Email.Create(cmd.Email), !string.IsNullOrEmpty(cmd.Phone) ? Phone.Create(cmd.Phone) : null)
            : Customer.Create(FullName.Create(cmd.FirstName, cmd.LastName), Email.Create(cmd.Email), !string.IsNullOrEmpty(cmd.Phone) ? Phone.Create(cmd.Phone) : null);

        await customers.AddAsync(customer, ct);
        await uow.SaveChangesAsync(ct);
        return Result<CustomerDto>.Success(customer.ToDto());
    }
}

public class UpdateCustomerHandler(ICustomerRepository customers, IUnitOfWork uow) : IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>
{
    public async Task<Result<CustomerDto>> Handle(UpdateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.Id, ct);
        if (customer == null) return Result<CustomerDto>.Failure("Client introuvable.");
        
        var name = FullName.Create(cmd.FirstName, cmd.LastName);
        var email = Email.Create(cmd.Email);
        var phone = !string.IsNullOrEmpty(cmd.Phone) ? Phone.Create(cmd.Phone) : null;
        var addr = !string.IsNullOrEmpty(cmd.Street) ? Address.Create(cmd.Street, cmd.City ?? "", cmd.PostalCode ?? "") : null;
        Enum.TryParse<ContactChannel>(cmd.PreferredContact, out var contact);

        customer.UpdateContact(name, email, phone, addr, cmd.Notes, cmd.Tags, contact, cmd.CompanyName, cmd.TaxId);
        await uow.SaveChangesAsync(ct);
        return Result<CustomerDto>.Success(customer.ToDto());
    }
}

public class GetCustomersPagedHandler(ICustomerRepository customers) : IRequestHandler<GetCustomersPagedQuery, Result<PagedResult<CustomerDto>>>
{
    public async Task<Result<PagedResult<CustomerDto>>> Handle(GetCustomersPagedQuery query, CancellationToken ct)
    {
        var (items, total) = await customers.GetPagedAsync(query.Page, query.PageSize, query.Search, ct);
        return Result<PagedResult<CustomerDto>>.Success(new PagedResult<CustomerDto>(items.Select(c => c.ToDto()), total, query.Page, query.PageSize));
    }
}

public class GetCustomerByIdHandler(ICustomerRepository customers, IRevisionRepository revisions) : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailDto>>
{
    public async Task<Result<CustomerDetailDto>> Handle(GetCustomerByIdQuery query, CancellationToken ct)
    {
        var customer = await customers.GetWithVehiclesAsync(query.Id, ct);
        if (customer == null) return Result<CustomerDetailDto>.Failure("Client introuvable.");
        
        var revs = new List<Revision>();
        foreach (var v in customer.Vehicles)
        {
            var vRevs = await revisions.GetByVehicleIdAsync(v.Id, ct);
            revs.AddRange(vRevs);
        }

        return Result<CustomerDetailDto>.Success(customer.ToDetailDto(revs.OrderByDescending(r => r.ScheduledDate).ToList()));
    }
}

public class AddLoyaltyPointsHandler(ICustomerRepository customers, IUnitOfWork uow) : IRequestHandler<AddLoyaltyPointsCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AddLoyaltyPointsCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer == null) return Result<bool>.Failure("Client introuvable.");
        customer.AddLoyaltyPoints(cmd.Points, cmd.Reason);
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class CreateVehicleHandler(IVehicleRepository vehicles, IUnitOfWork uow) : IRequestHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    public async Task<Result<VehicleDto>> Handle(CreateVehicleCommand cmd, CancellationToken ct)
    {
        var vehicle = Vehicle.Create(cmd.CustomerId, LicensePlate.Create(cmd.LicensePlate), cmd.Make, cmd.Model, cmd.Year, cmd.Mileage);
        await vehicles.AddAsync(vehicle, ct);
        await uow.SaveChangesAsync(ct);
        return Result<VehicleDto>.Success(vehicle.ToDto());
    }
}

public class GetVehiclesByCustomerHandler(IVehicleRepository vehicles) : IRequestHandler<GetVehiclesByCustomerQuery, Result<List<VehicleDto>>>
{
    public async Task<Result<List<VehicleDto>>> Handle(GetVehiclesByCustomerQuery query, CancellationToken ct)
    {
        var list = await vehicles.GetByCustomerIdAsync(query.CustomerId, ct);
        return Result<List<VehicleDto>>.Success(list.Select(v => v.ToDto()).ToList());
    }
}

public class GetRevisionDetailHandler(IRevisionRepository revisions) : IRequestHandler<GetRevisionDetailQuery, Result<RevisionDetailDto>>
{
    public async Task<Result<RevisionDetailDto>> Handle(GetRevisionDetailQuery query, CancellationToken ct)
    {
        var rev = await revisions.GetWithDetailsAsync(query.Id, ct);
        return rev == null ? Result<RevisionDetailDto>.Failure("Intervention introuvable.") : Result<RevisionDetailDto>.Success(rev.ToDetailDto());
    }
}

public class UpdateRevisionStatusHandler(IRevisionRepository revisions, IUnitOfWork uow) : IRequestHandler<UpdateRevisionStatusCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(UpdateRevisionStatusCommand cmd, CancellationToken ct)
    {
        var rev = await revisions.GetByIdAsync(cmd.Id, ct);
        if (rev == null) return Result<bool>.Failure("Intervention introuvable.");
        
        if (Enum.TryParse<RevisionStatus>(cmd.Status, out var status))
        {
            rev.SetStatus(status);
            await uow.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }
        return Result<bool>.Failure("Statut invalide.");
    }
}

public class GetWorkshopScheduleHandler(IRevisionRepository revisions) : IRequestHandler<GetWorkshopScheduleQuery, Result<List<WorkshopScheduleDto>>>
{
    public async Task<Result<List<WorkshopScheduleDto>>> Handle(GetWorkshopScheduleQuery query, CancellationToken ct)
    {
        var (items, _) = await revisions.GetPagedAsync(1, 1000, null, ct);
        
        var groups = items.Where(r => r.ScheduledDate >= query.Start && r.ScheduledDate <= query.End)
            .GroupBy(r => r.ScheduledDate.Date)
            .Select(g => new WorkshopScheduleDto(g.Key, g.Select(r => new AppointmentDto(
                r.Id, r.Type, "Intervention maintenance", r.Status.ToString(), r.ScheduledDate, r.EstimatedDurationMinutes, "PONT_" + (Math.Abs(r.Id.GetHashCode()) % 3 + 1)
            )).ToList()))
            .OrderBy(s => s.Date)
            .ToList();
            
        return Result<List<WorkshopScheduleDto>>.Success(groups);
    }
}

public class GetPartsPagedHandler(IPartRepository parts) : IRequestHandler<GetPartsPagedQuery, Result<PagedResult<PartDto>>>
{
    public async Task<Result<PagedResult<PartDto>>> Handle(GetPartsPagedQuery query, CancellationToken ct)
    {
        var all = await parts.GetAllAsync(ct);
        var filtered = all.Where(p => (string.IsNullOrEmpty(query.Search) || p.Name.Contains(query.Search, StringComparison.OrdinalIgnoreCase) || p.Reference.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                                   && (string.IsNullOrEmpty(query.Category) || p.Category == query.Category));
        
        var total = filtered.Count();
        var items = filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(p => p.ToDto());
        
        return Result<PagedResult<PartDto>>.Success(new PagedResult<PartDto>(items, total, query.Page, query.PageSize));
    }
}

public class AdjustStockHandler(IPartRepository parts, IUnitOfWork uow) : IRequestHandler<AdjustStockCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var part = await parts.GetByIdAsync(cmd.Id, ct);
        if (part == null) return Result<bool>.Failure("Pièce introuvable.");
        part.AdjustStock(cmd.Delta);
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}

public class GetPartCategoriesHandler(IPartRepository parts) : IRequestHandler<GetPartCategoriesQuery, Result<List<string>>>
{
    public async Task<Result<List<string>>> Handle(GetPartCategoriesQuery query, CancellationToken ct)
    {
        var all = await parts.GetAllAsync(ct);
        return Result<List<string>>.Success(all.Select(p => p.Category).Distinct().ToList());
    }
}
