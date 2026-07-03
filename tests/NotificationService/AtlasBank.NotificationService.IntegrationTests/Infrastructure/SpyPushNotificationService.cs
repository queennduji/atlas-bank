using AtlasBank.NotificationService.Infrastructure;

namespace AtlasBank.NotificationService.IntegrationTests.Infrastructure;

public class SpyPushNotificationService : IPushNotificationService
{
    private readonly List<(string Token, string Title, string Body)> _sent = [];

    public IReadOnlyList<(string Token, string Title, string Body)> Sent => _sent;

    public Task SendAsync(string deviceToken, string title, string body, CancellationToken ct = default)
    {
        _sent.Add((deviceToken, title, body));
        return Task.CompletedTask;
    }
}
