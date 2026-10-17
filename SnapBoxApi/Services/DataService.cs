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

        public async Task<List<ItemSimpleDto>> FindItems(string searchText, int count = 5, bool useVectorSearch = true)
        {
            // Substring matches first: vector search cannot tell component codes like "BC817" apart.
            // Cosmos DB full-text search doesn't support Finnish, but CONTAINS is plain substring matching.
            var items = await _cosmosDbService.QueryByTextAsync(searchText, count);

            if (useVectorSearch && items.Count < count)
            {
                // Fill the rest with vector search - works excellently with Finnish language
                var searchVector = await _imageDescriptionService.GetEmbeddingAsync(searchText);
                var vectorItems = await _cosmosDbService.QueryTopByFullTextEmbeddingAsync(searchVector, count);
                var found = items.Select(i => i.id).ToHashSet();
                items.AddRange(vectorItems.Where(i => !found.Contains(i.id)).Take(count - items.Count));
            }

            return items.Select(item => item.ToSimpleDto()).ToList();
        }

        public async Task<bool> DeleteItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                throw new ArgumentException("Item ID cannot be null or empty.", nameof(itemId));

            var item = await _cosmosDbService.GetItemAsync(itemId);
            if (item == null)
                return false;

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
