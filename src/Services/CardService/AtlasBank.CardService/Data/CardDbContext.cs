using AtlasBank.CardService.Domain.Entities;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.CardService.Data;

public class CardDbContext(DbContextOptions<CardDbContext> options) : DbContext(options)
{
    public DbSet<Card> Cards => Set<Card>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.CardNumber).HasMaxLength(16).IsRequired();
            e.Property(c => c.MaskedCardNumber).HasMaxLength(24).IsRequired();
            e.Property(c => c.CardHolderName).HasMaxLength(128).IsRequired();
            e.Property(c => c.SpendingLimit).HasPrecision(18, 4);
            e.HasIndex(c => c.AccountId);
            e.HasIndex(c => c.CustomerId);
            e.HasIndex(c => c.CardNumber).IsUnique();
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
