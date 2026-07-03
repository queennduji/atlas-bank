using AtlasBank.NotificationService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.NotificationService.Data.Repositories;

public class NotificationRepository(NotificationDbContext db) : INotificationRepository
{
    public async Task AddAsync(Notification notification, CancellationToken ct = default)
        => await db.Notifications.AddAsync(notification, ct);

    public async Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);

    public async Task<IReadOnlyList<Notification>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default)
        => await db.Notifications
            .Where(n => n.CustomerId == customerId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
