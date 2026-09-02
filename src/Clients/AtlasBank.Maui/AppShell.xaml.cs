using AtlasBank.Maui.Services.Navigation;
using AtlasBank.Maui.Views;

namespace AtlasBank.Maui;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Pushed on demand with a required parameter rather than declared as ShellContent –
        // see Services/Navigation/Routes.cs for the route names these pages navigate to.
        Routing.RegisterRoute(Routes.AccountDetail, typeof(AccountDetailPage));
        Routing.RegisterRoute(Routes.Transfer, typeof(TransferPage));
        Routing.RegisterRoute(Routes.StatementDetail, typeof(StatementDetailPage));
    }
}
