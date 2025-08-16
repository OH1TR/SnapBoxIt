using System;

namespace SnapBoxApp.Model;

    public class ItemSimpleDto
    {
        public string id { get; set; }
        public string Type { get; set; }
        public string BlobId { get; set; }
        public string BoxId { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string DetailedDescription { get; set; }
        public List<string> Colors { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
