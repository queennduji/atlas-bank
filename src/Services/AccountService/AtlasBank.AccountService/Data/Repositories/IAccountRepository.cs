using AtlasBank.AccountService.Domain.Entities;

namespace AtlasBank.AccountService.Data.Repositories;

public interface IAccountRepository
{
    Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default);
    Task AddAsync(Account account, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
