using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenAI;
using OpenAI.Chat;
using SnapBoxApi.Model;
using SnapBoxApi.Services;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace SnapBoxApi.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosDbService _cosmosService;
        private readonly OpenAIClient _openAIClient;
        private readonly ImageDescriptionService _imageDescriptionService;
        private readonly ILogger<ImageController> _logger;

        public ImageController(
            BlobServiceClient blobServiceClient, 
            CosmosDbService cosmosService, 
            OpenAIClient openAIClient, 
            ImageDescriptionService imageDescriptionService,
            ILogger<ImageController> logger)
        {
            _blobServiceClient = blobServiceClient;
            _cosmosService = cosmosService;
            _openAIClient = openAIClient;
            _imageDescriptionService = imageDescriptionService;
            _logger = logger;
        }

        [HttpPut("upload/{boxId}")]
        public async Task<IActionResult> UploadImage(string boxId, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("Upload attempted with no file");
                    return BadRequest("No file uploaded.");
                }

                if (string.IsNullOrWhiteSpace(boxId))
                {
                    _logger.LogWarning("Upload attempted with empty boxId");
                    return BadRequest("Box ID is required.");
                }

                _logger.LogInformation($"Starting upload for box: {boxId}, file size: {file.Length} bytes, content type: {file.ContentType}");

                var blobId = $"{Guid.NewGuid()}";

                using var originalStream = new MemoryStream();
                await file.CopyToAsync(originalStream);
                _logger.LogInformation($"Copied file to memory stream, stream length: {originalStream.Length} bytes");
                
                originalStream.Position = 0;

                // Upload to blob storage first
                _logger.LogInformation($"Starting blob upload for {blobId}");
                await UploadImageToBlobAsync(originalStream, blobId, file.ContentType);
                _logger.LogInformation($"Blob upload completed for {blobId}");

                // Kuvan analyysi - reset position before analysis
                originalStream.Position = 0;
                _logger.LogInformation($"Starting image analysis for {blobId}, stream position: {originalStream.Position}, length: {originalStream.Length}");
                var description = await _imageDescriptionService.GetImageDescriptionAsync(originalStream, file.ContentType);
                description.BlobId = blobId;
                description.BoxId = boxId;
                description.Count = 1;
                
                _logger.LogInformation($"Image analysis completed, saving to database");
                await _cosmosService.AddItemAsync(description);

                _logger.LogInformation($"Successfully uploaded image {blobId} for box {boxId}");
                return Ok(description.ToSimpleDto());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error uploading image for box {boxId}: {ex.Message}");
                return StatusCode(500, $"Error uploading image: {ex.Message}");
            }
        }
        
        private async Task UploadImageToBlobAsync(MemoryStream originalStream, string blobId, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("images");

            originalStream.Position = 0;
            await UploadToBlobAsync(containerClient, blobId, originalStream, contentType);

            originalStream.Position = 0;
            using (var image = await Image.LoadAsync(originalStream))
            {
                var newWidth = (int)(image.Width * 0.01 * Tools.ThumbPercent);
                var newHeight = (int)(image.Height * 0.01 * Tools. ThumbPercent);
                image.Mutate(x => x.Resize(newWidth, newHeight));
                using var thumbStream = new MemoryStream();
                await image.SaveAsync(thumbStream, new JpegEncoder());
                thumbStream.Position = 0;
                var thumbBlobId = $"{blobId}_thumb{Tools.ThumbPercent}";
                await UploadToBlobAsync(containerClient, thumbBlobId, thumbStream, "image/jpeg");
            }
        }

        private async Task UploadToBlobAsync(BlobContainerClient containerClient, string blobId, Stream stream, string contentType)
        {
            stream.Position = 0;
            var blobClient = containerClient.GetBlobClient(blobId);
            await blobClient.UploadAsync(stream, overwrite: true);
            await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType });
        }
    }
}