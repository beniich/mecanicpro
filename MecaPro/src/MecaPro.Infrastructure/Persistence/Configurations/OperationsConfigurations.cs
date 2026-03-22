using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MecaPro.Domain.Modules.Operations;

namespace MecaPro.Infrastructure.Persistence.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(v => v.Id);
        builder.OwnsOne(v => v.LicensePlate);
        builder.OwnsOne(v => v.VIN);
    }
}

public class RevisionConfiguration : IEntityTypeConfiguration<Revision>
{
    public void Configure(EntityTypeBuilder<Revision> builder)
    {
        builder.HasKey(r => r.Id);
        builder.OwnsOne(r => r.EstimatedCost);
        builder.OwnsOne(r => r.ActualCost);
        builder.HasMany(r => r.Tasks).WithOne().HasForeignKey(t => t.RevisionId);
        builder.HasMany(r => r.Parts).WithOne().HasForeignKey(p => p.RevisionId);
    }
}

public class RevisionPartConfiguration : IEntityTypeConfiguration<RevisionPart>
{
    public void Configure(EntityTypeBuilder<RevisionPart> builder)
    {
        builder.HasKey(rp => rp.Id);
        builder.OwnsOne(rp => rp.UnitPrice);
    }
}
