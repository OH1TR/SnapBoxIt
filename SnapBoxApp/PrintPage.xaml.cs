using SnapBoxApp.Services;

namespace SnapBoxApp;

public partial class PrintPage : ContentPage
{
    private readonly ApiService _apiService;

    public PrintPage()
    {
        InitializeComponent();
        _apiService = new ApiService();
    }

    private async void OnPrintClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PrintTextEditor.Text))
        {
            await DisplayAlert("Virhe", "Syötä tulostettava teksti", "OK");
            return;
        }

        try
        {
            PrintButton.IsEnabled = false;
            PrintButton.Text = "Tulostetaan...";

            string printType = QrLabelRadio.IsChecked ? "qrlabel" : "label";
            
            var success = await _apiService.PrintLabel(printType, PrintTextEditor.Text);
            
            if (success)
            {
                await DisplayAlert("Onnistui", "Tulostepyyntö lähetetty onnistuneesti", "OK");
                PrintTextEditor.Text = string.Empty;
            }
            else
            {
                await DisplayAlert("Virhe", "Tulostepyynnön lähetys epäonnistui", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Virhe", $"Virhe tulostuksessa: {ex.Message}", "OK");
        }
        finally
        {
            PrintButton.IsEnabled = true;
            PrintButton.Text = "Tulosta";
        }
    }
}
