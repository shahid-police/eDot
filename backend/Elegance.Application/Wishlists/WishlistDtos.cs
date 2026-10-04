using System.ComponentModel.DataAnnotations;

namespace Elegance.Application.Wishlists;

public class WishlistDto
{
    public required IReadOnlyList<WishlistItemDto> Items { get; init; }
    public required int Count { get; init; }
}

public class WishlistItemDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public long? VariantId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string Sku { get; init; }
    public string? VariantName { get; init; }
    public string? ImageUrl { get; init; }
    public required decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public required string Currency { get; init; }
    public required bool InStock { get; init; }
    public required DateTime AddedAt { get; init; }
}

public class AddWishlistItemRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; init; }

    public long? VariantId { get; init; }
}
