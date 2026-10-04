# ARCHITECTURE.md — E. (elegence.pk) Backend & Integration Architecture

Status: **Proposed, not yet implemented.** Pending your review per Phase 1 scope.

---

## 1. High-Level Shape

```
frontend/ (existing Vemus Next.js app — preserved)
   │  frontend/lib/api/*.js   (new: centralized fetch layer)
   ▼
   REST over HTTPS, JSON, /api/v1/*
   ▼
backend/ (new modular monolith — ASP.NET Core / .NET 10)
   Elegance.Api            → controllers, auth, validation, Swagger
   Elegance.Application    → DTOs, services, business rules, validation
   Elegance.Domain         → entities, value objects, enums, invariants
   Elegance.Infrastructure → EF Core, MySQL, S3 storage, Redis, email/SMS, payment gateways
   ▼
MySQL 8.4 (system of record) · Redis (hot-path cache only) · S3-compatible storage (images) · CDN (Cloudflare-compatible)
```

Single deployable API (modular monolith), not microservices. No Elasticsearch, no Kubernetes — MySQL-native search/filtering is sufficient at this scale.

---

## 2. Backend Project Structure

```
backend/
├── Elegance.sln (or .slnx)
├── Elegance.Api/
│   ├── Controllers/
│   │   ├── V1/ (ProductsController, CategoriesController, CollectionsController,
│   │   │        CartController, OrdersController, AuthController, WishlistController,
│   │   │        Admin/ProductsController, Admin/OrdersController, Admin/InventoryController)
│   ├── Middleware/ (ExceptionHandlingMiddleware, RequestLoggingMiddleware)
│   ├── Filters/ (ValidationFilter)
│   ├── Extensions/ (ServiceCollectionExtensions — DI wiring per layer)
│   └── Program.cs
├── Elegance.Application/
│   ├── Products/ (Dtos, IProductService, ProductService, Validators)
│   ├── Categories/
│   ├── Cart/
│   ├── Orders/
│   ├── Auth/
│   ├── Wishlist/
│   ├── Inventory/
│   └── Common/ (PagedResult<T>, Result<T>, exceptions)
├── Elegance.Domain/
│   ├── Catalog/ (Product, ProductVariant, Category, ProductImage, ProductAttribute...)
│   ├── Inventory/ (InventoryItem, StockMovement, InventoryReservation)
│   ├── Customers/ (Customer, CustomerAddress, Wishlist)
│   ├── Orders/ (Order, OrderItem, Payment, Shipment)
│   ├── Content/ (Banner, Collection, Page, Review)
│   └── Identity/ (User, Role, Permission)
└── Elegance.Infrastructure/
    ├── Persistence/ (EleganceDbContext, Migrations/, EntityConfigurations/)
    ├── Storage/ (IObjectStorageService, S3ObjectStorageService)
    ├── Caching/ (IRedisCacheService — introduced only when Phase 11 identifies a real hot path)
    ├── Identity/ (JWT token service, password hasher)
    ├── Payments/ (IPaymentGateway abstraction + provider adapters, starting with COD/Bank Transfer stubs)
    └── Email/ (IEmailSender)
```

**Rules carried over from the brief**: no repository-per-entity by default — `Elegance.Application` services talk to `EleganceDbContext` directly via EF Core where that's simplest; introduce a repository only where an entity has genuinely complex query logic reused across services (e.g. a `ProductCatalogQueries` read-model class for the filtered/paginated product search, not a generic `IRepository<T>`). DTOs only cross the API boundary — EF entities never get serialized directly.

---

## 3. API Conventions

