using AtlasBank.LedgerService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.LedgerService.Data;

public class LedgerDbContext(DbContextOptions<LedgerDbContext> options) : DbContext(options)
{
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LedgerEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Reference).HasMaxLength(64).IsRequired();
            e.Property(x => x.TransactionType).HasMaxLength(32).IsRequired();
            e.HasIndex(x => x.AccountId);
            e.HasIndex(x => x.TransactionId);
        });
    }
}
