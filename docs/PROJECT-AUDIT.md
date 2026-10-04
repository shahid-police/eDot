# PROJECT-AUDIT.md — E. (elegence.pk) / Vemus Template Technical Audit

Date: 2026-10-05
Scope: Phase 1 inspection only. No implementation performed. Repository has 592 files staged with **zero commits** on `master` — this is the initial import of the purchased Vemus template plus a bare ASP.NET Core scaffold.

---

## 1. Current Architecture

### 1.1 Frontend (`frontend/`)
- **Framework**: Next.js `^15.5.12`, App Router (`frontend/app/`), React `^19.0.0` / React DOM `^19.0.0`.
- **Language**: Plain JavaScript/JSX — **not TypeScript**. No `tsconfig.json`. There is a `frontend/jsconfig.json` providing only the `@/*` → `./*` path alias; no type-checking exists.
- **Styling**: Bootstrap 5.3.2 + Sass (SCSS). **No Tailwind CSS** (contrary to the task brief's assumption — confirmed no `tailwind.config.*` and no `tailwindcss` dependency). Global stylesheet entry is `frontend/public/scss/main.scss`, imported once in `frontend/app/layout.js`. Bootstrap's JS (offcanvas/modal/dropdown behavior) is lazy-loaded at runtime in `components/common/LayoutWrapper.jsx`.
- **Package manager**: npm (`package-lock.json` present; no yarn/pnpm lockfiles).
- **No devDependencies at all** — no ESLint config despite a `lint` script, no TypeScript tooling.
- **Image handling**: `next.config.mjs` sets `images.unoptimized: true`. The template uses raw `<img>` tags everywhere, not `next/image`. Next's image optimization pipeline is effectively bypassed.
- **Other notable deps**: `axios`, `swiper`, `photoswipe`, `drift-zoom` (image zoom), `jarallax` (parallax), `gsap`, `@google/model-viewer` (drives the `/product-3d` route).
- **Routing structure**: One root layout only (`frontend/app/layout.js`) — no nested `layout.jsx` per route group. Each `page.jsx` manually composes its own header + sections + footer. Route groups:
  - `(homes)` — home-2..home-6 (home-1 is the root `app/page.jsx`)
  - `(shop-pages)` — 11 shop/catalog grid layout variants
  - `(products-single)` — 20 product-detail page variants (incl. 3D viewer, video, engrave, swatch-image, size-guide)
  - `(account)` — account-page, account-addresses, account-orders, account-orders-empty
  - `(otherPages)` — checkout, shop-cart, wishlist, compare, our-store, about-us, contact-us, faq, 404, coming-soon, thank-you, before-you-leave, newspaper-popup, product-popup
  - `(blogs)` — blog-list-style-1/2, blog-single
- **Design intent of the template**: this is a ThemeForest demo-pack — most "components" are intentionally duplicated per visual variant (6 home hero sections, 5 shop grid components, 17 product-detail-section components, 3 footers, 4 headers) rather than parametrized. This is normal for a purchased template and is **not a defect to fix**, but it means "the Vemus frontend" is really N cosmetic variants layered over one business model — we should pick one canonical variant per page type to wire to the real backend, and leave the rest as unused demo alternatives (or prune later, with your approval).

### 1.2 Backend (`backend/`)
- Bare ASP.NET Core Web API scaffold, created by `dotnet new webapi` and never modified:
  - `backend/VemusEcommerce.Api/Program.cs` — default minimal pipeline (`AddControllers`, `AddOpenApi`, `MapControllers`), no DbContext, no auth, no custom services.
  - `backend/VemusEcommerce.Api/WeatherForecastController.cs` + `WeatherForecast.cs` — default template leftovers, safe to delete.
  - `.csproj` targets **`net10.0`** with `Nullable`/`ImplicitUsings` enabled — matches the brief's ".NET 10 LTS" requirement.
  - `backend/VemusEcommerce.slnx` references only the one API project — no Application/Domain/Infrastructure split yet.
  - `appsettings.json` has no connection strings, no JWT config, no storage config — clean slate.
  - `launchSettings.json`: HTTP on `:5071`, HTTPS on `:7201`.
- **Naming flag**: the solution/project is named `VemusEcommerce` (matching the folder/template name), not `Elegance`, which is the brand the brief specifies for all new backend code (`Elegance.Api`, `Elegance.Domain`, etc.). This needs a decision before scaffolding the modular monolith — see open question at the end of this document.

---

## 2. Existing Ecommerce Functionality (all client-side, no backend)

| Feature | Implementation | Functional? |
|---|---|---|
| Cart | `frontend/context/Context.jsx` (`useContextElement`), persisted to `localStorage["cartList"]` | Partially — add/remove/update quantity work client-side only; cart item = full product object + `quantity`, no variant/SKU captured |
| Wishlist | Same context, `localStorage["wishlist"]`, stores **IDs only**, seeded with hardcoded `[1,2,3]` | Works client-side only |
| Compare | Same context, **no persistence**, seeded with hardcoded `[1,2,3,4]` | Works client-side only, resets on reload |
| Checkout | `components/otherPages/Checkout.jsx` | **Non-functional** — form has `onSubmit={e => e.preventDefault()}`, hardcoded discount/shipping/tax line items unrelated to cart total, "Place order" is a plain `<Link href="/thank-you">` — no order is ever created |
| Login/Register/Reset | `components/modals/Login.jsx`, `Register.jsx`, `ResetPass.jsx` | **Non-functional** — static forms, `preventDefault()`, dead OAuth buttons, no token/session anywhere |
| Account pages | `app/(account)/**` | **Static only** — hardcoded name "VINCENT PHAM", hardcoded order/wishlist counts, no route protection — any visitor can open them directly |
| Search | `components/modals/Search.jsx` | **Decorative only** — no `onChange`, no filtering; "suggestions" is always `products1.slice(0,2)` regardless of query |
| Shop filtering | `reducer/filterReducer.js` + `components/shop/Products1..5.jsx` | **Functional, but 100% client-side** array filter/intersect over a static in-memory array — no API involved |
| Product detail lookup | `allProducts.filter(p => p.id == id)[0] ?? allProducts[0]` in each `(products-single)/*/[id]/page.jsx` | Works, but silently falls back to product #1 on an invalid ID (no 404) |
| Newsletter signup | `Footer1.jsx`, `Footer3.jsx`, `modals/NewsLetter.jsx` | **The only real network call in the whole app** — POSTs to a third-party demo endpoint `https://express-brevomail.vercel.app/api/contacts`, unrelated to our backend |

**No `process.env.NEXT_PUBLIC_*` usage exists anywhere** — confirms there is currently zero environment-based API configuration to build on; we're starting from nothing on that front, which is good (no conflicting conventions to unwind).

---

## 3. Existing State Management

- Single global React Context (`frontend/context/Context.jsx`) exposes: `cartProducts`, `addProductToCart(id, qty, isModal)`, `isAddedToCartProducts`, `updateQuantity`, `setCartProducts` (used directly by UI for removal — no dedicated `removeFromCart` method), `wishList`, `addToWishlist` (toggle), `removeFromWishlist`, `compareItem`, `addToCompareItem`, `removeFromCompareItem`, `quickViewItem`, `quickAddItem`, `totalPrice` (derived via `useEffect`).
- `frontend/reducer/filterReducer.js` — local `useReducer` for shop-grid filtering only (price/color/size/category/material/availability/sort/pagination). Not global state.
- No Redux/Zustand/MobX/React Query/SWR anywhere in `package.json`.

## 4. Existing Data Sources

`frontend/data/products.js` (1112 lines) — plain JS module, **not JSON**, exporting 15+ arrays (`products1`…`products15`, plus video variants) concatenated into one `allProducts` export.

**Critical finding — no single canonical product schema.** Field richness varies by which array a product was seeded into:
- Minimal: `{ id, imgSrc, hoverImgSrc, title, price, oldPrice, textColor }`
- Richest (`products15`, used by the filterable shop grid): adds `badge, badgeType, variantText, variantType, outOfStock, availability, category, material, filterColors[], size`

No product has: description, SKU, stock quantity, a real image gallery, real color/size variant rows with their own price/stock/image, reviews, tags, brand, weight/dimensions, or slug. Everything that *looks* like variant selection on the product detail page (gallery images, Gold/Rose/Titanium swatches, 48/50/52/54 sizes) is **hardcoded identically on every product** inside `ColorSelect.jsx`, `SizeSelect.jsx`, and the `Details1.jsx` gallery array — completely disconnected from `product.id`. This confirms the real catalogue/variant data model must be designed from scratch; nothing in the template can be "lifted" as-is.

Other static data modules follow the same pattern: `blogs.js`, `categories.js`, `collections.js`, `features.js`, `heroSlides.js`, `menu.js`, `testimonials.js`.

## 5. Existing API/Service Calls

Only 3 call sites in the entire frontend, all identical newsletter POSTs to the same third-party demo service (see table above). No other `fetch`/`axios` usage anywhere — not in cart, checkout, auth, product fetch, or search.

## 6. Components Structure (for reuse reference)

11 folders under `frontend/components/`: `account`, `blogs`, `common`, `footers`, `headers`, `homes`, `modals`, `otherPages`, `product-details`, `shop`.

Reusable building blocks worth keeping as-is and wiring to real data: `common/AddtoCart.jsx`, `AddtoWishlist.jsx`, `AddtoCompare.jsx`, `QuickView.jsx`, `QuantitySelect.jsx`, `Pagination.jsx`, `CurrencySelect.jsx`. **There is no standalone `ProductCard` component** — every grid component inlines its own card markup (~20 duplicated copies). This is a real refactor opportunity later (extract one `ProductCard`), but it is **out of scope for Phase 1** and should only be done once real data is flowing, to avoid double-churn.

---

## 7. Recommended Integration Points

1. **New `frontend/lib/api/` layer** (per brief): `products.ts`/`.js`, `categories.js`, `cart.js`, `orders.js`, `auth.js` — centralizes all `fetch` calls, reads `NEXT_PUBLIC_API_BASE_URL` from env. Since the project is plain JS, decide now whether to introduce TypeScript for *new* code only (recommended: keep `.jsx` for template pages, allow `.js`/`.ts` for the new `lib/api` layer — see open question).
2. **Context.jsx rewrite, not replacement**: keep the same `useContextElement()` consumer API (so `AddtoCart.jsx` etc. don't need touching) but change its internals to call the cart API instead of mutating a local array, and change `allProducts` references to come from the API layer instead of `data/products.js`.
3. **One canonical page per type**: integrate `product-default`, `shop-default` (or whichever grid variant you prefer), `shop-cart`, `checkout`, `account-page`, `wishlist`, `compare` first. Leave the other ~40 cosmetic variants on static/mock data until/unless you want them live too.
4. **Image URLs**: since `images.unoptimized: true` is already set and the template already uses raw `<img>`, backend-provided CDN URLs will drop in with no frontend image-pipeline changes needed initially.
5. **Auth**: Login/Register modals already exist with the right fields — just need real `onSubmit` handlers wired to `/api/v1/auth/*` and a token storage strategy (httpOnly cookie recommended over localStorage for the refresh token).

## 8. Technical Risks

- **No commits yet** — recommend committing the untouched template as "initial import" before any modification, so we have a clean diff baseline and can always diff against pristine Vemus.
- **No TypeScript/ESLint** — raises risk of silent errors as we wire in real data; recommend at minimum adding ESLint (`eslint-config-next`) now, and considering TS for new files only.
- **Backend solution name mismatch** (`VemusEcommerce` vs `Elegance`) — needs a decision before we scaffold Domain/Application/Infrastructure projects, since it affects every namespace going forward.
- **Product data model fully absent** — the catalogue/variant/image schema must be designed from zero; nothing in the template's mock data is reusable as a migration source, only as a UX reference for what fields the UI needs.
- **Cart/checkout math is currently fake** (hardcoded discount/shipping/tax unrelated to actual totals) — must not let this leak into the real implementation; all pricing must be server-computed per the brief's security rules.
- **`images.unoptimized: true`** disables Next.js's own optimization — fine short-term since we'll serve pre-optimized WebP/AVIF variants from object storage/CDN, but worth revisiting once the image pipeline exists (may re-enable Next image optimization for responsive `srcset` on top of CDN URLs).
- **592 uncommitted files** is a lot to carry through the first commit — recommend a single clean "initial import" commit before any further work, per Git Workflow rules in the brief.

## 9. Files That Should NOT Be Modified Unnecessarily

- All of `frontend/public/scss/**`, `frontend/public/css/**`, `frontend/public/fonts/**`, `frontend/public/icon/**` — vendor/compiled assets, part of the purchased template license.
- All ~40 cosmetic page/component variants not selected as the "canonical" integration target (home-2..6, the 10 non-canonical shop grids, the 19 non-canonical product-detail variants, extra footers/headers) — leave on static data, don't touch, until/unless you ask to bring them live too.
- `frontend/utils/wow.js` (vendored WOW.js) and other vendored utility files.
- `frontend/jsconfig.json` path alias — reuse `@/*`, don't introduce a second alias convention.

---

## Open Question for You

**Backend naming**: keep the already-scaffolded `VemusEcommerce.Api`/`VemusEcommerce.slnx` naming, or rename everything to `Elegance.Api` / `Elegance.Domain` / `Elegance.Application` / `Elegance.Infrastructure` / `Elegance.sln` to match the `E.` / elegence.pk brand before any real backend code is written? This is a one-time, low-risk rename now; it gets expensive once controllers/namespaces/migrations pile up on top of it.
