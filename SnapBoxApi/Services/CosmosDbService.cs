using Microsoft.Azure.Cosmos;
using SnapBoxApi.Model;
using System.Collections.ObjectModel;
using System.Net;


namespace SnapBoxApi.Services
{
    public class CosmosDbService
    {
        const string MyPartitionKey = "item";
        private readonly Microsoft.Azure.Cosmos.Container _container;

        public CosmosDbService(IConfiguration configuration)
        {
            var endpoint = configuration["CosmosDb:Endpoint"];
            var key = configuration["CosmosDb:Key"];
            var databaseName = configuration["CosmosDb:DatabaseName"];
            var containerName = "Items";

            var cosmosClient = new CosmosClient(endpoint, key);

            // Ensure database exists
            var databaseResponse = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName).GetAwaiter().GetResult();
            var database = databaseResponse.Database;

            List<Embedding> embeddings = new List<Embedding>()
              {
                  new Embedding()
                  {
                      Path = "/TitleEmbedding",
                      DataType = VectorDataType.Float32,
                      DistanceFunction = DistanceFunction.Cosine,
                      Dimensions = 1536,
                  },
                  new Embedding()
                  {
                      Path = "/CategoryEmbedding",
                      DataType = VectorDataType.Float32,
                      DistanceFunction = DistanceFunction.Cosine,
                      Dimensions = 1536,
                  },
                  new Embedding()
                  {
                      Path = "/DetailedDescriptionEmbedding",
                      DataType = VectorDataType.Float32,
                      DistanceFunction = DistanceFunction.Cosine,
                      Dimensions = 1536,
                  },
                  new Embedding()
                  {
                      Path = "/FullTextEmbedding",
                      DataType = VectorDataType.Float32,
                      DistanceFunction = DistanceFunction.Cosine,
                      Dimensions = 1536,
                  }
              };
            Collection<Embedding> collection = new Collection<Embedding>(embeddings);
            // Ensure container exists (partition key path must match your ItemDto, e.g. "/partitionKey")

            var containerProperties = new ContainerProperties
            {
                Id = containerName,
                PartitionKeyPath = "/PartitionKey",
                VectorEmbeddingPolicy = new(collection),
                IndexingPolicy = new IndexingPolicy()
                {
                    VectorIndexes = new()
                        {
                            new VectorIndexPath()
                            {
                                Path = "/TitleEmbedding",
                                Type = VectorIndexType.QuantizedFlat,
                            },
                            new VectorIndexPath()
                            {
                                Path = "/CategoryEmbedding",
                                Type = VectorIndexType.QuantizedFlat,
                            },
                            new VectorIndexPath()
                            {
                                Path = "/DetailedDescriptionEmbedding",
                                Type = VectorIndexType.QuantizedFlat,
                            },
                             new VectorIndexPath()
                            {
                                Path = "/FullTextEmbedding",
                                Type = VectorIndexType.QuantizedFlat,
                            }
                        },

                },
            };
            containerProperties.IndexingPolicy.IncludedPaths.Add(new IncludedPath { Path = "/*" });
            containerProperties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/TitleEmbedding/*" });
            containerProperties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/CategoryEmbedding/*" });
            containerProperties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/DetailedDescriptionEmbedding/*" });
            containerProperties.IndexingPolicy.ExcludedPaths.Add(new ExcludedPath { Path = "/FullTextEmbedding/*" });

            var containerResponse = database.CreateContainerIfNotExistsAsync(
                containerProperties).GetAwaiter().GetResult();

            _container = containerResponse.Container;
        }

        public async Task AddItemAsync(ItemDto data)
        {
            try
            {

                    data.PartitionKey = MyPartitionKey;

                await _container.CreateItemAsync(data, new PartitionKey(data.PartitionKey));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
            {
                throw new InvalidOperationException($"Document with ID {data.id} already exists.");
            }
        }


        /// <summary>
        /// Sample method for querying entries.
        /// Example: Queries entries where Description contains the specified keyword.
        /// Uses parameterized SQL to avoid injection.
        /// </summary>
        public async Task<IEnumerable<ItemDto>> QueryImageEntriesAsync(string keyword)
        {
            var queryText = "SELECT * FROM c WHERE CONTAINS(c.Description, @keyword, true)";
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@keyword", keyword);

            var iterator = _container.GetItemQueryIterator<ItemDto>(queryDefinition);

            var results = new List<ItemDto>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<List<ItemDto>> QueryTopByFullTextEmbeddingAsync(float[] queryVector, int topN = 1)
        {
            // Cosmos DB expects the vector as a JSON array
            var query = $@"
            SELECT TOP {topN} *
            FROM c
            ORDER BY VectorDistance(c.FullTextEmbedding, @query_vector, false, {{'distanceFunction':'cosine', 'dataType':'float32'}})
    ";

            var queryDefinition = new QueryDefinition(query)
                .WithParameter("@query_vector", queryVector);

            var iterator = _container.GetItemQueryIterator<ItemDto>(queryDefinition);

            var results = new List<ItemDto>();
            while (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task DeleteItemAsync(string itemId)
        {
            try
            {
                await _container.DeleteItemAsync<ItemDto>(itemId, new PartitionKey(MyPartitionKey));
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException($"Item with ID {itemId} not found.");
            }
        }

        public async Task<ItemDto?> GetItemAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<ItemDto>(id, new PartitionKey(MyPartitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
   
    }
}
