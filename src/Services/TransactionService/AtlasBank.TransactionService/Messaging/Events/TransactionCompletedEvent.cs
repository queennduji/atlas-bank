namespace AtlasBank.TransactionService.Messaging.Events;

public record TransactionCompletedEvent(
    Guid TransactionId,
    Guid AccountId,
    Guid? ToAccountId,
    string TransactionType,
    decimal Amount,
    string Currency,
    string Reference,
    DateTimeOffset CompletedAt);
