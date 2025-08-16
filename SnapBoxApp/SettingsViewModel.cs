using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using SnapBoxApp.Services;

namespace SnapBoxApp;

public class SettingsViewModel : INotifyPropertyChanged
{
    private string _apiBaseUrl = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetProperty(ref _apiBaseUrl, value);
    }

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public ICommand SaveCommand { get; }

    public SettingsViewModel()
    {
        SaveCommand = new Command(async () => await SaveAsync());
    }

    // Lataa tallennetut arvot (kutsutaan OnAppearingissä)
    public void Load()
    {
        ApiBaseUrl = SettingsService.ApiBaseUrl;
        Username = SettingsService.Username;
        Password = SettingsService.Password;
    }

    private async Task SaveAsync()
    {
        SettingsService.ApiBaseUrl = ApiBaseUrl?.Trim() ?? string.Empty;
        SettingsService.Username = Username?.Trim() ?? string.Empty;
        SettingsService.Password = Password ?? string.Empty;

        // Yksinkertainen kuittaus
        if (Application.Current?.MainPage is Page page)
            await page.DisplayAlert("Asetukset", "Tallennettu.", "OK");
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value)) return false;
        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    #endregion
}
