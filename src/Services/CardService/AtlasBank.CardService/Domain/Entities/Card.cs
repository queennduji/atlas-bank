using AtlasBank.CardService.Domain.Enums;

namespace AtlasBank.CardService.Domain.Entities;

public class Card
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CardNumber { get; private set; } = default!;
    public string MaskedCardNumber { get; private set; } = default!;
    public string CardHolderName { get; private set; } = default!;
    public CardType Type { get; private set; }
    public CardStatus Status { get; private set; }
    public decimal SpendingLimit { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Card() { }

    public static Card Issue(Guid accountId, Guid customerId, string cardHolderName, CardType type, decimal spendingLimit)
    {
        var number = GenerateCardNumber();
        return new Card
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            CustomerId = customerId,
            CardNumber = number,
            MaskedCardNumber = MaskNumber(number),
            CardHolderName = cardHolderName.ToUpperInvariant(),
            Type = type,
            Status = CardStatus.Active,
            SpendingLimit = spendingLimit,
            ExpiryDate = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(4)),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Freeze()
    {
        if (Status != CardStatus.Active)
            throw new InvalidOperationException("Only active cards can be frozen.");
        Status = CardStatus.Frozen;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Unfreeze()
    {
        if (Status != CardStatus.Frozen)
            throw new InvalidOperationException("Only frozen cards can be unfrozen.");
        Status = CardStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateSpendingLimit(decimal newLimit)
    {
        if (Status == CardStatus.Cancelled || Status == CardStatus.Expired)
            throw new InvalidOperationException("Cannot update spending limit on a cancelled or expired card.");
        SpendingLimit = newLimit;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel()
    {
        if (Status == CardStatus.Cancelled)
            throw new InvalidOperationException("Card is already cancelled.");
        Status = CardStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string GenerateCardNumber()
    {
        var rng = Random.Shared;
        return $"{rng.Next(1000, 9999)}{rng.Next(1000, 9999)}{rng.Next(1000, 9999)}{rng.Next(1000, 9999)}";
    }

    private static string MaskNumber(string number) =>
        $"**** **** **** {number[^4..]}";
}
