using Foundation;
using Microsoft.Maui.ApplicationModel;
using UIKit;

namespace AtlasBank.Maui;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    // Forwards the "atlasbank://callback" redirect Keycloak sends after sign-in to
    // WebAuthenticator (see Services/Auth/MobileOAuthBrowserLauncher.cs), which is what
    // resumes the awaited AuthenticateAsync call.
    public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options) =>
        Platform.OpenUrl(app, url, options) || base.OpenUrl(app, url, options);
}
