namespace AtlasBank.Maui.Services.Navigation;

/// <summary>Route names registered in AppShell.xaml / AppShell.xaml.cs – collected here so a
/// typo in a navigation call is a compile error instead of a silent dead button.</summary>
public static class Routes
{
    public const string Login = nameof(Login);
    public const string Register = nameof(Register);

    public const string AppTabs = nameof(AppTabs);
    public const string Dashboard = nameof(Dashboard);
    public const string Cards = nameof(Cards);
    public const string Statements = nameof(Statements);
    public const string Profile = nameof(Profile);

    public const string AccountDetail = nameof(AccountDetail);
    public const string Transfer = nameof(Transfer);
    public const string StatementDetail = nameof(StatementDetail);

    public static string TabRoute(string tab) => $"//{AppTabs}/{tab}";
}
