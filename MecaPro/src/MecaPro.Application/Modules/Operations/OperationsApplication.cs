using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Application.Common;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Operations;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Inventory;
using MediatR;

namespace MecaPro.Application.Modules.Operations;

// DTOs
public record VehicleDto(Guid Id, string LicensePlate, string Make, string Model, int Year, string Status, int Mileage = 0);
public record VehicleDetailDto(Guid Id, string LicensePlate, string? VIN, string Make, string Model, int Year, int Mileage, string? FuelType, string? Color, string Status, string QrCodeToken, DateTime CreatedAt, string? CustomerName);
public record QrCodeDto(string Token, string Url, string Image, string LicensePlate);
public record DiagnosticDto(Guid Id, Guid VehicleId, string FaultCode, string Description, string Severity, string Status, DateTime CreatedAt);
public record RevisionDto(Guid Id, Guid VehicleId, string Type, DateTime ScheduledDate, string Status, decimal EstimatedCost, int EstimatedDuration);
public record DashboardStatsDto(int VehiclesInProgress, int ActiveDiagnostics, int TotalClients, int TodayRevisions);
public record RevisionTaskDto(Guid Id, string Description, int EstimatedMinutes, int? ActualMinutes, bool IsCompleted);
public record RevisionPartDto(Guid Id, string PartName, int Quantity, decimal UnitPrice, decimal Total);
public record RevisionDetailDto(
    Guid Id, Guid VehicleId, string Type, DateTime ScheduledDate, DateTime? CompletedDate,
    string Status, decimal EstimatedCost, decimal? ActualCost, string? Notes,
    List<RevisionTaskDto> Tasks, List<RevisionPartDto> Parts
);
public record WorkshopScheduleDto(DateTime Date, List<AppointmentDto> Appointments);
public record AppointmentDto(Guid Id, string Title, string Description, string Status, DateTime Start, int DurationMinutes, string? ResourceName);

// Mapping Extensions
public static class OperationsMappingExtensions
{
    public static VehicleDto ToDto(this Vehicle v) => new(v.Id, v.LicensePlate.Value, v.Make, v.Model, v.Year, v.Status.ToString(), v.Mileage);
    public static VehicleDetailDto ToDetailDto(this Vehicle v, string? customerName = null) => new(v.Id, v.LicensePlate.Value, v.VIN?.Value, v.Make, v.Model, v.Year, v.Mileage, v.FuelType, v.Color, v.Status.ToString(), v.QrCodeToken, v.CreatedAt, customerName);
    public static DiagnosticDto ToDto(this Diagnostic d) => new(d.Id, d.VehicleId, d.FaultCode, d.Description, d.Severity.ToString(), d.Status.ToString(), d.CreatedAt);
    public static RevisionDto ToDto(this Revision r) => new(r.Id, r.VehicleId, r.Type, r.ScheduledDate, r.Status.ToString(), r.EstimatedCost.Amount, r.EstimatedDurationMinutes);
    public static RevisionDetailDto ToDetailDto(this Revision r) => new(
        r.Id, r.VehicleId, r.Type, r.ScheduledDate, r.CompletedDate,
        r.Status.ToString(), r.EstimatedCost.Amount, r.ActualCost?.Amount, r.Notes,
        r.Tasks.Select(t => new RevisionTaskDto(t.Id, t.Description, t.EstimatedMinutes, t.ActualMinutes, t.IsCompleted)).ToList(),
        r.Parts.Select(p => new RevisionPartDto(p.Id, p.PartName, p.Quantity, p.UnitPrice.Amount, p.Total)).ToList()
    );
}

