using Elegance.Application.Admin;
using Elegance.Application.Common;
using Elegance.Infrastructure.Admin;
using Elegance.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Tests.Admin;

public class AdminCatalogServiceSkuTests
{
    private static AdminProductRequest MinimalProductRequest(long categoryId, string? sku = null) => new()
    {
        Sku = sku,
        Name = "Test Product",
        Slug = $"test-product-{Guid.NewGuid():N}",
        Price = 1000,
        Currency = "PKR",
        CategoryId = categoryId,
        ProductType = "Simple",
        StockQuantity = 1,
    };

    [Fact]
    public async Task PreviewNextSku_for_an_empty_category_starts_at_001()
    {
        using var db = new SqliteTestDb();
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");

        var result = await service.PreviewNextSkuAsync(category.Id);

        Assert.Equal("JEW-NSET-001", result.Sku);
    }

    [Fact]
    public async Task PreviewNextSku_uses_the_highest_existing_number_not_the_row_count()
    {
        // A gap (002-004 missing) must still produce 006, never a reused or duplicate number -
        // counting rows (which would give 003 here) would be wrong.
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-001");
        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-005");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));

        var result = await service.PreviewNextSkuAsync(category.Id);

        Assert.Equal("JEW-NSET-006", result.Sku);
    }

    [Fact]
    public async Task PreviewNextSku_ignores_numbers_from_other_categories()
    {
        using var db = new SqliteTestDb();
        var nset = await TestCategory.CreateAsync(db.Context, name: "Necklace Sets", slug: "necklaces-sets", code: "NSET");
        var ring = await TestCategory.CreateAsync(db.Context, name: "Rings", slug: "rings", code: "RING");
        await TestProduct.CreateAsync(db.Context, ring.Id, "JEW-RING-010");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));

        var result = await service.PreviewNextSkuAsync(nset.Id);

        Assert.Equal("JEW-NSET-001", result.Sku);
    }

    [Fact]
    public async Task PreviewNextSku_pads_to_three_digits_and_grows_beyond_without_truncating()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-009");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));

        Assert.Equal("JEW-NSET-010", (await service.PreviewNextSkuAsync(category.Id)).Sku);

        await TestProduct.CreateAsync(db.Context, category.Id, "JEW-NSET-999");
        Assert.Equal("JEW-NSET-1000", (await service.PreviewNextSkuAsync(category.Id)).Sku);
    }

    [Fact]
    public async Task PreviewNextSku_throws_a_clear_validation_error_when_the_category_has_no_code()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, name: "Gift Collections", slug: "gift-collections", code: null);
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => service.PreviewNextSkuAsync(category.Id));

        Assert.Equal(FailureKind.Validation, ex.Kind);
        Assert.Contains("no SKU code configured", ex.Message);
    }

    [Fact]
    public async Task PreviewNextSku_for_an_unknown_category_throws_not_found()
    {
        using var db = new SqliteTestDb();
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() => service.PreviewNextSkuAsync(999));

        Assert.Equal(FailureKind.NotFound, ex.Kind);
    }

    [Fact]
    public async Task CreateProduct_with_a_blank_sku_auto_generates_it_from_the_category()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var created = await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, sku: null));

        Assert.Equal("JEW-NSET-001", created.Sku);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateProduct_treats_whitespace_only_sku_as_blank_and_auto_generates(string blankSku)
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var created = await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, sku: blankSku));

        Assert.Equal("JEW-NSET-001", created.Sku);
    }

    [Fact]
    public async Task CreateProduct_with_an_explicit_sku_is_not_overridden_by_auto_generation()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var created = await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, sku: "CUSTOM-SKU-1"));

        Assert.Equal("CUSTOM-SKU-1", created.Sku);
    }

    [Fact]
    public async Task CreateProduct_rejects_a_duplicate_explicit_sku_without_silently_renaming_it()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");
        await service.CreateProductAsync(actor, MinimalProductRequest(category.Id, sku: "TAKEN-1"));

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            service.CreateProductAsync(actor, MinimalProductRequest(category.Id, sku: "TAKEN-1")));

        Assert.Equal(FailureKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task CreateProduct_with_a_blank_sku_and_no_category_code_fails_clearly_instead_of_creating_a_malformed_sku()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, name: "Gift Collections", slug: "gift-collections", code: null);
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            service.CreateProductAsync(actor, MinimalProductRequest(category.Id, sku: null)));

        Assert.Equal(FailureKind.Validation, ex.Kind);
        Assert.Empty(await db.Context.Products.ToListAsync());
    }

    [Fact]
    public async Task CreateProduct_sequential_auto_generated_skus_increment_correctly()
    {
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var first = await service.CreateProductAsync(actor, MinimalProductRequest(category.Id));
        var second = await service.CreateProductAsync(actor, MinimalProductRequest(category.Id));
        var third = await service.CreateProductAsync(actor, MinimalProductRequest(category.Id));

        Assert.Equal(["JEW-NSET-001", "JEW-NSET-002", "JEW-NSET-003"], new[] { first.Sku, second.Sku, third.Sku });
    }

    [Fact]
    public async Task CreateProduct_switching_category_before_saving_changes_which_prefix_is_generated()
    {
        // Simulates the admin picking category A, then changing to category B before saving -
        // the request that actually gets submitted carries B's id, so generation must follow it.
        using var db = new SqliteTestDb();
        var nset = await TestCategory.CreateAsync(db.Context, name: "Necklace Sets", slug: "necklaces-sets", code: "NSET");
        var ring = await TestCategory.CreateAsync(db.Context, name: "Rings", slug: "rings", code: "RING");
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var previewForFirstChoice = await service.PreviewNextSkuAsync(nset.Id);
        Assert.Equal("JEW-NSET-001", previewForFirstChoice.Sku);

        var created = await service.CreateProductAsync(actor, MinimalProductRequest(ring.Id));

        Assert.Equal("JEW-RING-001", created.Sku);
    }

    [Fact]
    public async Task Concurrent_product_creation_in_the_same_category_never_produces_duplicate_skus()
    {
        // Two independent AdminCatalogService instances (own DbContext/connection each) against
        // the same underlying database - genuine concurrency, exercising the retry-on-conflict
        // path in CreateProductAsync rather than a sequential approximation of it.
        using var db = new SqliteTestDb();
        var category = await TestCategory.CreateAsync(db.Context, code: "NSET");
        var actor = new AdminActor(1, "admin@test.local");

        var serviceA = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var contextB = db.CreateAdditionalContext();
        var serviceB = new AdminCatalogService(contextB, new AuditLogService(contextB));

        var taskA = serviceA.CreateProductAsync(actor, MinimalProductRequest(category.Id));
        var taskB = serviceB.CreateProductAsync(actor, MinimalProductRequest(category.Id));
        var results = await Task.WhenAll(taskA, taskB);

        Assert.NotEqual(results[0].Sku, results[1].Sku);
        Assert.Equal(["JEW-NSET-001", "JEW-NSET-002"], results.Select(r => r.Sku).OrderBy(s => s));
    }

    [Fact]
    public async Task Category_code_is_normalized_to_uppercase_and_validated_for_shape()
    {
        using var db = new SqliteTestDb();
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var created = await service.CreateCategoryAsync(actor, new AdminCategoryRequest { Name = "Hoops", Slug = "hoops-2", Code = "hoop" });
        Assert.Equal("HOOP", created.Code);

        var tooShort = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            service.CreateCategoryAsync(actor, new AdminCategoryRequest { Name = "X", Slug = "x-cat", Code = "A" }));
        Assert.Equal(FailureKind.Validation, tooShort.Kind);

        var notAlphanumeric = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            service.CreateCategoryAsync(actor, new AdminCategoryRequest { Name = "Y", Slug = "y-cat", Code = "AB-CD" }));
        Assert.Equal(FailureKind.Validation, notAlphanumeric.Kind);
    }

    [Fact]
    public async Task Category_code_must_be_unique_across_categories()
    {
        using var db = new SqliteTestDb();
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");
        await service.CreateCategoryAsync(actor, new AdminCategoryRequest { Name = "Necklace Sets", Slug = "necklaces-sets", Code = "NSET" });

        var ex = await Assert.ThrowsAsync<ApplicationFailure>(() =>
            service.CreateCategoryAsync(actor, new AdminCategoryRequest { Name = "Something Else", Slug = "something-else", Code = "NSET" }));

        Assert.Equal(FailureKind.Conflict, ex.Kind);
    }

    [Fact]
    public async Task Category_without_a_code_can_still_be_created_and_updated()
    {
        using var db = new SqliteTestDb();
        var service = new AdminCatalogService(db.Context, new AuditLogService(db.Context));
        var actor = new AdminActor(1, "admin@test.local");

        var created = await service.CreateCategoryAsync(actor, new AdminCategoryRequest { Name = "Gift Collections", Slug = "gifts-2", Code = null });
        Assert.Null(created.Code);

        var updated = await service.UpdateCategoryAsync(actor, created.Id, new AdminCategoryRequest { Name = "Gift Collections", Slug = "gifts-2", Code = "GIFT" });
        Assert.Equal("GIFT", updated.Code);
    }
}
