using System.Diagnostics;
using System.Net;
using System.Web;

namespace AtlasBank.Clients.Core.Auth;

/// <summary>
/// Desktop OAuth redirect capture: opens the authorization URL in the system's default
/// browser, then waits for Keycloak's redirect to hit a tiny local HTTP listener – no
/// custom URL scheme needed on Windows. Pure BCL, no UI framework dependency, so
/// AtlasBank.Maui's Windows target and AtlasBank.Wpf can both use it unchanged.
/// </summary>
public sealed class LoopbackOAuthBrowserLauncher : IOAuthBrowserLauncher, IDisposable
{
    // Fixed port rather than an OS-assigned one, on purpose: Keycloak matches redirect_uri
    // as an exact string, and "http://127.0.0.1:*/..." isn't a pattern it understands – the
    // port has to be part of what's registered in keycloak/realm-export.json. Downside is
    // sign-in breaks if something else already has this port; acceptable for a demo app.
    public const int DefaultPort = 51739;

    private const string SuccessHtml =
        "<html><body style=\"font-family:sans-serif;text-align:center;padding-top:4em\">" +
        "<h2>Signed in to Atlas Bank</h2><p>You can close this window and return to the app.</p></body></html>";

    private const string FailureHtml =
        "<html><body style=\"font-family:sans-serif;text-align:center;padding-top:4em\">" +
        "<h2>Sign-in didn't complete</h2><p>You can close this window and return to the app.</p></body></html>";

    private readonly HttpListener _listener;

    public Uri RedirectUri { get; }

    public LoopbackOAuthBrowserLauncher(int port = DefaultPort)
    {
        RedirectUri = new Uri($"http://127.0.0.1:{port}/atlasbank-callback/");

        _listener = new HttpListener();
        _listener.Prefixes.Add(RedirectUri.ToString());
        _listener.Start();
    }

    public async Task<IReadOnlyDictionary<string, string>> AuthenticateAsync(Uri authorizationUri, CancellationToken ct)
    {
        Process.Start(new ProcessStartInfo(authorizationUri.ToString()) { UseShellExecute = true });

        using var registration = ct.Register(() => _listener.Stop());
        HttpListenerContext context;
        try
        {
            context = await _listener.GetContextAsync().ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
        {
            ct.ThrowIfCancellationRequested();
            throw;
        }

        var query = HttpUtility.ParseQueryString(context.Request.Url?.Query ?? string.Empty);
        var result = query.AllKeys
            .Where(key => key is not null)
            .ToDictionary(key => key!, key => query[key] ?? string.Empty);

        var responseHtml = result.ContainsKey("code") ? SuccessHtml : FailureHtml;
        var buffer = System.Text.Encoding.UTF8.GetBytes(responseHtml);
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = buffer.Length;
        await context.Response.OutputStream.WriteAsync(buffer, ct).ConfigureAwait(false);
        context.Response.OutputStream.Close();

        return result;
    }

    public void Dispose()
    {
        if (_listener.IsListening)
        {
            _listener.Stop();
        }
        ((IDisposable)_listener).Dispose();
    }
}
