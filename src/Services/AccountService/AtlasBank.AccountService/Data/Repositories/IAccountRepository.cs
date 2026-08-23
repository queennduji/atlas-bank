using AtlasBank.AccountService.Domain.Entities;

namespace AtlasBank.AccountService.Data.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Re-reads <paramref name="account"/>'s current column values (including its
    /// concurrency token) from the database into the already-tracked instance. Used to
    /// recover from a <see cref="Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException"/>
    /// by retrying against the latest state instead of the stale one that just lost the race.
    /// </summary>
    Task ReloadAsync(Account account, CancellationToken ct = default);
}
