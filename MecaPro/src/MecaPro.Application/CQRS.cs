using MediatR;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using MecaPro.Domain.Common;
using QRCoder;
using System.Text.Json;

namespace MecaPro.Application.Common;

public interface ICurrentUserService { string? UserId { get; } string? IpAddress { get; } bool IsAuthenticated { get; } }

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

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators) => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (!_validators.Any()) return await next();
        var context = new ValidationContext<TRequest>(request);
        var failures = _validators.Select(v => v.Validate(context)).SelectMany(r => r.Errors).Where(f => f != null).ToList();
        if (failures.Any()) throw new ValidationException(failures);
        return await next();
    }
}

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var name = typeof(TRequest).Name;
        logger.LogInformation("[START] {RequestName} {@Request}", name, request);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();
            logger.LogInformation("[END] {RequestName} ({ElapsedMs}ms)", name, sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "[ERROR] {RequestName} ({ElapsedMs}ms)", name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}

public interface ICacheableQuery { string CacheKey { get; } TimeSpan CacheDuration { get; } }

public class CachingBehavior<TRequest, TResponse>(IDistributedCache cache, ILogger<CachingBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (request is not ICacheableQuery cacheable) return await next();
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


public interface IQuery { }

// ─────────────────────────────────────────────────────────────
// MODULE VÉHICULES
// ─────────────────────────────────────────────────────────────

public record CreateVehicleCommand(Guid CustomerId, string LicensePlate, string? VIN, string Make, string Model, int Year, int Mileage, string? FuelType, string? Color) : IRequest<Result<VehicleDto>>;
public record UpdateVehicleCommand(Guid VehicleId, string Make, string Model, int Mileage, string? FuelType, string? Color) : IRequest<Result<VehicleDto>>;
public record GenerateQrCodeCommand(Guid VehicleId) : IRequest<Result<QrCodeDto>>;
public record DeleteVehicleCommand(Guid VehicleId) : IRequest<Result<bool>>;

public record GetVehicleByIdQuery(Guid VehicleId) : IRequest<Result<VehicleDetailDto>>;
public record GetVehicleByQrQuery(string QrToken) : IRequest<Result<VehicleDetailDto>>;
public record GetVehiclesByCustomerQuery(Guid CustomerId) : IRequest<Result<IEnumerable<VehicleDto>>>;
public record GetVehiclesPagedQuery(int Page, int PageSize, string? Search, Guid? GarageId) : IRequest<Result<PagedResult<VehicleDto>>>, ICacheableQuery
{
    public string CacheKey => $"vehicles:paged:{Page}:{PageSize}:{Search}:{GarageId}";
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(2);
}

public class CreateVehicleHandler(IVehicleRepository vehicles, ICustomerRepository customers, IUnitOfWork uow) : IRequestHandler<CreateVehicleCommand, Result<VehicleDto>>
{
    public async Task<Result<VehicleDto>> Handle(CreateVehicleCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer == null) return Result<VehicleDto>.Failure($"Client {cmd.CustomerId} introuvable.");
        var existing = await vehicles.GetByLicensePlateAsync(cmd.LicensePlate, ct);
        if (existing != null) return Result<VehicleDto>.Failure($"Immatriculation '{cmd.LicensePlate}' déjà enregistrée.");
        var plate = LicensePlate.Create(cmd.LicensePlate);
        var vehicle = Vehicle.Create(cmd.CustomerId, plate, cmd.Make, cmd.Model, cmd.Year, cmd.Mileage);
        await vehicles.AddAsync(vehicle, ct);
        await uow.SaveChangesAsync(ct);
        return Result<VehicleDto>.Success(vehicle.ToDto());
    }
}

