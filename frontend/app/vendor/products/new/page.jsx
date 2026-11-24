"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { vendorRequest, vendorUpload } from "@/lib/vendor/api";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5071";
const IMAGE_ACCEPT = "image/jpeg,image/png,image/webp";
const MAX_IMAGES = 10;

export default function NewVendorProductPage() {
  const router = useRouter();
  const [categories, setCategories] = useState([]);
  const [productName, setProductName] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [wholesalePrice, setWholesalePrice] = useState("");
  const [vendorProductCode, setVendorProductCode] = useState("");
  const [images, setImages] = useState([]);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    fetch(new URL("/api/v1/categories", API_BASE_URL))
      .then((r) => r.json())
      .then(setCategories)
      .catch(() => setError("Could not load categories."));
  }, []);

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const submission = await vendorRequest("/products", {
        method: "POST",
        body: {
          productName: productName.trim(),
          categoryId: Number(categoryId),
          wholesalePrice: Number(wholesalePrice),
          vendorProductCode: vendorProductCode.trim(),
        },
      });

      for (const file of images) {
        const data = new FormData();
        data.append("file", file);
        // eslint-disable-next-line no-await-in-loop
        await vendorUpload(`/products/${submission.id}/images`, data);
      }

      router.replace(`/vendor/products/${submission.id}`);
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  function addImages(fileList) {
    const files = Array.from(fileList ?? []).slice(0, MAX_IMAGES - images.length);
    setImages((current) => [...current, ...files]);
  }

  function removeImage(index) {
    setImages((current) => current.filter((_, i) => i !== index));
  }

  return (
    <div style={{ maxWidth: 640 }}>
      <h1 className="h4 mb-4">Add new product</h1>
      <form onSubmit={submit} className="vstack gap-3">
        <div>
          <label className="form-label">Product name</label>
          <input className="form-control" value={productName} onChange={(e) => setProductName(e.target.value)} required />
        </div>
        <div>
          <label className="form-label">Category</label>
          <select className="form-select" value={categoryId} onChange={(e) => setCategoryId(e.target.value)} required>
            <option value="">Choose a category</option>
            {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div>
          <label className="form-label">Wholesale price (PKR)</label>
          <input type="number" min="0" step="0.01" className="form-control" value={wholesalePrice} onChange={(e) => setWholesalePrice(e.target.value)} required />
        </div>
        <div>
          <label className="form-label">Your product code</label>
          <input className="form-control" value={vendorProductCode} onChange={(e) => setVendorProductCode(e.target.value)} required />
          <div className="form-text">Your own reference code for this product. Does not need to be unique across vendors.</div>
        </div>

        <div>
          <label className="form-label d-block">Product images</label>
          <div className="d-flex flex-wrap gap-2 mb-2">
            {images.map((file, i) => (
              <div key={i} className="position-relative">
                <img src={URL.createObjectURL(file)} alt="" width={100} height={125} className="border rounded d-block" style={{ objectFit: "cover" }} />
                <button
                  type="button"
                  className="btn btn-sm btn-danger position-absolute top-0 end-0"
                  onClick={() => removeImage(i)}
                >
                  &times;
                </button>
              </div>
            ))}
          </div>
          {images.length < MAX_IMAGES && (
            <input
              type="file"
              accept={IMAGE_ACCEPT}
              multiple
              className="form-control form-control-sm"
              onChange={(e) => { addImages(e.target.files); e.target.value = ""; }}
            />
          )}
          <div className="form-text">JPG, PNG, or WebP. Up to {MAX_IMAGES} images. These are reference images for Admin review only.</div>
        </div>

        {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
        <button type="submit" className="btn btn-dark" disabled={busy}>
          {busy ? "Submitting..." : "Submit for Admin review"}
        </button>
      </form>
    </div>
  );
}
