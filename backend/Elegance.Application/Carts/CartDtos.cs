using System.ComponentModel.DataAnnotations;

namespace Elegance.Application.Carts;

public class CartDto
{
    public required IReadOnlyList<CartItemDto> Items { get; init; }
    public required int ItemCount { get; init; }
    public required decimal Subtotal { get; init; }
    public required string Currency { get; init; }

    public static CartDto Empty(string currency) => new()
    {
        Items = [],
        ItemCount = 0,
        Subtotal = 0m,
        Currency = currency,
    };
}

public class CartItemDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public long? VariantId { get; init; }
    public required string ProductName { get; init; }
    public required string ProductSlug { get; init; }
    public required string Sku { get; init; }
    public string? VariantName { get; init; }
    public string? ImageUrl { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal LineTotal { get; init; }
    public required int Quantity { get; init; }
    public required int AvailableQuantity { get; init; }
    public required bool IsAvailable { get; init; }
}

public class AddCartItemRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; init; }

    public long? VariantId { get; init; }

    [Range(1, 99)]
    public int Quantity { get; init; } = 1;
}

public class UpdateCartItemRequest
{
    [Range(1, 99)]
    public int Quantity { get; init; }
}
