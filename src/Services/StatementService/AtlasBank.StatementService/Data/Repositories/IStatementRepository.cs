using AtlasBank.StatementService.Domain.Entities;

namespace AtlasBank.StatementService.Data.Repositories;

public interface IStatementRepository
{
    Task AddAsync(Statement statement, CancellationToken ct = default);
    Task<Statement?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Statement>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
