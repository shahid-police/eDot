using Elegance.Domain.Catalog;
using Elegance.Domain.Catalog.Enums;
using Elegance.Domain.Inventory;
using Elegance.Domain.Media;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Persistence.Seed;

/// <summary>
/// Development-only demo catalogue: a realistic category tree and ~60 products covering the
/// field/scenario combinations the Add Product form supports (see docs/SEED-DATA-PLAN.md for the
/// full inspection and the schema gaps this intentionally does not work around).
///
/// Off by default. Runs only when both ASPNETCORE_ENVIRONMENT=Development and
/// Seed:LoadDemoCatalog=true, so an ordinary dev run keeps the original 3-product baseline from
/// DevelopmentSeeder. Idempotent: categories are upserted by slug, products by SKU - an existing
/// row (including one an admin has since edited) is never touched or duplicated by a re-run.
///
/// Images are placeholders from picsum.photos, one deterministic seed per product/variant, same
/// convention DevelopmentSeeder already uses. Not representative of real E. / elegence.pk
/// inventory - replace with real photography before any public use.
/// </summary>
public static class CatalogDemoSeeder
{
    private sealed record CategorySpec(string Slug, string Name, string? ParentSlug, string? Description, int SortOrder, string? SeoTitle = null, string? SeoDescription = null);

    private sealed record VariantSpec(string SkuSuffix, string Name, string? Color, string? Size, int Stock, decimal? PriceOverride = null, decimal? SaleOverride = null);

    private sealed record ProductSpec(
        string Sku, string Slug, string Name, string CategorySlug, ProductType Type,
        decimal Price, decimal? SalePrice, decimal? CostPrice, int Stock, int? LowStockThreshold,
        string? Material, string? Color, string? Occasion, decimal? WeightGrams,
        bool IsFeatured, bool IsNew, bool IsBestSeller, bool IsActive,
        string ShortDescription, string Description,
        string? SeoTitle, string? SeoDescription, string? SeoKeywords,
        int GalleryImages, VariantSpec[]? Variants);

    public static async Task<(int CategoriesAdded, int ProductsAdded, int ProductsSkipped)> SeedAsync(
        EleganceDbContext dbContext, bool enabled, CancellationToken cancellationToken = default)
    {
        if (!enabled)
        {
            return (0, 0, 0);
        }

        var now = DateTime.UtcNow;
        var categoriesAdded = await SeedCategoriesAsync(dbContext, now, cancellationToken);
        var (productsAdded, productsSkipped) = await SeedProductsAsync(dbContext, now, cancellationToken);

        Console.WriteLine($"[CatalogDemoSeeder] categories added: {categoriesAdded}, products added: {productsAdded}, products skipped (already existed): {productsSkipped}");
        return (categoriesAdded, productsAdded, productsSkipped);
    }

    // ---- categories -------------------------------------------------------

    private static readonly CategorySpec[] CategorySpecs =
    [
        new("bridal-jewellery", "Bridal Jewellery", null, "Sets, chokers and headpieces for brides.", 10,
            "Bridal Jewellery | E.", "Kundan and polki-inspired bridal sets, chokers and tikka for your wedding day."),
        new("traditional-jewellery", "Traditional Jewellery", null, "Kundan, meenakari and other traditional styles.", 20),
        new("party-wear-jewellery", "Party Wear Jewellery", null, "Statement pieces for evenings out.", 30),
        new("bangles-bracelets", "Bangles & Bracelets", null, "Traditional bangles, kada and everyday bracelets.", 40),
        new("pendants-chains", "Pendants & Chains", null, "Standalone pendants and chains.", 50),
        new("maang-tikka-mathapatti", "Maang Tikka & Mathapatti", null, "Headpieces for bridal and festive wear.", 60),
        new("daily-wear-jewellery", "Daily Wear Jewellery", null, "Lightweight pieces for everyday wear.", 70),
        new("gift-collections", "Gift Collections", null, "Curated picks for birthdays, Eid and weddings.", 80),

        new("earrings-studs", "Studs", "earrings", "Simple stud earrings.", 1),
        new("earrings-hoops", "Hoops", "earrings", "Classic and statement hoop earrings.", 2),
        new("earrings-jhumkas", "Jhumkas", "earrings", "Traditional bell-shaped jhumka earrings.", 3),
        new("earrings-chandbalis", "Chandbalis", "earrings", "Crescent-shaped chandbali earrings.", 4),

        new("necklaces-sets", "Necklace Sets", "necklaces", "Necklace and earring sets.", 1),
        new("necklaces-chokers", "Chokers", "necklaces", "Close-fit choker necklaces.", 2),
        new("necklaces-pendant", "Pendant Necklaces", "necklaces", "Necklaces with a pendant centrepiece.", 3),

        new("rings-statement", "Statement Rings", "rings", "Bold, eye-catching rings.", 1),
        new("rings-adjustable", "Adjustable Rings", "rings", "One-size adjustable rings.", 2),

        new("bangles-traditional", "Traditional Bangles", "bangles-bracelets", "Classic traditional bangles.", 1),
        new("bangles-kada", "Kada", "bangles-bracelets", "Single, wide kada bangles.", 2),
        new("bracelets", "Bracelets", "bangles-bracelets", "Everyday and statement bracelets.", 3),
    ];

