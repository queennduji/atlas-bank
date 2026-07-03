using AtlasBank.TransactionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.TransactionService.Data.Repositories;

public class TransactionRepository(TransactionDbContext db) : ITransactionRepository
{
    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Transactions.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<Transaction>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
        => db.Transactions
            .Where(t => t.AccountId == accountId || t.ToAccountId == accountId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Transaction transaction, CancellationToken ct = default)
        => await db.Transactions.AddAsync(transaction, ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
