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
            t.HasIndex(x => x.AccountId);
            t.HasIndex(x => x.Reference).IsUnique();
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