    private static async Task<int> SeedCategoriesAsync(EleganceDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Categories.ToDictionaryAsync(c => c.Slug, cancellationToken);
        var added = 0;

        // Parents are listed before their children in CategorySpecs, so a single pass is enough.
        foreach (var spec in CategorySpecs)
        {
            if (existing.ContainsKey(spec.Slug))
            {
                continue;
            }

            long? parentId = spec.ParentSlug is null ? null : existing[spec.ParentSlug].Id;
            var category = new Category
            {
                Name = spec.Name,
                Slug = spec.Slug,
                Description = spec.Description,
                ParentCategoryId = parentId,
                SortOrder = spec.SortOrder,
                SeoTitle = spec.SeoTitle,
                SeoDescription = spec.SeoDescription,
                CreatedAt = now,
                UpdatedAt = now,
            };
            dbContext.Categories.Add(category);
            await dbContext.SaveChangesAsync(cancellationToken); // need the Id before a child can reference it
            existing[spec.Slug] = category;
            added++;
        }

        return added;
    }

    // ---- products -----------------------------------------------------------

    private static MediaFile Media(string seedKey, string alt, DateTime now) => new()
    {
        StorageKey = $"seed/{seedKey}.jpg",
        Url = $"https://picsum.photos/seed/{seedKey}/1000/1250",
        AltText = alt,
        CreatedAt = now,
    };

    private static async Task<(int Added, int Skipped)> SeedProductsAsync(EleganceDbContext dbContext, DateTime now, CancellationToken cancellationToken)
    {
        var categoryIdsBySlug = await dbContext.Categories.ToDictionaryAsync(c => c.Slug, c => c.Id, cancellationToken);
        var existingSkus = (await dbContext.Products.Select(p => p.Sku).ToListAsync(cancellationToken)).ToHashSet();

        var added = 0;
        var skipped = 0;

        foreach (var spec in ProductSpecs)
        {
            if (existingSkus.Contains(spec.Sku))
            {
                skipped++;
                continue;
            }

            if (!categoryIdsBySlug.TryGetValue(spec.CategorySlug, out var categoryId))
            {
                throw new InvalidOperationException($"CatalogDemoSeeder: unknown category slug '{spec.CategorySlug}' for product '{spec.Sku}'.");
            }

            var product = new Product
            {
                Sku = spec.Sku,
                Name = spec.Name,
                Slug = spec.Slug,
                ShortDescription = spec.ShortDescription,
                Description = spec.Description,
                Price = spec.Price,
                SalePrice = spec.SalePrice,
                CostPrice = spec.CostPrice,
                Currency = "PKR",
                CategoryId = categoryId,
                ProductType = spec.Type,
                Material = spec.Material,
                Color = spec.Color,
                Occasion = spec.Occasion,
                WeightGrams = spec.WeightGrams,
                StockQuantity = spec.Type == ProductType.Simple ? spec.Stock : 0,
                LowStockThreshold = spec.LowStockThreshold,
                IsFeatured = spec.IsFeatured,
                IsNew = spec.IsNew,
                IsBestSeller = spec.IsBestSeller,
                IsActive = spec.IsActive,
                SeoTitle = spec.SeoTitle,
                SeoDescription = spec.SeoDescription,
                SeoKeywords = spec.SeoKeywords,
                CreatedAt = now,
                UpdatedAt = now,
            };

            product.Images.Add(new ProductImage { MediaFile = Media($"{spec.Sku}-primary", spec.Name, now), ImageType = ProductImageType.Primary, SortOrder = 0, CreatedAt = now });
            product.Images.Add(new ProductImage { MediaFile = Media($"{spec.Sku}-hover", $"{spec.Name}, alternate angle", now), ImageType = ProductImageType.Hover, SortOrder = 1, CreatedAt = now });
            for (var i = 0; i < spec.GalleryImages; i++)
            {
                product.Images.Add(new ProductImage { MediaFile = Media($"{spec.Sku}-gallery-{i}", $"{spec.Name}, detail", now), ImageType = ProductImageType.Gallery, SortOrder = 2 + i, CreatedAt = now });
            }

            // StockMovement only has scalar ProductId/VariantId FKs (no navigation properties), so
            // track which variant (if any) each opening-stock row belongs to and resolve the real
            // Ids after the product/variants are saved and have generated Ids.
            var pendingMovements = new List<(ProductVariant? Variant, int Delta)>();
            if (spec.Type == ProductType.Simple && product.StockQuantity > 0)
            {
                pendingMovements.Add((null, product.StockQuantity));
            }

            if (spec.Variants is { Length: > 0 })
            {
                foreach (var variantSpec in spec.Variants)
                {
                    var variant = new ProductVariant
                    {
                        Sku = $"{spec.Sku}-{variantSpec.SkuSuffix}",
                        VariantName = variantSpec.Name,
                        Color = variantSpec.Color,
                        Size = variantSpec.Size,
                        Price = variantSpec.PriceOverride,
                        SalePrice = variantSpec.SaleOverride,
                        StockQuantity = variantSpec.Stock,
                        ImageMediaFile = Media($"{spec.Sku}-{variantSpec.SkuSuffix}".ToLowerInvariant(), $"{spec.Name}, {variantSpec.Name}", now),
                        CreatedAt = now,
                        UpdatedAt = now,
                    };
                    product.Variants.Add(variant);
                    if (variantSpec.Stock > 0)
                    {
                        pendingMovements.Add((variant, variantSpec.Stock));
                    }
                }
            }

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync(cancellationToken); // need generated Ids before writing the ledger rows

            var openingMovements = pendingMovements.Select(m => new StockMovement
            {
                ProductId = product.Id,
                VariantId = m.Variant?.Id,
                QuantityDelta = m.Delta,
                Reason = StockMovementReason.Adjustment,
                Note = "Opening stock (demo catalogue seed)",
                CreatedAt = now,
            });
            dbContext.StockMovements.AddRange(openingMovements);
            await dbContext.SaveChangesAsync(cancellationToken);

            existingSkus.Add(spec.Sku);
            added++;
        }

        return (added, skipped);
    }

