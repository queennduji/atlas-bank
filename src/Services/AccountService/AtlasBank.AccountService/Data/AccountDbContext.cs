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
            // Optimistic concurrency on every write, keyed off Postgres's built-in xmin
            // system column (mapped as a shadow property – no new column or backfill
            // needed). Without this, two concurrent Credit/Debit calls on the same
            // account (e.g. two deposits landing at once) each load the same Balance,
            // compute independently, and the second SaveChanges silently overwrites the
            // first – a real lost-update bug, not a theoretical one, for money.
            e.Property<uint>("xmin").HasColumnName("xmin").IsRowVersion();
        });
    }
}
