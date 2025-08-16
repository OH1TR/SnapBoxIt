using SnapBoxApp.Services;
using System.IO;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;

namespace SnapBoxApp;

public partial class UploadPage : ContentPage
{
	private readonly ApiService _apiService = new ApiService();

	public UploadPage()
	{
		InitializeComponent();
		//camera.MediaCaptured += OnCaptureClicked;
	}

	private async void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
	{
		try
		{
			using var stream = new MemoryStream();
			await e.Media.CopyToAsync(stream);
			stream.Position = 0;
			var response = await _apiService.Upload(stream);
			if (response.IsSuccessStatusCode)
			{
				await DisplayAlert("Success", "Photo uploaded successfully!", "OK");
			}
			else
			{
				await DisplayAlert("Upload Failed", $"Server returned: {response.StatusCode}", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred while capturing or uploading the photo: {ex.Message}", "OK");
		}
	}
	// Remove OnMediaCaptured if you are not using it, or replace MediaCapturedEventArgs with the correct type from your camera library.
	// If you intended to use OnCaptureClicked only, you can safely remove this method.

	private async void OnCaptureClicked(object? sender, EventArgs e)
	{
		try
		{
			var startCameraPreviewTCS = new CancellationTokenSource(TimeSpan.FromSeconds(3));
			var photo = await camera.CaptureImage(startCameraPreviewTCS.Token);


			if (photo != null)
			{
				using var stream = new MemoryStream();
				await photo.CopyToAsync(stream);
				stream.Position = 0;
				var response = await _apiService.Upload(stream);
				if (response.IsSuccessStatusCode)
				{
					await DisplayAlert("Success", "Photo uploaded successfully!", "OK");
				}
				else
				{
					await DisplayAlert("Upload Failed", $"Server returned: {response.StatusCode}", "OK");
				}
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred while capturing or uploading the photo: {ex.Message}", "OK");
		}
	}
}