using AtlasBank.NotificationService.Data.Repositories;
using AtlasBank.NotificationService.Domain.Entities;
using AtlasBank.NotificationService.Infrastructure;
using AtlasBank.Shared.Messaging.Events;
using MassTransit;

namespace AtlasBank.NotificationService.Messaging.Consumers;

public class TransactionCompletedConsumer(
    IAccountServiceClient accountClient,
    ICustomerServiceClient customerClient,
    IEmailService emailService,
    ISmsService smsService,
    IPushNotificationService pushService,
    INotificationRepository repo,
    ILogger<TransactionCompletedConsumer> logger) : IConsumer<TransactionCompletedEvent>
{
    public async Task Consume(ConsumeContext<TransactionCompletedEvent> context)
    {
        var evt = context.Message;
        var ct = context.CancellationToken;

        var account = await accountClient.GetByIdAsync(evt.AccountId, ct);
        if (account is null)
        {
            logger.LogWarning("Account {AccountId} not found for transaction {TransactionId}", evt.AccountId, evt.TransactionId);
            return;
        }

        var customer = await customerClient.GetByIdAsync(account.CustomerId, ct);
        if (customer is null)
        {
            logger.LogWarning("Customer {CustomerId} not found for account {AccountId}", account.CustomerId, evt.AccountId);
            return;
        }

        var (subject, emailBody) = BuildEmailMessage(evt, customer.FirstName);
        var smsText = BuildSmsMessage(evt, customer.FirstName);

        var emailNotification = Notification.CreateEmail(
            customer.Id, customer.Email, subject, emailBody, evt.TransactionId);

        var smsNotification = Notification.CreateSms(
            customer.Id, customer.PhoneNumber, subject, smsText, evt.TransactionId);

        await repo.AddAsync(emailNotification, ct);
        await repo.AddAsync(smsNotification, ct);

        if (customer.DeviceToken is not null)
        {
            var pushNotification = Notification.CreatePush(
                customer.Id, customer.DeviceToken, subject, smsText, evt.TransactionId);
            await repo.AddAsync(pushNotification, ct);
            await repo.SaveChangesAsync(ct);
            await SendPushAsync(pushNotification, customer.DeviceToken, subject, smsText, ct);
        }
        else
        {
            await repo.SaveChangesAsync(ct);
        }

        await SendEmailAsync(emailNotification, customer.Email, subject, emailBody, ct);
        await SendSmsAsync(smsNotification, customer.PhoneNumber, smsText, ct);

        await repo.SaveChangesAsync(ct);
    }

    private async Task SendEmailAsync(Notification notification, string email, string subject, string body, CancellationToken ct)
    {
        try
        {
            await emailService.SendAsync(email, subject, body, ct);
            notification.MarkSent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {Email}", email);
            notification.MarkFailed(ex.Message);
        }
    }

    private async Task SendSmsAsync(Notification notification, string phoneNumber, string message, CancellationToken ct)
    {
        try
        {
            await smsService.SendAsync(phoneNumber, message, ct);
            notification.MarkSent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send SMS to {PhoneNumber}", phoneNumber);
            notification.MarkFailed(ex.Message);
        }
    }

    private async Task SendPushAsync(Notification notification, string deviceToken, string title, string body, CancellationToken ct)
    {
        try
        {
            await pushService.SendAsync(deviceToken, title, body, ct);
            notification.MarkSent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send push to device {DeviceToken}", deviceToken);
            notification.MarkFailed(ex.Message);
        }
    }

    private static (string Subject, string Body) BuildEmailMessage(TransactionCompletedEvent evt, string firstName) =>
        evt.TransactionType switch
        {
            "Deposit" => (
                $"Deposit of {evt.Amount:N2} {evt.Currency} received",
                $"Hi {firstName},\n\nA deposit of {evt.Amount:N2} {evt.Currency} has been credited to your account.\nReference: {evt.Reference}\nDate: {evt.CompletedAt:f}\n\nAtlas Bank"),
            "Withdrawal" => (
                $"Withdrawal of {evt.Amount:N2} {evt.Currency} processed",
                $"Hi {firstName},\n\nA withdrawal of {evt.Amount:N2} {evt.Currency} has been debited from your account.\nReference: {evt.Reference}\nDate: {evt.CompletedAt:f}\n\nAtlas Bank"),
            "Transfer" => (
                $"Transfer of {evt.Amount:N2} {evt.Currency} processed",
                $"Hi {firstName},\n\nA transfer of {evt.Amount:N2} {evt.Currency} has been processed from your account.\nReference: {evt.Reference}\nDate: {evt.CompletedAt:f}\n\nAtlas Bank"),
            _ => (
                "Transaction alert",
                $"Hi {firstName},\n\nA transaction of {evt.Amount:N2} {evt.Currency} was processed on your account.\nReference: {evt.Reference}\n\nAtlas Bank")
        };

    private static string BuildSmsMessage(TransactionCompletedEvent evt, string firstName) =>
        evt.TransactionType switch
        {
            "Deposit"    => $"Atlas Bank: Hi {firstName}, {evt.Amount:N2} {evt.Currency} deposited. Ref: {evt.Reference}",
            "Withdrawal" => $"Atlas Bank: Hi {firstName}, {evt.Amount:N2} {evt.Currency} withdrawn. Ref: {evt.Reference}",
            "Transfer"   => $"Atlas Bank: Hi {firstName}, {evt.Amount:N2} {evt.Currency} transferred. Ref: {evt.Reference}",
            _            => $"Atlas Bank: A transaction of {evt.Amount:N2} {evt.Currency} was processed. Ref: {evt.Reference}"
        };
}
