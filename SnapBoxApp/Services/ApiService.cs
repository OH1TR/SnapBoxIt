using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using SnapBoxApp.Model;

namespace SnapBoxApp.Services;

public class ApiService
{
    public async Task<byte[]?> GetImageBytes(string blobId, bool thumbnail = false)
    {
        try
        {
            var url = $"{_apiUrl}/Data/Image/{Uri.EscapeDataString(blobId)}" + (thumbnail ? "?thumb=true" : "");
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
        }
        catch (Exception)
        {
            // Handle exceptions as needed
        }
        return null;
    }
    private readonly string _apiUrl;
    private readonly string _username;
    private readonly string _password;
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        _apiUrl = SettingsService.ApiBaseUrl;
        _username = SettingsService.Username;
        _password = SettingsService.Password;
        _httpClient = new HttpClient();

        var byteArray = Encoding.ASCII.GetBytes($"{_username}:{_password}");
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }

    public async Task<HttpResponseMessage> Upload(MemoryStream imageStream,string boxId)
    {
        imageStream.Position = 0;
        using var form = new MultipartFormDataContent();

        using var content = new StreamContent(imageStream);
        form.Add(content, "file", Path.GetFileName("image.jpg"));


        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        var response = await _httpClient.PutAsync(_apiUrl + "/Image/upload/"+Uri.EscapeDataString(boxId), form).ConfigureAwait(false);
        return response;
    }

    public async Task<List<ItemSimpleDto>> SearchItems(SearchRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync(_apiUrl + "/Data/FindItems", content).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var items = JsonSerializer.Deserialize<List<ItemSimpleDto>>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                return items ?? new List<ItemSimpleDto>();
            }
            
            return new List<ItemSimpleDto>();
        }
        catch (Exception)
        {
            return new List<ItemSimpleDto>();
        }
    }

    public async Task<ImageSource?> GetImage(string blobId, bool thumbnail = false)
    {
        try
        {
            var url = $"{_apiUrl}/Data/Image/{Uri.EscapeDataString(blobId)}" + (thumbnail ? "?thumb=true" : "");
            var response = await _httpClient.GetAsync(url).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                var imageStream = await response.Content.ReadAsStreamAsync();
                imageStream.Position = 0;
                return ImageSource.FromStream(() => imageStream);
            }
        }
        catch (Exception)
        {
            // Handle exceptions as needed
        }
        return null;
    }
}
