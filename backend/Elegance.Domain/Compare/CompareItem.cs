using Elegance.Domain.Catalog;

namespace Elegance.Domain.Compare;

public class CompareItem
{
    public long Id { get; set; }

    public long CompareListId { get; set; }
    public CompareList CompareList { get; set; } = null!;

    public long ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public DateTime AddedAt { get; set; }
}
