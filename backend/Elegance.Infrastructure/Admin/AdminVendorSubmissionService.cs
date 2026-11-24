using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Domain.Vendors;
using Elegance.Infrastructure.Media;
using Elegance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Admin;

public class AdminVendorSubmissionService(
    EleganceDbContext dbContext,
    IFileStorage fileStorage,
    IAdminMediaService adminMediaService,
    IAuditLogService auditLog) : IAdminVendorSubmissionService
{
    public async Task<PagedResult<AdminVendorSubmissionDto>> ListSubmissionsAsync(AdminVendorSubmissionQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var submissions = dbContext.VendorProductSubmissions.AsNoTracking().AsQueryable();
        if (query.VendorId is { } vendorId)
        {
            submissions = submissions.Where(s => s.VendorId == vendorId);
        }
        if (query.CategoryId is { } categoryId)
        {
            submissions = submissions.Where(s => s.CategoryId == categoryId);
        }
        if (!string.IsNullOrWhiteSpace(query.Status) && Enum.TryParse<VendorSubmissionStatus>(query.Status, true, out var status))
        {
            submissions = submissions.Where(s => s.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            submissions = submissions.Where(s => s.ProductName.Contains(search) || s.VendorProductCode.Contains(search));
        }

        var total = await submissions.CountAsync(cancellationToken);
        var ids = await submissions
            .OrderByDescending(s => s.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        var items = new List<AdminVendorSubmissionDto>(ids.Count);
        foreach (var id in ids)
        {
            items.Add(await LoadDtoAsync(id, cancellationToken));
        }

        return new PagedResult<AdminVendorSubmissionDto> { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<AdminVendorSubmissionDto> GetSubmissionAsync(long submissionId, CancellationToken cancellationToken = default) =>
        await LoadDtoAsync(submissionId, cancellationToken);

    public async Task<AdminVendorSubmissionDto> ApproveAsync(AdminActor actor, long submissionId, CancellationToken cancellationToken = default)
    {
        var submission = await RequireSubmissionAsync(submissionId, cancellationToken);
        if (submission.Status != VendorSubmissionStatus.PendingApproval)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Only a submission pending approval can be approved.");
        }

        var previous = submission.Status;
        submission.Status = VendorSubmissionStatus.Approved;
        submission.ReviewedByAdminId = actor.Id;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
        AddHistory(submissionId, "submission.approve", previous, VendorSubmissionStatus.Approved, actor.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.submission.approve", "VendorProductSubmission", submissionId.ToString(), $"Approved submission for '{submission.ProductName}'", cancellationToken);
        return await LoadDtoAsync(submissionId, cancellationToken);
    }

    public async Task<AdminVendorSubmissionDto> RejectAsync(AdminActor actor, long submissionId, AdminRejectSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        var submission = await RequireSubmissionAsync(submissionId, cancellationToken);
        if (submission.Status != VendorSubmissionStatus.PendingApproval)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Only a submission pending approval can be rejected.");
        }

        var previous = submission.Status;
        submission.Status = VendorSubmissionStatus.Rejected;
        submission.RejectionReason = request.Reason.Trim();
        submission.ReviewedByAdminId = actor.Id;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
        AddHistory(submissionId, "submission.reject", previous, VendorSubmissionStatus.Rejected, actor.Id, request.Reason.Trim());
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.submission.reject", "VendorProductSubmission", submissionId.ToString(), $"Rejected submission for '{submission.ProductName}'", cancellationToken);
        return await LoadDtoAsync(submissionId, cancellationToken);
    }

    public async Task<AdminVendorSubmissionDto> ReturnForCorrectionAsync(AdminActor actor, long submissionId, AdminReturnSubmissionForCorrectionRequest request, CancellationToken cancellationToken = default)
    {
        var submission = await RequireSubmissionAsync(submissionId, cancellationToken);
        if (submission.Status != VendorSubmissionStatus.PendingApproval)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Only a submission pending approval can be returned for correction.");
        }

        var previous = submission.Status;
        submission.Status = VendorSubmissionStatus.ReturnedForCorrection;
        submission.CorrectionFieldsRequested = request.FieldsRequested.Trim();
        submission.AdminRemarks = request.Remarks?.Trim();
        submission.ReviewedByAdminId = actor.Id;
        submission.ReviewedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;
        AddHistory(submissionId, "submission.return", previous, VendorSubmissionStatus.ReturnedForCorrection, actor.Id, request.Remarks);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.submission.return", "VendorProductSubmission", submissionId.ToString(), $"Returned submission for '{submission.ProductName}' for correction", cancellationToken);
        return await LoadDtoAsync(submissionId, cancellationToken);
    }

    public async Task<AdminVendorSubmissionDto> LinkProductAsync(AdminActor actor, long submissionId, AdminLinkSubmissionToProductRequest request, CancellationToken cancellationToken = default)
    {
        var submission = await RequireSubmissionAsync(submissionId, cancellationToken);
        if (submission.Status != VendorSubmissionStatus.Approved)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "Only an approved submission can be linked to a product.");
        }
        if (submission.ProductId is not null)
        {
            throw new ApplicationFailure(FailureKind.Conflict, "This submission is already linked to a product.");
        }

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken)
            ?? throw new ApplicationFailure(FailureKind.Validation, "Choose a valid product.");

        submission.ProductId = product.Id;
        submission.UpdatedAt = DateTime.UtcNow;
        product.VendorId = submission.VendorId;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.submission.link-product", "VendorProductSubmission", submissionId.ToString(), $"Linked submission to product {product.Sku}", cancellationToken);
        return await LoadDtoAsync(submissionId, cancellationToken);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> DownloadSubmissionImageAsync(long submissionId, long imageId, CancellationToken cancellationToken = default)
    {
        var image = await dbContext.VendorProductImages.AsNoTracking()
            .Include(i => i.MediaFile)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.SubmissionId == submissionId, cancellationToken)
            ?? throw NotFound();

        var stream = await fileStorage.GetAsync(image.MediaFile.StorageKey, cancellationToken);
        var fileName = image.MediaFile.OriginalFilename ?? Path.GetFileName(image.MediaFile.StorageKey);
        return (stream, image.MediaFile.MimeType ?? "application/octet-stream", fileName);
    }

    public async Task<AdminProductImageDto> PublishSubmissionImageAsync(AdminActor actor, long submissionId, long imageId, string imageType, CancellationToken cancellationToken = default)
    {
        var submission = await RequireSubmissionAsync(submissionId, cancellationToken);
        if (submission.ProductId is not { } productId)
        {
            throw new ApplicationFailure(FailureKind.Validation, "Create or link this submission's product before publishing images.");
        }

        var image = await dbContext.VendorProductImages.AsNoTracking()
            .Include(i => i.MediaFile)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.SubmissionId == submissionId, cancellationToken)
            ?? throw NotFound();

        // Re-streams the vendor's original bytes through the existing, unmodified admin upload
        // pipeline (signature check, dimension guard, resize, re-encode) - this is the only code
        // path by which a vendor image can ever reach the public ProductImage table.
        await using var content = await fileStorage.GetAsync(image.MediaFile.StorageKey, cancellationToken);
        var published = await adminMediaService.UploadProductImageAsync(
            actor, productId, imageType, content, image.MediaFile.MimeType ?? "image/jpeg", image.MediaFile.FileSizeBytes ?? 0, cancellationToken);

        await auditLog.RecordAsync(actor, "vendor.image.publish", "VendorProductSubmission", submissionId.ToString(), $"Published vendor image {imageId} as {imageType}", cancellationToken);
        return published;
    }

    private async Task<VendorProductSubmission> RequireSubmissionAsync(long submissionId, CancellationToken cancellationToken) =>
        await dbContext.VendorProductSubmissions.FirstOrDefaultAsync(s => s.Id == submissionId, cancellationToken) ?? throw NotFound();

    private void AddHistory(long submissionId, string action, VendorSubmissionStatus previousStatus, VendorSubmissionStatus newStatus, long adminId, string? remarks) =>
        dbContext.VendorProductSubmissionHistory.Add(new VendorProductSubmissionHistory
        {
            SubmissionId = submissionId,
            Action = action,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            PerformedByType = VendorSubmissionPerformedByType.Admin,
            PerformedById = adminId,
            Remarks = remarks,
            CreatedAt = DateTime.UtcNow,
        });

    private async Task<AdminVendorSubmissionDto> LoadDtoAsync(long submissionId, CancellationToken cancellationToken)
    {
        var submission = await dbContext.VendorProductSubmissions.AsNoTracking()
            .Include(s => s.Images).ThenInclude(i => i.MediaFile)
            .FirstAsync(s => s.Id == submissionId, cancellationToken);

        var vendor = await dbContext.Vendors.AsNoTracking()
            .Where(v => v.Id == submission.VendorId)
            .Select(v => new { v.VendorCode, v.ShopName })
            .FirstAsync(cancellationToken);

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

        return new AdminVendorSubmissionDto
        {
            Id = submission.Id,
            VendorId = submission.VendorId,
            VendorCode = vendor.VendorCode,
            ShopName = vendor.ShopName,
            ProductName = submission.ProductName,
            CategoryId = submission.CategoryId,
            CategoryName = categoryName,
            WholesalePrice = submission.WholesalePrice,
            VendorProductCode = submission.VendorProductCode,
            Status = submission.Status.ToString(),
            SubmittedAt = submission.SubmittedAt,
            UpdatedAt = submission.UpdatedAt,
            ReviewedByAdminId = submission.ReviewedByAdminId,
            ReviewedAt = submission.ReviewedAt,
            RejectionReason = submission.RejectionReason,
            AdminRemarks = submission.AdminRemarks,
            CorrectionFieldsRequested = submission.CorrectionFieldsRequested,
            ProductId = submission.ProductId,
            ProductSku = productSku,
            Images = submission.Images
                .OrderBy(i => i.SortOrder)
                .Select(i => new AdminVendorSubmissionImageDto { Id = i.Id, SortOrder = i.SortOrder })
                .ToList(),
        };
    }

    private static ApplicationFailure NotFound() => new(FailureKind.NotFound, "The requested resource was not found.");
}
