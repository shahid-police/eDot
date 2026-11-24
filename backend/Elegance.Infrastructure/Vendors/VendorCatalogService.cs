using Elegance.Application.Common;
using Elegance.Application.Vendors;
using Elegance.Domain.Media;
using Elegance.Domain.Vendors;
using Elegance.Infrastructure.Media;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Vendors;

public class VendorCatalogService(EleganceDbContext dbContext, IFileStorage fileStorage) : IVendorCatalogService
{
    private const int MaxImagesPerSubmission = 10;
    private static readonly VendorSubmissionStatus[] EditableStatuses =
        [VendorSubmissionStatus.PendingApproval, VendorSubmissionStatus.ReturnedForCorrection];

    public async Task<VendorProfileDto> GetProfileAsync(long vendorId, CancellationToken cancellationToken = default)
    {
        var vendor = await dbContext.Vendors.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vendorId, cancellationToken)
            ?? throw NotFound();

        return new VendorProfileDto
        {
            Id = vendor.Id,
            Email = vendor.Email,
            VendorCode = vendor.VendorCode,
            ShopName = vendor.ShopName,
            ContactPerson = vendor.ContactPerson,
            Phone = vendor.Phone,
            Address = vendor.Address,
            City = vendor.City,
        };
    }

    public async Task<PagedResult<VendorSubmissionDto>> ListMySubmissionsAsync(long vendorId, VendorSubmissionQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var submissions = dbContext.VendorProductSubmissions.AsNoTracking().Where(s => s.VendorId == vendorId);
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VendorSubmissionStatus>(query.Status, true, out var status))
        {
            submissions = submissions.Where(s => s.Status == status);
        }

        var total = await submissions.CountAsync(cancellationToken);
        var ids = await submissions
            .OrderByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var items = new List<VendorSubmissionDto>(ids.Count);
        foreach (var id in ids)
        {
            items.Add(await LoadDtoAsync(id, cancellationToken));
        }

        return new PagedResult<VendorSubmissionDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<VendorSubmissionDto> GetSubmissionAsync(long vendorId, long submissionId, CancellationToken cancellationToken = default)
    {
        await RequireOwnedSubmissionAsync(vendorId, submissionId, cancellationToken);
        return await LoadDtoAsync(submissionId, cancellationToken);
    }

    public async Task<VendorSubmissionDto> CreateSubmissionAsync(long vendorId, VendorSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.Categories.AsNoTracking().AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Choose a valid category.");
        }

        var code = request.VendorProductCode.Trim();
        if (await dbContext.VendorProductSubmissions.AsNoTracking()
            .AnyAsync(s => s.VendorId == vendorId && s.VendorProductCode == code, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "You already have a submission with this product code.");
        }

        var now = DateTime.UtcNow;
        var submission = new VendorProductSubmission
        {
            VendorId = vendorId,
            ProductName = request.ProductName.Trim(),
            CategoryId = request.CategoryId,
            WholesalePrice = request.WholesalePrice,
            VendorProductCode = code,
            Status = VendorSubmissionStatus.PendingApproval,
            SubmittedAt = now,
            UpdatedAt = now,
        };
        dbContext.VendorProductSubmissions.Add(submission);
        await dbContext.SaveChangesAsync(cancellationToken);

        AddHistory(submission.Id, "submission.create", null, VendorSubmissionStatus.PendingApproval, VendorSubmissionPerformedByType.Vendor, vendorId, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadDtoAsync(submission.Id, cancellationToken);
    }

    public async Task<VendorSubmissionDto> UpdateSubmissionAsync(long vendorId, long submissionId, VendorSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var submission = await RequireOwnedSubmissionAsync(vendorId, submissionId, cancellationToken);
        if (!EditableStatuses.Contains(submission.Status))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This submission can no longer be edited.");
        }

        if (!await dbContext.Categories.AsNoTracking().AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Choose a valid category.");
        }

        var code = request.VendorProductCode.Trim();
        if (await dbContext.VendorProductSubmissions.AsNoTracking()
            .AnyAsync(s => s.VendorId == vendorId && s.Id != submissionId && s.VendorProductCode == code, cancellationToken))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "You already have a submission with this product code.");
        }

        submission.ProductName = request.ProductName.Trim();
        submission.CategoryId = request.CategoryId;
        submission.WholesalePrice = request.WholesalePrice;
        submission.VendorProductCode = code;
        submission.UpdatedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadDtoAsync(submissionId, cancellationToken);
    }

    public async Task<VendorSubmissionDto> ResubmitAsync(long vendorId, long submissionId, CancellationToken cancellationToken = default)
    {
        var submission = await RequireOwnedSubmissionAsync(vendorId, submissionId, cancellationToken);
        if (submission.Status != VendorSubmissionStatus.ReturnedForCorrection)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Only a submission returned for correction can be resubmitted.");
        }

        var previous = submission.Status;
        submission.Status = VendorSubmissionStatus.PendingApproval;
        submission.UpdatedAt = DateTime.UtcNow;
        AddHistory(submissionId, "submission.resubmit", previous, VendorSubmissionStatus.PendingApproval, VendorSubmissionPerformedByType.Vendor, vendorId, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await LoadDtoAsync(submissionId, cancellationToken);
    }

    public async Task WithdrawAsync(long vendorId, long submissionId, CancellationToken cancellationToken = default)
    {
        var submission = await dbContext.VendorProductSubmissions
            .Include(s => s.Images).ThenInclude(i => i.MediaFile)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.VendorId == vendorId, cancellationToken)
            ?? throw NotFound();

        if (submission.Status != VendorSubmissionStatus.PendingApproval)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Only a submission still pending approval can be withdrawn.");
        }

        foreach (var image in submission.Images.ToList())
        {
            await DeleteVendorImageAsync(image, cancellationToken);
        }

        dbContext.VendorProductSubmissions.Remove(submission);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<VendorSubmissionImageDto> UploadSubmissionImageAsync(
        long vendorId, long submissionId, Stream content, string contentType, long contentLength, CancellationToken cancellationToken = default)
    {
        var submission = await RequireOwnedSubmissionAsync(vendorId, submissionId, cancellationToken);
        if (!EditableStatuses.Contains(submission.Status))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This submission can no longer be edited.");
        }

        var imageCount = await dbContext.VendorProductImages.CountAsync(i => i.SubmissionId == submissionId, cancellationToken);
        if (imageCount >= MaxImagesPerSubmission)
        {
            throw new ApplicationFailure(FailureKind.Conflict, $"You can upload at most {MaxImagesPerSubmission} images per submission.");
        }

        var processed = await ProductImageProcessor.ReadAndProcessAsync(content, contentType, contentLength, cancellationToken);

        var now = DateTime.UtcNow;
        var key = $"vendor-originals/{vendorId}/{Guid.NewGuid():N}.{processed.Extension}";
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

        var nextSortOrder = 1 + await MaxSortOrderAsync(submissionId, cancellationToken);
        var image = new VendorProductImage
        {
            SubmissionId = submissionId,
            VendorId = vendorId,
            MediaFile = mediaFile,
            SortOrder = nextSortOrder,
            UploadedAt = now,
        };
        dbContext.VendorProductImages.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new VendorSubmissionImageDto { Id = image.Id, SortOrder = image.SortOrder };
    }

    public async Task RemoveSubmissionImageAsync(long vendorId, long submissionId, long imageId, CancellationToken cancellationToken = default)
    {
        var submission = await RequireOwnedSubmissionAsync(vendorId, submissionId, cancellationToken);
        if (!EditableStatuses.Contains(submission.Status))
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This submission can no longer be edited.");
        }

        var image = await dbContext.VendorProductImages
            .Include(i => i.MediaFile)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.SubmissionId == submissionId && i.VendorId == vendorId, cancellationToken)
            ?? throw NotFound();

        await DeleteVendorImageAsync(image, cancellationToken);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> DownloadSubmissionImageAsync(
        long vendorId, long submissionId, long imageId, CancellationToken cancellationToken = default)
    {
        await RequireOwnedSubmissionAsync(vendorId, submissionId, cancellationToken);

        var image = await dbContext.VendorProductImages.AsNoTracking()
            .Include(i => i.MediaFile)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.SubmissionId == submissionId && i.VendorId == vendorId, cancellationToken)
            ?? throw NotFound();

        var stream = await fileStorage.GetAsync(image.MediaFile.StorageKey, cancellationToken);
        var fileName = image.MediaFile.OriginalFilename ?? Path.GetFileName(image.MediaFile.StorageKey);
        return (stream, image.MediaFile.MimeType ?? "application/octet-stream", fileName);
    }

    private async Task DeleteVendorImageAsync(VendorProductImage image, CancellationToken cancellationToken)
    {
        var mediaFile = image.MediaFile;
        dbContext.VendorProductImages.Remove(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.MediaFiles.Remove(mediaFile);
        await dbContext.SaveChangesAsync(cancellationToken);
        await fileStorage.DeleteAsync(mediaFile.StorageKey, cancellationToken);
    }

    private async Task<int> MaxSortOrderAsync(long submissionId, CancellationToken cancellationToken)
    {
        var orders = await dbContext.VendorProductImages
            .Where(i => i.SubmissionId == submissionId)
            .Select(i => i.SortOrder)
            .ToListAsync(cancellationToken);
        return orders.Count == 0 ? -1 : orders.Max();
    }

    // Returns NotFound (never Forbidden) on a cross-vendor access attempt, so one vendor can't
    // learn that a given submission id belongs to someone else.
    private async Task<VendorProductSubmission> RequireOwnedSubmissionAsync(long vendorId, long submissionId, CancellationToken cancellationToken) =>
        await dbContext.VendorProductSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId && s.VendorId == vendorId, cancellationToken)
            ?? throw NotFound();

    private async Task<VendorSubmissionDto> LoadDtoAsync(long submissionId, CancellationToken cancellationToken)
    {
        var submission = await dbContext.VendorProductSubmissions.AsNoTracking()
            .Include(s => s.Images).ThenInclude(i => i.MediaFile)
            .FirstAsync(s => s.Id == submissionId, cancellationToken);

        var categoryName = await dbContext.Categories.AsNoTracking()
            .Where(c => c.Id == submission.CategoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        string? productSku = null;
        if (submission.ProductId is { } productId)
        {
            productSku = await dbContext.Products.AsNoTracking()
                .Where(p => p.Id == productId)
                .Select(p => p.Sku)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new VendorSubmissionDto
        {
            Id = submission.Id,
            ProductName = submission.ProductName,
            CategoryId = submission.CategoryId,
            CategoryName = categoryName,
            WholesalePrice = submission.WholesalePrice,
            VendorProductCode = submission.VendorProductCode,
            Status = submission.Status.ToString(),
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt,
            RejectionReason = submission.RejectionReason,
            AdminRemarks = submission.AdminRemarks,
            CorrectionFieldsRequested = submission.CorrectionFieldsRequested,
            ProductId = submission.ProductId,
            ProductSku = productSku,
            Images = submission.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new VendorSubmissionImageDto { Id = i.Id, SortOrder = i.SortOrder })
                .ToList(),
        };
    }

    private void AddHistory(
        long submissionId, string action, VendorSubmissionStatus? previousStatus, VendorSubmissionStatus newStatus,
        VendorSubmissionPerformedByType performedByType, long performedById, string? remarks) =>
        dbContext.VendorProductSubmissionHistory.Add(new VendorProductSubmissionHistory
        {
            SubmissionId = submissionId,
            Action = action,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PerformedByType = performedByType,
            PerformedById = performedById,
            Remarks = remarks,
            CreatedAt = DateTime.UtcNow,
        });

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
