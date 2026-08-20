using AtlasBank.LedgerService.Domain.Entities;
using AtlasBank.LedgerService.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.LedgerService.Data.Repositories;

public class LedgerRepository(LedgerDbContext db) : ILedgerRepository
{
    public async Task AddAsync(LedgerEntry entry, CancellationToken ct = default)
        => await db.LedgerEntries.AddAsync(entry, ct);

    public Task<bool> ExistsByTransactionIdAsync(Guid transactionId, CancellationToken ct = default)
        => db.LedgerEntries.AnyAsync(e => e.TransactionId == transactionId, ct);

    public async Task<IReadOnlyList<LedgerEntry>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
        => await db.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.PostedAt)
            .ToListAsync(ct);

    public async Task<decimal> GetBalanceAsync(Guid accountId, CancellationToken ct = default)
    {
        var entries = await db.LedgerEntries
            .Where(e => e.AccountId == accountId)
            .Select(e => new { e.Type, e.Amount })
            .ToListAsync(ct);

        return entries.Sum(e => e.Type == LedgerEntryType.Credit ? e.Amount : -e.Amount);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
