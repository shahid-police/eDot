# Seed data guide: demo jewellery catalogue

See `docs/SEED-DATA-PLAN.md` for the inspection findings, the schema gaps this works around (or
deliberately doesn't), and the scenario coverage this seed data was designed to hit.

## What gets created

Running the seeder (see below) against an empty dev database produces:

- **23 categories** — the original 3 (Rings, Necklaces, Earrings) plus 8 new top-level categories
  and 12 subcategories, in a proper parent/child hierarchy.
- **62 products**, on top of the original 3 from `DevelopmentSeeder`, across every category.
  - **8 variant products** (colour and/or size options, including one option deliberately at zero
    stock) and 54 simple products.
  - **~20 discounted** (sale price below regular price), the rest at regular price.
  - **3 inactive/draft** products — confirmed excluded from the public API (`GET .../products`
    and `GET .../products/{slug}` both return them as not found).
  - Stock levels spanning healthy, at/below each product's low-stock threshold, and zero.
  - `featured` / `new` / `best-seller` flags in varied combinations, plus plain catalogue items.
  - Prices from PKR 400 (adjustable band ring) to PKR 24,900 (bridal sets).
  - A few SEO/slug edge cases: a short name ("Gold Studs"), a punctuated name ("Chand's Delight –
    Crescent Jhumka Earrings"), a long descriptive name, and two similarly-named rose-gold bangles
    with distinct slugs.
- **Images** are `picsum.photos` placeholders, one deterministic seed per product (and per variant
  with its own photo) — visually distinct per product, clearly non-representative of real
  inventory, same convention the original 3-product seed already used.
- **Stock ledger rows** (`StockMovement`, reason `Adjustment`, note "Opening stock (demo catalogue
  seed)") for every product/variant that starts with stock above zero.

## How to run it

The seeder is off by default — an ordinary `dotnet run` in Development still only gets the
original 3-product baseline. To load the full demo catalogue, set the opt-in flag when starting
the API:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:Seed__LoadDemoCatalog = "true"
dotnet run --project backend/Elegance.Api
```

It only runs inside the existing `if (app.Environment.IsDevelopment())` block in `Program.cs`, so
it's structurally impossible for it to run in production regardless of this flag.

## Resetting / re-running

Safe to run repeatedly. Categories are upserted by slug and products by SKU: a row that already
exists (including one you've since edited through the admin panel) is left untouched, never
duplicated or overwritten. Verified by running it twice in a row:

```
[CatalogDemoSeeder] categories added: 20, products added: 62, products skipped (already existed): 0
[CatalogDemoSeeder] categories added: 0, products added: 0, products skipped (already existed): 62
```

To remove the demo catalogue and start clean, drop and recreate the dev database, then run the
migrations and this seeder again — there's no "unseed" command, by design (this seeder only ever
inserts, matching "never delete or overwrite data").

## Verified

- Build: 0 warnings, 0 errors.
- Both seeder runs above, confirming idempotency.
- Public product list total count increased by exactly 62 after the first run, unchanged after
  the second.
- Category hierarchy fetched via `GET /api/v1/categories` matches the planned 23-category tree
  exactly (8 new top-level categories, 12 new subcategories under the right parents).
- A variant product (`polki-inspired-choker-set`) returns one option in stock and one correctly
  marked out of stock.
- A discounted product (`royal-kundan-bridal-set`) returns both `price` and `salePrice`, and its 3
  extra gallery images alongside Primary/Hover (5 images total).
- An inactive product (`emerald-green-statement-bridal-necklace`) returns 404 from the public
  detail endpoint.
- Rendered in the browser on `product-default/[slug]`: correct price/discount display, correct
  breadcrumb category, correct image gallery, correct variant option picker.

## Known limitations (see SEED-DATA-PLAN.md §3 for the full list)

- Every product has exactly one category — the schema doesn't support more than one.
- Discounts have no start/end date — `salePrice` is either set or it isn't.
- "Draft" and "deactivated" are the same thing (`isActive = false`) — there's no separate draft
  status in the schema.
- Category SEO title/description were set directly via EF for a couple of categories to
  demonstrate the columns exist, but the admin UI has no field to edit them yet.
