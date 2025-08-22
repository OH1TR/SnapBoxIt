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

        public ImageController(BlobServiceClient blobServiceClient, CosmosDbService cosmosService, OpenAIClient openAIClient, ImageDescriptionService imageDescriptionService)
        {
            _blobServiceClient = blobServiceClient;
            _cosmosService = cosmosService;
            _openAIClient = openAIClient;
            _imageDescriptionService = imageDescriptionService;
        }

        [HttpPut("upload/{boxId}")]
        public async Task<IActionResult> UploadImage(string boxId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var blobId = $"{Guid.NewGuid()}";

            using var originalStream = new MemoryStream();
            await file.CopyToAsync(originalStream);

            var upTask = UploadImageToBlobAsync(originalStream, blobId, file.ContentType);

            // Kuvan analyysi
            originalStream.Position = 0;
            var description = await _imageDescriptionService.GetImageDescriptionAsync(originalStream);
            description.BlobId = blobId;
            description.BoxId = boxId;
            description.Count = 1;
            var dbTask = _cosmosService.AddItemAsync(description);
            Task.WaitAll(upTask, dbTask);

            return Ok(description.ToSimpleDto());
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
                var newHeight = (int)(image.Height * 0.01 * Tools.ThumbPercent);
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