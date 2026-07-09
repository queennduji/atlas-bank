namespace AtlasBank.Shared.Messaging.Events;

public record CardIssuedEvent(
    Guid CardId,
    Guid AccountId,
    Guid CustomerId,
    string MaskedCardNumber,
    string CardHolderName,
    string CardType,
    DateOnly ExpiryDate,
    DateTimeOffset IssuedAt);
