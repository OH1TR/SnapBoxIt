namespace SnapBoxApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(UploadPage), typeof(UploadPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
		Routing.RegisterRoute(nameof(SearchPage), typeof(SearchPage));
		Routing.RegisterRoute(nameof(PrintPage), typeof(PrintPage));
		Routing.RegisterRoute(nameof(BoxViewPage), typeof(BoxViewPage));
	}
}
