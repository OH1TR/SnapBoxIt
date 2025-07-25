using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OpenAI;
using OpenAI.Chat;
using SnapBoxApi.Model;
using SnapBoxApi.Services;
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

        public ImageController(BlobServiceClient blobServiceClient, CosmosDbService cosmosService, OpenAIClient openAIClient)
        {
            _blobServiceClient = blobServiceClient;
            _cosmosService=cosmosService;
            _openAIClient = openAIClient;
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



            // Tallenna MongoDB:hen
            var imageData = new ImageData { BlobId = blobId, Description = "" };
            await _cosmosService.AddImageEntryAsync(imageData);

            return Ok(imageData);
        }
    }
}