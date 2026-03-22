using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.Operations;

public interface IVehicleRepository : IRepository<Vehicle, Guid>
{
    Task<Vehicle?> GetByQrTokenAsync(string token, CancellationToken ct = default);
    Task<Vehicle?> GetByLicensePlateAsync(string plate, CancellationToken ct = default);
    Task<IEnumerable<Vehicle>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task<bool> UserHasAccessAsync(Guid userId, Guid vehicleId, CancellationToken ct = default);
}

public interface IRevisionRepository : IRepository<Revision, Guid>
{
    Task<Revision?> GetWithDetailsAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<Revision>> GetByVehicleIdAsync(Guid vehicleId, CancellationToken ct = default);
    Task<(IEnumerable<Revision> Items, int Total)> GetPagedByVehicleAsync(Guid vehicleId, int page, int pageSize, CancellationToken ct = default);
    Task<(IEnumerable<Revision> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
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
