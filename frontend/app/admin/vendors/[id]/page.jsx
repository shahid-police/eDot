"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const STATUSES = ["Active", "Suspended", "Deactivated"];

export default function AdminVendorDetailPage() {
  const { id } = useParams();
  const [vendor, setVendor] = useState(null);
  const [submissions, setSubmissions] = useState(null);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [resetPassword, setResetPassword] = useState(null);

  const load = useCallback(() => {
    adminRequest(`/vendors/${id}`).then(setVendor).catch((err) => setError(err.message));
    adminRequest("/vendor-submissions", { query: { vendorId: id, pageSize: 50 } }).then(setSubmissions).catch(() => {});
  }, [id]);

  useEffect(() => { load(); }, [load]);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!vendor) return <p>Loading...</p>;

  async function setStatus(status) {
    setBusy(true);
    setNotice("");
    try {
      await adminRequest(`/vendors/${id}/status`, { method: "PUT", body: { status } });
      setNotice(`Status set to ${status}.`);
      load();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function resetCredentials() {
    if (!window.confirm("Reset this vendor's password and sign them out of all sessions?")) return;
    setBusy(true);
    try {
      const result = await adminRequest(`/vendors/${id}/reset-credentials`, { method: "POST" });
      setResetPassword(result.temporaryPassword);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div>
      <h1 className="h4 mb-1">{vendor.shopName}</h1>
      <p className="text-muted mb-3">
        Code <code>{vendor.vendorCode}</code> &middot; {vendor.email} &middot; Created {formatDateTime(vendor.createdAt)}
      </p>

      <div className="row g-4">
        <div className="col-md-5">
          <h2 className="h6">Profile</h2>
          <dl className="row small">
            <dt className="col-5">Contact</dt><dd className="col-7">{vendor.contactPerson}</dd>
            <dt className="col-5">Phone</dt><dd className="col-7">{vendor.phone}</dd>
            <dt className="col-5">Address</dt><dd className="col-7">{vendor.address ?? "-"}</dd>
            <dt className="col-5">City</dt><dd className="col-7">{vendor.city ?? "-"}</dd>
            <dt className="col-5">Status</dt><dd className="col-7">{vendor.status}</dd>
            <dt className="col-5">Remarks</dt><dd className="col-7">{vendor.adminRemarks ?? "-"}</dd>
          </dl>

          {notice && <div className="alert alert-success py-2">{notice}</div>}
          {resetPassword && (
            <div className="alert alert-success py-2">
              New temporary password (communicate securely - shown once): <code>{resetPassword}</code>
            </div>
          )}

          <div className="d-flex flex-wrap gap-2 mb-2">
            {STATUSES.filter((s) => s !== vendor.status).map((s) => (
              <button key={s} className="btn btn-outline-dark btn-sm" disabled={busy} onClick={() => setStatus(s)}>
                Set {s}
              </button>
            ))}
          </div>
          <button className="btn btn-outline-secondary btn-sm" disabled={busy} onClick={resetCredentials}>
            Reset credentials
          </button>
        </div>

        <div className="col-md-7">
          <h2 className="h6">Submissions</h2>
          {submissions && (
            <table className="table table-sm align-middle">
              <thead><tr><th>Product</th><th>Status</th><th>Submitted</th></tr></thead>
              <tbody>
                {submissions.items.map((s) => (
                  <tr key={s.id}>
                    <td><Link href={`/admin/vendor-submissions/${s.id}`}>{s.productName}</Link></td>
                    <td>{s.status}</td>
                    <td>{formatDateTime(s.submittedAt)}</td>
                  </tr>
                ))}
                {submissions.items.length === 0 && (
                  <tr><td colSpan={3} className="text-muted">No submissions yet.</td></tr>
                )}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
