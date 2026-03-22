using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Operations;
using MecaPro.Domain.Modules.Inventory;
using MecaPro.Domain.Modules.Invoicing;

namespace MecaPro.Infrastructure.Persistence.Repositories;

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
