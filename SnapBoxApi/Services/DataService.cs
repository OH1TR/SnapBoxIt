using Azure.Storage.Blobs;
using SnapBoxApi.Model;
using SnapBoxApi.Services;

namespace SnapBoxApi.Services
{
    public class DataService
    {
        private readonly CosmosDbService _cosmosDbService;
        private readonly ImageDescriptionService _imageDescriptionService;
        private readonly BlobServiceClient _blobServiceClient;

        public DataService(CosmosDbService cosmosDbService, ImageDescriptionService imageDescriptionService, BlobServiceClient blobServiceClient)
        {
            _cosmosDbService = cosmosDbService;
            _imageDescriptionService = imageDescriptionService;
            _blobServiceClient = blobServiceClient;
        }

        public async Task<List<ItemSimpleDto>> FindItems(string searchText, int count = 5)
        {
            // Get embedding for the search text
            var searchVector = await _imageDescriptionService.GetEmbeddingAsync(searchText);

            // Find top matches using vector search with specified count
            var items = await _cosmosDbService.QueryTopByFullTextEmbeddingAsync(searchVector, count);

            // Convert to ItemSimpleDto
            var result = items.Select(item => new ItemSimpleDto
            {
                id = item.id,
                Type = item.Type,
                BlobId = item.BlobId,
                BoxId = item.BoxId,
                Title = item.Title,
                Category = item.Category,
                DetailedDescription = item.DetailedDescription,
                Colors = item.Colors,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            }).ToList();

            return result;
        }

        public async Task<bool> DeleteItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Item ID cannot be null or empty.", nameof(itemId));

            var item = await _cosmosDbService.GetItemAsync(itemId);

            item.BlobId = item?.BlobId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(item.BlobId))
                throw new InvalidOperationException("Item does not have a valid BlobId.");

            // Delete the image blob if it exists
            var containerClient = _blobServiceClient.GetBlobContainerClient("images");

            foreach (var id in new[] { item.BlobId, $"{item.BlobId}_thumb{Tools.ThumbPercent}" })
            {
                if (string.IsNullOrWhiteSpace(id))
                    continue;

                var blobClient = containerClient.GetBlobClient(id);
                await blobClient.DeleteIfExistsAsync();
            }

            // Delete the item from Cosmos DB
            await _cosmosDbService.DeleteItemAsync(itemId);
            return true;
        }
    }
}
