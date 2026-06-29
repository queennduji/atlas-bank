using AtlasBank.AccountService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.AccountService.Data;

public class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.AccountNumber).HasMaxLength(20).IsRequired();
            e.HasIndex(a => a.AccountNumber).IsUnique();
            e.HasIndex(a => a.CustomerId);
            e.Property(a => a.Balance).HasColumnType("decimal(18,4)");
            e.Property(a => a.Currency).HasMaxLength(3).IsRequired();
            e.Property(a => a.Type).HasConversion<string>();
            e.Property(a => a.Status).HasConversion<string>();
        });
    }
}
