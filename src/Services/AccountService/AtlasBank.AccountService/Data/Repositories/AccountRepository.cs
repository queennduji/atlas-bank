using AtlasBank.AccountService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.AccountService.Data.Repositories;

public class AccountRepository(AccountDbContext db) : IAccountRepository
{
    public Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Account?> GetByAccountNumberAsync(string accountNumber, CancellationToken ct = default) =>
        db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, ct);

    public async Task<IReadOnlyList<Account>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) =>
        await db.Accounts.Where(a => a.OwnerId == ownerId).ToListAsync(ct);

    public async Task AddAsync(Account account, CancellationToken ct = default) =>
        await db.Accounts.AddAsync(account, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}
