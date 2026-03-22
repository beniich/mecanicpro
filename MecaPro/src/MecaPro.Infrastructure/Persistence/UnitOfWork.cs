using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MecaPro.Domain.Common;

namespace MecaPro.Infrastructure.Persistence;

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
