# Seed data plan: demo jewellery catalogue

## 1. What was inspected

- **Add/Edit Product form:** `frontend/components/admin/ProductForm.jsx`, backed by
  `AdminProductRequest` / `AdminProductDto` (`backend/Elegance.Application/Admin/AdminContracts.cs`)
  and `AdminCatalogService` (`backend/Elegance.Infrastructure/Admin/AdminCatalogService.cs`).
- **Entities:** `Product`, `ProductVariant`, `Category`, `ProductImage`, `MediaFile`
  (`backend/Elegance.Domain/Catalog/`, `backend/Elegance.Domain/Media/`).
- **Category management:** single-parent hierarchy via `Category.ParentCategoryId`, admin
  endpoints in `AdminCategoriesController`.
- **Image upload:** `AdminMediaService` — real JPEG/PNG/WebP uploads, 1000×1250px minimum,
  8MB cap (see `docs/MEDIA-STORAGE.md`). Not something a seeder can produce from literal data
  without a real decodable file, so seed images continue the placeholder convention the existing
  `DevelopmentSeeder` already uses (see §5).
- **Inventory:** `StockMovement` ledger, `StockMovementReason.Adjustment` for manual/opening
  stock (`AdminInventoryService`, `AdminCatalogService.Movement()`).
- **Existing seed mechanism:** `DevelopmentSeeder.cs` — gated on "no categories exist yet",
  seeds exactly 3 categories (Rings, Necklaces, Earrings) and 3 products. Still runs first and is
  left untouched; this plan adds a second, separately-gated seeder on top of it.
- **Public catalogue filtering:** `ProductService.GetProductsAsync` / `GetProductBySlugAsync`
  both filter `Where(p => p.IsActive)` — inactive products are already excluded from public
  responses, confirmed by reading the code, not assumed.

**Source of truth:** the existing `Product`/`ProductVariant`/`Category` schema and the
`AdminProductRequest` contract. Nothing below adds a column or table just to make seed data fit.

## 2. Fields the schema actually supports

| Field | Product | Variant | Category |
|---|---|---|---|
| SKU / slug | ✓ unique | ✓ unique | slug ✓ unique |
| Name, short/long description | ✓ | name only | ✓ |
| Price, sale price, cost price | ✓ | price/sale override | — |
| Stock quantity, low-stock threshold | ✓ (simple products) | ✓ | — |
| Material, colour, occasion | ✓ | colour, size only | — |
| Weight (grams) | ✓ | ✓ | — |
| Featured / new / best-seller / active flags | ✓ | active only | active only |
| SEO title/description/keywords | ✓ | — | title/description exist on the entity, **not exposed by `AdminCategoryRequest`** |
| Video URL | ✓ | — | — |
| Images | Primary / Hover / Gallery via `ProductImage` | one photo via `ImageMediaFile` | one photo via `ImageMediaFileId`, **not exposed by `AdminCategoryRequest`** |
| Parent/child hierarchy | — | — | ✓ `ParentCategoryId` |

## 3. Gaps versus the generic brief (documented, not worked around)

The requesting brief assumes a generic ecommerce schema. Where this project's schema differs,
this plan follows the schema rather than inventing new columns for a seeder:

- **One category per product.** `Product.CategoryId` is a single foreign key, not a many-to-many
  mapping. "Scenario E: multiple categories" is not implemented; each seed product gets its single
  most-specific applicable category instead.
- **No sale date range.** `SalePrice` is either set or not — there's no start/end date, so a
  "scheduled sale" scenario isn't modeled. Discounted products just have `SalePrice < Price`,
  active immediately.
- **No draft status distinct from "inactive".** There's one `IsActive` flag, and the public API
  already excludes `IsActive = false` products (verified in `ProductService`). "Draft" and
  "deactivated" are the same state in this schema, so Scenario K and Scenario N's
  draft/deactivated cases are both represented as `IsActive = false`.
- **No generic attribute table.** Colour, material, occasion, size, and weight are plain columns,
  not a key/value attribute system. Seed data populates those columns directly.
- **Category SEO fields and category images exist on the entity but aren't in
  `AdminCategoryRequest`.** The seeder (writing directly via `DbContext`, not the HTTP contract)
  populates `SeoTitle`/`SeoDescription` on a few categories to demonstrate the column exists, but
  the admin UI has no field to edit them yet — flagged here as a product-admin gap, not fixed as
  part of this task.