- Base path: `/api/v1/...`, versioned via URL segment (simplest, no header-negotiation complexity needed yet).
- Public endpoints: products, categories, collections, cart, wishlist, auth, orders (customer's own).
- Admin endpoints: `/api/v1/admin/...`, separate authorization policy (`RequireRole("Admin","SuperAdmin",...)`), never reachable by a customer JWT.
- Standard envelope for lists: `{ items: [...], page, pageSize, totalCount }` (no need for HATEOAS/complex envelopes).
- Errors: RFC 7807 `application/problem+json` via a global exception-handling middleware — no raw exception messages or stack traces ever returned.
- Swagger/OpenAPI generated from the same controllers (`AddOpenApi`, already present in the scaffold) — becomes the live companion to `docs/API-SPECIFICATION.md`.

---

## 4. Authentication Strategy

- Customer auth: email+password registration/login, ASP.NET Core Identity password hashing (or `IPasswordHasher<T>` directly without full Identity if we want a lighter footprint — decide in Phase 5), JWT access token (short-lived, ~15 min) + refresh token (long-lived, stored httpOnly secure cookie, rotated on use, persisted server-side in a `RefreshTokens` table so it can be revoked).
- Admin auth: same JWT mechanism, separate role claims; admin accounts are never self-registerable via the public API.
- CORS: explicit allow-list of the frontend origin(s) only, credentials enabled for the refresh-token cookie.
- Guest cart: a signed, random opaque identifier (e.g. a GUID in an httpOnly cookie, `elegence_cart_id`) — never a guessable sequential ID — associated server-side with a `Carts` row that has `CustomerId = NULL`. On login, merge guest cart into the customer's server cart.

---

## 5. Cart & Checkout Integrity (non-negotiable per brief §13–14)

1. Client sends only `{ productId, variantId?, quantity }` — never price.
2. Server re-reads current product/variant price, sale status, and stock on every cart mutation and again at checkout.
3. Order creation runs inside a single DB transaction: re-validate stock → reserve/deduct inventory → recompute totals → validate coupon → persist `Order` + `OrderItems` + `OrderStatusHistory("Pending")` → commit.
4. Inventory deduction uses either row-level locking or an optimistic `RowVersion`/`xmin`-style concurrency token on `InventoryItem` to prevent overselling under concurrent checkouts — exact mechanism decided in Phase 9 once load characteristics are known.
5. Payment is behind an `IPaymentGateway` interface from day one, even though Phase 1–9 only implement Cash-on-Delivery and Bank Transfer; this avoids a rewrite when a real gateway (e.g. a Pakistani payment processor, Stripe for international) is added later.

---

## 6. Images

- Object storage: S3-compatible bucket (MinIO locally / Cloudflare R2 or AWS S3 in production), accessed via `IObjectStorageService` abstraction in `Elegance.Infrastructure.Storage`.
- MySQL stores only metadata: `MediaFiles` table (`StorageKey, Url, OriginalFilename, MimeType, Width, Height, FileSizeBytes, AltText`) plus a join table `ProductImages` (`ProductId, VariantId?, MediaFileId, SortOrder, ImageType`).
- On upload, generate `original`, `large`, `medium`, `thumbnail` variants server-side (or via an async worker) in WebP (AVIF later if encoder support is easy to add), store each as its own `MediaFiles` row or as sibling keys under one logical image — exact convention decided in Phase 2 when the upload pipeline is built.
- Frontend already uses raw `<img>` + `images.unoptimized:true`, so CDN URLs with a `srcset`-style set of pre-generated sizes will work without needing `next/image` right away.

---

## 7. Caching

- **Default: no Redis.** Start with MySQL + EF Core query projections + proper indexes + pagination.
- Introduce Redis only once a specific hot path is measured/obvious — most likely candidates, to revisit in Phase 11: category tree (rarely changes, read on every page), homepage featured/best-seller product lists, and session-adjacent guest-cart lookups if MySQL round-trips become a bottleneck. Do not pre-build a cache layer speculatively.

---

## 8. Frontend Integration Layer

New folder: `frontend/lib/api/` (plain `.js` to match the existing codebase's JS-only convention — see audit's open question on whether to introduce TypeScript for new files only).

```
frontend/lib/api/
├── client.js       (one fetch wrapper: base URL from NEXT_PUBLIC_API_BASE_URL, JSON headers, error normalization, credentials:'include' for cart/auth cookies)
├── products.js     (getProducts(params), getProductBySlug(slug))
├── categories.js   (getCategories(), getCategoryBySlug(slug))
├── cart.js         (getCart(), addItem(), updateItem(), removeItem())
├── orders.js       (createOrder(), getOrders(), getOrder(id))
├── auth.js         (register(), login(), logout(), refresh(), me())
└── wishlist.js     (getWishlist(), addItem(), removeItem())
```

`frontend/context/Context.jsx` keeps its existing public shape (`useContextElement()` and all the method names components already call) but its internals are rewritten to call this API layer and hold server-confirmed state instead of mutating a local array — this means **zero changes** to `AddtoCart.jsx`, `AddtoWishlist.jsx`, `AddtoCompare.jsx`, `QuickView.jsx`, etc.

---

## 9. Deployment Architecture (target, built out in Phase 12)

- Ubuntu LTS host, Nginx as reverse proxy + static asset/CDN origin, HTTPS via Let's Encrypt (or Cloudflare-terminated TLS).
- API runs as either a systemd service (`dotnet Elegance.Api.dll`) or inside Docker Compose alongside MySQL and Redis — final choice deferred to Phase 12, both are compatible with this architecture.
- `docker-compose.yml` at repo root for local dev: `mysql`, `redis` (once introduced), `minio` (local S3-compatible storage for dev), `api`.
- Automated nightly `mysqldump` backups, rotated; `GET /health` and a DB-connectivity readiness check wired into whatever uptime monitor you choose later.
- Structured logging (e.g. Serilog) to files/stdout, no secrets ever logged; `appsettings.*.json` never contains real secrets — all via environment variables / `.env` (gitignored) with `.env.example` committed.

---

## 10. Phased Rollout (from the brief, unchanged)

1. Audit (this doc + PROJECT-AUDIT.md + DATABASE-DESIGN.md) → **you review before Phase 2 starts**
2. Product/Category/Image domain + API, EF Core migrations, MySQL connection, `/health`
3. Connect one canonical shop-grid page to the API
4. Connect one canonical product-detail page to the API
5. Auth + Customers + Addresses
6. Cart (guest + registered)
7. Wishlist
8. Checkout
9. Orders + Payments abstraction + Inventory integrity
10. Admin API (+ later, admin UI)
11. SEO, performance, caching decisions
12. Production deployment

Each phase ends with: build passes, (eventually) tests pass, manual verification, and a short doc update — not a giant big-bang integration.
