"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { vendorRequest, vendorUpload, formatDateTime, getVendorToken } from "@/lib/vendor/api";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5071";
const IMAGE_ACCEPT = "image/jpeg,image/png,image/webp";
const MAX_IMAGES = 10;
const EDITABLE_STATUSES = ["PendingApproval", "ReturnedForCorrection"];

// Vendor-original images have no public URL - they're served only through this authenticated,
// ownership-checked endpoint, so a plain <img src> (which can't carry a bearer token) can't load
// them directly. Fetch each as a blob and display that instead.
function usePrivateImageUrls(submissionId, imageIds) {
  const [urls, setUrls] = useState({});

  useEffect(() => {
    let active = true;
    const created = [];
    (async () => {
      for (const imageId of imageIds) {
        try {
          const url = new URL(`/api/v1/vendor/products/${submissionId}/images/${imageId}`, API_BASE_URL);
          const token = getVendorToken();
          // eslint-disable-next-line no-await-in-loop
          const response = await fetch(url, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
          if (!response.ok) continue;
          // eslint-disable-next-line no-await-in-loop
          const blob = await response.blob();
          const objectUrl = URL.createObjectURL(blob);
          created.push(objectUrl);
          if (active) setUrls((current) => ({ ...current, [imageId]: objectUrl }));
        } catch {
          // Skip images that fail to load; the page still works without a preview.
        }
      }
    })();
    return () => {
      active = false;
      created.forEach((u) => URL.revokeObjectURL(u));
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [submissionId, imageIds.join(",")]);

  return urls;
}

export default function VendorProductDetailPage() {
  const { id } = useParams();
  const router = useRouter();
  const [submission, setSubmission] = useState(null);
  const [categories, setCategories] = useState([]);
  const [form, setForm] = useState(null);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);

  function load() {
    vendorRequest(`/products/${id}`)
      .then((data) => {
        setSubmission(data);
        setForm({
          productName: data.productName,
          categoryId: String(data.categoryId),
          wholesalePrice: String(data.wholesalePrice),
          vendorProductCode: data.vendorProductCode,
        });
      })
      .catch((err) => setError(err.message));
  }

  useEffect(() => {
    load();
    fetch(new URL("/api/v1/categories", API_BASE_URL)).then((r) => r.json()).then(setCategories).catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const imageIds = (submission?.images ?? []).map((i) => i.id);
  const previewUrls = usePrivateImageUrls(id, imageIds);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!submission || !form) return <p>Loading...</p>;

  const editable = EDITABLE_STATUSES.includes(submission.status);

  async function save(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    setMessage("");
    try {
      await vendorRequest(`/products/${id}`, {
        method: "PUT",
        body: {
          productName: form.productName.trim(),
          categoryId: Number(form.categoryId),
          wholesalePrice: Number(form.wholesalePrice),
          vendorProductCode: form.vendorProductCode.trim(),
        },
      });
      setMessage("Changes saved.");
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function resubmit() {
    setBusy(true);
    setError("");
    try {
      await vendorRequest(`/products/${id}/resubmit`, { method: "POST" });
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function withdraw() {
    if (!window.confirm("Withdraw this submission? This cannot be undone.")) return;
    setBusy(true);
    setError("");
    try {
      await vendorRequest(`/products/${id}`, { method: "DELETE" });
      router.replace("/vendor/products");
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  async function addImages(fileList) {
    const files = Array.from(fileList ?? []).slice(0, MAX_IMAGES - submission.images.length);
    setError("");
    for (const file of files) {
      try {
        const data = new FormData();
        data.append("file", file);
        // eslint-disable-next-line no-await-in-loop
        await vendorUpload(`/products/${id}/images`, data);
      } catch (err) {
        setError(err.message);
        break;
      }
    }
    load();
  }

  async function removeImage(imageId) {
    try {
      await vendorRequest(`/products/${id}/images/${imageId}`, { method: "DELETE" });
      load();
    } catch (err) {
      setError(err.message);
    }
  }

  return (
    <div style={{ maxWidth: 640 }}>
      <h1 className="h4 mb-1">{submission.productName}</h1>
      <p className="text-muted mb-3">
        Status: <strong>{submission.status}</strong> &middot; Submitted {formatDateTime(submission.submittedAt)}
        {submission.productSku && <> &middot; SKU <code>{submission.productSku}</code></>}
      </p>

      {submission.status === "Rejected" && submission.rejectionReason && (
        <div className="alert alert-danger">Rejected: {submission.rejectionReason}</div>
      )}
      {submission.status === "ReturnedForCorrection" && (
        <div className="alert alert-warning">
          Please correct: {submission.correctionFieldsRequested}
          {submission.adminRemarks && <div className="mt-1">{submission.adminRemarks}</div>}
        </div>
      )}

      <form onSubmit={save} className="vstack gap-3 mb-4">
        <div>
          <label className="form-label">Product name</label>
          <input className="form-control" value={form.productName} disabled={!editable}
            onChange={(e) => setForm({ ...form, productName: e.target.value })} required />
        </div>
        <div>
          <label className="form-label">Category</label>
          <select className="form-select" value={form.categoryId} disabled={!editable}
            onChange={(e) => setForm({ ...form, categoryId: e.target.value })} required>
            <option value="">Choose a category</option>
            {categories.map((c) => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
        </div>
        <div>
          <label className="form-label">Wholesale price (PKR)</label>
          <input type="number" min="0" step="0.01" className="form-control" value={form.wholesalePrice} disabled={!editable}
            onChange={(e) => setForm({ ...form, wholesalePrice: e.target.value })} required />
        </div>
        <div>
          <label className="form-label">Your product code</label>
          <input className="form-control" value={form.vendorProductCode} disabled={!editable}
            onChange={(e) => setForm({ ...form, vendorProductCode: e.target.value })} required />
        </div>

        {message && <div className="alert alert-success py-2 mb-0">{message}</div>}
        {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}

        {editable && (
          <div className="d-flex gap-2">
            <button type="submit" className="btn btn-dark" disabled={busy}>Save changes</button>
            {submission.status === "ReturnedForCorrection" && (
              <button type="button" className="btn btn-success" disabled={busy} onClick={resubmit}>Resubmit</button>
            )}
            {submission.status === "PendingApproval" && (
              <button type="button" className="btn btn-outline-danger" disabled={busy} onClick={withdraw}>Withdraw</button>
            )}
          </div>
        )}
      </form>

      <h2 className="h6">Images</h2>
      <div className="d-flex flex-wrap gap-2 mb-2">
        {submission.images.map((img) => (
          <div key={img.id} className="position-relative">
            {previewUrls[img.id] ? (
              <img src={previewUrls[img.id]} alt="" width={100} height={125} className="border rounded d-block" style={{ objectFit: "cover" }} />
            ) : (
              <div className="bg-light border rounded d-flex align-items-center justify-content-center" style={{ width: 100, height: 125 }}>
                <span className="small text-muted">Loading...</span>
              </div>
            )}
            {editable && (
              <button type="button" className="btn btn-sm btn-danger position-absolute top-0 end-0" onClick={() => removeImage(img.id)}>
                &times;
              </button>
            )}
          </div>
        ))}
        {submission.images.length === 0 && <p className="text-muted">No images uploaded yet.</p>}
      </div>
      {editable && submission.images.length < MAX_IMAGES && (
        <input
          type="file"
          accept={IMAGE_ACCEPT}
          multiple
          className="form-control form-control-sm"
          style={{ maxWidth: 320 }}
          onChange={(e) => { addImages(e.target.files); e.target.value = ""; }}
        />
      )}
    </div>
  );
}
