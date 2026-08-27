using AtlasBank.Clients.Core.Models;

namespace AtlasBank.Clients.Core.Api;

public sealed partial class AtlasApiClient
{
    public Task<IReadOnlyList<Transaction>> GetAccountTransactionsAsync(Guid accountId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Transaction>>($"/api/transactions/account/{accountId}", ct);

    public Task<Transaction> GetTransactionAsync(Guid transactionId, CancellationToken ct = default) =>
        GetAsync<Transaction>($"/api/transactions/{transactionId}", ct);

    public Task<Transaction> DepositAsync(DepositRequest request, string? idempotencyKey = null, CancellationToken ct = default) =>
        PostAsync<Transaction>("/api/transactions/deposit", request, ct, idempotencyKey ?? Guid.NewGuid().ToString());

    public Task<Transaction> WithdrawAsync(WithdrawRequest request, string? idempotencyKey = null, CancellationToken ct = default) =>
        PostAsync<Transaction>("/api/transactions/withdraw", request, ct, idempotencyKey ?? Guid.NewGuid().ToString());

    public Task<Transaction> TransferAsync(TransferRequest request, string? idempotencyKey = null, CancellationToken ct = default) =>
        PostAsync<Transaction>("/api/transactions/transfer", request, ct, idempotencyKey ?? Guid.NewGuid().ToString());
}
