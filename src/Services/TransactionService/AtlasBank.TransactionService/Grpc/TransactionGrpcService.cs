using AtlasBank.Grpc;
using AtlasBank.TransactionService.Data.Repositories;
using Grpc.Core;

namespace AtlasBank.TransactionService.Grpc;

public class TransactionGrpcServer(ITransactionRepository repo) : AtlasBank.Grpc.TransactionGrpcService.TransactionGrpcServiceBase
{
    public override async Task<TransactionListReply> GetTransactionsByAccount(
        GetTransactionsByAccountRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.AccountId, out var accountId))
            return new TransactionListReply();

        if (!DateTimeOffset.TryParse(request.PeriodStart, out var from) ||
            !DateTimeOffset.TryParse(request.PeriodEnd, out var to))
            return new TransactionListReply();

        var transactions = await repo.GetByAccountIdAsync(accountId, from, to, context.CancellationToken);

        var reply = new TransactionListReply();
        foreach (var t in transactions)
        {
            reply.Transactions.Add(new TransactionMessage
            {
                Id = t.Id.ToString(),
                AccountId = t.AccountId.ToString(),
                ToAccountId = t.ToAccountId?.ToString() ?? "",
                Type = t.Type.ToString(),
                Status = t.Status.ToString(),
                Amount = (double)t.Amount,
                Currency = t.Currency,
                Reference = t.Reference,
                Description = t.Description ?? "",
                CreatedAt = t.CreatedAt.ToString("O"),
                CompletedAt = t.CompletedAt?.ToString("O") ?? ""
            });
        }

        return reply;
    }
}
