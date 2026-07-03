using AtlasBank.NotificationService.Infrastructure;

namespace AtlasBank.NotificationService.IntegrationTests.Infrastructure;

public class SpyEmailService : IEmailService
{
    private readonly List<(string To, string Subject, string Body)> _sent = [];

    public IReadOnlyList<(string To, string Subject, string Body)> Sent => _sent;

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        _sent.Add((to, subject, body));
        return Task.CompletedTask;
    }
}
