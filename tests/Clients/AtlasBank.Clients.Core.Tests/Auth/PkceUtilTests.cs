using AtlasBank.Clients.Core.Auth;
using FluentAssertions;

namespace AtlasBank.Clients.Core.Tests.Auth;

public class PkceUtilTests
{
    [Fact]
    public void CreateCodeChallenge_MatchesRfc7636AppendixBTestVector()
    {
        // verifier/challenge pair straight from RFC 7636 Appendix B – if this stops matching,
        // it's the S256 implementation that broke, not the test
        const string verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        const string expectedChallenge = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM";

        PkceUtil.CreateCodeChallenge(verifier).Should().Be(expectedChallenge);
    }

    [Fact]
    public void CreateCodeVerifier_ProducesRfcCompliantLengthAndCharset()
    {
        var verifier = PkceUtil.CreateCodeVerifier();

        verifier.Length.Should().BeInRange(43, 128);
        verifier.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }

    [Fact]
    public void CreateCodeVerifier_IsRandomPerCall()
    {
        PkceUtil.CreateCodeVerifier().Should().NotBe(PkceUtil.CreateCodeVerifier());
    }

    [Fact]
    public void CreateState_IsUrlSafeAndRandomPerCall()
    {
        var first = PkceUtil.CreateState();
        var second = PkceUtil.CreateState();

        first.Should().NotBe(second);
        first.Should().MatchRegex("^[A-Za-z0-9_-]+$");
    }
}
