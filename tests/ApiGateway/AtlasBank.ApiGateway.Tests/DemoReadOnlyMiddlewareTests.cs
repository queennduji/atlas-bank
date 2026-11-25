using System.Security.Claims;
using AtlasBank.ApiGateway;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace AtlasBank.ApiGateway.Tests;

public class DemoReadOnlyMiddlewareTests
{
    private const string DemoClaim = """{"roles":["demo-viewer","offline_access"]}""";
    private const string CustomerClaim = """{"roles":["user"]}""";

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task DemoUser_cannot_use_unsafe_methods(string method)
    {
        var (status, reachedService) = await Send(method, authenticated: true, realmAccess: DemoClaim);

        status.Should().Be(StatusCodes.Status403Forbidden);
        reachedService.Should().BeFalse();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    public async Task DemoUser_can_use_safe_methods(string method)
    {
        var (status, reachedService) = await Send(method, authenticated: true, realmAccess: DemoClaim);

        status.Should().Be(StatusCodes.Status200OK);
        reachedService.Should().BeTrue();
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Regular_customer_can_still_write(string method)
    {
        var (status, reachedService) = await Send(method, authenticated: true, realmAccess: CustomerClaim);

        status.Should().Be(StatusCodes.Status200OK);
        reachedService.Should().BeTrue();
    }

    [Fact]
    public async Task Anonymous_request_is_left_for_the_auth_policy_to_judge()
    {
        // Registration is a public POST; this middleware must not get in its way.
        var (status, reachedService) = await Send("POST", authenticated: false, realmAccess: null);

        status.Should().Be(StatusCodes.Status200OK);
        reachedService.Should().BeTrue();
    }

    [Fact]
    public async Task Token_without_realm_access_is_not_treated_as_demo()
    {
        var (_, reachedService) = await Send("POST", authenticated: true, realmAccess: null);

        reachedService.Should().BeTrue();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"roles":"demo-viewer"}""")]
    [InlineData("""{"other":["demo-viewer"]}""")]
    public async Task Malformed_realm_access_is_not_treated_as_demo(string claim)
    {
        var (_, reachedService) = await Send("POST", authenticated: true, realmAccess: claim);

        reachedService.Should().BeTrue();
    }

    // The backstop: if the role never reaches the token, the demo account is still recognised.
    [Theory]
    [InlineData("preferred_username")]
    [InlineData("email")]
    [InlineData(ClaimTypes.Email)]
    public async Task Demo_account_is_blocked_by_identity_even_without_the_role(string claimType)
    {
        var (status, reachedService) = await Send("POST", authenticated: true, realmAccess: CustomerClaim,
            extraClaims: [new Claim(claimType, "demo@atlasbank.dev")]);

        status.Should().Be(StatusCodes.Status403Forbidden);
        reachedService.Should().BeFalse();
    }

    [Fact]
    public async Task Demo_identity_match_ignores_case()
    {
        var (_, reachedService) = await Send("POST", authenticated: true, realmAccess: null,
            extraClaims: [new Claim("email", "Demo@AtlasBank.dev")]);

        reachedService.Should().BeFalse();
    }

    [Fact]
    public async Task Demo_account_can_still_read_when_recognised_by_identity()
    {
        var (_, reachedService) = await Send("GET", authenticated: true, realmAccess: null,
            extraClaims: [new Claim("preferred_username", "demo@atlasbank.dev")]);

        reachedService.Should().BeTrue();
    }

    [Fact]
    public async Task Other_users_are_not_caught_by_the_identity_check()
    {
        var (_, reachedService) = await Send("POST", authenticated: true, realmAccess: CustomerClaim,
            extraClaims: [new Claim("email", "someone@example.com"), new Claim("preferred_username", "someone@example.com")]);

        reachedService.Should().BeTrue();
    }

    [Fact]
    public async Task Demo_email_can_be_configured()
    {
        var (_, blockedReached) = await Send("POST", authenticated: true, realmAccess: null,
            extraClaims: [new Claim("email", "staging-demo@example.com")], configuredDemoEmail: "staging-demo@example.com");
        var (_, defaultReached) = await Send("POST", authenticated: true, realmAccess: null,
            extraClaims: [new Claim("email", "demo@atlasbank.dev")], configuredDemoEmail: "staging-demo@example.com");

        blockedReached.Should().BeFalse();
        defaultReached.Should().BeTrue("a configured address replaces the default rather than adding to it");
    }

    private static async Task<(int Status, bool ReachedService)> Send(
        string method,
        bool authenticated,
        string? realmAccess,
        IEnumerable<Claim>? extraClaims = null,
        string? configuredDemoEmail = null)
    {
        var reachedService = false;
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configuredDemoEmail is null
                ? []
                : [new KeyValuePair<string, string?>("Demo:Email", configuredDemoEmail)])
            .Build();
        var middleware = new DemoReadOnlyMiddleware(_ =>
        {
            reachedService = true;
            return Task.CompletedTask;
        }, config);

        var claims = new List<Claim>(extraClaims ?? []);
        if (realmAccess is not null) claims.Add(new Claim("realm_access", realmAccess));

        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(authenticated
                ? new ClaimsIdentity(claims, authenticationType: "Bearer")
                : new ClaimsIdentity()),
        };
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        return (context.Response.StatusCode, reachedService);
    }
}
