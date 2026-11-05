namespace Elegance.Application.Search;

// Structured filters extracted from a customer's free-text search query. Only ever populated
// from a value the customer actually implied - never invented - and every field is validated
// against real data (category slugs, gender enum) before it reaches the database query.
public class ProductSearchSpec
{
    public string? CategorySlug { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Material { get; set; }
    public string? Color { get; set; }
    public string? Occasion { get; set; }
    public string? Gender { get; set; }
    public List<string> Keywords { get; set; } = [];
}
