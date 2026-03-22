using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Operations;
using MecaPro.Infrastructure.Persistence;
using MecaPro.Application.Modules.Operations;
using MecaPro.Application.Modules.Customers;

namespace MecaPro.Infrastructure.Modules.CRM;

public record OrderDto(Guid Id, string Status, decimal Total, DateTime Date);
public record Customer360Dto(MecaPro.Application.Modules.Customers.CustomerDto Customer, IEnumerable<VehicleDto> Vehicles, IEnumerable<RevisionDto> RecentRevisions, IEnumerable<DiagnosticDto> ActiveDiagnostics, IEnumerable<OrderDto> RecentOrders, decimal LifetimeValue, string LoyaltyLevel);

public interface ICrmService { Task<Customer360Dto> GetCustomer360Async(Guid customerId); }

public class CrmService(ICustomerRepository customers, IVehicleRepository vehicles, AppDbContext db, IUnitOfWork uow) : ICrmService
{
    public async Task<Customer360Dto> GetCustomer360Async(Guid customerId)
    {
        var customer = await customers.GetByIdAsync(customerId) ?? throw new Domain.Common.NotFoundException("Customer", customerId);
        var vehiclesList = await vehicles.GetByCustomerIdAsync(customerId);
        
        return new Customer360Dto(
            customer.ToDto(),
            vehiclesList.Select(v => v.ToDto()),
            new List<RevisionDto>(),
            new List<DiagnosticDto>(),
            new List<OrderDto>(),
            0,
            customer.Loyalty.Level.ToString());
    }
}
