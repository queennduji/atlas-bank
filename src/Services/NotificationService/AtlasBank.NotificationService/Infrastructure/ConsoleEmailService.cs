namespace AtlasBank.NotificationService.Infrastructure;

public class ConsoleEmailService(ILogger<ConsoleEmailService> logger) : IEmailService
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] To: {To} | Subject: {Subject}\n{Body}",
            to, subject, body);
        return Task.CompletedTask;
    }
}
