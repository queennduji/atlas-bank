using AtlasBank.TransactionService.Domain.Entities;

namespace AtlasBank.TransactionService.Data.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task<List<Transaction>> GetByAccountIdAsync(Guid accountId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    Task AddAsync(Transaction transaction, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
