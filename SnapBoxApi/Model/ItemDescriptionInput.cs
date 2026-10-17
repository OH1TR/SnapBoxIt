namespace SnapBoxApi.Model
{
    /// <summary>
    /// Optional description sent with an image upload. When Title is set, the server
    /// skips its own image analysis and stores these values instead.
    /// </summary>
    public class ItemDescriptionInput
    {
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? DetailedDescription { get; set; }
        public List<string>? Colors { get; set; }
        public string? UserDescription { get; set; }
        public float? Count { get; set; }
    }
}
