using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Application.Common;
using MecaPro.Application.Modules.Operations;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Customers;
using MecaPro.Domain.Modules.Operations;
using MediatR;

namespace MecaPro.Application.Modules.Customers;

// DTOs
public record CustomerDto(Guid Id, string FirstName, string LastName, string Email, string? Phone, string Segment, int LoyaltyPoints, DateTime CreatedAt, bool IsBusiness = false, string? CompanyName = null);
public record CustomerDetailDto(
    Guid Id, string FirstName, string LastName, string Email, string? Phone, string? Street, string? City, string? PostalCode,
    string Segment, int LoyaltyPoints, string? Notes, string? Tags, string PreferredContact, DateTime CreatedAt,
    List<VehicleDto> Vehicles, List<LoyaltyTransactionDto> LoyaltyHistory, List<RevisionDto> Revisions,
    bool IsBusiness = false, string? CompanyName = null, string? TaxId = null
);
public record LoyaltyTransactionDto(int Points, string Reason, DateTime Date);

// Mapping Extensions
public static class CustomersMappingExtensions
{
    public static CustomerDto ToDto(this Customer c) => new(c.Id, c.Name.FirstName, c.Name.LastName, c.Email.Value, c.Phone?.Value, c.Segment.ToString(), c.Loyalty.Points, c.CreatedAt, c.IsBusiness, c.CompanyName);
}

// Commands & Queries
public record CreateCustomerCommand(string FirstName, string LastName, string Email, string? Phone, bool IsBusiness = false, string? CompanyName = null, string? TaxId = null) : IRequest<Result<CustomerDto>>;
public record UpdateCustomerCommand(Guid Id, string FirstName, string LastName, string Email, string? Phone, string? Street, string? City, string? PostalCode, string? Notes, string? Tags, string PreferredContact, string? CompanyName = null, string? TaxId = null) : IRequest<Result<CustomerDto>>;
public record GetCustomerByIdQuery(Guid Id) : IRequest<Result<CustomerDetailDto>>;
public record GetCustomersPagedQuery(int Page, int PageSize, string? Search) : IRequest<Result<PagedResult<CustomerDto>>>;
public record AddLoyaltyPointsCommand(Guid CustomerId, int Points, string Reason) : IRequest<Result<bool>>;

// Handlers
public class CustomersHandlers(
    ICustomerRepository customers, 
    IVehicleRepository vehicles,
    IRevisionRepository revisions, 
    IUnitOfWork uow) : 
    IRequestHandler<CreateCustomerCommand, Result<CustomerDto>>,
    IRequestHandler<UpdateCustomerCommand, Result<CustomerDto>>,
    IRequestHandler<GetCustomersPagedQuery, Result<PagedResult<CustomerDto>>>,
    IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailDto>>,
    IRequestHandler<AddLoyaltyPointsCommand, Result<bool>>
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

    public async Task<Result<PagedResult<CustomerDto>>> Handle(GetCustomersPagedQuery query, CancellationToken ct)
    {
        var (items, total) = await customers.GetPagedAsync(query.Page, query.PageSize, query.Search, ct);
        return Result<PagedResult<CustomerDto>>.Success(new PagedResult<CustomerDto>(items.Select(c => c.ToDto()), total, query.Page, query.PageSize));
    }

    public async Task<Result<CustomerDetailDto>> Handle(GetCustomerByIdQuery query, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(query.Id, ct);
        if (customer == null) return Result<CustomerDetailDto>.Failure("Client introuvable.");
        
        var vehiclesList = (await vehicles.GetByCustomerIdAsync(customer.Id, ct)).ToList();
        var revs = new List<Revision>();
        foreach (var v in vehiclesList)
        {
            var vRevs = await revisions.GetByVehicleIdAsync(v.Id, ct);
            revs.AddRange(vRevs);
        }
        
        // Manual mapping because ToDetailDto might expect customer.Vehicles
        return Result<CustomerDetailDto>.Success(new CustomerDetailDto(
            customer.Id, customer.Name.FirstName, customer.Name.LastName, customer.Email.Value, customer.Phone?.Value, 
            customer.Address?.Street, customer.Address?.City, customer.Address?.PostalCode,
            customer.Segment.ToString(), customer.Loyalty.Points, customer.Notes, customer.Tags, customer.PreferredContact.ToString(), customer.CreatedAt,
            vehiclesList.Select(v => v.ToDto()).ToList(),
            customer.Loyalty.Transactions.Select(t => new LoyaltyTransactionDto(t.Points, t.Reason, t.Date)).ToList(),
            revs.Select(r => r.ToDto()).ToList(),
            customer.IsBusiness, customer.CompanyName, customer.TaxId
        ));
    }

    public async Task<Result<bool>> Handle(AddLoyaltyPointsCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct);
        if (customer == null) return Result<bool>.Failure("Client introuvable.");
        customer.AddLoyaltyPoints(cmd.Points, cmd.Reason);
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }
}
