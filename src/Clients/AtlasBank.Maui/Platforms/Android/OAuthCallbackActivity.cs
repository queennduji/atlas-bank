using Android.App;
using Android.Content;
using Android.Content.PM;
using AtlasBank.Maui.Config;
using Microsoft.Maui.Authentication;

namespace AtlasBank.Maui;

/// <summary>
/// Registers the "atlasbank://" scheme with Android so the browser tab
/// <see cref="Services.Auth.MobileOAuthBrowserLauncher"/> opens can hand the redirect back to
/// this app instead of just sitting there.
/// </summary>
[Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = AppConfig.MobileRedirectScheme)]
public class OAuthCallbackActivity : WebAuthenticatorCallbackActivity
{
}