// Commandes & Queries
public record GetRevisionDetailQuery(Guid Id) : IRequest<Result<RevisionDetailDto>>;
public record UpdateRevisionStatusCommand(Guid Id, string Status) : IRequest<Result<bool>>;
public record GetWorkshopScheduleQuery(DateTime Start, DateTime End) : IRequest<Result<List<WorkshopScheduleDto>>>;
public record CreateVehicleCommand(Guid CustomerId, string LicensePlate, string Make, string Model, int Year, int Mileage) : IRequest<Result<VehicleDto>>;
public record GetVehiclesByCustomerQuery(Guid CustomerId) : IRequest<Result<List<VehicleDto>>>;
public record GetVehiclesPagedQuery(int Page, int PageSize, string? Search, string? Status) : IRequest<Result<PagedResult<VehicleDto>>>;
public record GetVehicleByIdQuery(Guid Id) : IRequest<Result<VehicleDetailDto>>;
public record GetVehicleByQrQuery(string Token) : IRequest<Result<VehicleDetailDto>>;
public record GenerateQrCodeCommand(Guid VehicleId) : IRequest<Result<QrCodeDto>>;
public record AddDiagnosticCommand(Guid VehicleId, string FaultCode, string Description, string Severity, string? Tool = null, string? Causes = null) : IRequest<Result<DiagnosticDto>>;
public record GetRevisionsQuery(int Page, int PageSize, string? Search) : IRequest<Result<PagedResult<RevisionDto>>>;
public record CreateRevisionCommand(Guid VehicleId, string Type, DateTime ScheduledDate, int EstimatedDurationMinutes, decimal EstimatedCost, int Mileage) : IRequest<Result<RevisionDto>>;

