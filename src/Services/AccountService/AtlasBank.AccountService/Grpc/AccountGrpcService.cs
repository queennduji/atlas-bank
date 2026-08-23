using AtlasBank.AccountService.Data.Repositories;
using AtlasBank.AccountService.Domain.Entities;
using AtlasBank.Grpc;
using Grpc.Core;

namespace AtlasBank.AccountService.Grpc;

public class AccountGrpcService(IAccountRepository repo) : AtlasBank.Grpc.AccountGrpcService.AccountGrpcServiceBase
{
    public override async Task<AccountReply> GetAccount(GetAccountRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var id))
            return new AccountReply { Found = false };

        var account = await repo.GetByIdAsync(id, context.CancellationToken);
        if (account is null)
            return new AccountReply { Found = false };

        return new AccountReply
        {
            Id = account.Id.ToString(),
            CustomerId = account.CustomerId.ToString(),
            AccountNumber = account.AccountNumber,
            Status = (int)account.Status,
            Balance = (double)account.Balance,
            Currency = account.Currency,
            Found = true
        };
    }

    public override async Task<BalanceChangeReply> Credit(BalanceChangeRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var id))
            return new BalanceChangeReply { Success = false, Error = "Invalid account ID." };

        var account = await repo.GetByIdAsync(id, context.CancellationToken);
        if (account is null)
            return new BalanceChangeReply { Success = false, Error = "Account not found." };

        var result = await AccountConcurrencyRetry.ApplyAsync(
            account, a => a.Credit((decimal)request.Amount), repo, context.CancellationToken);
        return result.Outcome == ConcurrencyOutcome.Success
            ? new BalanceChangeReply { Success = true, NewBalance = (double)account.Balance }
            : new BalanceChangeReply { Success = false, Error = result.Message };
    }

    public override async Task<BalanceChangeReply> Debit(BalanceChangeRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var id))
            return new BalanceChangeReply { Success = false, Error = "Invalid account ID." };

        var account = await repo.GetByIdAsync(id, context.CancellationToken);
        if (account is null)
            return new BalanceChangeReply { Success = false, Error = "Account not found." };

        var result = await AccountConcurrencyRetry.ApplyAsync(
            account, a => a.Debit((decimal)request.Amount), repo, context.CancellationToken);
        return result.Outcome == ConcurrencyOutcome.Success
            ? new BalanceChangeReply { Success = true, NewBalance = (double)account.Balance }
            : new BalanceChangeReply { Success = false, Error = result.Message };
    }
}
