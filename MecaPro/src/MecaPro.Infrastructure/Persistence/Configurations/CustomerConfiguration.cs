using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MecaPro.Domain.Modules.Customers;

namespace MecaPro.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);
        builder.OwnsOne(c => c.Name);
        builder.OwnsOne(c => c.Email);
        builder.OwnsOne(c => c.Phone);
        builder.OwnsOne(c => c.Address);
        builder.OwnsOne(c => c.Loyalty, l => 
        {
            l.ToJson();
            l.OwnsMany(la => la.Transactions);
        });
    }
}
