using System.Security.Claims;
using System.Text.Json;

namespace AtlasBank.ApiGateway;

/// <summary>
/// Makes the public demo account read-only. Anyone recognised as the demo user can browse,
/// but every request that isn't a safe method (GET/HEAD/OPTIONS) is turned away here,
/// before it reaches a service. Doing this once at the gateway means a service added later
/// is covered without having to remember to opt it in.
/// </summary>
/// <remarks>
/// The demo user is recognised two ways, and either one is enough: the <c>demo-viewer</c>
/// realm role, or the demo account's own username/email. The second is a backstop – if a
/// Keycloak change ever stopped the role from reaching the token, the check would otherwise
/// fail open and hand a public-password account write access.
/// </remarks>
public sealed class DemoReadOnlyMiddleware(RequestDelegate next, IConfiguration config)
{
    public const string DemoRole = "demo-viewer";
    public const string DefaultDemoEmail = "demo@atlasbank.dev";

    private readonly string _demoEmail = config["Demo:Email"] is { Length: > 0 } email ? email : DefaultDemoEmail;

    public Task InvokeAsync(HttpContext context)
    {
        if (IsSafeMethod(context.Request.Method) || !IsDemoUser(context.User))
            return next(context);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new
        {
            message = "The demo account is read-only. Register for a real account to move money.",
        });
    }

    private static bool IsSafeMethod(string method) =>
        HttpMethods.IsGet(method) || HttpMethods.IsHead(method) || HttpMethods.IsOptions(method);

    private bool IsDemoUser(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return false;

        return HasDemoRole(user) || IsDemoAccount(user);
    }

    // The username can't be edited from Keycloak's self-service pages, but the email can, so
    // check every spelling the claim might arrive under (the JWT handler may remap "email").
    private bool IsDemoAccount(ClaimsPrincipal user)
    {
        foreach (var type in new[] { "preferred_username", "email", ClaimTypes.Email })
            foreach (var claim in user.FindAll(type))
                if (string.Equals(claim.Value, _demoEmail, StringComparison.OrdinalIgnoreCase))
                    return true;

        return false;
    }

    // Keycloak puts realm roles in a nested "realm_access": { "roles": [...] } claim, which
    // the JWT handler surfaces as a JSON string.
    private static bool HasDemoRole(ClaimsPrincipal user)
    {
        var realmAccess = user.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccess)) return false;

        try
        {
            using var doc = JsonDocument.Parse(realmAccess);
            if (!doc.RootElement.TryGetProperty("roles", out var roles) || roles.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var role in roles.EnumerateArray())
                if (role.ValueKind == JsonValueKind.String && role.GetString() == DemoRole)
                    return true;
        }
        catch (JsonException)
        {
            // A malformed claim can't have come from Keycloak; treat it as "no demo role".
        }

        return false;
    }
}