## 4. Category hierarchy

Reuses the 3 existing top-level categories from `DevelopmentSeeder` untouched (`rings`,
`necklaces`, `earrings` — found by slug, never recreated or renamed). Adds, upserted by slug so
re-running never duplicates:

**New top-level:** Bridal Jewellery, Traditional Jewellery, Party Wear Jewellery, Bangles &
Bracelets, Pendants & Chains, Maang Tikka & Mathapatti, Daily Wear Jewellery, Gift Collections.

**New subcategories:** Earrings → Studs, Hoops, Jhumkas, Chandbalis. Necklaces → Necklace Sets,
Chokers, Pendant Necklaces. Rings → Statement Rings, Adjustable Rings. Bangles & Bracelets →
Traditional Bangles, Kada, Bracelets.

23 categories total (3 existing + 8 new top-level + 12 new subcategories).

## 5. Images

Continues the existing `DevelopmentSeeder` convention exactly: a `MediaFile` row per image
pointing at a deterministic `picsum.photos/seed/<key>/...` placeholder URL, `StorageKey` under
`seed/`, clearly documented as non-representative dev-only placeholder photography (same comment
block as the existing seeder). The brief's instruction to avoid "invented CDN URLs" is about not
*pretending* seeded placeholders are real hosted product photography; `picsum.photos` is already
how this exact codebase marks placeholder images, so this plan keeps that one convention rather
than introducing a second, inconsistent one. Each product's placeholder seed key is derived from
its SKU, so every product gets a distinct (still placeholder) image rather than visually
duplicate ones.

## 6. ~60 products across categories and scenarios

One data table in `CatalogDemoSeeder.cs`, upserted by SKU (skip if the SKU already exists — never
overwrites an admin-edited product). Scenario coverage:

- **A Basic:** several simple products, required fields only, one image.
- **B Premium bridal:** detailed description, 3+ gallery images, SEO fields populated, premium
  material/occasion.
- **C Discounted:** `SalePrice < Price` across several items at different price tiers.
- **D Variants:** colour/size options with per-variant SKU, stock, and (for some) price override.
- **F Stock situations:** in-stock, at/below `LowStockThreshold`, and zero-stock examples.
- **G Flags:** featured, new, best-seller, and plain ordinary products in different combinations.
- **H Attributes:** material/colour/occasion/weight populated with real variety, not copy-pasted.
- **I SEO/slug edge cases:** a long name, a short name, a punctuated name, and two similarly-named
  products that need distinguishable slugs.
- **J Optional fields:** some products with every optional field populated, others with
  legitimately empty optional fields (no short description, no weight, no material).
- **K Inactive:** a few products seeded with `IsActive = false`, verified excluded from the public
  API.
- **L Images:** mix of Primary+Hover only vs. Primary+Hover+Gallery, plus variant-level photos.
- **M Pricing:** from a cheap daily-wear piece (~PKR 450) to a premium bridal set (~PKR 24,900).
  No invalid records (e.g. `SalePrice > Price`) in the seed set.

SKUs follow the category-prefixed convention from the brief (`BR-KD-###`, `ER-JH-###`,
`NK-CH-###`, etc.), checked for uniqueness against both the new table and the existing `EL-*`
products at write time.

## 7. Idempotency and environment guard

- `CatalogDemoSeeder.SeedAsync` runs only when `app.Environment.IsDevelopment()` **and**
  `Seed:LoadDemoCatalog` is `true` in configuration — off by default, so an ordinary `dotnet run`
  in Development still only gets the original 3-product baseline unless explicitly opted in. This
  keeps existing dev setups and tests that assume the small baseline unaffected.
- Categories are upserted by slug, products by SKU: existing rows are left untouched, missing ones
  are inserted. Running it twice produces the same end state, not duplicates.
- Reports inserted/skipped counts to the console on completion.
- Never deletes anything, never touches `IsActive`/pricing on a row that already exists (an admin
  may have since edited it).

## 8. What this does not cover

- Image files aren't real photographs — see §5. Replace with real product photography before
  any public use, exactly as flagged when this catalogue work started.
- No synthetic orders/customers are added by this seeder; that's a separate concern from the
  product catalogue and isn't part of this task.
- Admin UI still can't edit category SEO fields or category images (see §3) — worth a follow-up
  if those are wanted.