public class GenerateQrCodeHandler(IVehicleRepository vehicles, IUnitOfWork uow) : IRequestHandler<GenerateQrCodeCommand, Result<QrCodeDto>>
{
    public async Task<Result<QrCodeDto>> Handle(GenerateQrCodeCommand cmd, CancellationToken ct)
    {
        var vehicle = await vehicles.GetByIdAsync(cmd.VehicleId, ct);
        if (vehicle == null) return Result<QrCodeDto>.Failure("Véhicule introuvable.");
        vehicle.RegenerateQrToken();
        vehicles.Update(vehicle);
        await uow.SaveChangesAsync(ct);
        using var qrGenerator = new QRCodeGenerator();
        var url = $"https://mecapro.app/v/{vehicle.QrCodeToken}";
        var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        var base64 = Convert.ToBase64String(qrCode.GetGraphic(10));
        return Result<QrCodeDto>.Success(new QrCodeDto(vehicle.QrCodeToken, url, $"data:image/png;base64,{base64}", vehicle.LicensePlate.Value));
    }
}

// ─────────────────────────────────────────────────────────────
// MODULE DIAGNOSTICS
// ─────────────────────────────────────────────────────────────

public record AddDiagnosticCommand(Guid VehicleId, string FaultCode, string Description, int Severity, string? DiagnosticTool, string? ProbableCauses) : IRequest<Result<DiagnosticDto>>;
public record ResolveDiagnosticCommand(Guid DiagnosticId, string Resolution) : IRequest<Result<DiagnosticDto>>;

public class AddDiagnosticHandler(IVehicleRepository v, ICurrentUserService u, IUnitOfWork uow) : IRequestHandler<AddDiagnosticCommand, Result<DiagnosticDto>>
{
    public async Task<Result<DiagnosticDto>> Handle(AddDiagnosticCommand cmd, CancellationToken ct)
    {
        var vehicle = await v.GetByIdAsync(cmd.VehicleId, ct);
        if (vehicle == null) return Result<DiagnosticDto>.Failure("Véhicule introuvable.");
        var mechanicId = Guid.Parse(u.UserId ?? Guid.Empty.ToString());
        var diag = Diagnostic.Create(vehicle.Id, mechanicId, cmd.FaultCode, cmd.Description, (DiagnosticSeverity)cmd.Severity, cmd.DiagnosticTool, cmd.ProbableCauses);
        vehicle.AddDiagnostic(diag);
        v.Update(vehicle);
        await uow.SaveChangesAsync(ct);
        return Result<DiagnosticDto>.Success(diag.ToDto());
    }
}

// ─────────────────────────────────────────────────────────────
// VALIDATORS
// ─────────────────────────────────────────────────────────────

public class CreateVehicleCommandValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.LicensePlate).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Make).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Model).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1);
        RuleFor(x => x.Mileage).GreaterThanOrEqualTo(0);
    }
}

// ─────────────────────────────────────────────────────────────
// DTOs & MAPPING
// ─────────────────────────────────────────────────────────────

public record VehicleDto(Guid Id, string LicensePlate, string? VIN, string Make, string Model, int Year, int Mileage, string? FuelType, string? Color, string Status, string QrCodeToken, DateTime CreatedAt);
public record VehicleDetailDto(VehicleDto Vehicle, IEnumerable<DiagnosticDto> ActiveDiagnostics, object? NextRevision, IEnumerable<object> RevisionHistory, IEnumerable<object> Images, object Customer);
public record DiagnosticDto(Guid Id, string FaultCode, string Description, string Severity, string Status, string? Tool, string? ProbableCauses, string? Resolution, DateTime DiagnosedAt, DateTime? ResolvedAt);
public record QrCodeDto(string Token, string Url, string ImageBase64, string LicensePlate);

public static class MappingExtensions
{
    public static VehicleDto ToDto(this Vehicle v) => new(v.Id, v.LicensePlate.Value, v.VIN?.Value, v.Make, v.Model, v.Year, v.Mileage, v.FuelType, v.Color, v.Status.ToString(), v.QrCodeToken, v.CreatedAt);
    public static DiagnosticDto ToDto(this Diagnostic d) => new(d.Id, d.FaultCode, d.Description, d.Severity.ToString(), d.Status.ToString(), d.DiagnosticTool, d.ProbableCauses, d.Resolution, d.DiagnosedAt, d.ResolvedAt);
}
