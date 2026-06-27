using AtlasBank.AccountService.Domain.Enums;

namespace AtlasBank.AccountService.Domain.Entities;

public class Account
{
    public Guid Id { get; private set; }
    public string OwnerId { get; private set; } = default!;
    public string AccountNumber { get; private set; } = default!;
    public AccountType Type { get; private set; }
    public AccountStatus Status { get; private set; }
    public decimal Balance { get; private set; }
    public string Currency { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private Account() { }

    public static Account Create(string ownerId, AccountType type, string currency = "USD")
    {
        return new Account
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            AccountNumber = GenerateAccountNumber(),
            Type = type,
            Status = AccountStatus.Active,
            Balance = 0,
            Currency = currency,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Credit(decimal amount)
    {
        EnsureActive();
        if (amount <= 0) throw new InvalidOperationException("Credit amount must be positive.");
        Balance += amount;
    }

    public void Debit(decimal amount)
    {
        EnsureActive();
        if (amount <= 0) throw new InvalidOperationException("Debit amount must be positive.");
        if (amount > Balance) throw new InvalidOperationException("Insufficient funds.");
        Balance -= amount;
    }

    public void Freeze()
    {
        if (Status != AccountStatus.Active)
            throw new InvalidOperationException("Only active accounts can be frozen.");
        Status = AccountStatus.Frozen;
    }

    public void Close()
    {
        if (Balance != 0)
            throw new InvalidOperationException("Account must have zero balance before closing.");
        Status = AccountStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureActive()
    {
        if (Status != AccountStatus.Active)
            throw new InvalidOperationException($"Account is {Status} and cannot be transacted on.");
    }

    private static string GenerateAccountNumber()
    {
        return string.Concat("ATL", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()[^7..],
            Random.Shared.Next(100, 999).ToString());
    }
}
