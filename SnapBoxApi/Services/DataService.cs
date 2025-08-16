using SnapBoxApi.Model;
using SnapBoxApi.Services;

namespace SnapBoxApi.Services
{
    public class DataService
    {
        private readonly CosmosDbService _cosmosDbService;
        private readonly ImageDescriptionService _imageDescriptionService;

        public DataService(CosmosDbService cosmosDbService, ImageDescriptionService imageDescriptionService)
        {
            _cosmosDbService = cosmosDbService;
            _imageDescriptionService = imageDescriptionService;
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
    }
}
