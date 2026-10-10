"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { adminRequest, adminUpload } from "@/lib/admin/api";

const EMPTY_VARIANT = { id: null, sku: "", variantName: "", price: "", salePrice: "", stockQuantity: 0, size: "", color: "", isActive: true, imageUrl: null };

const EMPTY_PRODUCT = {
  sku: "",
  name: "",
  slug: "",
  shortDescription: "",
  description: "",
  categoryId: "",
  productType: "Simple",
  price: "",
  salePrice: "",
  stockQuantity: 0,
  lowStockThreshold: "",
  material: "",
  color: "",
  occasion: "",
  weightGrams: "",
  videoUrl: "",
  isActive: true,
  isFeatured: false,
  isNew: false,
  isBestSeller: false,
  variants: [],
  images: [],
};

// Accepted by the admin media upload endpoints (Elegance.Infrastructure/Admin/AdminMediaService.cs).
const IMAGE_ACCEPT = "image/jpeg,image/png,image/webp";
const MAX_GALLERY_IMAGES = 8;

function slugify(value) {
  return value.toLowerCase().trim().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
}

// e.g. "Gold" + "48" -> "Gold / 48". Used to suggest a display name as colour/size are filled in.
function deriveVariantName(color, size) {
  return [color, size].filter(Boolean).join(" / ");
}

// Turns a YouTube/Vimeo watch link into its embeddable form, for the inline preview only.
// Returns null for anything else - the raw link is still saved either way.
function toEmbedUrl(url) {
  try {
    const u = new URL(url);
    if (u.hostname.includes("youtube.com")) {
      const id = u.searchParams.get("v");
      return id ? `https://www.youtube.com/embed/${id}` : null;
    }
    if (u.hostname === "youtu.be") {
      const id = u.pathname.slice(1);
      return id ? `https://www.youtube.com/embed/${id}` : null;
    }
    if (u.hostname.includes("vimeo.com")) {
      const id = u.pathname.split("/").filter(Boolean).pop();
      return id ? `https://player.vimeo.com/video/${id}` : null;
    }
  } catch {
    return null;
  }
  return null;
}

function ImageSlot({ label, imageUrl, onUpload, onRemove, busy, compact = false }) {
  const width = compact ? 90 : 120;
  return (
    <div>
      <label className="form-label small d-block">{label}</label>
      {imageUrl ? (
        <div>
          <img src={imageUrl} alt={label} width={width} height={Math.round(width * 1.25)} className="border rounded d-block mb-1" style={{ objectFit: "cover" }} />
          {onRemove && (
            <button type="button" className="btn btn-outline-danger btn-sm" onClick={onRemove} disabled={busy}>Remove</button>
          )}
        </div>
      ) : (
        <>
          <input
            type="file"
            accept={IMAGE_ACCEPT}
            className="form-control form-control-sm"
            style={{ width: compact ? 160 : undefined }}
            disabled={busy}
            onChange={(e) => {
              const file = e.target.files?.[0];
              e.target.value = "";
              if (file && onUpload) onUpload(file);
            }}
          />
          {busy && <div className="small text-muted mt-1">Uploading...</div>}
        </>
      )}
    </div>
  );
}

function toNumberOrNull(value) {
  return value === "" || value === null || value === undefined ? null : Number(value);
}

export function fromApi(product) {
  return {
    ...EMPTY_PRODUCT,
    sku: product.sku,
    name: product.name,
    slug: product.slug,
    shortDescription: product.shortDescription ?? "",
    description: product.description ?? "",
    categoryId: String(product.categoryId),
    productType: product.productType,
    price: String(product.price),
    salePrice: product.salePrice == null ? "" : String(product.salePrice),
    stockQuantity: product.stockQuantity,
    lowStockThreshold: product.lowStockThreshold == null ? "" : String(product.lowStockThreshold),
    material: product.material ?? "",
    color: product.color ?? "",
    occasion: product.occasion ?? "",
    weightGrams: product.weightGrams == null ? "" : String(product.weightGrams),
    videoUrl: product.videoUrl ?? "",
    isActive: product.isActive,
    isFeatured: product.isFeatured,
    isNew: product.isNew,
    isBestSeller: product.isBestSeller,
    variants: product.variants
      .filter((v) => v.isActive)
      .map((v) => ({
        id: v.id,
        sku: v.sku,
        variantName: v.variantName,
        price: v.price == null ? "" : String(v.price),
        salePrice: v.salePrice == null ? "" : String(v.salePrice),
        stockQuantity: v.stockQuantity,
        size: v.size ?? "",
        color: v.color ?? "",
        isActive: v.isActive,
        imageUrl: v.imageUrl ?? null,
      })),
    images: product.images ?? [],
  };
}

