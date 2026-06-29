using AtlasBank.AccountService.Domain.Enums;

namespace AtlasBank.AccountService.Features.Accounts;

public record CreateAccountRequest(AccountType Type, string Currency = "USD");

public record AccountResponse(
    Guid Id,
    Guid CustomerId,
    string AccountNumber,
    AccountType Type,
    AccountStatus Status,
    decimal Balance,
    string Currency,
    DateTimeOffset CreatedAt
);
