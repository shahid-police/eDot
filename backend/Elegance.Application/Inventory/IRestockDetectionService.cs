namespace Elegance.Application.Inventory;

// Called from every code path that changes Product/ProductVariant.StockQuantity, right before
// that caller's own SaveChangesAsync - so the RestockEvent row (if any) commits in the exact same
// transaction as the stock change itself. Internally a no-op unless this is a genuine
// zero-to-positive transition (never fires for e.g. 5 -> 10), so callers don't need to duplicate
// that check themselves.
public interface IRestockDetectionService
{
    void RecordStockChange(long productId, long? variantId, int previousStock, int newStock);
}
