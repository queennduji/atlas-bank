using AtlasBank.Clients.Core.Models;

namespace AtlasBank.Clients.Core.Api;

public sealed partial class AtlasApiClient
{
    public Task<IReadOnlyList<Card>> GetAccountCardsAsync(Guid accountId, CancellationToken ct = default) =>
        GetAsync<IReadOnlyList<Card>>($"/api/cards/account/{accountId}", ct);

    public Task<Card> GetCardAsync(Guid cardId, CancellationToken ct = default) =>
        GetAsync<Card>($"/api/cards/{cardId}", ct);

    public Task<Card> IssueCardAsync(IssueCardRequest request, CancellationToken ct = default) =>
        PostAsync<Card>("/api/cards", request, ct);

    public Task<Card> FreezeCardAsync(Guid cardId, CancellationToken ct = default) =>
        PostAsync<Card>($"/api/cards/{cardId}/freeze", body: null, ct);

    public Task<Card> UnfreezeCardAsync(Guid cardId, CancellationToken ct = default) =>
        PostAsync<Card>($"/api/cards/{cardId}/unfreeze", body: null, ct);

    public Task<Card> UpdateSpendingLimitAsync(Guid cardId, UpdateSpendingLimitRequest request, CancellationToken ct = default) =>
        PutAsync<Card>($"/api/cards/{cardId}/spendingLimit", request, ct);
}
