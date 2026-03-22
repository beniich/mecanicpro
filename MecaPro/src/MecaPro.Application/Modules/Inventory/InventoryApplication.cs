using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Application.Common;
using MecaPro.Domain.Common;
using MecaPro.Domain.Modules.Inventory;
using MediatR;

namespace MecaPro.Application.Modules.Inventory;

// DTOs
public record PartDto(Guid Id, string Reference, string Name, string Category, string? Brand, decimal UnitPrice, int StockQuantity, bool IsLowStock);

// Mapping Extensions
public static class InventoryMappingExtensions
{
    public static PartDto ToDto(this Part p) => new(p.Id, p.Reference, p.Name, p.Category, p.Brand, p.UnitPrice.Amount, p.StockQuantity, p.IsLowStock);
}

// Commands & Queries
public record GetPartsPagedQuery(int Page, int PageSize, string? Search = null, string? Category = null) : IRequest<Result<PagedResult<PartDto>>>;
public record GetPartByReferenceQuery(string Reference) : IRequest<Result<PartDto>>;
public record AdjustStockCommand(Guid Id, int Delta) : IRequest<Result<bool>>;
public record GetPartCategoriesQuery() : IRequest<Result<List<string>>>;

// Handlers
public class InventoryHandlers(IPartRepository parts, IUnitOfWork uow) : 
    IRequestHandler<GetPartsPagedQuery, Result<PagedResult<PartDto>>>,
    IRequestHandler<AdjustStockCommand, Result<bool>>,
    IRequestHandler<GetPartCategoriesQuery, Result<List<string>>>
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

    public async Task<Result<bool>> Handle(AdjustStockCommand cmd, CancellationToken ct)
    {
        var part = await parts.GetByIdAsync(cmd.Id, ct);
        if (part == null) return Result<bool>.Failure("Pièce introuvable.");
        part.AdjustStock(cmd.Delta);
        await uow.SaveChangesAsync(ct);
        return Result<bool>.Success(true);
    }

    public async Task<Result<List<string>>> Handle(GetPartCategoriesQuery query, CancellationToken ct)
    {
        var all = await parts.GetAllAsync(ct);
        return Result<List<string>>.Success(all.Select(p => p.Category).Distinct().ToList());
    }
}
