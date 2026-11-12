using Elegance.Application.Inventory;
using Elegance.Domain.Inventory;
using Elegance.Infrastructure.Persistence;

namespace Elegance.Infrastructure.Inventory;

public class RestockDetectionService(EleganceDbContext dbContext) : IRestockDetectionService
{
    public void RecordStockChange(long productId, long? variantId, int previousStock, int newStock)
    {
        // Strictly 0 -> positive, not just "went up" (e.g. 5 -> 10 never fires this) and not
        // "already positive and stayed positive".
        if (previousStock > 0 || newStock <= 0)
        {
            return;
        }

        dbContext.RestockEvents.Add(new RestockEvent
        {
            ProductId = productId,
            VariantId = variantId,
            PreviousStock = previousStock,
            NewStock = newStock,
            DetectedAt = DateTime.UtcNow,
        });
    }
}
