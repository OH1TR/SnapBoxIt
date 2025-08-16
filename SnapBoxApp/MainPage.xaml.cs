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
}
