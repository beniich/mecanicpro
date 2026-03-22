using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MecaPro.Auth.Domain;

namespace MecaPro.Auth.Infrastructure;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<AppUser>(b =>
        {
            b.HasIndex(u => u.GarageId);
            b.HasMany(u => u.RefreshTokens)
             .WithOne(rt => rt.User)
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        
        builder.Entity<RefreshToken>(b =>
        {
            b.HasIndex(rt => rt.TokenHash).IsUnique();
            b.HasIndex(rt => rt.Family);
        });
    }
}
