using AtlasBank.Clients.Core.Api;
using AtlasBank.Clients.Core.Auth;
using AtlasBank.Clients.Core.Http;
using AtlasBank.Maui.Config;
using AtlasBank.Maui.Services.Auth;
using AtlasBank.Maui.Services.Navigation;
using AtlasBank.Maui.Services.Offline;
using AtlasBank.Maui.ViewModels;
using AtlasBank.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;

namespace AtlasBank.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        RegisterAuth(builder.Services);
        RegisterApi(builder.Services);
        RegisterAppServices(builder.Services);
        RegisterPagesAndViewModels(builder.Services);

        return builder.Build();
    }

    private static void RegisterAuth(IServiceCollection services)
    {
        services.AddSingleton(new AtlasAuthOptions
        {
            Authority = AppConfig.KeycloakAuthority,
            ClientId = AppConfig.KeycloakClientId,
        });

        services.AddSingleton<ITokenStore, MauiSecureTokenStore>();

#if WINDOWS
        services.AddSingleton<IOAuthBrowserLauncher, LoopbackOAuthBrowserLauncher>();
#else
        services.AddSingleton<IOAuthBrowserLauncher, MobileOAuthBrowserLauncher>();
#endif

        // plain, unauthenticated client for hitting Keycloak's discovery/token endpoints –
        // not the same one BearerTokenHandler decorates, since a bearer token on a *token*
        // request wouldn't make sense
        services.AddHttpClient("AtlasBank.Auth");
        services.AddSingleton(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new OidcAuthenticator(
                httpClientFactory.CreateClient("AtlasBank.Auth"),
                sp.GetRequiredService<AtlasAuthOptions>(),
                sp.GetRequiredService<IOAuthBrowserLauncher>(),
                sp.GetRequiredService<ITokenStore>());
        });
    }

    private static void RegisterApi(IServiceCollection services)
    {
        services.AddTransient<BearerTokenHandler>();

        services.AddHttpClient<AtlasApiClient>(client =>
            {
                client.BaseAddress = new Uri(AppConfig.GatewayBaseUrl);
            })
            .AddHttpMessageHandler<BearerTokenHandler>()
            // same resilience package the API Gateway uses server-side – retry with
            // jittered backoff, a circuit breaker, per-attempt timeout – so a blip talking
            // to the gateway doesn't immediately turn into an error banner
            .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = 2;
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
            });
    }

    private static void RegisterAppServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationService, ShellNavigationService>();
        services.AddSingleton<IOfflineCache, JsonFileOfflineCache>();
    }

    private static void RegisterPagesAndViewModels(IServiceCollection services)
    {
        services.AddSingleton<AppShell>();

        services.AddTransient<LoginPage>();
        services.AddTransient<LoginViewModel>();

        services.AddTransient<RegisterPage>();
        services.AddTransient<RegisterViewModel>();

        services.AddTransient<DashboardPage>();
        services.AddTransient<DashboardViewModel>();

        services.AddTransient<AccountDetailPage>();
        services.AddTransient<AccountDetailViewModel>();

        services.AddTransient<TransferPage>();
        services.AddTransient<TransferViewModel>();

        services.AddTransient<CardsPage>();
        services.AddTransient<CardsViewModel>();

        services.AddTransient<StatementsPage>();
        services.AddTransient<StatementsViewModel>();

        services.AddTransient<StatementDetailPage>();
        services.AddTransient<StatementDetailViewModel>();

        services.AddTransient<ProfilePage>();
        services.AddTransient<ProfileViewModel>();
    }
}
