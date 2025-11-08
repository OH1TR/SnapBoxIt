using Microsoft.AspNetCore.Mvc;
using OpenAI;
using System.Text.Json;

namespace SnapBoxApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RealtimeController : ControllerBase
{
    private readonly OpenAIClient _openAiClient;
    private readonly IConfiguration _configuration;

    public RealtimeController(OpenAIClient openAiClient, IConfiguration configuration)
    {
        _openAiClient = openAiClient;
        _configuration = configuration;
    }

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest? request)
    {
        try
        {
            var apiKey = _configuration["OpenAI:ApiKeyRealtime"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return StatusCode(500, new { error = "OpenAI API key not configured" });
            }

            // Create ephemeral token request
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "SnapBoxIt/1.0");

            var requestBody = new
            {
                model = request?.Model ?? "gpt-4o-realtime-preview-2024-12-17",
                voice = request?.Voice ?? "alloy",
                instructions = request?.Instructions ?? "You are a helpful assistant for the SnapBox inventory management system. Help users manage their inventory, search for items, and organize their storage boxes."
            };

            var content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync(
                "https://api.openai.com/v1/realtime/sessions",
                content
            );

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, new { error = errorContent });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var sessionData = JsonSerializer.Deserialize<JsonElement>(responseContent);

            return Ok(new
            {
                client_secret = sessionData.GetProperty("client_secret").GetProperty("value").GetString(),
                expires_at = sessionData.GetProperty("client_secret").GetProperty("expires_at").GetInt64(),
                model = sessionData.GetProperty("model").GetString(),
                modalities = sessionData.GetProperty("modalities").EnumerateArray().Select(x => x.GetString()).ToArray(),
                voice = sessionData.GetProperty("voice").GetString()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class CreateSessionRequest
{
    public string? Model { get; set; }
    public string? Voice { get; set; }
    public string? Instructions { get; set; }
}
