using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MecaPro.Domain.Common;

namespace MecaPro.Domain.Modules.Inventory;

public class Part : AggregateRoot<Guid>
{
    public string Reference { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public string? Brand { get; private set; }
    public string? Description { get; private set; }
    public Money UnitPrice { get; private set; } = null!;
    public int StockQuantity { get; private set; }
    public int MinStockAlert { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsAvailable => !IsDeleted && StockQuantity > 0;
    public bool IsLowStock => StockQuantity <= MinStockAlert;
    public List<string> CompatibleVehicles { get; private set; } = new();

    public static Part Create(string refCode, string name, string cat, Money price, int stock, string? brand = null)
    {
        return new Part { Id = Guid.NewGuid(), Reference = refCode, Name = name, Category = cat, UnitPrice = price, StockQuantity = stock, MinStockAlert = 5, Brand = brand };
    }

    public void AdjustStock(int delta) 
    { 
        if (StockQuantity + delta < 0) throw new BusinessRuleViolationException("Insufficient stock.");
        StockQuantity += delta; 
        MarkUpdated(); 
    }
    public void UpdatePrice(Money newPrice) { UnitPrice = newPrice; MarkUpdated(); }
}

public interface IPartRepository : IRepository<Part, Guid>
{
    Task<IEnumerable<Part>> GetByCategoryAsync(string category, CancellationToken ct = default);
    Task<Part?> GetByReferenceAsync(string reference, CancellationToken ct = default);
}

public enum OrderStatus { Draft, Pending, Paid, Shipped, Cancelled }

public class Order : AggregateRoot<Guid>
{
    public Guid CustomerId { get; private set; }
    public Guid GarageId { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = null!;
    public decimal TaxRate { get; private set; } = 0.20m;
    public Money TaxAmount => Money.Create(TotalAmount.Amount * TaxRate);
    public string IdempotencyKey { get; private set; } = Guid.NewGuid().ToString();
    public string? StripePaymentIntentId { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(Guid customerId, Guid garageId, List<(Guid PartId, string Name, Money Price, int Qty)> items)
    {
        var order = new Order { Id = Guid.NewGuid(), CustomerId = customerId, GarageId = garageId, Status = OrderStatus.Pending };
        decimal total = 0;
        foreach (var itemData in items)
        {
            var item = new OrderItem(order.Id, itemData.PartId, itemData.Name, itemData.Price, itemData.Qty);
            order._items.Add(item);
            total += item.LineTotal;
        }
        order.TotalAmount = Money.Create(total);
        return order;
    }

    public void MarkPaid(string paymentIntentId) { StripePaymentIntentId = paymentIntentId; Status = OrderStatus.Paid; MarkUpdated(); }
}

public class OrderItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OrderId { get; private set; }
    public Guid PartId { get; private set; }
    public string PartName { get; private set; }
    public Money UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotal => UnitPrice.Amount * Quantity;

    public OrderItem(Guid orderId, Guid partId, string name, Money price, int qty)
    { OrderId = orderId; PartId = partId; PartName = name; UnitPrice = price; Quantity = qty; }

    private OrderItem() { PartName = null!; UnitPrice = null!; }
}

public interface IOrderRepository : IRepository<Order, Guid> { }
