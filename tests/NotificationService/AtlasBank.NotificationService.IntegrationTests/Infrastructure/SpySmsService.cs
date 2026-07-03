using AtlasBank.NotificationService.Infrastructure;

namespace AtlasBank.NotificationService.IntegrationTests.Infrastructure;

public class SpySmsService : ISmsService
{
    private readonly List<(string To, string Message)> _sent = [];

    public IReadOnlyList<(string To, string Message)> Sent => _sent;

    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        _sent.Add((phoneNumber, message));
        return Task.CompletedTask;
    }
}