    // Scenario tags in the comments below map to docs/SEED-DATA-PLAN.md §6.
    private static readonly ProductSpec[] ProductSpecs =
    [
        // ---- Bridal Jewellery (5) ----
        new("BR-KD-001", "royal-kundan-bridal-set", "Royal Kundan Bridal Set", "bridal-jewellery", ProductType.Simple,
            22500m, 19900m, 12500m, 6, 3, "Kundan", "Gold", "Wedding", 185.5m, true, true, false, true,
            "Hand-set kundan stones in a layered bridal silhouette.",
            "A statement bridal necklace with hand-set kundan stones across a layered gold-tone silhouette, finished with matching drop earrings. Artificial fashion jewellery, not genuine gemstones.",
            "Royal Kundan Bridal Set | E.", "A layered kundan bridal necklace set with matching earrings, in gold-tone artificial jewellery.", "kundan bridal set, bridal necklace, kundan jewellery",
            3, null), // Scenario B (premium, multi-gallery, SEO) + C (discounted) + F (low stock)

        new("BR-PK-002", "polki-inspired-choker-set", "Polki-Inspired Choker Set", "bridal-jewellery", ProductType.Variant,
            24900m, null, null, 0, 5, "Polki", null, "Wedding", null, true, false, true, true,
            "A polki-inspired choker set in a close-fit silhouette.",
            "A bridal choker set styled on traditional polki work, available in gold-tone and antique gold-tone finishes. Artificial fashion jewellery.",
            "Polki-Inspired Bridal Choker Set | E.", "A polki-inspired bridal choker and earring set in two finishes.", "polki choker, bridal choker set",
            2, [new("GLD", "Gold-tone", "Gold", null, 4), new("ANT", "Antique gold-tone", "Antique Gold", null, 0)]), // Scenario D (variants) + F (one variant out of stock)

        new("BR-CH-003", "champagne-crystal-bridal-set", "Champagne Crystal Bridal Set", "bridal-jewellery", ProductType.Simple,
            18500m, 15900m, null, 9, 3, "Crystal", "Champagne", "Wedding", 160m, false, true, false, true,
            "Champagne-tone crystal necklace and earring set.",
            "A champagne-tone crystal bridal set with a statement necklace and matching drop earrings, suited to daytime wedding functions.",
            null, null, null, 1, null), // Scenario C

        new("BR-MR-004", "maroon-and-gold-bridal-set", "Maroon and Gold Bridal Set", "bridal-jewellery", ProductType.Simple,
            16900m, null, null, 4, 5, "Alloy", "Maroon", "Wedding", 150m, false, false, true, true,
            "A maroon and gold-tone bridal set for mehndi and reception functions.",
            "A maroon-stone bridal necklace set on a gold-tone base, paired with matching jhumka-style earrings.",
            null, null, null, 0, null), // Scenario F (low stock)

        new("BR-EM-005", "emerald-green-statement-bridal-necklace", "Emerald-Green Statement Bridal Necklace", "bridal-jewellery", ProductType.Simple,
            20900m, null, 11000m, 0, 3, "Alloy", "Emerald Green", "Wedding", 175m, false, false, false, false,
            "A statement emerald-green bridal necklace.",
            "A bold emerald-green statement necklace in a gold-tone setting, designed for bridal and reception wear. Currently unpublished while stock is replenished.",
            null, null, null, 0, null), // Scenario K (inactive/draft) + F (zero stock)

        // ---- Traditional Jewellery (4) ----
        new("TR-ME-001", "meenakari-jhumka-set", "Meenakari Jhumka Set", "traditional-jewellery", ProductType.Simple,
            7900m, 6900m, null, 15, 5, "Meenakari", "Multicolour", "Festive", 45m, true, false, false, true,
            "Hand-painted meenakari jhumka earrings and tikka set.",
            "A traditional meenakari-enamel jhumka and tikka set in a multicolour palette, suited to festive and mehndi occasions.",
            null, null, null, 0, null), // Scenario C

        new("TR-KD-002", "kundan-and-pearl-necklace", "Kundan and Pearl Necklace", "traditional-jewellery", ProductType.Simple,
            9500m, null, null, 10, null, "Kundan", "White", "Festive", null, false, true, false, true,
            "A kundan necklace trimmed with pearl drops.",
            "A traditional kundan necklace with hanging pearl-effect drops, finished in a gold-tone setting.",
            null, null, null, 0, null), // Scenario J (no weight set)

        new("TR-BN-003", "temple-style-bangles-set-of-2", "Temple-Style Bangles (Set of 2)", "traditional-jewellery", ProductType.Simple,
            4200m, null, null, 20, 5, "Alloy", "Gold", "Festive", 60m, false, false, false, true,
            "A pair of temple-motif bangles.",
            "Two temple-motif bangles in a gold-tone finish, sold as a set, suited to festive wear.",
            null, null, null, 0, null),

        new("TR-MP-004", "antique-gold-mathapatti", "Antique Gold Mathapatti", "traditional-jewellery", ProductType.Simple,
            6500m, null, null, 6, 5, "Alloy", "Antique Gold", "Festive", 55m, false, false, false, true,
            "An antique gold-tone mathapatti headpiece.",
            "A traditional mathapatti in an antique gold-tone finish with a centre maang tikka drop.",
            null, null, null, 0, null), // Scenario F (low stock)

        // ---- Party Wear Jewellery (4) ----
        new("PW-ST-001", "crystal-statement-earrings", "Crystal Statement Earrings", "party-wear-jewellery", ProductType.Simple,
            3200m, 2600m, null, 25, 5, "Crystal", "Clear", "Party", 18m, true, false, true, true,
            "Oversized crystal statement earrings.",
            "Oversized clear-crystal statement earrings designed to catch the light at evening events.",
            null, null, null, 0, null), // Scenario C

        new("PW-CR-002", "cocktail-ring-emerald-cut", "Cocktail Ring, Emerald Cut", "party-wear-jewellery", ProductType.Variant,
            2800m, null, null, 0, 5, "Alloy", null, "Party", null, false, true, false, true,
            "An emerald-cut cocktail ring in an adjustable band.",
            "A bold emerald-cut cocktail ring on an adjustable gold-tone band, available in two stone colours.",
            null, null, null, 0, [new("GRN", "Green stone", "Green", "Adjustable", 8), new("BLU", "Blue stone", "Blue", "Adjustable", 6)]), // Scenario D

        new("PW-NK-003", "layered-chain-necklace", "Layered Chain Necklace", "party-wear-jewellery", ProductType.Simple,
            3600m, null, null, 18, 5, "Alloy", "Gold", "Party", 25m, false, false, false, true,
            "A three-layer chain necklace.",
            "A three-layer gold-tone chain necklace of varying lengths, worn alone or over an outfit.",
            null, null, null, 0, null),

        new("PW-BR-004", "rhinestone-cuff-bracelet", "Rhinestone Cuff Bracelet", "party-wear-jewellery", ProductType.Simple,
            2400m, 1900m, null, 14, 5, "Alloy", "Silver", "Party", 30m, false, false, false, true,
            "An open rhinestone cuff bracelet.",
            "An open-cuff bracelet lined with rhinestones, in a silver-tone finish.",
            null, null, null, 0, null), // Scenario C

        // ---- Earrings: Studs (3) ----
        new("ER-ST-001", "minimal-pearl-studs", "Minimal Pearl Studs", "earrings-studs", ProductType.Simple,
            950m, null, null, 40, 10, "Pearl-effect", "White", "Daily", 4m, false, false, true, true,
            "Simple round pearl-effect studs.",
            "Simple round pearl-effect stud earrings for everyday wear.",
            null, null, null, 0, null), // Scenario M (low price tier)

        new("ER-ST-002", "gold-studs", "Gold Studs", "earrings-studs", ProductType.Simple,
            750m, null, null, 50, 10, null, "Gold", null, null, false, false, false, true,
            "Small gold-tone studs.",
            "Small round gold-tone stud earrings.",
            null, null, null, 0, null), // Scenario I (short name) + J (material/occasion null)

        new("ER-ST-003", "cubic-zirconia-stud-set", "Cubic Zirconia Stud Set", "earrings-studs", ProductType.Simple,
            1400m, 1100m, null, 22, 10, "Cubic Zirconia", "Clear", "Daily", 5m, false, true, false, true,
            "A set of three cubic zirconia studs.",
            "A set of three cubic zirconia stud pairs in graduated sizes, for mixing and matching.",
            null, null, null, 0, null), // Scenario C

        // ---- Earrings: Hoops (3) ----
        new("ER-HP-001", "classic-gold-hoops", "Classic Gold Hoops", "earrings-hoops", ProductType.Simple,
            1200m, null, null, 30, 10, "Alloy", "Gold", "Daily", 8m, false, false, true, true,
            "Classic medium gold-tone hoops.",
            "Classic medium-sized gold-tone hoop earrings for everyday wear.",
            null, null, null, 0, null),

        new("ER-HP-002", "oversized-statement-hoops", "Oversized Statement Hoops", "earrings-hoops", ProductType.Simple,
            1800m, null, null, 0, 5, "Alloy", "Gold", "Party", 14m, true, false, false, true,
            "Large statement hoop earrings.",
            "Large gold-tone statement hoops, designed as a standalone evening accent.",
            null, null, null, 0, null), // Scenario F (zero stock)

        new("ER-HP-003", "textured-huggie-hoops", "Textured Huggie Hoops", "earrings-hoops", ProductType.Simple,
            980m, null, null, 35, 10, "Alloy", "Silver", "Daily", 6m, false, true, false, true,
            "Small textured huggie hoops.",
            "Small textured huggie-style hoops in a silver-tone finish.",
            null, null, null, 0, null),

        // ---- Earrings: Jhumkas (4) ----
        new("ER-JH-001", "jhumka-drop-earrings-rose", "Jhumka Drop Earrings, Rose Gold", "earrings-jhumkas", ProductType.Simple,
            2200m, null, null, 16, 5, "Alloy", "Rose Gold", "Festive", 20m, false, false, false, true,
            "Rose gold-tone jhumka drop earrings.",
            "Traditional bell-shaped jhumka drop earrings in a rose gold-tone finish.",
            null, null, null, 0, null),

        new("ER-JH-002", "pearl-drop-jhumkas", "Pearl Drop Jhumkas", "earrings-jhumkas", ProductType.Simple,
            2600m, 2100m, null, 12, 5, "Pearl-effect", "White", "Festive", 22m, false, false, false, true,
            "Jhumka earrings with pearl-effect drops.",
            "Jhumka earrings finished with a fringe of pearl-effect drops.",
            null, null, null, 0, null), // Scenario C

        new("ER-JH-003", "chand-s-delight-crescent-jhumka-earrings", "Chand's Delight – Crescent Jhumka Earrings", "earrings-jhumkas", ProductType.Simple,
            2900m, null, null, 8, 5, "Alloy", "Gold", "Festive", 24m, true, true, false, true,
            "Crescent-topped jhumka earrings.",
            "Jhumka earrings with a crescent-moon top and a beaded fringe, in a gold-tone finish.",
            null, null, null, 0, null), // Scenario I (punctuation in name)

        new("ER-JH-004", "meenakari-jhumka-earrings-only", "Meenakari Jhumka Earrings", "earrings-jhumkas", ProductType.Simple,
            2400m, null, null, 0, 5, "Meenakari", "Multicolour", "Festive", 21m, false, false, false, false,
            "Hand-painted meenakari jhumka earrings.",
            "Meenakari-enamel jhumka earrings in a multicolour palette. Temporarily unpublished.",
            null, null, null, 0, null), // Scenario K (inactive) + F (zero stock)

        // ---- Earrings: Chandbalis (3) ----
        new("ER-CB-001", "classic-chandbali-earrings", "Classic Chandbali Earrings", "earrings-chandbalis", ProductType.Simple,
            2100m, null, null, 14, 5, "Alloy", "Gold", "Festive", 19m, false, false, true, true,
            "Classic crescent chandbali earrings.",
            "Classic crescent-shaped chandbali earrings in a gold-tone finish.",
            null, null, null, 0, null),

        new("ER-CB-002", "kundan-chandbali-earrings", "Kundan Chandbali Earrings", "earrings-chandbalis", ProductType.Simple,
            3400m, 2900m, null, 10, 5, "Kundan", "Gold", "Wedding", 23m, false, false, false, true,
            "Kundan-set chandbali earrings.",
            "Chandbali earrings set with kundan stones along the crescent, for bridal and festive wear.",
            null, null, null, 0, null), // Scenario C

        new("ER-CB-003", "silver-tone-chandbalis", "Silver-Tone Chandbalis", "earrings-chandbalis", ProductType.Simple,
            1700m, null, null, 20, 5, "Alloy", "Silver", "Daily", 16m, false, true, false, true,
            "Lightweight silver-tone chandbalis.",
            "A lighter-weight chandbali earring in a silver-tone finish, suited to daily wear.",
            null, null, null, 0, null),

        // ---- Necklaces: Sets (4) ----
        new("NK-ST-001", "pearl-and-kundan-necklace-set", "Pearl and Kundan Necklace Set", "necklaces-sets", ProductType.Simple,
            8900m, null, null, 11, 5, "Kundan", "White", "Wedding", 95m, true, false, false, true,
            "A pearl and kundan necklace and earring set.",
            "A necklace and earring set combining pearl-effect drops with kundan stonework.",
            null, null, null, 1, null),

        new("NK-ST-002", "festive-gift-jewellery-set", "Festive Gift Jewellery Set", "necklaces-sets", ProductType.Simple,
            3900m, 3200m, null, 16, 5, "Alloy", "Gold", "Festive", 50m, false, true, false, true,
            "A boxed necklace and earring set for gifting.",
            "A gold-tone necklace and earring set presented in a gift box, suited to Eid and festive gifting.",
            null, null, null, 0, null), // Scenario C

        new("NK-ST-003", "layered-pendant-necklace-set", "Layered Pendant Necklace Set", "necklaces-sets", ProductType.Variant,
            5400m, null, null, 0, 5, "Alloy", null, "Party", null, false, false, false, true,
            "A layered necklace and earring set.",
            "A layered pendant necklace and earring set, available in gold-tone and silver-tone finishes.",
            null, null, null, 0, [new("GLD", "Gold-tone", "Gold", null, 7), new("SLV", "Silver-tone", "Silver", null, 9)]), // Scenario D

        new("NK-ST-004", "multicolour-enamel-necklace-set", "Multicolour Enamel Necklace Set", "necklaces-sets", ProductType.Simple,
            4600m, null, null, 9, 5, "Enamel", "Multicolour", "Festive", 60m, false, false, false, true,
            "A multicolour enamel necklace and earring set.",
            "A necklace and earring set finished in hand-painted multicolour enamel.",
            null, null, null, 0, null),

        // ---- Necklaces: Chokers (3) ----
        new("NK-CH-001", "velvet-base-choker", "Velvet-Base Choker", "necklaces-chokers", ProductType.Simple,
            2800m, null, null, 17, 5, "Velvet, Alloy", "Black", "Party", 35m, false, false, false, true,
            "A stone-studded choker on a velvet base.",
            "A close-fit choker with stonework set on a velvet base.",
            null, null, null, 0, null),

        new("NK-CH-002", "polki-inspired-choker", "Polki-Inspired Choker", "necklaces-chokers", ProductType.Simple,
            7200m, 6400m, null, 7, 5, "Polki", "Gold", "Wedding", 110m, false, false, true, true,
            "A standalone polki-inspired choker.",
            "A standalone choker styled on traditional polki work, in a gold-tone finish.",
            null, null, null, 1, null), // Scenario C

        new("NK-CH-003", "minimal-layered-choker", "Minimal Layered Choker", "necklaces-chokers", ProductType.Simple,
            1900m, null, null, 24, 10, "Alloy", "Gold", "Daily", 15m, false, true, false, true,
            "A minimal two-layer choker.",
            "A minimal two-layer choker in a fine gold-tone chain.",
            null, null, null, 0, null),

        // ---- Necklaces: Pendant style (3) ----
        new("NK-PD-001", "floral-pendant-necklace", "Floral Pendant Necklace", "necklaces-pendant", ProductType.Simple,
            1600m, null, null, 28, 10, "Alloy", "Gold", "Daily", 12m, false, false, false, true,
            "A floral-motif pendant necklace.",
            "A delicate floral-motif pendant on a fine gold-tone chain.",
            null, null, null, 0, null),

        new("NK-PD-002", "solitaire-style-pendant-necklace", "Solitaire-Style Pendant Necklace", "necklaces-pendant", ProductType.Simple,
            1750m, 1450m, null, 20, 10, "Cubic Zirconia", "Clear", "Daily", 10m, false, false, false, true,
            "A single-stone solitaire-style pendant.",
            "A single cubic zirconia stone set in a solitaire-style pendant.",
            null, null, null, 0, null), // Scenario C

        new("NK-PD-003", "initial-letter-pendant-necklace", "Initial Letter Pendant Necklace", "necklaces-pendant", ProductType.Simple,
            1350m, null, null, 0, 10, "Alloy", "Gold", "Daily", 8m, false, true, false, true,
            "A personalised initial-letter pendant.",
            "A single initial letter pendant on a fine gold-tone chain.",
            null, null, null, 0, null), // Scenario F (zero stock)

        // ---- Pendants & Chains (3) ----
        new("PD-CN-001", "fine-gold-tone-chain", "Fine Gold-Tone Chain", "pendants-chains", ProductType.Simple,
            900m, null, null, 32, 10, "Alloy", "Gold", "Daily", 6m, false, false, false, true,
            "A fine standalone chain.",
            "A fine gold-tone chain, sold without a pendant, for layering or pairing with your own pendant.",
            null, null, null, 0, null),

        new("PD-CN-002", "box-chain-silver-tone", "Box Chain, Silver-Tone", "pendants-chains", ProductType.Simple,
            850m, null, null, 26, 10, "Alloy", "Silver", "Daily", 7m, false, false, false, true,
            "A standalone box-link chain.",
            "A silver-tone box-link chain, sold on its own.",
            null, null, null, 0, null),

        new("PD-CN-003", "crescent-moon-pendant", "Crescent Moon Pendant", "pendants-chains", ProductType.Simple,
            1100m, null, null, 19, 10, "Alloy", "Gold", "Daily", 5m, false, false, false, true,
            "A standalone crescent moon pendant.",
            "A small crescent moon pendant, sold with a matching fine chain.",
            null, null, null, 0, null),

        // ---- Maang Tikka & Mathapatti (3) ----
        new("MT-TK-001", "kundan-maang-tikka", "Kundan Maang Tikka", "maang-tikka-mathapatti", ProductType.Simple,
            2300m, null, null, 13, 5, "Kundan", "Gold", "Wedding", 18m, true, false, false, true,
            "A kundan-set maang tikka.",
            "A kundan-set maang tikka on an adjustable chain, for bridal and festive wear.",
            null, null, null, 0, null),

        new("MT-TK-002", "pearl-drop-maang-tikka", "Pearl Drop Maang Tikka", "maang-tikka-mathapatti", ProductType.Simple,
            1900m, 1500m, null, 15, 5, "Pearl-effect", "White", "Festive", 14m, false, false, false, true,
            "A maang tikka with a pearl-effect drop.",
            "A maang tikka finished with a single pearl-effect drop.",
            null, null, null, 0, null), // Scenario C

        new("MT-MP-003", "bridal-mathapatti-with-passa", "Bridal Mathapatti with Passa", "maang-tikka-mathapatti", ProductType.Simple,
            5800m, null, 3100m, 5, 5, "Alloy", "Gold", "Wedding", 70m, true, true, false, true,
            "A full mathapatti with side passa chains.",
            "A bridal mathapatti with side passa chains and a centre tikka drop, in a gold-tone finish.",
            "Bridal Mathapatti with Passa | E.", "A full bridal mathapatti headpiece with side passa chains.", "mathapatti, bridal headpiece, passa",
            1, null), // Scenario B-lite (SEO + gallery) + F (low stock)

        // ---- Daily Wear Jewellery (3) ----
        new("DW-ER-001", "lightweight-daily-hoops", "Lightweight Daily Hoops", "daily-wear-jewellery", ProductType.Simple,
            650m, null, null, 45, 15, "Alloy", "Gold", "Daily", 4m, false, false, true, true,
            "Lightweight everyday hoop earrings.",
            "Small, lightweight gold-tone hoops designed for all-day wear.",
            null, null, null, 0, null), // Scenario M (lowest price tier)

        new("DW-RG-002", "delicate-stacking-ring", "Delicate Stacking Ring", "daily-wear-jewellery", ProductType.Variant,
            550m, null, null, 0, 10, "Alloy", null, "Daily", null, false, true, false, true,
            "A delicate adjustable stacking ring.",
            "A thin, delicate ring designed for stacking, available in two tones.",
            null, null, null, 0, [new("GLD", "Gold-tone", "Gold", "Adjustable", 30), new("SLV", "Silver-tone", "Silver", "Adjustable", 28)]), // Scenario D

        new("DW-BR-003", "minimal-chain-bracelet", "Minimal Chain Bracelet", "daily-wear-jewellery", ProductType.Simple,
            700m, null, null, 38, 15, "Alloy", "Gold", "Daily", 6m, false, false, false, true,
            "A minimal everyday chain bracelet.",
            "A thin gold-tone chain bracelet for everyday wear.",
            null, null, null, 0, null),

        // ---- Bangles: Traditional (3) ----
        new("BG-TR-001", "classic-rose-gold-bangle", "Classic Rose Gold Bangle", "bangles-traditional", ProductType.Variant,
            1600m, null, null, 0, 5, "Alloy", "Rose Gold", "Festive", null, false, false, false, true,
            "A classic rose gold-tone bangle, sized to fit.",
            "A classic rose gold-tone bangle in a plain band, available in three sizes.",
            null, null, null, 0, [new("24", "Size 2.4", "Rose Gold", "2.4", 6), new("26", "Size 2.6", "Rose Gold", "2.6", 9), new("28", "Size 2.8", "Rose Gold", "2.8", 4)]), // Scenario D (size variants)

        new("BG-TR-002", "rose-gold-bangle-wide-cut", "Rose Gold Bangle – Wide Cut", "bangles-traditional", ProductType.Simple,
            2100m, null, null, 12, 5, "Alloy", "Rose Gold", "Festive", 45m, false, false, false, true,
            "A wide-cut rose gold-tone bangle.",
            "A wider-cut version of our rose gold-tone bangle, for a bolder look.",
            null, null, null, 0, null), // Scenario I (near-duplicate name needing a distinct slug)

        new("BG-TR-003", "multicolour-enamel-bangles-set-of-4", "Multicolour Enamel Bangles (Set of 4)", "bangles-traditional", ProductType.Simple,
            3200m, 2700m, null, 10, 5, "Enamel", "Multicolour", "Festive", 80m, false, true, false, true,
            "A stacked set of four enamel bangles.",
            "Four hand-painted enamel bangles in a multicolour palette, sold as a stacking set.",
            null, null, null, 0, null), // Scenario C

        // ---- Bangles: Kada (2) ----
        new("BG-KD-001", "single-gold-tone-kada", "Single Gold-Tone Kada", "bangles-kada", ProductType.Simple,
            2900m, null, null, 8, 5, "Alloy", "Gold", "Festive", 95m, false, false, true, true,
            "A single wide kada bangle.",
            "A single, wide gold-tone kada bangle with a textured surface.",
            null, null, null, 0, null),

        new("BG-KD-002", "engraved-silver-tone-kada", "Engraved Silver-Tone Kada", "bangles-kada", ProductType.Simple,
            2600m, null, null, 0, 5, "Alloy", "Silver", "Festive", 90m, false, false, false, true,
            "An engraved silver-tone kada.",
            "A wide kada bangle in a silver-tone finish with an engraved pattern.",
            null, null, null, 0, null), // Scenario F (zero stock)

        // ---- Bracelets (4) ----
        new("BC-BR-001", "crystal-tennis-bracelet", "Crystal Tennis Bracelet", "bracelets", ProductType.Simple,
            2200m, 1800m, null, 16, 5, "Crystal", "Clear", "Party", 20m, false, false, true, true,
            "A crystal tennis-style bracelet.",
            "A single-row crystal tennis-style bracelet in a silver-tone setting.",
            null, null, null, 0, null), // Scenario C

        new("BC-BR-002", "beaded-stretch-bracelet-set", "Beaded Stretch Bracelet Set", "bracelets", ProductType.Simple,
            800m, null, null, 30, 10, "Glass Bead", "Multicolour", "Daily", 12m, false, false, false, true,
            "A stacking set of beaded stretch bracelets.",
            "A set of three beaded stretch bracelets in a mixed colour palette, for stacking.",
            null, null, null, 0, null),

        new("BC-BR-003", "charm-bracelet-gold-tone", "Charm Bracelet, Gold-Tone", "bracelets", ProductType.Simple,
            1500m, null, null, 20, 10, "Alloy", "Gold", "Daily", 18m, false, true, false, true,
            "A gold-tone charm bracelet.",
            "A gold-tone chain bracelet with three decorative charms.",
            null, null, null, 0, null),

        new("BC-BR-004", "adjustable-cuff-bracelet", "Adjustable Cuff Bracelet", "bracelets", ProductType.Simple,
            1300m, null, null, 0, 5, "Alloy", "Silver", "Party", 25m, false, false, false, false,
            "An open, adjustable cuff bracelet.",
            "An open-ended adjustable cuff bracelet in a silver-tone finish. Currently unpublished.",
            null, null, null, 0, null), // Scenario K (inactive) + F (zero stock)

        // ---- Rings: Statement (3) ----
        new("RG-ST-001", "statement-floral-ring", "Statement Floral Ring", "rings-statement", ProductType.Simple,
            1200m, null, null, 22, 10, "Alloy", "Gold", "Party", 9m, false, false, false, true,
            "A floral-motif statement ring.",
            "A floral-motif statement ring in a gold-tone finish.",
            null, null, null, 0, null),

        new("RG-ST-002", "oversized-cocktail-ring", "Oversized Cocktail Ring", "rings-statement", ProductType.Simple,
            1600m, 1300m, null, 11, 5, "Alloy", "Silver", "Party", 11m, false, false, false, true,
            "A large single-stone cocktail ring.",
            "An oversized cocktail ring with a single large stone, in a silver-tone setting.",
            null, null, null, 0, null), // Scenario C

        new("RG-ST-003", "double-finger-ring", "Double Finger Ring", "rings-statement", ProductType.Simple,
            1450m, null, null, 0, 5, "Alloy", "Gold", "Party", 10m, false, true, false, true,
            "A connected double-finger ring.",
            "A statement ring connecting two fingers with a chain link, in a gold-tone finish.",
            null, null, null, 0, null), // Scenario F (zero stock)

        // ---- Rings: Adjustable (3) ----
        new("RG-AD-001", "adjustable-floral-ring", "Adjustable Floral Ring", "rings-adjustable", ProductType.Simple,
            450m, null, null, 48, 15, "Alloy", "Gold", "Daily", 4m, true, false, true, true,
            "A one-size adjustable floral ring.",
            "A simple floral-motif ring on an adjustable band, one size fits most.",
            null, null, null, 0, null), // Scenario M (cheapest item in the catalogue)

        new("RG-AD-002", "minimal-band-ring", "Minimal Band Ring", "rings-adjustable", ProductType.Simple,
            400m, null, null, 52, 15, "Alloy", "Silver", "Daily", 3m, false, false, false, true,
            "A plain minimal adjustable band ring.",
            "A plain, thin adjustable band ring in a silver-tone finish.",
            null, null, null, 0, null),

        new("RG-AD-003", "adjustable-knot-ring", "Adjustable Knot Ring", "rings-adjustable", ProductType.Simple,
            500m, null, null, 40, 15, "Alloy", "Gold", "Daily", 4m, false, true, false, true,
            "A knot-motif adjustable ring.",
            "A knot-motif ring on an adjustable band, in a gold-tone finish.",
            null, null, null, 0, null),

        // ---- Gift Collections (2) ----
        new("GF-JW-001", "gifts-under-2000-earring-duo", "Gifts Under PKR 2,000: Earring Duo", "gift-collections", ProductType.Simple,
            1850m, null, null, 24, 10, "Alloy", "Gold", "Gift", 10m, false, true, false, true,
            "Two pairs of earrings, gift-boxed.",
            "A gift-boxed duo of stud and hoop earrings, priced under PKR 2,000.",
            null, null, null, 0, null),

        new("GF-JW-002", "birthday-gift-jewellery-box", "Birthday Gift Jewellery Box", "gift-collections", ProductType.Simple,
            3500m, null, null, 15, 5, "Alloy", "Gold", "Gift", 35m, true, false, false, true,
            "A boxed necklace and earring gift set.",
            "A necklace and earring set presented in a keepsake gift box, suited to birthdays.",
            "Birthday Gift Jewellery Box | E.", "A gift-boxed necklace and earring set for birthdays.", "jewellery gift, birthday gift, gift set",
            0, null),
    ];
}
