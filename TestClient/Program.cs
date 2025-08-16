using System.Text;
using Microsoft.Extensions.Configuration;

namespace TestClient
{
    internal class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static IConfiguration? configuration;
        
        static async Task Main(string[] args)
        {
            // Build configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddUserSecrets<Program>();
            
            configuration = builder.Build();
            
            // Set the base URL for your API
            string baseUrl = "http://localhost:5160"; // Adjust port as needed
            //string baseUrl = "https://snapboxit.azurewebsites.net"; // Adjust port as needed
            string imagePath = "pr.jpg"; // Path to your test image
            
            try
            {
               // await UploadImageAsync(baseUrl, imagePath);
                
                // Also test the FindItems query
                Console.WriteLine("\n--- Testing FindItems Query ---");
                await QueryFindItemsAsync(baseUrl, args[0], 3);

                // Test GetImageAsync
                Console.WriteLine("\n--- Testing GetImageAsync ---");
                string blobId = "c4885d00-39f0-499d-bc83-7a9a647aab36"; // Replace with a valid blob ID from your test data
                await GetImageAsync(baseUrl, blobId,true);
                
                // Test GetImageAsync with thumbnail
                Console.WriteLine("\n--- Testing GetImageAsync with Thumbnail ---");
                //await GetImageAsync(baseUrl, blobId, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
        
        static async Task UploadImageAsync(string baseUrl, string imagePath)
        {
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"Image file not found: {imagePath}");
                Console.WriteLine("Please place a test image file in the TestClient directory or update the path.");
                return;
            }
            
            // Add Basic Authentication header from configuration
            string? username = configuration?["BasicAuth:Username"];
            string? password = configuration?["BasicAuth:Password"];
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Error: Username or password not found in configuration.");
                Console.WriteLine("Please set user secrets or update appsettings.json:");
                Console.WriteLine("  dotnet user-secrets set \"BasicAuth:Username\" \"your-username\"");
                Console.WriteLine("  dotnet user-secrets set \"BasicAuth:Password\" \"your-password\"");
                return;
            }
            
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            
            // Provide feedback about credential source (without revealing the password)
            Console.WriteLine($"Using authentication for user: {username}");
            Console.WriteLine("Credentials loaded from configuration (user secrets take priority over appsettings.json)");
            
            using var form = new MultipartFormDataContent();
            using var fileStream = File.OpenRead(imagePath);
            using var streamContent = new StreamContent(fileStream);
            
            // Set content type based on file extension
            string contentType = Path.GetExtension(imagePath).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
            
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(streamContent, "file", Path.GetFileName(imagePath));
            
            string url = $"{baseUrl}/api/Image/upload";
            Console.WriteLine($"Uploading image to: {url}");
            
            var response = await httpClient.PutAsync(url, form);
            
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Upload successful!");
                Console.WriteLine($"Response: {responseContent}");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Upload failed with status: {response.StatusCode}");
                Console.WriteLine($"Error: {errorContent}");
            }
        }

        static async Task QueryFindItemsAsync(string baseUrl, string searchQuery, int count = 5)
        {
            // Add Basic Authentication header from configuration
            string? username = configuration?["BasicAuth:Username"];
            string? password = configuration?["BasicAuth:Password"];
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Error: Username or password not found in configuration.");
                return;
            }
            
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            
            // Create the search request JSON
            var searchRequest = new
            {
                Query = searchQuery,
                Count = count
            };
            
            var jsonContent = System.Text.Json.JsonSerializer.Serialize(searchRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            
            string url = $"{baseUrl}/api/Data/FindItems";
            Console.WriteLine($"Querying FindItems at: {url}");
            Console.WriteLine($"Search query: '{searchQuery}' (max results: {count})");
            
            var response = await httpClient.PostAsync(url, content);
            
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Query successful!");
                Console.WriteLine($"Response: {responseContent}");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Query failed with status: {response.StatusCode}");
                Console.WriteLine($"Error: {errorContent}");
            }
        }

        static async Task GetImageAsync(string baseUrl, string blobId, bool thumbnail = false)
        {
            // Add Basic Authentication header from configuration
            string? username = configuration?["BasicAuth:Username"];
            string? password = configuration?["BasicAuth:Password"];
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Error: Username or password not found in configuration.");
                return;
            }
            
            string credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            
            string url = $"{baseUrl}/api/Data/Image/{blobId}";
            if (thumbnail)
            {
                url += "?thumb=true";
            }
            
            Console.WriteLine($"Getting image from: {url}");
            Console.WriteLine($"Blob ID: {blobId}, Thumbnail: {thumbnail}");
            
            var response = await httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                // Get the image content
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                string contentType = response.Content.Headers.ContentType?.ToString() ?? "unknown";
                
                Console.WriteLine("Image retrieved successfully!");
                Console.WriteLine($"Content-Type: {contentType}");
                Console.WriteLine($"Image size: {imageBytes.Length} bytes");
                
                // Optionally save the image to a file
                string fileName = thumbnail ? $"thumb_{blobId}" : blobId;
                string extension = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png" => ".png",
                    "image/gif" => ".gif",
                    "image/bmp" => ".bmp",
                    _ => ".bin"
                };
                
                string outputPath = Path.Combine(Directory.GetCurrentDirectory(), $"{fileName}{extension}");
                await File.WriteAllBytesAsync(outputPath, imageBytes);
                Console.WriteLine($"Image saved to: {outputPath}");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to retrieve image with status: {response.StatusCode}");
                Console.WriteLine($"Error: {errorContent}");
            }
        }
    }
}
