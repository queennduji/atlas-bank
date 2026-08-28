using AtlasBank.Clients.Core.Auth;
using FluentAssertions;

namespace AtlasBank.Clients.Core.Tests.Auth;

file sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

public class TokenSetTests
{
    private static TokenSet TokenExpiringAt(DateTimeOffset expiresAtUtc) => new()
    {
        AccessToken = "access-token",
        RefreshToken = "refresh-token",
        IdToken = "id-token",
        AccessTokenExpiresAtUtc = expiresAtUtc,
    };

    [Fact]
    public void NeedsRefresh_IsFalse_WellBeforeExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var token = TokenExpiringAt(now.AddMinutes(5));

        token.NeedsRefresh(new FixedTimeProvider(now)).Should().BeFalse();
    }

    [Fact]
    public void NeedsRefresh_IsTrue_OnceInsideTheRefreshSkewWindow()
    {
        var now = DateTimeOffset.UtcNow;
        // expires in 10 seconds, inside the 30-second skew TokenSet refreshes ahead of
        var token = TokenExpiringAt(now.AddSeconds(10));

        token.NeedsRefresh(new FixedTimeProvider(now)).Should().BeTrue();
    }

    [Fact]
    public void NeedsRefresh_IsTrue_AfterExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var token = TokenExpiringAt(now.AddMinutes(-1));

        token.NeedsRefresh(new FixedTimeProvider(now)).Should().BeTrue();
    }
}
