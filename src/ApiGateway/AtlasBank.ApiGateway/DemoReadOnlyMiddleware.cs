using System.Text.Json;

namespace AtlasBank.ApiGateway;

/// <summary>
/// Makes the public demo account read-only. Anyone holding a token with the
/// <c>demo-viewer</c> realm role can browse, but every request that isn't a
/// safe method (GET/HEAD/OPTIONS) is turned away here, before it reaches a service.
/// Doing this once at the gateway means a service added later is covered without
/// having to remember to opt it in.
/// </summary>
public sealed class DemoReadOnlyMiddleware(RequestDelegate next)
{
    public const string DemoRole = "demo-viewer";

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

    // Keycloak puts realm roles in a nested "realm_access": { "roles": [...] } claim, which
    // the JWT handler surfaces as a JSON string.
    private static bool IsDemoUser(System.Security.Claims.ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true) return false;

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
