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
	}

	
	private async void OnCaptureClicked(object? sender, EventArgs e)
	{
		   try
		   {
			string boxId=BoxName.Text;
			MainThread.BeginInvokeOnMainThread(() => {
			   CaptureBtn.IsEnabled = false;
			});
			   var startCameraPreviewTCS = new CancellationTokenSource(TimeSpan.FromSeconds(3));
			   var photo = await camera.CaptureImage(startCameraPreviewTCS.Token);

			   if (photo != null)
			   {
				   using var stream = new MemoryStream();
				   await photo.CopyToAsync(stream);
				   stream.Position = 0;
				   var response = await _apiService.Upload(stream, boxId);
				   if (response.IsSuccessStatusCode)
				   {
					   var json = await response.Content.ReadAsStringAsync();
					   var item = System.Text.Json.JsonSerializer.Deserialize<Model.ItemSimpleDto>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					   BindingContext = item;
					   await DisplayAlert("Success", $"Photo uploaded successfully! Item ID: {item?.id}", "OK");
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
		   finally
		   {
				MainThread.BeginInvokeOnMainThread(() => {
			   CaptureBtn.IsEnabled = true;
				});
		   }
	}
}