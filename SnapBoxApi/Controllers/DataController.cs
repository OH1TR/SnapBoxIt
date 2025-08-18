using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapBoxApi.Model;
using SnapBoxApi.Services;

namespace SnapBoxApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly DataService _dataService;
        private readonly BlobServiceClient _blobServiceClient;

        public DataController(DataService dataService, BlobServiceClient blobServiceClient)
        {
            _dataService = dataService;
            _blobServiceClient = blobServiceClient;
        }

        [HttpPost("FindItems")]
        public async Task<ActionResult<List<ItemSimpleDto>>> FindItems([FromBody] SearchRequest request)
        {
            try
            {
                if (request.Count > 10)
                    request.Count = 10;
                var items = await _dataService.FindItems(request.Query, request.Count);
                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("Image/{blobId}")]
        public async Task<IActionResult> GetImage(string blobId, [FromQuery] bool thumb = false)
        {
            try
            {
                var containerClient = _blobServiceClient.GetBlobContainerClient("images");

                string blobName = thumb ? $"{blobId}_thumb{Tools.ThumbPercent}" : blobId;

                var blobClient = containerClient.GetBlobClient(blobName);

                if (!await blobClient.ExistsAsync())
                {
                    return NotFound("Image not found.");
                }

                var response = await blobClient.DownloadAsync();
                var imageStream = response.Value.Content;

                return File(imageStream, response.Value.ContentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving image: {ex.Message}");
            }
        }
    }
}