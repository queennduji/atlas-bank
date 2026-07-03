using AtlasBank.NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.NotificationService.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.Property(n => n.Recipient).HasMaxLength(256).IsRequired();
            e.Property(n => n.Channel).IsRequired();
            e.Property(n => n.Subject).HasMaxLength(256).IsRequired();
            e.Property(n => n.Body).HasMaxLength(2000).IsRequired();
            e.Property(n => n.FailureReason).HasMaxLength(500);
            e.HasIndex(n => n.CustomerId);
            e.HasIndex(n => n.RelatedTransactionId);
        });
    }
}
