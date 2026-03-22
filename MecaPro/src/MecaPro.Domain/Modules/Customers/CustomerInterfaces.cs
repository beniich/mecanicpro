using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.Customers;

public interface ICustomerRepository : IRepository<Customer, Guid>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<(IEnumerable<Customer> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search, CancellationToken ct = default);
}

public interface ISubscriptionRepository : IRepository<Subscription, Guid> { }

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
