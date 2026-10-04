using Elegance.Application.Common;
using Elegance.Application.Compare;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Compare;
using Elegance.Infrastructure.Auth;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Compare;

public class CompareService(EleganceDbContext dbContext) : ICompareService
{
    public async Task<CompareDto> GetCompareAsync(ListOwner owner, CancellationToken cancellationToken = default)
    {
        var list = await FindListAsync(owner, cancellationToken);
        return list is null ? new CompareDto { Items = [], Count = 0 } : await BuildDtoAsync(list.Id, cancellationToken);
    }

    public async Task<CompareDto> AddItemAsync(ListOwner owner, AddCompareItemRequest request, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "This product is not available.");

        var list = await FindListAsync(owner, cancellationToken)
            ?? await CreateListAsync(owner, cancellationToken);

        var alreadyAdded = await dbContext.CompareItems
            .AnyAsync(i => i.CompareListId == list.Id && i.ProductId == product.Id, cancellationToken);

        if (!alreadyAdded)
        {
            await EnsureRoomAsync(list.Id, cancellationToken);
            dbContext.CompareItems.Add(new CompareItem
            {
                CompareListId = list.Id,
                ProductId = product.Id,
                AddedAt = DateTime.UtcNow,
            });
            list.UpdatedAt = DateTime.UtcNow;
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                dbContext.ChangeTracker.Clear();
            }
        }

        return await BuildDtoAsync(list.Id, cancellationToken);
    }

    public async Task<CompareDto> RemoveItemAsync(ListOwner owner, long itemId, CancellationToken cancellationToken = default)
    {
        var list = await FindListAsync(owner, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested comparison item was not found.");

        var item = await dbContext.CompareItems
            .FirstOrDefaultAsync(i => i.Id == itemId && i.CompareListId == list.Id, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.NotFound, "The requested comparison item was not found.");

        dbContext.CompareItems.Remove(item);
        list.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await BuildDtoAsync(list.Id, cancellationToken);
    }

    public async Task MergeGuestCompareAsync(long customerId, string guestToken, CancellationToken cancellationToken = default)
    {
        var guestList = await FindListAsync(new ListOwner(null, guestToken), cancellationToken);
        if (guestList is null)
        {
            return;
        }

        var customerList = await FindListAsync(new ListOwner(customerId, null), cancellationToken)
            ?? await CreateListAsync(new ListOwner(customerId, null), cancellationToken);

        var guestItems = await dbContext.CompareItems
            .Where(i => i.CompareListId == guestList.Id)
            .OrderBy(i => i.AddedAt)
            .ToListAsync(cancellationToken);

        foreach (var guestItem in guestItems)
        {
            var alreadyAdded = await dbContext.CompareItems
                .AnyAsync(i => i.CompareListId == customerList.Id && i.ProductId == guestItem.ProductId, cancellationToken);
            if (alreadyAdded)
            {
                dbContext.CompareItems.Remove(guestItem);
                continue;
            }

            await EnsureRoomAsync(customerList.Id, cancellationToken);
            guestItem.CompareListId = customerList.Id;
        }

        dbContext.CompareLists.Remove(guestList);
        customerList.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRoomAsync(long listId, CancellationToken cancellationToken)
    {
        var count = await dbContext.CompareItems.CountAsync(i => i.CompareListId == listId, cancellationToken);
        if (count >= CompareDto.MaxItems)
        {
            throw new ApplicationFailure(FailureKind.Conflict, $"You can compare up to {CompareDto.MaxItems} products. Remove one to add another.");
        }
    }

    private Task<CompareList?> FindListAsync(ListOwner owner, CancellationToken cancellationToken)
    {
        if (owner.CustomerId is { } customerId)
        {
            return dbContext.CompareLists.FirstOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);
        }

        if (!string.IsNullOrEmpty(owner.GuestToken))
        {
            var hash = TokenService.HashToken(owner.GuestToken);
            return dbContext.CompareLists.FirstOrDefaultAsync(c => c.GuestTokenHash == hash, cancellationToken);
        }

        return Task.FromResult<CompareList?>(null);
    }

    private async Task<CompareList> CreateListAsync(ListOwner owner, CancellationToken cancellationToken)
    {
        if (owner.CustomerId is null && string.IsNullOrEmpty(owner.GuestToken))
        {
            throw new InvalidOperationException("A comparison requires either a customer or a guest token.");
        }

        var now = DateTime.UtcNow;
        var list = new CompareList
        {
            CustomerId = owner.CustomerId,
            GuestTokenHash = owner.GuestToken is null ? null : TokenService.HashToken(owner.GuestToken),
            CreatedAt = now,
            UpdatedAt = now,
        };
        dbContext.CompareLists.Add(list);
        await dbContext.SaveChangesAsync(cancellationToken);
        return list;
    }

    private async Task<CompareDto> BuildDtoAsync(long listId, CancellationToken cancellationToken)
    {
        var items = await dbContext.CompareItems
            .AsNoTracking()
            .Where(i => i.CompareListId == listId)
            .OrderBy(i => i.AddedAt)
            .ThenBy(i => i.Id)
            .Include(i => i.Product).ThenInclude(p => p.Category)
            .Include(i => i.Product).ThenInclude(p => p.Images).ThenInclude(img => img.MediaFile)
            .Include(i => i.Product).ThenInclude(p => p.Variants)
            .ToListAsync(cancellationToken);

        var lines = items.Select(ToItem).ToList();
        return new CompareDto { Items = lines, Count = lines.Count };
    }

    private static CompareItemDto ToItem(CompareItem item)
    {
        var product = item.Product;
        var primaryImage = product.Images
            .Where(img => img.ImageType == ProductImageType.Primary)
            .OrderBy(img => img.SortOrder)
            .FirstOrDefault();

        return new CompareItemDto
        {
            Id = item.Id,
            ProductId = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            CategoryName = product.Category.Name,
            ImageUrl = primaryImage?.MediaFile.Url,
            Price = product.Price,
            SalePrice = product.SalePrice,
            Currency = product.Currency,
            Material = product.Material,
            Color = product.Color,
            InStock = product.ProductType == ProductType.Simple
                ? product.StockQuantity > 0
                : product.Variants.Any(v => v.IsActive && v.StockQuantity > 0),
        };
    }
}
