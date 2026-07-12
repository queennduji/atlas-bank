using AtlasBank.StatementService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.StatementService.Data.Repositories;

public class StatementRepository(StatementDbContext db) : IStatementRepository
{
    public async Task AddAsync(Statement statement, CancellationToken ct = default)
        => await db.Statements.AddAsync(statement, ct);

    public Task<Statement?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Statements
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Statement>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
        => await db.Statements
            .Where(s => s.AccountId == accountId)
            .OrderByDescending(s => s.PeriodStart)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
