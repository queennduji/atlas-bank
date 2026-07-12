using AtlasBank.StatementService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.StatementService.Data;

public class StatementDbContext(DbContextOptions<StatementDbContext> options) : DbContext(options)
{
    public DbSet<Statement> Statements => Set<Statement>();
    public DbSet<StatementLine> StatementLines => Set<StatementLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Statement>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.AccountNumber).HasMaxLength(20);
            e.Property(s => s.CustomerName).HasMaxLength(200);
            e.Property(s => s.Currency).HasMaxLength(3);
            e.Property(s => s.OpeningBalance).HasPrecision(18, 4);
            e.Property(s => s.ClosingBalance).HasPrecision(18, 4);
            e.Property(s => s.TotalCredits).HasPrecision(18, 4);
            e.Property(s => s.TotalDebits).HasPrecision(18, 4);
            e.HasMany(s => s.Lines).WithOne().HasForeignKey(l => l.StatementId);
            e.HasIndex(s => s.AccountId);
            e.HasIndex(s => s.CustomerId);
        });

        modelBuilder.Entity<StatementLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.Reference).HasMaxLength(50);
            e.Property(l => l.Description).HasMaxLength(500);
            e.Property(l => l.Type).HasMaxLength(20);
            e.Property(l => l.Amount).HasPrecision(18, 4);
            e.Property(l => l.RunningBalance).HasPrecision(18, 4);
        });
    }
}
