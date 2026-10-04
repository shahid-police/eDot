# DATABASE-DESIGN.md — E. (elegence.pk) MySQL 8.4 Schema Proposal

Status: **Proposed, not yet implemented.** No migrations have been created. Pending your review.

Conventions: `BIGINT UNSIGNED AUTO_INCREMENT` surrogate PKs everywhere (simple, EF Core-friendly, avoids UUID index fragmentation at this scale); `utf8mb4` everywhere for proper emoji/Urdu-script support in product names/reviews; `created_at`/`updated_at` (`DATETIME(6)`, UTC) on every table that benefits from audit history; soft concepts (`is_active`) preferred over hard deletes for catalog/customer data, hard deletes fine for cart/session ephemera.

---

## 1. Catalog

### `categories`
`id, name, slug (unique), description, parent_category_id (nullable FK → categories.id, self-referencing for hierarchy), image_media_file_id (nullable FK → media_files.id), sort_order, seo_title, seo_description, is_active, created_at, updated_at`
Index: `slug` (unique), `parent_category_id`.

### `collections`
Curated merchandising groups independent of the category tree (e.g. "Eid Collection", "Bridal 2026") — brief §6 lists these separately from categories.
`id, name, slug (unique), description, banner_media_file_id, is_active, starts_at, ends_at, created_at, updated_at`

### `products`
`id, sku (unique), name, slug (unique), short_description, description, price (DECIMAL(10,2)), sale_price (nullable DECIMAL(10,2)), cost_price (nullable DECIMAL(10,2)), currency (CHAR(3), default 'PKR'), category_id (FK → categories.id), product_type (enum: Simple, Variant), material, color (nullable — only meaningful for simple/no-variant products), occasion, gender (nullable enum), weight_grams (nullable DECIMAL), is_featured, is_new, is_best_seller, is_active, stock_quantity (nullable — authoritative stock lives in `inventory_items`; this column is a denormalized read-optimization for simple products without variants, kept in sync by the same transaction), low_stock_threshold, seo_title, seo_description, seo_keywords, created_at, updated_at`
Indexes: `sku` (unique), `slug` (unique), `category_id`, `is_active`, `is_featured`, `is_new`, `is_best_seller`, `created_at`.

### `product_variants`
Only populated for `product_type = Variant` products (brief §7: "do not assume every jewellery product needs variants").
`id, product_id (FK), sku (unique), variant_name (e.g. "Rose Gold / Size 50"), price (nullable — overrides product.price if set), sale_price (nullable), stock_quantity (denormalized, see inventory note above), weight_grams (nullable), size (nullable), color (nullable), image_media_file_id (nullable FK → media_files.id), is_active, created_at, updated_at`
Index: `product_id`, `sku` (unique).

### `product_images`
`id, product_id (FK), variant_id (nullable FK → product_variants.id), media_file_id (FK → media_files.id), sort_order, image_type (enum: Primary, Hover, Gallery, Swatch), created_at`
Index: `product_id`, `variant_id`.

### `product_attributes` / `product_attribute_values`
Flexible facets beyond the hardcoded columns above (e.g. "Gemstone", "Finish", "Chain Length") so new jewellery-specific facets don't require schema migrations every time.
- `product_attributes`: `id, name (unique), display_name, is_filterable`
- `product_attribute_values`: `id, product_id (FK), attribute_id (FK), value` — index on `(attribute_id, value)` for filter queries, `(product_id)` for product page.

### `product_tags` / `product_tag_mappings`
`product_tags`: `id, name (unique), slug (unique)`
`product_tag_mappings`: `product_id (FK), tag_id (FK)` — composite PK `(product_id, tag_id)`.

---

## 2. Inventory

### `warehouses`
Deferred complexity — brief says "if required." Start with a single implicit warehouse row; table exists so multi-warehouse is additive later, not a rewrite.
`id, name, address, is_active`

### `inventory_items`
Authoritative stock ledger. One row per sellable unit (product without variants, or each variant).
`id, product_id (FK, nullable if variant-based), variant_id (nullable FK), warehouse_id (FK, default row), quantity_on_hand, quantity_reserved, row_version (for optimistic concurrency on checkout), updated_at`
Unique index on `(product_id, variant_id, warehouse_id)`. `quantity_available` is computed as `quantity_on_hand - quantity_reserved` at query time, not stored.

