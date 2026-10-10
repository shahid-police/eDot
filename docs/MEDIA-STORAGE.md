# Product media: images and video

## Storage backend

Two providers, selected by `Storage:Provider` in configuration:

- **`Local`** (development default) — files are written to `Elegance.Api/wwwroot/uploads` and
  served by the API itself via `UseStaticFiles()`. Not viable in production: a typical host has
  ephemeral or single-instance disk, so files vanish on redeploy and don't replicate across
  instances.
- **`R2`** (what production runs) — Cloudflare R2, uploaded through the S3-compatible API via the
  official AWS SDK (`AWSSDK.S3`) pointed at R2's endpoint. R2 has no egress fees, which matters
  for product photography served to price-sensitive storefront traffic.

Both implement `IFileStorage` (`Elegance.Infrastructure/Media/IFileStorage.cs`). Selection and
binding happen in `DependencyInjection.cs` from the `Storage` config section:

```json
"Storage": {
  "Provider": "R2",
  "PublicBaseUrl": "https://cdn.elegence.pk",
  "R2": {
    "AccountId": "...",
    "AccessKey": "...",
    "SecretKey": "...",
    "Bucket": "elegance-media"
  }
}
```

`AccountId`, `AccessKey`, and `SecretKey` must come from environment variables
(`Storage__R2__AccountId`, etc.) or another secret store in production — never commit real
values. `PublicBaseUrl` is the bucket's public/custom domain (or the API's own base URL for the
`Local` provider).

## Upload flow

The admin picks a file in the product form → the browser sends it as multipart form data to the
API → the API validates, processes, and stores it, and records the result.

```
POST /api/v1/admin/products/{productId}/images       (file, imageType: Primary|Hover|Gallery)
DELETE /api/v1/admin/products/{productId}/images/{id}
PUT /api/v1/admin/products/{productId}/images/{id}    (sortOrder, for reordering the gallery)

POST /api/v1/admin/products/{productId}/variants/{variantId}/image   (file)
DELETE /api/v1/admin/products/{productId}/variants/{variantId}/image
```

A product's own images (`Primary`, `Hover`, `Gallery`) become `ProductImage` rows. A variant's
photo (e.g. the "Gold" vs "Rose Gold" shot) sets `ProductVariant.ImageMediaFileId` directly — this
mirrors how the catalog already modeled it before upload existed (see `DevelopmentSeeder`), rather
than introducing a new path. The `ProductImageType.Swatch` enum value predates this work and isn't
used by the admin endpoints.

This is a server-proxied upload, not a direct-to-bucket presigned upload: simpler to secure at
admin-only, catalog-sized volume, and it lets the server validate and re-encode before anything
touches storage. If upload volume grows (bulk catalog import), presigned direct-to-bucket upload
is the natural next step.

## Validation and processing

Implemented in `Elegance.Infrastructure/Admin/AdminMediaService.cs`, using SkiaSharp (MIT-licensed
— deliberately not SixLabors.ImageSharp, whose Split License requires a paid license once a
business crosses a revenue threshold) to decode and re-encode.

| Rule | Value |
|---|---|
| Accepted formats | JPEG, PNG, WebP |
| Max upload size | 8 MB |
| Max input dimensions | 4000×4000 px (reject above; ask the admin to downscale) |
| Minimum dimensions | 1000×1250 px (matches the storefront's portrait product card ratio) |
| Stored size | downscaled to a 2000 px long edge if larger, same format, quality 85 |
| Gallery cap | 8 images per product |
| Primary / Hover | exactly 0 or 1 each; a new upload replaces the existing one automatically |

A corrupt or unsupported file fails to decode and is rejected with a plain validation error, same
as a file that's the wrong size or type.

## Video

Not an uploaded file. `Product.VideoUrl` stores a link to a YouTube or Vimeo video — paste one in
the admin product form and it's saved with the rest of the product. This avoids self-hosting raw
video (no adaptive bitrate, expensive bandwidth) in favor of free transcoding and streaming from
an established host. The admin form shows a live embed preview when the link is recognized as
YouTube or Vimeo; any other URL is still saved, just without a preview.

If self-hosted video or a branded player is ever needed, the options are Cloudflare Stream or Mux
rather than storing raw files in R2 directly.

## What's not built

- No bulk/CSV image import.
- No presigned direct-to-bucket upload path (see above).
- No image alt text editor in the admin UI yet, though `MediaFile.AltText` exists in the schema.
- `SkiaSharp.NativeAssets.Linux` is not yet restored in this repo's lockfile — needed before a
  Linux container deploy; add it back to `Elegance.Infrastructure.csproj` and restore before then.
