using AtlasBank.AccountService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.AccountService.Data.Repositories;

public class AccountRepository(AccountDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken ct = default) =>
        db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, ct);

    public async Task<IReadOnlyList<Account>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct = default) =>
        await db.Accounts.Where(a => a.CustomerId == customerId).ToListAsync(ct);

    public async Task AddAsync(Account account, CancellationToken ct = default) =>
        await db.Accounts.AddAsync(account, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
