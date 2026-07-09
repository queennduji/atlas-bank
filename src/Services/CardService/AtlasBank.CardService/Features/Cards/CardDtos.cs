using AtlasBank.CardService.Domain.Enums;

namespace AtlasBank.CardService.Features.Cards;

public record IssueCardRequest(Guid AccountId, CardType Type, decimal SpendingLimit);

public record UpdateSpendingLimitRequest(decimal SpendingLimit);

public record CardResponse(
    Guid Id,
    Guid AccountId,
    Guid CustomerId,
    string MaskedCardNumber,
    string CardHolderName,
    CardType Type,
    CardStatus Status,
    decimal SpendingLimit,
    DateOnly ExpiryDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