### `stock_movements`
Append-only audit trail — never updated/deleted.
`id, inventory_item_id (FK), movement_type (enum: Purchase, Sale, Return, Adjustment, Reservation, ReservationRelease), quantity_delta (signed), reference_type (e.g. 'Order'), reference_id (nullable), note, created_by_user_id (nullable FK → users.id), created_at`
Index: `inventory_item_id`, `created_at`.

### `inventory_reservations`
Short-lived holds during active checkout, to prevent overselling between "add to cart" and "order confirmed."
`id, inventory_item_id (FK), cart_id (nullable FK), order_id (nullable FK), quantity, expires_at, created_at`
Index: `inventory_item_id`, `expires_at` (for a cleanup job).

---

## 3. Customers

### `customers`
`id, email (unique), phone (nullable, unique if present), password_hash, first_name, last_name, is_active, email_verified_at (nullable), created_at, updated_at`
Note: **never** return `password_hash` from any DTO.

### `customer_addresses`
`id, customer_id (FK), label (e.g. "Home"), full_name, phone, address_line1, address_line2, city, province, postal_code, country (default 'PK'), is_default_shipping, is_default_billing, created_at, updated_at`
Index: `customer_id`.

### `customer_sessions` / `refresh_tokens`
`id, customer_id (FK), token_hash (unique — never store the raw token), expires_at, revoked_at (nullable), replaced_by_token_id (nullable), created_by_ip, created_at`
Index: `customer_id`, `token_hash` (unique).

### `wishlists` / `wishlist_items`
`wishlists`: `id, customer_id (FK, unique — one wishlist per customer), created_at`
`wishlist_items`: `id, wishlist_id (FK), product_id (FK), variant_id (nullable FK), added_at` — unique `(wishlist_id, product_id, variant_id)`.

### `recently_viewed_products`
`id, customer_id (nullable FK — null for guest, keyed by cart/session id instead), session_id (nullable), product_id (FK), viewed_at`
Index: `(customer_id, viewed_at)`, `(session_id, viewed_at)`. Consider a retention job (e.g. keep last 20 per customer) rather than unbounded growth.

---

## 4. Shopping

### `carts`
`id, customer_id (nullable FK — null = guest cart), guest_token (nullable unique, the opaque cookie value), status (enum: Active, ConvertedToOrder, Abandoned), created_at, updated_at`
Index: `customer_id`, `guest_token` (unique).

### `cart_items`
`id, cart_id (FK), product_id (FK), variant_id (nullable FK), quantity, unit_price_snapshot (DECIMAL — last known price, display-only; **never trusted at checkout**, always re-read live), added_at, updated_at`
Unique `(cart_id, product_id, variant_id)`.

### `coupons`
`id, code (unique), discount_type (enum: Percentage, FixedAmount), discount_value, min_order_amount (nullable), max_uses (nullable), max_uses_per_customer (nullable), starts_at, ends_at, is_active, created_at`

### `coupon_usages`
`id, coupon_id (FK), customer_id (nullable FK), order_id (FK), used_at` — index `(coupon_id, customer_id)` to enforce per-customer limits.

---

## 5. Orders

### `orders`
`id, order_number (unique, human-readable e.g. "E-2026-000123"), customer_id (nullable FK — guest checkout allowed), email, phone, subtotal, discount_total, shipping_total, tax_total, grand_total, currency, coupon_id (nullable FK), status (enum: Pending, Confirmed, Processing, Packed, Shipped, OutForDelivery, Delivered, Cancelled, Returned, Refunded), notes (nullable, customer delivery notes), created_at, updated_at`
Index: `order_number` (unique), `customer_id`, `status`, `created_at`.

### `order_items`
Snapshot of product data at time of purchase — **never joins back to live product for historical display**, since prices/names can change later.
`id, order_id (FK), product_id (FK, nullable-safe even if product later deleted), variant_id (nullable FK), sku_snapshot, product_name_snapshot, unit_price, quantity, line_total`

### `order_addresses`
`id, order_id (FK), address_type (enum: Shipping, Billing), full_name, phone, address_line1, address_line2, city, province, postal_code, country`

### `order_status_history`
Append-only — brief §15 explicitly requires never overwriting history.
`id, order_id (FK), status, note (nullable), changed_by_user_id (nullable FK → users.id, null if system/customer-triggered), created_at`
Index: `order_id`.

