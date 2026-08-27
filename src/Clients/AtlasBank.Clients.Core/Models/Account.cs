namespace AtlasBank.Clients.Core.Models;

public sealed record CreateAccountRequest
{
    public required AccountType Type { get; init; }
    public string? Currency { get; init; }
}

public sealed record Account
{
    public required Guid Id { get; init; }
    public required Guid CustomerId { get; init; }
    public required string AccountNumber { get; init; }
    public required AccountType Type { get; init; }
    public required AccountStatus Status { get; init; }
    public required decimal Balance { get; init; }
    public required string Currency { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