function toPayload(form) {
  const isVariant = form.productType === "Variant";
  return {
    sku: form.sku.trim(),
    name: form.name.trim(),
    slug: form.slug.trim(),
    shortDescription: form.shortDescription || null,
    description: form.description || null,
    price: Number(form.price),
    salePrice: toNumberOrNull(form.salePrice),
    currency: "PKR",
    categoryId: Number(form.categoryId),
    productType: form.productType,
    material: form.material || null,
    color: form.color || null,
    occasion: form.occasion || null,
    weightGrams: toNumberOrNull(form.weightGrams),
    videoUrl: form.videoUrl.trim() || null,
    stockQuantity: isVariant ? 0 : Number(form.stockQuantity) || 0,
    lowStockThreshold: toNumberOrNull(form.lowStockThreshold),
    isFeatured: form.isFeatured,
    isNew: form.isNew,
    isBestSeller: form.isBestSeller,
    isActive: form.isActive,
    variants: isVariant
      ? form.variants.map((v) => ({
          id: v.id,
          sku: v.sku.trim(),
          variantName: v.variantName.trim(),
          price: toNumberOrNull(v.price),
          salePrice: toNumberOrNull(v.salePrice),
          stockQuantity: Number(v.stockQuantity) || 0,
          weightGrams: null,
          size: v.size || null,
          color: v.color || null,
          isActive: true,
        }))
      : [],
  };
}

