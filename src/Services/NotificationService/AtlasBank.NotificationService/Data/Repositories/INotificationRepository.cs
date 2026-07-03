using AtlasBank.NotificationService.Domain.Entities;

namespace AtlasBank.NotificationService.Data.Repositories;

public interface INotificationRepository
{
    Task AddAsync(Notification notification, CancellationToken ct = default);
    Task<Notification?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Notification>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
