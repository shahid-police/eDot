using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Media;
using Elegance.Infrastructure.Media;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace Elegance.Infrastructure.Admin;

public class AdminMediaService(EleganceDbContext dbContext, IFileStorage fileStorage, IAuditLogService auditLog) : IAdminMediaService
{
    // Rules documented for the admin at upload time and in docs/MEDIA-STORAGE.md. No minimum
    // dimensions are enforced - any image that decodes is accepted and auto-resized down to
    // MaxStoredEdge for storage. MaxInputEdge stays as a generous safety ceiling (not a UX
    // limit): without it, a small-but-highly-compressed file could decode into gigabytes of
    // pixel data in memory (a decompression-bomb DoS), regardless of the byte-size cap below.
    private const long MaxUploadBytes = 8 * 1024 * 1024;
    private const int MaxInputEdge = 10000;
    private const int MaxStoredEdge = 2000;
    private const int MaxGalleryImages = 8;

    private static readonly Dictionary<string, string> AllowedTypes = new()
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

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

    private static async Task<ProcessedImage> ReadAndProcessAsync(
        Stream content, string contentType, long contentLength, CancellationToken cancellationToken)
    {
        if (!AllowedTypes.TryGetValue(contentType, out var extension))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Upload a JPEG, PNG, or WebP image.");
        }
        if (contentLength <= 0 || contentLength > MaxUploadBytes)
        {
            throw new ApplicationFailure(FailureKind.Validation, $"Images must be {MaxUploadBytes / (1024 * 1024)} MB or smaller.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var original = buffer.ToArray();

        // SKBitmap.Decode doesn't just return null on unparseable bytes - it can throw
        // (e.g. ArgumentNullException when it can't construct a codec), so both are "not an image".
        SKBitmap? decoded;
        try
        {
            decoded = SKBitmap.Decode(original);
        }
        catch
        {
            decoded = null;
        }
        using var bitmap = decoded ?? throw new ApplicationFailure(FailureKind.Validation, "That file is not a valid image.");

        if (bitmap.Width > MaxInputEdge || bitmap.Height > MaxInputEdge)
        {
            throw new ApplicationFailure(FailureKind.Validation, $"Images must be {MaxInputEdge}×{MaxInputEdge} pixels or smaller. Resize it and try again.");
        }

        var longEdge = Math.Max(bitmap.Width, bitmap.Height);
        SKBitmap toEncode = bitmap;
        SKBitmap? resized = null;
        if (longEdge > MaxStoredEdge)
        {
            var scale = (float)MaxStoredEdge / longEdge;
            var newWidth = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
            var newHeight = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
            resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
            if (resized is not null)
            {
                toEncode = resized;
            }
        }

        var format = contentType switch
        {
            "image/jpeg" => SKEncodedImageFormat.Jpeg,
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg,
        };

        using var image = SKImage.FromBitmap(toEncode);
        using var encoded = image.Encode(format, 85);
        var bytes = encoded.ToArray();
        var result = new ProcessedImage(bytes, toEncode.Width, toEncode.Height, extension);
        resized?.Dispose();
        return result;
    }

    private static ProductImageType ParseImageType(string value) =>
        Enum.TryParse<ProductImageType>(value, ignoreCase: true, out var type) && Enum.IsDefined(type)
            ? type
            : throw new ApplicationFailure(FailureKind.Validation, "Choose Primary, Hover, or Gallery.");

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");

    private sealed record ProcessedImage(byte[] Bytes, int Width, int Height, string Extension);
}
