using System.Text.Json.Serialization;

namespace AtlasBank.Clients.Core.Models;

// Wire formats mirrored from frontend/src/api/types.ts. AccountService, TransactionService,
// and CustomerService don't register a JsonStringEnumConverter, so their enums come over the
// wire as plain integers — which is what a bare enum serializes as by default anyway, so
// nothing extra needed there. CardService and StatementService do register one, so those two
// enums get JsonStringEnumConverter applied explicitly below to match.

public enum AccountType
{
    Checking = 0,
    Savings = 1,
}

public enum AccountStatus
{
    Active = 0,
    Frozen = 1,
    Closed = 2,
}

public enum TransactionType
{
    Deposit = 0,
    Withdrawal = 1,
    Transfer = 2,
}

public enum TransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
}

public enum CustomerStatus
{
    Active = 0,
    Suspended = 1,
    Closed = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter<CardType>))]
public enum CardType
{
    Debit,
    Credit,
}

[JsonConverter(typeof(JsonStringEnumConverter<CardStatus>))]
public enum CardStatus
{
    Active,
    Frozen,
    Expired,
    Cancelled,
}
