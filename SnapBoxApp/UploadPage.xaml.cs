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
			string boxId = BoxName.Text;
			MainThread.BeginInvokeOnMainThread(() =>
			{
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
			MainThread.BeginInvokeOnMainThread(() =>
			{
				CaptureBtn.IsEnabled = true;
			});
		}
	}
	private async void OnRejectClicked(object? sender, EventArgs e)
	{
		try
		{
			if (BindingContext is Model.ItemSimpleDto item)
			{
				bool confirm = await DisplayAlert("Confirm", "Are you sure you want to reject this item?", "Yes", "No");
				if (confirm)
				{
					bool deleted = await _apiService.DeleteItem(item.id);
					if (deleted)
					{
						await DisplayAlert("Success", "Item rejected successfully.", "OK");
						BindingContext = null; // Clear the context after rejection
					}
					else
					{
						await DisplayAlert("Error", "Failed to reject the item. Please try again.", "OK");
					}
				}
			}
			else
			{
				await DisplayAlert("Error", "No item to reject.", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred: {ex.Message}", "OK");
		}
	}

	private async void OnSaveClicked(object? sender, EventArgs e)
	{
		try
		{
			if (BindingContext is Model.ItemSimpleDto item)
			{
				// Update the item with values from the UI
				if (float.TryParse(CountEntry.Text, out float count))
				{
					item.Count = count;
				}
				item.UserDescription = UserDescriptionEditor.Text;

				// Call the API service to save the item
				bool saved = await _apiService.SaveItem(item);
				if (!saved)
				{
					await DisplayAlert("Error", "Failed to save the item. Please try again.", "OK");
				}
			}
			else
			{
				await DisplayAlert("Error", "No item to save.", "OK");
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", $"An error occurred while saving: {ex.Message}", "OK");
		}
	}
}
