using System.ComponentModel.DataAnnotations;

namespace Elegance.Application.Compare;

public class CompareDto
{
    public const int MaxItems = 4;

    public required IReadOnlyList<CompareItemDto> Items { get; init; }
    public required int Count { get; init; }
    public int Max => MaxItems;
}

public class CompareItemDto
{
    public required long Id { get; init; }
    public required long ProductId { get; init; }
    public required string Name { get; init; }
    public required string Slug { get; init; }
    public required string Sku { get; init; }
    public required string CategoryName { get; init; }
    public string? ImageUrl { get; init; }
    public required decimal Price { get; init; }
    public decimal? SalePrice { get; init; }
    public required string Currency { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public required bool InStock { get; init; }
}

public class AddCompareItemRequest
{
    [Range(1, long.MaxValue)]
    public long ProductId { get; init; }
}
