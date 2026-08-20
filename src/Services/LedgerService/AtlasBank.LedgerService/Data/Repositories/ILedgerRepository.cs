using AtlasBank.LedgerService.Domain.Entities;

namespace AtlasBank.LedgerService.Data.Repositories;

public interface ILedgerRepository
{
    Task AddAsync(LedgerEntry entry, CancellationToken ct = default);
    Task<bool> ExistsByTransactionIdAsync(Guid transactionId, CancellationToken ct = default);
    Task<IReadOnlyList<LedgerEntry>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task<decimal> GetBalanceAsync(Guid accountId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
