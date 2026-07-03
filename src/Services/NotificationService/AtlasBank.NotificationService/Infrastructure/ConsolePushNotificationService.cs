namespace AtlasBank.NotificationService.Infrastructure;

public class ConsolePushNotificationService(ILogger<ConsolePushNotificationService> logger) : IPushNotificationService
{
    public Task SendAsync(string deviceToken, string title, string body, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[PUSH] Token: {DeviceToken} | Title: {Title}\n{Body}",
            deviceToken, title, body);
        return Task.CompletedTask;
    }
}
