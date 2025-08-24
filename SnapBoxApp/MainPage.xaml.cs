namespace SnapBoxApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnUploadClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(UploadPage));
	}

	private async void OnSettingsClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(SettingsPage));
	}

	private async void OnSearchClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(SearchPage));
	}

	private async void OnPrintClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(PrintPage));
	}
}
