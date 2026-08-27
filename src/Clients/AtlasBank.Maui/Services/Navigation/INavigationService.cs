namespace AtlasBank.Maui.Services.Navigation;

/// <summary>
/// Thin wrapper around Shell navigation so ViewModels depend on an interface instead of the
/// static <c>Shell.Current</c> — keeps navigation calls mockable from a ViewModel unit test
/// without spinning up a real Shell.
/// </summary>
public interface INavigationService
{
    Task GoToAsync(string route, IDictionary<string, object>? parameters = null);

    /// <summary>Navigates to an absolute tab route (e.g. "//AppTabs/Dashboard"), replacing
    /// the whole navigation stack — used for sign-in/sign-out transitions and for deep-linking
    /// into another tab (e.g. Statements) with a parameter already applied.</summary>
    Task GoToRootAsync(string route, IDictionary<string, object>? parameters = null);

    Task GoBackAsync();
}
