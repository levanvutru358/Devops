namespace EmoApi.Models;

public record Category(long Id, string Name);
public record Product(long Id, string Name, string? Description, decimal Price, long CategoryId, int Stock, string? ImageUrl);

