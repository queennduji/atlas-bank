namespace AtlasBank.Clients.Core.Models;

public sealed record IssueCardRequest
{
    public required Guid AccountId { get; init; }
    public required CardType Type { get; init; }
    public required decimal SpendingLimit { get; init; }
}

public sealed record UpdateSpendingLimitRequest
{
    public required decimal SpendingLimit { get; init; }
}

public sealed record Card
{
    public required Guid Id { get; init; }
    public required Guid AccountId { get; init; }
    public required Guid CustomerId { get; init; }
    public required string MaskedCardNumber { get; init; }
    public required string CardHolderName { get; init; }
    public required CardType Type { get; init; }
    public required CardStatus Status { get; init; }
    public required decimal SpendingLimit { get; init; }
    public required DateTimeOffset ExpiryDate { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
