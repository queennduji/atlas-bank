using Microsoft.Extensions.DependencyInjection;

namespace AtlasBank.Maui;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Resolved through DI (rather than "new AppShell()") so AppShell's constructor can
        // take OidcAuthenticator and check for a saved session before the user sees anything.
        var shell = _services.GetRequiredService<AppShell>();
        return new Window(shell);
    }
}
