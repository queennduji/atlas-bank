using AtlasBank.Grpc;

namespace AtlasBank.TransactionService.Infrastructure;

public interface IAccountServiceClient
{
    Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default);
    Task<bool> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default);
    Task<bool> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default);
}

public record AccountDto(Guid Id, Guid CustomerId, string AccountNumber, int Status, decimal Balance, string Currency);

public class AccountServiceClient(AccountGrpcService.AccountGrpcServiceClient grpcClient) : IAccountServiceClient
{
    public async Task<AccountDto?> GetByIdAsync(Guid accountId, CancellationToken ct = default)
    {
        var reply = await grpcClient.GetAccountAsync(
            new GetAccountRequest { AccountId = accountId.ToString() },
            cancellationToken: ct);

        if (!reply.Found) return null;

        return new AccountDto(
            Guid.Parse(reply.Id),
            Guid.Parse(reply.CustomerId),
            reply.AccountNumber,
            reply.Status,
            (decimal)reply.Balance,
            reply.Currency);
    }

    public async Task<bool> CreditAsync(Guid accountId, decimal amount, CancellationToken ct = default)
    {
        var reply = await grpcClient.CreditAsync(
            new BalanceChangeRequest { AccountId = accountId.ToString(), Amount = (double)amount },
            cancellationToken: ct);

        return reply.Success;
    }

    public async Task<bool> DebitAsync(Guid accountId, decimal amount, CancellationToken ct = default)
    {
        var reply = await grpcClient.DebitAsync(
            new BalanceChangeRequest { AccountId = accountId.ToString(), Amount = (double)amount },
            cancellationToken: ct);

        return reply.Success;
    }
}
