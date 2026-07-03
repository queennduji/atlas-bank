using AtlasBank.NotificationService.Domain.Enums;

namespace AtlasBank.NotificationService.Domain.Entities;

public class Notification
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Recipient { get; private set; } = default!;
    public NotificationChannel Channel { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string Subject { get; private set; } = default!;
    public string Body { get; private set; } = default!;
    public Guid? RelatedTransactionId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }

    private Notification() { }

    public static Notification CreateEmail(Guid customerId, string email, string subject, string body, Guid transactionId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Recipient = email,
            Channel = NotificationChannel.Email,
            Status = NotificationStatus.Pending,
            Subject = subject,
            Body = body,
            RelatedTransactionId = transactionId,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static Notification CreateSms(Guid customerId, string phoneNumber, string subject, string body, Guid transactionId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Recipient = phoneNumber,
            Channel = NotificationChannel.Sms,
            Status = NotificationStatus.Pending,
            Subject = subject,
            Body = body,
            RelatedTransactionId = transactionId,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static Notification CreatePush(Guid customerId, string deviceToken, string subject, string body, Guid transactionId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Recipient = deviceToken,
            Channel = NotificationChannel.Push,
            Status = NotificationStatus.Pending,
            Subject = subject,
            Body = body,
            RelatedTransactionId = transactionId,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public void MarkSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = reason;
        SentAt = DateTimeOffset.UtcNow;
    }
}
