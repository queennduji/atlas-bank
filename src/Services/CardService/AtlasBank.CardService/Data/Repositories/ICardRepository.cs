using AtlasBank.CardService.Domain.Entities;

namespace AtlasBank.CardService.Data.Repositories;

public interface ICardRepository
{
    Task AddAsync(Card card, CancellationToken ct = default);
    Task<Card?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Card>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
