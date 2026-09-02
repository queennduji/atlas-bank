using Microsoft.UI.Xaml;

namespace AtlasBank.Maui.WinUI;

// Windows entry point – just wires up WinUI and hands off to the shared MauiProgram setup.
public partial class App : MauiWinUIApplication
{
	public App()
	{
		this.InitializeComponent();
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}

