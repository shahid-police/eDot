using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Media;
using Elegance.Infrastructure.Media;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminMediaService(EleganceDbContext dbContext, IFileStorage fileStorage, IAuditLogService auditLog) : IAdminMediaService
{
    private const int MaxGalleryImages = 8;

    public async Task<AdminProductImageDto> UploadProductImageAsync(
        AdminActor actor, long productId, string imageType, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default)
    {
        var type = ParseImageType(imageType);
        if (type == ProductImageType.Swatch)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Use the option photo upload for a variant's own image.");
        }

        if (!await dbContext.Products.AnyAsync(p => p.Id == productId, cancellationToken))
        {
            throw NotFound();
        }

        var processed = await ReadAndProcessAsync(content, contentType, contentLength, cancellationToken);

        if (type is ProductImageType.Primary or ProductImageType.Hover)
        {
            var existing = await dbContext.ProductImages
                .Include(i => i.MediaFile)
                .Where(i => i.ProductId == productId && i.ImageType == type)
                .ToListAsync(cancellationToken);
            foreach (var old in existing)
            {
                await DeleteImageRowAsync(old, cancellationToken);
            }
        }
        else
        {
            var galleryCount = await dbContext.ProductImages.CountAsync(i => i.ProductId == productId && i.ImageType == ProductImageType.Gallery, cancellationToken);
            if (galleryCount >= MaxGalleryImages)
            {
                throw new ApplicationFailure(FailureKind.Conflict, $"This product already has the maximum of {MaxGalleryImages} gallery images.");
            }
        }

        var now = DateTime.UtcNow;
        var key = $"products/{productId}/{Guid.NewGuid():N}.{processed.Extension}";
        var stored = await fileStorage.PutAsync(new MemoryStream(processed.Bytes), key, contentType, cancellationToken);

        var mediaFile = new MediaFile
        {
            StorageKey = stored.StorageKey,
            Url = stored.Url,
            MimeType = contentType,
            Width = processed.Width,
            Height = processed.Height,
            FileSizeBytes = processed.Bytes.Length,
            CreatedAt = now,
        };

        var nextSortOrder = type == ProductImageType.Gallery
            ? 1 + await MaxSortOrderAsync(productId, type, cancellationToken)
            : 0;

        var image = new ProductImage
        {
            ProductId = productId,
            MediaFile = mediaFile,
            ImageType = type,
            SortOrder = nextSortOrder,
            CreatedAt = now,
        };
        dbContext.ProductImages.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "product.image.add", "Product", productId.ToString(), $"Added {type} image", cancellationToken);

        return new AdminProductImageDto { Id = image.Id, Url = mediaFile.Url, ImageType = type.ToString(), SortOrder = image.SortOrder };
    }

    public async Task DeleteProductImageAsync(AdminActor actor, long productId, long imageId, CancellationToken cancellationToken = default)
    {
        var image = await dbContext.ProductImages
            .Include(i => i.MediaFile)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken)
            ?? throw NotFound();

        var type = image.ImageType;
        await DeleteImageRowAsync(image, cancellationToken);
        await auditLog.RecordAsync(actor, "product.image.remove", "Product", productId.ToString(), $"Removed {type} image", cancellationToken);
    }

    public async Task<AdminProductImageDto> ReorderProductImageAsync(AdminActor actor, long productId, long imageId, int sortOrder, CancellationToken cancellationToken = default)
    {
        var image = await dbContext.ProductImages
            .Include(i => i.MediaFile)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken)
            ?? throw NotFound();

        image.SortOrder = sortOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditLog.RecordAsync(actor, "product.image.reorder", "Product", productId.ToString(), $"Image {imageId} moved to position {sortOrder}", cancellationToken);

        return new AdminProductImageDto { Id = image.Id, Url = image.MediaFile.Url, ImageType = image.ImageType.ToString(), SortOrder = image.SortOrder };
    }

    public async Task<string> UploadVariantImageAsync(
        AdminActor actor, long productId, long variantId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .Include(v => v.ImageMediaFile)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, cancellationToken)
            ?? throw NotFound();

        var processed = await ReadAndProcessAsync(content, contentType, contentLength, cancellationToken);

        var previousMediaFile = variant.ImageMediaFile;

        var now = DateTime.UtcNow;
        var key = $"products/{productId}/variants/{variantId}/{Guid.NewGuid():N}.{processed.Extension}";
        var stored = await fileStorage.PutAsync(new MemoryStream(processed.Bytes), key, contentType, cancellationToken);

        var mediaFile = new MediaFile
        {
            StorageKey = stored.StorageKey,
            Url = stored.Url,
            MimeType = contentType,
            Width = processed.Width,
            Height = processed.Height,
            FileSizeBytes = processed.Bytes.Length,
            CreatedAt = now,
        };
        dbContext.MediaFiles.Add(mediaFile);
        variant.ImageMediaFile = mediaFile;
        variant.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousMediaFile is not null)
        {
            await DeleteMediaFileAsync(previousMediaFile, cancellationToken);
        }

        await auditLog.RecordAsync(actor, "product.variant.image.add", "ProductVariant", variantId.ToString(), $"Set photo for {variant.Sku}", cancellationToken);
        return mediaFile.Url;
    }

    public async Task RemoveVariantImageAsync(AdminActor actor, long productId, long variantId, CancellationToken cancellationToken = default)
    {
        var variant = await dbContext.ProductVariants
            .Include(v => v.ImageMediaFile)
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, cancellationToken)
            ?? throw NotFound();

        var mediaFile = variant.ImageMediaFile;
        if (mediaFile is null)
        {
            return;
        }

        variant.ImageMediaFile = null;
        variant.ImageMediaFileId = null;
        variant.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await DeleteMediaFileAsync(mediaFile, cancellationToken);
        await auditLog.RecordAsync(actor, "product.variant.image.remove", "ProductVariant", variantId.ToString(), $"Removed photo for {variant.Sku}", cancellationToken);
    }

    public async Task<string> UploadNoShopVideoThumbnailAsync(
        AdminActor actor, long videoId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.NoShopVideos
            .Include(v => v.CustomThumbnailMediaFile)
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken)
            ?? throw NotFound();

        var processed = await ReadAndProcessAsync(content, contentType, contentLength, cancellationToken);

        var previousMediaFile = video.CustomThumbnailMediaFile;

        var now = DateTime.UtcNow;
        var key = $"noshop/videos/{videoId}/{Guid.NewGuid():N}.{processed.Extension}";
        var stored = await fileStorage.PutAsync(new MemoryStream(processed.Bytes), key, contentType, cancellationToken);

        var mediaFile = new MediaFile
        {
            StorageKey = stored.StorageKey,
            Url = stored.Url,
            MimeType = contentType,
            Width = processed.Width,
            Height = processed.Height,
            FileSizeBytes = processed.Bytes.Length,
            CreatedAt = now,
        };
        dbContext.MediaFiles.Add(mediaFile);
        video.CustomThumbnailMediaFile = mediaFile;
        video.UpdatedAt = now;
        video.UpdatedByAdminUserId = actor.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        if (previousMediaFile is not null)
        {
            await DeleteMediaFileAsync(previousMediaFile, cancellationToken);
        }

        await auditLog.RecordAsync(actor, "noshop.video.thumbnail.add", "NoShopVideo", videoId.ToString(), $"Set custom thumbnail for {video.Title}", cancellationToken);
        return mediaFile.Url;
    }

    public async Task RemoveNoShopVideoThumbnailAsync(AdminActor actor, long videoId, CancellationToken cancellationToken = default)
    {
        var video = await dbContext.NoShopVideos
            .Include(v => v.CustomThumbnailMediaFile)
            .FirstOrDefaultAsync(v => v.Id == videoId, cancellationToken)
            ?? throw NotFound();

        var mediaFile = video.CustomThumbnailMediaFile;
        if (mediaFile is null)
        {
            return;
        }

        video.CustomThumbnailMediaFile = null;
        video.CustomThumbnailMediaFileId = null;
        video.UpdatedAt = DateTime.UtcNow;
        video.UpdatedByAdminUserId = actor.Id;
        await dbContext.SaveChangesAsync(cancellationToken);

        await DeleteMediaFileAsync(mediaFile, cancellationToken);
        await auditLog.RecordAsync(actor, "noshop.video.thumbnail.remove", "NoShopVideo", videoId.ToString(), $"Removed custom thumbnail for {video.Title}", cancellationToken);
    }

    private async Task<int> MaxSortOrderAsync(long productId, ProductImageType type, CancellationToken cancellationToken)
    {
        var orders = await dbContext.ProductImages
            .Where(i => i.ProductId == productId && i.ImageType == type)
            .Select(i => i.SortOrder)
            .ToListAsync(cancellationToken);
        return orders.Count == 0 ? -1 : orders.Max();
    }

    private async Task DeleteImageRowAsync(ProductImage image, CancellationToken cancellationToken)
    {
        var mediaFile = image.MediaFile;
        dbContext.ProductImages.Remove(image);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DeleteMediaFileAsync(mediaFile, cancellationToken);
    }

    private async Task DeleteMediaFileAsync(MediaFile mediaFile, CancellationToken cancellationToken)
    {
        dbContext.MediaFiles.Remove(mediaFile);
        await dbContext.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(mediaFile.StorageKey, cancellationToken);
    }

    private static Task<ProcessedImage> ReadAndProcessAsync(
        Stream content, string contentType, long contentLength, CancellationToken cancellationToken) =>
        ProductImageProcessor.ReadAndProcessAsync(content, contentType, contentLength, cancellationToken);

    private static ProductImageType ParseImageType(string value) =>
        Enum.TryParse<ProductImageType>(value, ignoreCase: true, out var type) && Enum.IsDefined(type)
            ? type
            : throw new ApplicationFailure(FailureKind.Validation, "Choose Primary, Hover, or Gallery.");

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
