namespace AtlasBank.NotificationService.Infrastructure;

public class ConsoleSmsService(ILogger<ConsoleSmsService> logger) : ISmsService
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken ct = default)
    {
        logger.LogInformation("[SMS] To: {PhoneNumber}\n{Message}", phoneNumber, message);
        return Task.CompletedTask;
    }
}
