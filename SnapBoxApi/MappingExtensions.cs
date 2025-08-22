using SnapBoxApi.Model;

public static class MappingExtensions
{
    public static ItemSimpleDto ToSimpleDto(this ItemDto item)
    {
        return new ItemSimpleDto
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
            UpdatedAt = item.UpdatedAt,
            Count = item.Count,
            UserDescription = item.UserDescription
        };
    }
}