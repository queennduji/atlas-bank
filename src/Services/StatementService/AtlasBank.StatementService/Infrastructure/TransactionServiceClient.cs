using AtlasBank.Grpc;

namespace AtlasBank.StatementService.Infrastructure;

public class TransactionServiceClient(TransactionGrpcService.TransactionGrpcServiceClient grpcClient) : ITransactionServiceClient
{
    public async Task<IReadOnlyList<TransactionDto>> GetByAccountAsync(
        Guid accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var reply = await grpcClient.GetTransactionsByAccountAsync(
            new GetTransactionsByAccountRequest
            {
                AccountId = accountId.ToString(),
                PeriodStart = from.ToString("O"),
                PeriodEnd = to.ToString("O")
            }, cancellationToken: ct);

        return reply.Transactions.Select(t => new TransactionDto(
            Guid.Parse(t.Id),
            Guid.Parse(t.AccountId),
            string.IsNullOrEmpty(t.ToAccountId) ? null : Guid.Parse(t.ToAccountId),
            t.Type, t.Status, (decimal)t.Amount, t.Currency,
            t.Reference, string.IsNullOrEmpty(t.Description) ? null : t.Description,
            DateTimeOffset.Parse(t.CreatedAt),
            string.IsNullOrEmpty(t.CompletedAt) ? null : DateTimeOffset.Parse(t.CompletedAt)
        )).ToList();
    }
}
