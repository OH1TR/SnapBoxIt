namespace SnapBoxApp;

public partial class SettingsPage : ContentPage
{
    private readonly SettingsViewModel _vm;

    public SettingsPage()
    {
        InitializeComponent();

        _vm = new SettingsViewModel();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.Load();
    }
}