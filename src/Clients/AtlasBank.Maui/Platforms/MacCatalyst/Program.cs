using ObjCRuntime;
using UIKit;

namespace AtlasBank.Maui;

public class Program
{
	// MacCatalyst entry point – same AppDelegate/MauiProgram setup as iOS.
	static void Main(string[] args)
	{
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