export default function ProductForm({ productId = null, initial = null }) {
  const router = useRouter();
  const isNew = productId === null;
  const [form, setForm] = useState(initial ?? EMPTY_PRODUCT);
  const [categories, setCategories] = useState([]);
  const [slugTouched, setSlugTouched] = useState(!isNew);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  const [mediaError, setMediaError] = useState("");
  const [uploadingSlot, setUploadingSlot] = useState(null);

  useEffect(() => {
    adminRequest("/categories").then(setCategories).catch((err) => setError(err.message));
  }, []);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  function updateName(value) {
    setForm((current) => ({
      ...current,
      name: value,
      slug: slugTouched ? current.slug : slugify(value),
    }));
  }

  function updateVariant(index, field, value) {
    setForm((current) => ({
      ...current,
      variants: current.variants.map((v, i) => {
        if (i !== index) return v;
        const next = { ...v, [field]: value };
        if (field === "color" || field === "size") {
          // Only auto-fill the display name while the admin hasn't overridden it themselves.
          const suggestedBefore = deriveVariantName(v.color, v.size);
          if (!v.variantName || v.variantName === suggestedBefore) {
            next.variantName = deriveVariantName(next.color, next.size);
          }
        }
        return next;
      }),
    }));
  }

  function addVariant() {
    setForm((current) => ({ ...current, variants: [...current.variants, { ...EMPTY_VARIANT }] }));
  }

  function removeVariant(index) {
    setForm((current) => ({ ...current, variants: current.variants.filter((_, i) => i !== index) }));
  }

  async function uploadProductImage(imageType, file) {
    setMediaError("");
    setUploadingSlot(imageType);
    try {
      const data = new FormData();
      data.append("imageType", imageType);
      data.append("file", file);
      const image = await adminUpload(`/products/${productId}/images`, data);
      setForm((current) => ({
        // Primary/Hover replace in place server-side; Gallery just grows.
        ...current,
        images: [...current.images.filter((img) => imageType === "Gallery" || img.imageType !== imageType), image],
      }));
    } catch (err) {
      setMediaError(err.message);
    } finally {
      setUploadingSlot(null);
    }
  }

  async function removeProductImage(imageId) {
    setMediaError("");
    try {
      await adminRequest(`/products/${productId}/images/${imageId}`, { method: "DELETE" });
      setForm((current) => ({ ...current, images: current.images.filter((img) => img.id !== imageId) }));
    } catch (err) {
      setMediaError(err.message);
    }
  }

  async function uploadVariantImage(index, variantId, file) {
    setMediaError("");
    setUploadingSlot(`variant-${variantId}`);
    try {
      const data = new FormData();
      data.append("file", file);
      const result = await adminUpload(`/products/${productId}/variants/${variantId}/image`, data);
      updateVariant(index, "imageUrl", result.url);
    } catch (err) {
      setMediaError(err.message);
    } finally {
      setUploadingSlot(null);
    }
  }

  async function removeVariantImage(index, variantId) {
    setMediaError("");
    try {
      await adminRequest(`/products/${productId}/variants/${variantId}/image`, { method: "DELETE" });
      updateVariant(index, "imageUrl", null);
    } catch (err) {
      setMediaError(err.message);
    }
  }

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    setMessage("");
    try {
      const payload = toPayload(form);
      if (isNew) {
        const created = await adminRequest("/products", { method: "POST", body: payload });
        router.replace(`/admin/products/${created.id}`);
        return;
      }
      const saved = await adminRequest(`/products/${productId}`, { method: "PUT", body: payload });
      setForm(fromApi(saved));
      setMessage("Product saved.");
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  const isVariant = form.productType === "Variant";
  const primaryImage = form.images.find((img) => img.imageType === "Primary") ?? null;
  const hoverImage = form.images.find((img) => img.imageType === "Hover") ?? null;
  const galleryImages = form.images
    .filter((img) => img.imageType === "Gallery")
    .sort((a, b) => a.sortOrder - b.sortOrder);
  const videoEmbedUrl = form.videoUrl ? toEmbedUrl(form.videoUrl) : null;

  return (
    <form onSubmit={submit} className="vstack gap-4">
      <div className="d-flex align-items-center gap-3">
        <h1 className="h4 mb-0">{isNew ? "New product" : `Edit ${form.sku}`}</h1>
        <Link href="/admin/products" className="small">Back to products</Link>
      </div>

      {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
      {message && <div className="alert alert-success py-2 mb-0">{message}</div>}

      <section className="row g-3">
        <div className="col-md-4">
          <label className="form-label">SKU</label>
          <input className="form-control" value={form.sku} onChange={(e) => update("sku", e.target.value)} required maxLength={64} />
        </div>
        <div className="col-md-8">
          <label className="form-label">Name</label>
          <input className="form-control" value={form.name} onChange={(e) => updateName(e.target.value)} required maxLength={300} />
        </div>
        <div className="col-md-8">
          <label className="form-label">Slug</label>
          <input
            className="form-control"
            value={form.slug}
            onChange={(e) => { setSlugTouched(true); update("slug", e.target.value); }}
            required
            maxLength={320}
          />
        </div>
        <div className="col-md-4">
          <label className="form-label">Category</label>
          <select className="form-select" value={form.categoryId} onChange={(e) => update("categoryId", e.target.value)} required>
            <option value="">Choose...</option>
            {categories.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
            ))}
          </select>
        </div>
        <div className="col-12">
          <label className="form-label">Short description</label>
          <input className="form-control" value={form.shortDescription} onChange={(e) => update("shortDescription", e.target.value)} maxLength={500} />
        </div>
        <div className="col-12">
          <label className="form-label">Description</label>
          <textarea className="form-control" rows={3} value={form.description} onChange={(e) => update("description", e.target.value)} />
        </div>
        <div className="col-12">
          <label className="form-label">Video link (YouTube or Vimeo)</label>
          <input
            type="url"
            className="form-control"
            value={form.videoUrl}
            onChange={(e) => update("videoUrl", e.target.value)}
            placeholder="https://www.youtube.com/watch?v=..."
            maxLength={500}
          />
          {videoEmbedUrl && (
            <div className="ratio ratio-16x9 mt-2" style={{ maxWidth: 360 }}>
              <iframe src={videoEmbedUrl} title="Product video preview" allowFullScreen />
            </div>
          )}
        </div>
      </section>

      {!isNew ? (
        <section>
          <h2 className="h6 mb-2">Photos</h2>
          {mediaError && <div className="alert alert-danger py-2 mb-2">{mediaError}</div>}
          <div className="d-flex flex-wrap gap-4 mb-3">
            <ImageSlot
              label="Primary"
              imageUrl={primaryImage?.url}
              busy={uploadingSlot === "Primary"}
              onUpload={(file) => uploadProductImage("Primary", file)}
              onRemove={primaryImage ? () => removeProductImage(primaryImage.id) : undefined}
            />
            <ImageSlot
              label="Hover"
              imageUrl={hoverImage?.url}
              busy={uploadingSlot === "Hover"}
              onUpload={(file) => uploadProductImage("Hover", file)}
              onRemove={hoverImage ? () => removeProductImage(hoverImage.id) : undefined}
            />
            {galleryImages.map((img) => (
              <ImageSlot key={img.id} label="Gallery" imageUrl={img.url} compact onRemove={() => removeProductImage(img.id)} />
            ))}
            {galleryImages.length < MAX_GALLERY_IMAGES && (
              <ImageSlot
                label={`Add gallery photo (${galleryImages.length}/${MAX_GALLERY_IMAGES})`}
                compact
                busy={uploadingSlot === "Gallery"}
                onUpload={(file) => uploadProductImage("Gallery", file)}
              />
            )}
          </div>
          <p className="small text-muted mb-0">
            JPEG, PNG, or WebP, up to 8 MB, at least 1000{"×"}1250 px. Uploading a new Primary or Hover photo replaces the old one.
          </p>
        </section>
      ) : (
        <div className="alert alert-info py-2 mb-0">Save the product first, then come back here to add photos.</div>
      )}

      <section className="row g-3">
        <div className="col-md-3">
          <label className="form-label">Product type</label>
          <select className="form-select" value={form.productType} onChange={(e) => update("productType", e.target.value)}>
            <option value="Simple">Simple</option>
            <option value="Variant">Variant (with options)</option>
          </select>
        </div>
        <div className="col-md-3">
          <label className="form-label">Price (PKR)</label>
          <input type="number" min="0" step="0.01" className="form-control" value={form.price} onChange={(e) => update("price", e.target.value)} required />
        </div>
        <div className="col-md-3">
          <label className="form-label">Sale price (PKR)</label>
          <input type="number" min="0" step="0.01" className="form-control" value={form.salePrice} onChange={(e) => update("salePrice", e.target.value)} />
        </div>
        {!isVariant && (
          <div className="col-md-3">
            <label className="form-label">Stock</label>
            <input type="number" min="0" className="form-control" value={form.stockQuantity} onChange={(e) => update("stockQuantity", e.target.value)} />
          </div>
        )}
        <div className="col-md-3">
          <label className="form-label">Low stock threshold</label>
          <input type="number" min="0" className="form-control" value={form.lowStockThreshold} onChange={(e) => update("lowStockThreshold", e.target.value)} placeholder="Default 5" />
        </div>
        <div className="col-md-3">
          <label className="form-label">Material</label>
          <input className="form-control" value={form.material} onChange={(e) => update("material", e.target.value)} maxLength={100} />
        </div>
        <div className="col-md-3">
          <label className="form-label">Colour</label>
          <input className="form-control" value={form.color} onChange={(e) => update("color", e.target.value)} maxLength={50} />
        </div>
        <div className="col-md-3">
          <label className="form-label">Occasion</label>
          <input className="form-control" value={form.occasion} onChange={(e) => update("occasion", e.target.value)} maxLength={100} />
        </div>
        <div className="col-12 d-flex flex-wrap gap-3">
          {[["isActive", "Active"], ["isFeatured", "Featured"], ["isNew", "New"], ["isBestSeller", "Best seller"]].map(([field, label]) => (
            <div key={field} className="form-check">
              <input id={field} type="checkbox" className="form-check-input" checked={form[field]} onChange={(e) => update(field, e.target.checked)} />
              <label htmlFor={field} className="form-check-label">{label}</label>
            </div>
          ))}
        </div>
      </section>

      {isVariant && (
        <section>
          <div className="d-flex justify-content-between align-items-center mb-2">
            <h2 className="h6 mb-0">Options</h2>
            <button type="button" className="btn btn-outline-dark btn-sm" onClick={addVariant}>Add option</button>
          </div>
          {form.variants.length === 0 && <p className="small text-muted">Add at least one option, such as a colour/material and a size. Each option has its own SKU and stock.</p>}
          {form.variants.map((v, index) => (
            <div key={v.id ?? `new-${index}`} className="border rounded p-2 mb-2">
              <div className="row g-2 align-items-end mb-2">
                <div className="col-6 col-md-2">
                  <label className="form-label small">Option SKU</label>
                  <input className="form-control form-control-sm" value={v.sku} onChange={(e) => updateVariant(index, "sku", e.target.value)} required maxLength={64} />
                </div>
                <div className="col-6 col-md-2">
                  <label className="form-label small">Colour / material</label>
                  <input className="form-control form-control-sm" value={v.color} onChange={(e) => updateVariant(index, "color", e.target.value)} maxLength={50} placeholder="e.g. Gold" />
                </div>
                <div className="col-6 col-md-2">
                  <label className="form-label small">Size</label>
                  <input className="form-control form-control-sm" value={v.size} onChange={(e) => updateVariant(index, "size", e.target.value)} maxLength={50} placeholder="e.g. 48" />
                </div>
                <div className="col-6 col-md-2">
                  <label className="form-label small">Price</label>
                  <input type="number" min="0" step="0.01" className="form-control form-control-sm" value={v.price} onChange={(e) => updateVariant(index, "price", e.target.value)} placeholder="Product price" />
                </div>
                <div className="col-6 col-md-2">
                  <label className="form-label small">Sale price</label>
                  <input type="number" min="0" step="0.01" className="form-control form-control-sm" value={v.salePrice} onChange={(e) => updateVariant(index, "salePrice", e.target.value)} />
                </div>
                <div className="col-6 col-md-2">
                  <label className="form-label small">Stock</label>
                  <input type="number" min="0" className="form-control form-control-sm" value={v.stockQuantity} onChange={(e) => updateVariant(index, "stockQuantity", e.target.value)} />
                </div>
              </div>
              <div className="row g-2 align-items-end">
                <div className="col-md-5">
                  <label className="form-label small">Display name</label>
                  <input className="form-control form-control-sm" value={v.variantName} onChange={(e) => updateVariant(index, "variantName", e.target.value)} required maxLength={200} placeholder="Suggested from colour and size" />
                </div>
                <div className="col-md-4">
                  {v.id ? (
                    <ImageSlot
                      label="Photo"
                      imageUrl={v.imageUrl}
                      compact
                      busy={uploadingSlot === `variant-${v.id}`}
                      onUpload={(file) => uploadVariantImage(index, v.id, file)}
                      onRemove={v.imageUrl ? () => removeVariantImage(index, v.id) : undefined}
                    />
                  ) : (
                    <div className="small text-muted">Save the product to add a photo</div>
                  )}
                </div>
                <div className="col-md-3 d-flex justify-content-end">
                  <button type="button" className="btn btn-outline-danger btn-sm" onClick={() => removeVariant(index)}>Remove</button>
                </div>
              </div>
            </div>
          ))}
        </section>
      )}

      <div>
        <button type="submit" className="btn btn-dark" disabled={busy}>
          {busy ? "Saving..." : isNew ? "Create product" : "Save changes"}
        </button>
      </div>
    </form>
  );
}
