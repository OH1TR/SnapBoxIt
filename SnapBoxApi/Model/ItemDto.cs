namespace SnapBoxApi.Model
{
    public class ItemDto
    {
        public string id { get; set; } = Guid.NewGuid().ToString();

        public string Type { get; set; } = "component";

        public string PartitionKey { get; set; } = "item";

        public string BlobId { get; set; }

        public string BoxId { get; set; }

        public string Title { get; set; } = default!;

        public float[] TitleEmbedding { get; set; } = Array.Empty<float>();

        public string Category { get; set; } = default!;

        public float[] CategoryEmbedding { get; set; } = Array.Empty<float>();

        public string DetailedDescription { get; set; } = default!;

        public float[] DetailedDescriptionEmbedding { get; set; } = Array.Empty<float>();

        public List<string> Colors { get; set; } = new();

        public List<float[]> ColorsEmbeddings { get; set; } = new();

        public float[]? FullTextEmbedding { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public DateTimeOffset? UpdatedAt { get; set; }

        public float Count { get; set; } = default!;

        public string? UserDescription { get; set; } = default!;

        public float[] UserDescriptionEmbedding { get; set; } = Array.Empty<float>();

    }
}
