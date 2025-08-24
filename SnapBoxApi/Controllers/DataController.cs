using Azure.Storage.Blobs;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SnapBoxApi.Model;
using SnapBoxApi.Services;
using Microsoft.Extensions.Configuration;

namespace SnapBoxApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly DataService _dataService;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly CosmosDbService _cosmosDbService;
        private readonly ImageDescriptionService _imageDescriptionService;
        private readonly IConfiguration _configuration;

        public DataController(DataService dataService, BlobServiceClient blobServiceClient, CosmosDbService cosmosDbService, ImageDescriptionService imageDescriptionService, IConfiguration configuration)
        {
            _dataService = dataService;
            _blobServiceClient = blobServiceClient;
            _cosmosDbService = cosmosDbService;
            _imageDescriptionService = imageDescriptionService;
            _configuration = configuration;
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

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(string id)
        {
            try
            {
                var result = await _dataService.DeleteItem(id);
                if (result)
                {
                    return Ok("Item deleted successfully.");
                }
                return NotFound("Item not found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("Save/{id}")]
        public async Task<ActionResult<ItemDto>> SaveItem(string id, [FromBody] ItemSimpleDto itemDto)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest("Item ID is required.");
                }

                if (itemDto == null)
                {
                    return BadRequest("Item data is required.");
                }

                float[] userDescriptionEmbedding = null;

                if (itemDto.UserDescription != null)
                {
                    userDescriptionEmbedding = await _imageDescriptionService.GetEmbeddingAsync(itemDto.UserDescription);
                }
                var updatedItem = await _cosmosDbService.UpdateItemAsync(id, itemDto,userDescriptionEmbedding);
                return Ok(updatedItem);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPost("PrintLabel")]
        public async Task<IActionResult> PrintLabel([FromBody] PrintLabelRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Type) || string.IsNullOrEmpty(request.Text))
                {
                    return BadRequest("Type and Text are required.");
                }

                var connectionString = _configuration["ServiceBus:ConnectionString"];
                var queueName = _configuration["ServiceBus:QueueName"] ?? "PrintJob";

                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, "Service Bus connection string not configured.");
                }

                // Create message in the format expected by LabelPrintingAgent: "type|text"
                var messageBody = $"{request.Type}|{request.Text}";

                // Send message to Azure Service Bus
                await using var client = new ServiceBusClient(connectionString);
                await using var sender = client.CreateSender(queueName);
                
                var message = new ServiceBusMessage(messageBody);
                await sender.SendMessageAsync(message);

                return Ok(new { message = "Print job sent successfully", type = request.Type, text = request.Text });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending print job: {ex.Message}");
            }
        }


    }
}