using AtlasBank.TransactionService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.TransactionService.Data;

public class TransactionDbContext(DbContextOptions<TransactionDbContext> options) : DbContext(options)
{
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(t =>
        {
            t.HasKey(x => x.Id);
            t.Property(x => x.Amount).HasPrecision(18, 4);
            t.Property(x => x.Currency).HasMaxLength(3);
            t.Property(x => x.Reference).HasMaxLength(50);
            t.Property(x => x.Description).HasMaxLength(500);
            t.Property(x => x.FailureReason).HasMaxLength(500);
            t.Property(x => x.IdempotencyKey).HasMaxLength(200);
            t.HasIndex(x => x.AccountId);
            t.HasIndex(x => x.Reference).IsUnique();
            // Partial index: only enforced when a key was actually supplied, so the many
            // existing/older rows with no key (and any caller that opts out) don't collide
            // with each other on a shared NULL.
            t.HasIndex(x => x.IdempotencyKey).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL");
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
