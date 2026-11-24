"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { adminRequest, adminDownload, formatDateTime, getAdminToken } from "@/lib/admin/api";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_ADMIN_API_BASE_URL || process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5071";

// Vendor-original images are admin-only and gated by Bearer auth, so a plain <img src> (which
// can't carry an Authorization header) can't load them - fetch as a blob and show that instead.
function usePrivateImageUrls(submissionId, imageIds) {
  const [urls, setUrls] = useState({});

  useEffect(() => {
    let active = true;
    const created = [];
    (async () => {
      for (const imageId of imageIds) {
        try {
          const url = new URL(`/api/v1/admin/vendor-submissions/${submissionId}/images/${imageId}/download`, API_BASE_URL);
          const token = getAdminToken();
          // eslint-disable-next-line no-await-in-loop
          const response = await fetch(url, { headers: token ? { Authorization: `Bearer ${token}` } : {} });
          if (!response.ok) continue;
          // eslint-disable-next-line no-await-in-loop
          const blob = await response.blob();
          const objectUrl = URL.createObjectURL(blob);
          created.push(objectUrl);
          if (active) setUrls((current) => ({ ...current, [imageId]: objectUrl }));
        } catch {
          // Skip images that fail to load; the review screen still works without a preview.
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

export default function AdminVendorSubmissionDetailPage() {
  const { id } = useParams();
  const [submission, setSubmission] = useState(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [imageTypeChoice, setImageTypeChoice] = useState({});

  const load = useCallback(() => {
    adminRequest(`/vendor-submissions/${id}`).then(setSubmission).catch((err) => setError(err.message));
  }, [id]);

  useEffect(() => { load(); }, [load]);

  const imageIds = (submission?.images ?? []).map((i) => i.id);
  const previewUrls = usePrivateImageUrls(id, imageIds);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!submission) return <p>Loading...</p>;

  async function approve() {
    setBusy(true);
    setError("");
    try {
      await adminRequest(`/vendor-submissions/${id}/approve`, { method: "POST" });
      setNotice("Submission approved.");
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function reject() {
    const reason = window.prompt("Reason for rejection (shown to the vendor):");
    if (!reason) return;
    setBusy(true);
    setError("");
    try {
      await adminRequest(`/vendor-submissions/${id}/reject`, { method: "POST", body: { reason } });
      setNotice("Submission rejected.");
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function returnForCorrection() {
    const fieldsRequested = window.prompt("Which fields need correction? (shown to the vendor)");
    if (!fieldsRequested) return;
    const remarks = window.prompt("Additional remarks (optional):") || undefined;
    setBusy(true);
    setError("");
    try {
      await adminRequest(`/vendor-submissions/${id}/return-for-correction`, { method: "POST", body: { fieldsRequested, remarks } });
      setNotice("Returned to vendor for correction.");
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function publishImage(imageId) {
    const imageType = imageTypeChoice[imageId] || "Gallery";
    setBusy(true);
    setError("");
    try {
      await adminRequest(`/vendor-submissions/${id}/images/${imageId}/publish`, { method: "POST", body: { imageType } });
      setNotice(`Image published as ${imageType} on the product.`);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  function downloadImage(imageId) {
    adminDownload(`/vendor-submissions/${id}/images/${imageId}/download`, { filename: `vendor-image-${imageId}` });
  }

  return (
    <div style={{ maxWidth: 760 }}>
      <h1 className="h4 mb-1">{submission.productName}</h1>
      <p className="text-muted mb-3">
        {submission.shopName} ({submission.vendorCode}) &middot; {submission.categoryName} &middot; Wholesale {submission.wholesalePrice}
        {" "}&middot; Vendor code <code>{submission.vendorProductCode}</code>
      </p>
      <p className="text-muted mb-3">
        Status: <strong>{submission.status}</strong> &middot; Submitted {formatDateTime(submission.submittedAt)}
        {submission.productSku && <> &middot; Linked SKU <code>{submission.productSku}</code></>}
      </p>

      {notice && <div className="alert alert-success py-2">{notice}</div>}
      {error && <div className="alert alert-danger py-2">{error}</div>}

      {submission.status === "PendingApproval" && (
        <div className="d-flex flex-wrap gap-2 mb-4">
          <button className="btn btn-success btn-sm" disabled={busy} onClick={approve}>Approve</button>
          <button className="btn btn-outline-warning btn-sm" disabled={busy} onClick={returnForCorrection}>Return for correction</button>
          <button className="btn btn-outline-danger btn-sm" disabled={busy} onClick={reject}>Reject</button>
        </div>
      )}

      {submission.productId && (
        <div className="alert alert-info">
          A draft product was created automatically for this submission:{" "}
          <Link href={`/admin/products/${submission.productId}`}>{submission.productSku}</Link> (currently hidden).
          Open it to set pricing, description, and other attributes, use &quot;Publish&quot; below on any image you
          want to make public, then activate the product when it's ready to go live.
        </div>
      )}

      <h2 className="h6">Vendor's original images</h2>
      <div className="d-flex flex-wrap gap-3">
        {submission.images.map((img) => (
          <div key={img.id} className="border rounded p-2" style={{ width: 160 }}>
            {previewUrls[img.id] ? (
              <img src={previewUrls[img.id]} alt="" width={144} height={180} className="d-block mb-2" style={{ objectFit: "cover" }} />
            ) : (
              <div className="bg-light d-flex align-items-center justify-content-center mb-2" style={{ width: 144, height: 180 }}>
                <span className="small text-muted">Loading...</span>
              </div>
            )}
            <select
              className="form-select form-select-sm mb-2"
              value={imageTypeChoice[img.id] || "Gallery"}
              onChange={(e) => setImageTypeChoice({ ...imageTypeChoice, [img.id]: e.target.value })}
            >
              <option value="Primary">Primary</option>
              <option value="Hover">Hover</option>
              <option value="Gallery">Gallery</option>
            </select>
            <div className="d-grid gap-1">
              <button className="btn btn-dark btn-sm" disabled={busy || !submission.productId} onClick={() => publishImage(img.id)}>
                Use as product image
              </button>
              <button className="btn btn-outline-secondary btn-sm" onClick={() => downloadImage(img.id)}>Download</button>
            </div>
          </div>
        ))}
        {submission.images.length === 0 && <p className="text-muted">No images uploaded.</p>}
      </div>
    </div>
  );
}
