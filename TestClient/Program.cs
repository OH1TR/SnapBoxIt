using System.Text;

namespace TestClient
{
    internal class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        
        static async Task Main(string[] args)
        {
            // Set the base URL for your API
            string baseUrl = "http://localhost:5160"; // Adjust port as needed
            string imagePath = "Prototype_image.png"; // Path to your test image
            
            try
            {
                await UploadImageAsync(baseUrl, imagePath);
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
    }
}
