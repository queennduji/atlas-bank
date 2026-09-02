using ObjCRuntime;
using UIKit;

namespace AtlasBank.Maui;

public class Program
{
	// iOS entry point – AppDelegate.cs handles the OAuth redirect, everything else goes
	// through the shared MauiProgram setup like the other platforms.
	static void Main(string[] args)
	{
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}
