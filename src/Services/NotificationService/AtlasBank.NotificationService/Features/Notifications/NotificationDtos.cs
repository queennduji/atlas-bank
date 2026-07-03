using AtlasBank.NotificationService.Domain.Enums;

namespace AtlasBank.NotificationService.Features.Notifications;

public record NotificationResponse(
    Guid Id,
    Guid CustomerId,
    string Recipient,
    NotificationChannel Channel,
    NotificationStatus Status,
    string Subject,
    string Body,
    Guid? RelatedTransactionId,
    string? FailureReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset? SentAt);
