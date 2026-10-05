namespace Elegance.Domain.Inventory;

public class StockMovement
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long? VariantId { get; set; }
    public int QuantityDelta { get; set; }
    public StockMovementReason Reason { get; set; }
    public long? OrderId { get; set; }
    public string? Note { get; set; }
    public long? AdminUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}
