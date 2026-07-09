using AtlasBank.CardService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AtlasBank.CardService.Data.Repositories;

public class CardRepository(CardDbContext db) : ICardRepository
{
    public async Task AddAsync(Card card, CancellationToken ct = default)
        => await db.Cards.AddAsync(card, ct);

    public async Task<Card?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Cards.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Card>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default)
        => await db.Cards
            .Where(c => c.AccountId == accountId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await db.SaveChangesAsync(ct);
}
