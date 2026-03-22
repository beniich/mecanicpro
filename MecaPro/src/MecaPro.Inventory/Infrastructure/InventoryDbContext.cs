using Microsoft.EntityFrameworkCore;
using MecaPro.Domain.Modules.Inventory;

namespace MecaPro.Inventory.Infrastructure;

public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Part> Parts => Set<Part>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<Part>(b => {
            b.ToTable("Parts");
            b.HasKey(x => x.Id);
            b.OwnsOne(x => x.UnitPrice);
        });

        builder.Entity<Order>(b => {
            b.ToTable("Orders");
            b.HasKey(x => x.Id);
            b.OwnsOne(x => x.TotalAmount);
            b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.OrderId);
        });

        builder.Entity<OrderItem>(b => {
            b.ToTable("OrderItems");
            b.HasKey(x => x.Id);
            b.OwnsOne(x => x.UnitPrice);
        });
    }
}
