using AtlasBank.Clients.Core.Auth;

namespace AtlasBank.Clients.Core.Tests.TestSupport;

public sealed class InMemoryTokenStore : ITokenStore
{
    public TokenSet? Saved { get; private set; }
    public int SaveCount { get; private set; }
    public int ClearCount { get; private set; }

    public InMemoryTokenStore(TokenSet? initial = null) => Saved = initial;

    public Task SaveAsync(TokenSet tokens)
    {
        Saved = tokens;
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<TokenSet?> LoadAsync() => Task.FromResult(Saved);

    public Task ClearAsync()
    {
        Saved = null;
        ClearCount++;
        return Task.CompletedTask;
    }
}
