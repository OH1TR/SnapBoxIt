
namespace SnapBoxApp.Services;

public static class SettingsService
{
    // Preferences-avaimet
    private const string ApiBaseUrlKey = "api_base_url";
    private const string UsernameKey = "username";
    // SecureStorage-avain
    private const string PasswordKey = "password";

    // ApiBaseUrl (Preferences)
    public static string ApiBaseUrl
    {
        get => Preferences.Get(ApiBaseUrlKey, "https://api.example.com");
        set => Preferences.Set(ApiBaseUrlKey, value);
    }

    // Username (Preferences)
    public static string Username
    {
        get => Preferences.Get(UsernameKey, string.Empty);
        set => Preferences.Set(UsernameKey, value);
    }

    public static string Password
    {
        get => Preferences.Get(PasswordKey, string.Empty);
        set => Preferences.Set(PasswordKey, value);
    }
    public static void ClearAll()
    {
        Preferences.Remove(ApiBaseUrlKey);
        Preferences.Remove(UsernameKey);
        Preferences.Remove(PasswordKey);
    }
}