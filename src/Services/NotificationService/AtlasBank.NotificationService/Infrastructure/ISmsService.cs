namespace AtlasBank.NotificationService.Infrastructure;

public interface ISmsService
{
    Task SendAsync(string phoneNumber, string message, CancellationToken ct = default);
}
