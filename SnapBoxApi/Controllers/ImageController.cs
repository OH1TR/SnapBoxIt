using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenAI;
using OpenAI.Chat;
using SnapBoxApi.Model;
using SnapBoxApi.Services;
using System.IO;
using System.Text;

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
            _cosmosService=cosmosService;
            _openAIClient = openAIClient;
            _imageDescriptionService = imageDescriptionService;
        }

        [HttpPut("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            // Tallenna Azure Blob Storageen
            var containerClient = _blobServiceClient.GetBlobContainerClient("images"); // Kontin nimi
            var blobId = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var blobClient = containerClient.GetBlobClient(blobId);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }
      
            var ms = new MemoryStream();
            using (var stream = file.OpenReadStream())
            {
                stream.CopyTo(ms);
            }
           
            ms.Position = 0;

            var description = await _imageDescriptionService.GetImageDescriptionAsync(ms);
            description.BlobId = blobId;
            await _cosmosService.AddItemAsync(description);    

            return Ok(description);
        }
    }
}