### `payments` / `payment_transactions`
`payments`: `id, order_id (FK), method (enum: CashOnDelivery, BankTransfer, OnlineGateway), status (enum: Pending, Authorized, Captured, Failed, Refunded), amount, currency, created_at, updated_at`
`payment_transactions`: `id, payment_id (FK), provider (e.g. 'manual', future gateway name), provider_transaction_id (nullable), raw_status, amount, occurred_at` — this table absorbs gateway-specific detail so `payments` stays provider-agnostic (brief §14: never hard-code one payment provider).

### `shipments` / `shipment_tracking`
`shipments`: `id, order_id (FK), carrier (nullable), tracking_number (nullable), status, shipped_at (nullable), delivered_at (nullable)`
`shipment_tracking`: `id, shipment_id (FK), status, location (nullable), note (nullable), occurred_at` — append-only event log.

---

## 6. Content

- `banners`: `id, title, image_media_file_id (FK), link_url, sort_order, placement (enum: Home, Category, Sale, ...), starts_at, ends_at, is_active`
- `promotional_sections`: `id, title, subtitle, cta_text, cta_url, image_media_file_id, sort_order, is_active` — backs the various homepage marketing blocks.
- `faqs`: `id, question, answer, sort_order, is_active`
- `pages`: `id, slug (unique), title, body (long text/HTML), seo_title, seo_description, is_active` — for About Us, policies, etc.
- `reviews`: `id, product_id (FK), customer_id (nullable FK), rating (TINYINT 1-5), title (nullable), body, is_approved, created_at` — moderation flag so reviews don't go live unmoderated.

---

## 7. Administration

- `users`: `id, email (unique), password_hash, first_name, last_name, is_active, created_at, updated_at` — **separate table from `customers`**, since admin accounts should never be reachable via the customer auth surface.
- `roles`: `id, name (unique)` — seed: SuperAdmin, Admin, CatalogManager, OrderManager, InventoryManager, ContentManager.
- `permissions`: `id, name (unique)` (optional fine-grained layer — start with role-only checks, add permission rows only if role-level RBAC proves insufficient).
- `user_roles`: `(user_id, role_id)` composite PK.
- `audit_logs`: `id, user_id (nullable FK), action, entity_type, entity_id (nullable), changes_json (nullable), ip_address, created_at` — every admin mutation (product edit, order status change, inventory adjustment) writes here.

---

## 8. System

- `media_files`: `id, storage_key (unique), url, cdn_url (nullable), original_filename, mime_type, width, height, file_size_bytes, alt_text (nullable), created_at` — referenced by `product_images`, `categories.image_media_file_id`, `banners.image_media_file_id`, etc. No BLOBs ever.
- `settings`: `key (PK, varchar)`, `value (text)` — simple key/value store for site-wide config that shouldn't require a redeploy (e.g. free-shipping threshold, tax rate default).

---

## 9. Indexing Summary (brief §20)

Already called out inline above; consolidated list of the indexes that matter most for query performance at launch:
- `products.sku`, `products.slug` (unique, hot path for every PDP load)
- `products.category_id`, `products.is_active`, `products.is_featured/is_new/is_best_seller` (listing/homepage queries)
- `product_variants.product_id`
- `inventory_items(product_id, variant_id, warehouse_id)` unique
- `carts.guest_token` unique, `carts.customer_id`
- `orders.order_number` unique, `orders.customer_id`, `orders.status`, `orders.created_at`
- `customers.email` unique
- `refresh_tokens.token_hash` unique

No full-text/Elasticsearch index planned initially — MySQL `FULLTEXT` index on `products(name, description)` (or a simpler `LIKE`/prefix-index approach) is sufficient for Phase 12 search volume; revisit only if search becomes a demonstrated bottleneck (brief §12).

---

## 10. Open Design Questions for You

1. **Simple vs. variant products**: proposing a `product_type` enum (`Simple` | `Variant`) on `products` so the API/UI know whether to render a variant picker at all — matches brief §7's instruction not to assume every product has variants. Confirm this matches your mental model of the catalogue (e.g. a plain pendant = Simple, a ring with multiple sizes = Variant).
2. **Guest checkout**: schema allows `orders.customer_id` to be nullable (guest checkout without an account). Confirm this is desired for launch, or whether account creation should be mandatory at checkout.
3. **Reviews moderation**: `reviews.is_approved` defaults to requiring manual approval before showing publicly — confirm that's the desired policy vs. auto-publish.
