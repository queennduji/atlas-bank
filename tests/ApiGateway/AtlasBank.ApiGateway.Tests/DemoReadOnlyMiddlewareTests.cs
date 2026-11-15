using System.Security.Claims;
using AtlasBank.ApiGateway;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

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

    private static async Task<(int Status, bool ReachedService)> Send(string method, bool authenticated, string? realmAccess)
    {
        var reachedService = false;
        var middleware = new DemoReadOnlyMiddleware(_ =>
        {
            reachedService = true;
            return Task.CompletedTask;
        });

        var claims = new List<Claim>();
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
