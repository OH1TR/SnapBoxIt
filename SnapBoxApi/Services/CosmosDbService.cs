using SnapBoxApi.Model;
using System.Net;
using Microsoft.Azure.Cosmos;


namespace SnapBoxApi.Services
{
    public class CosmosDbService
    {
        private readonly Container _container;

        public CosmosDbService(IConfiguration configuration)
        {
            var endpoint = configuration["CosmosDb:Endpoint"];
            var key = configuration["CosmosDb:Key"];
            var databaseName = configuration["CosmosDb:DatabaseName"];
            var containerName = configuration["CosmosDb:ContainerName"];

            var cosmosClient = new CosmosClient(endpoint, key);
            var database = cosmosClient.GetDatabase(databaseName);
            _container = database.GetContainer(containerName);
        }

        /// <summary>
        /// Adds an image entry (metadata) to Cosmos DB.
        /// Sets Id = BlobId for simplicity.
        /// </summary>
        public async Task AddImageEntryAsync(ImageData data)
        {
            if (string.IsNullOrEmpty(data.BlobId))
                throw new ArgumentException("BlobId is required.");

            data.Id = data.BlobId; // Use BlobId as the document ID

            try
            {
                await _container.CreateItemAsync(data, new PartitionKey(data.BlobId));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException($"Document with ID {data.Id} already exists.");
            }
        }

        /// <summary>
        /// Retrieves an image entry by BlobId.
        /// </summary>
        public async Task<ImageData?> GetImageEntryAsync(string blobId)
        {
            try
            {
                var response = await _container.ReadItemAsync<ImageData>(blobId, new PartitionKey(blobId));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Sample method for querying entries.
        /// Example: Queries entries where Description contains the specified keyword.
        /// Uses parameterized SQL to avoid injection.
        /// </summary>
        public async Task<IEnumerable<ImageData>> QueryImageEntriesAsync(string keyword)
        {
            var queryText = "SELECT * FROM c WHERE CONTAINS(c.Description, @keyword, true)";
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@keyword", keyword);

            var iterator = _container.GetItemQueryIterator<ImageData>(queryDefinition);

            var results = new List<ImageData>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }
    }
}
