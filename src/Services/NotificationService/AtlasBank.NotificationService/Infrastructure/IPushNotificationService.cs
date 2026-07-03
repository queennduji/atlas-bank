namespace AtlasBank.NotificationService.Infrastructure;

public interface IPushNotificationService
{
    Task SendAsync(string deviceToken, string title, string body, CancellationToken ct = default);
}