// Handlers
public class OperationsHandlers(
    IVehicleRepository vehicles, 
    IRevisionRepository revisions, 
    ICustomerRepository customers,
    IPartRepository parts,
    IUnitOfWork uow, 
    ICurrentUserService currentU) : 
    IRequestHandler<CreateVehicleCommand, Result<VehicleDto>>,
    IRequestHandler<GetVehiclesByCustomerQuery, Result<List<VehicleDto>>>,
    IRequestHandler<GetRevisionDetailQuery, Result<RevisionDetailDto>>,
    IRequestHandler<UpdateRevisionStatusCommand, Result<bool>>,
    IRequestHandler<GetWorkshopScheduleQuery, Result<List<WorkshopScheduleDto>>>,
    IRequestHandler<GetVehiclesPagedQuery, Result<PagedResult<VehicleDto>>>,
    IRequestHandler<GetVehicleByIdQuery, Result<VehicleDetailDto>>,
    IRequestHandler<GetVehicleByQrQuery, Result<VehicleDetailDto>>,
    IRequestHandler<GenerateQrCodeCommand, Result<QrCodeDto>>,
    IRequestHandler<AddDiagnosticCommand, Result<DiagnosticDto>>,
    IRequestHandler<GetRevisionsQuery, Result<PagedResult<RevisionDto>>>,
    IRequestHandler<CreateRevisionCommand, Result<RevisionDto>>
{
    public async Task<Result<VehicleDto>> Handle(CreateVehicleCommand cmd, CancellationToken ct)
    {
        var vehicle = Vehicle.Create(cmd.CustomerId, LicensePlate.Create(cmd.LicensePlate), cmd.Make, cmd.Model, cmd.Year, cmd.Mileage);
        await vehicles.AddAsync(vehicle, ct);
        await uow.SaveChangesAsync(ct);
        return Result<VehicleDto>.Success(vehicle.ToDto());
    }

    public async Task<Result<List<VehicleDto>>> Handle(GetVehiclesByCustomerQuery query, CancellationToken ct)
    {
        var list = await vehicles.GetByCustomerIdAsync(query.CustomerId, ct);
        return Result<List<VehicleDto>>.Success(list.Select(v => v.ToDto()).ToList());
    }

    public async Task<Result<RevisionDetailDto>> Handle(GetRevisionDetailQuery query, CancellationToken ct)
    {
        var rev = await revisions.GetWithDetailsAsync(query.Id, ct);
        return rev == null ? Result<RevisionDetailDto>.Failure("Intervention introuvable.") : Result<RevisionDetailDto>.Success(rev.ToDetailDto());
    }

    public async Task<Result<bool>> Handle(UpdateRevisionStatusCommand cmd, CancellationToken ct)
    {
        var rev = await revisions.GetWithDetailsAsync(cmd.Id, ct);
        if (rev == null) return Result<bool>.Failure("Intervention introuvable.");

        if (Enum.TryParse<RevisionStatus>(cmd.Status, out var status))
        {
            if (status == RevisionStatus.Completed && rev.Status != RevisionStatus.Completed)
            {
                foreach (var p in rev.Parts)
                {
                    var part = await parts.GetByIdAsync(p.PartId, ct);
                    if (part != null)
                    {
                        try {
                            part.AdjustStock(-p.Quantity);
                        } catch (Exception ex) {
                            return Result<bool>.Failure($"Erreur de stock pour {part.Name} : {ex.Message}");
                        }
                    }
                }
            }

            rev.SetStatus(status);
            await uow.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }
        return Result<bool>.Failure("Statut invalide.");
    }

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

    public async Task<Result<PagedResult<VehicleDto>>> Handle(GetVehiclesPagedQuery query, CancellationToken ct)
    {
        var all = await vehicles.GetAllAsync(ct);
        var filtered = all.Where(v => (string.IsNullOrEmpty(query.Search) || v.LicensePlate.Value.Contains(query.Search, StringComparison.OrdinalIgnoreCase) || v.Make.Contains(query.Search, StringComparison.OrdinalIgnoreCase))
                                   && (string.IsNullOrEmpty(query.Status) || v.Status.ToString() == query.Status));
        var total = filtered.Count();
        var items = filtered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).Select(v => v.ToDto());
        return Result<PagedResult<VehicleDto>>.Success(new PagedResult<VehicleDto>(items, total, query.Page, query.PageSize));
    }

    public async Task<Result<VehicleDetailDto>> Handle(GetVehicleByIdQuery query, CancellationToken ct)
    {
        var v = await vehicles.GetByIdAsync(query.Id, ct);
        if (v == null) return Result<VehicleDetailDto>.Failure("Véhicule introuvable.");
        var c = await customers.GetByIdAsync(v.CustomerId, ct);
        return Result<VehicleDetailDto>.Success(v.ToDetailDto(c != null ? $"{c.Name.FirstName} {c.Name.LastName}" : null));
    }

    public async Task<Result<VehicleDetailDto>> Handle(GetVehicleByQrQuery query, CancellationToken ct)
    {
        var v = await vehicles.GetByQrTokenAsync(query.Token, ct);
        if (v == null) return Result<VehicleDetailDto>.Failure("QR Code invalide.");
        var c = await customers.GetByIdAsync(v.CustomerId, ct);
        return Result<VehicleDetailDto>.Success(v.ToDetailDto(c != null ? $"{c.Name.FirstName} {c.Name.LastName}" : null));
    }

    public async Task<Result<QrCodeDto>> Handle(GenerateQrCodeCommand cmd, CancellationToken ct)
    {
        var v = await vehicles.GetByIdAsync(cmd.VehicleId, ct);
        if (v == null) return Result<QrCodeDto>.Failure("Véhicule introuvable.");
        v.RegenerateQrToken();
        await uow.SaveChangesAsync(ct);
        return Result<QrCodeDto>.Success(new QrCodeDto(v.QrCodeToken, $"https://mecapro.app/v/{v.QrCodeToken}", "", v.LicensePlate.Value));
    }

    public async Task<Result<DiagnosticDto>> Handle(AddDiagnosticCommand cmd, CancellationToken ct)
    {
        var v = await vehicles.GetByIdAsync(cmd.VehicleId, ct);
        if (v == null) return Result<DiagnosticDto>.Failure("Véhicule introuvable.");
        Enum.TryParse<DiagnosticSeverity>(cmd.Severity, out var sev);
        var d = Diagnostic.Create(v.Id, Guid.Parse(currentU.UserId ?? Guid.Empty.ToString()), cmd.FaultCode, cmd.Description, sev, cmd.Tool, cmd.Causes);
        v.AddDiagnostic(d);
        await uow.SaveChangesAsync(ct);
        return Result<DiagnosticDto>.Success(d.ToDto());
    }

    public async Task<Result<PagedResult<RevisionDto>>> Handle(GetRevisionsQuery query, CancellationToken ct)
    {
        var (items, total) = await revisions.GetPagedAsync(query.Page, query.PageSize, query.Search, ct);
        return Result<PagedResult<RevisionDto>>.Success(new PagedResult<RevisionDto>(items.Select(r => r.ToDto()), total, query.Page, query.PageSize));
    }

    public async Task<Result<RevisionDto>> Handle(CreateRevisionCommand cmd, CancellationToken ct)
    {
        var v = await vehicles.GetByIdAsync(cmd.VehicleId, ct);
        if (v == null) return Result<RevisionDto>.Failure("Véhicule introuvable.");
        var r = Revision.Create(v.Id, cmd.Type, cmd.ScheduledDate, cmd.EstimatedDurationMinutes, Money.Create(cmd.EstimatedCost), cmd.Mileage);
        await revisions.AddAsync(r, ct);
        await uow.SaveChangesAsync(ct);
        return Result<RevisionDto>.Success(r.ToDto());
    }
}
