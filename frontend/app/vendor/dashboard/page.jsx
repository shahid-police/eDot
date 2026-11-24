"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { vendorRequest } from "@/lib/vendor/api";

export default function VendorDashboardPage() {
  const [profile, setProfile] = useState(null);
  const [counts, setCounts] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    vendorRequest("/profile").then(setProfile).catch((err) => setError(err.message));
    vendorRequest("/products", { query: { pageSize: 1 } })
      .then(async (all) => {
        const pending = await vendorRequest("/products", { query: { status: "PendingApproval", pageSize: 1 } });
        const approved = await vendorRequest("/products", { query: { status: "Approved", pageSize: 1 } });
        const rejected = await vendorRequest("/products", { query: { status: "Rejected", pageSize: 1 } });
        const corrections = await vendorRequest("/products", { query: { status: "ReturnedForCorrection", pageSize: 1 } });
        setCounts({
          total: all.totalCount,
          pending: pending.totalCount,
          approved: approved.totalCount,
          rejected: rejected.totalCount,
          corrections: corrections.totalCount,
        });
      })
      .catch((err) => setError(err.message));
  }, []);

  if (error) return <div className="alert alert-danger">{error}</div>;

  return (
    <div>
      <h1 className="h4 mb-3">Overview</h1>
      {profile && (
        <p className="text-muted mb-4">
          {profile.shopName} &middot; Vendor code <code>{profile.vendorCode}</code>
        </p>
      )}

      {counts && (
        <div className="row g-3 mb-4">
          {[
            ["Total submitted", counts.total],
            ["Pending approval", counts.pending],
            ["Approved", counts.approved],
            ["Rejected", counts.rejected],
            ["Returned for correction", counts.corrections],
          ].map(([label, value]) => (
            <div className="col-6 col-md-3" key={label}>
              <div className="border rounded p-3 h-100">
                <div className="h3 mb-0">{value}</div>
                <div className="small text-muted">{label}</div>
              </div>
            </div>
          ))}
        </div>
      )}

      <Link href="/vendor/products/new" className="btn btn-dark">Add new product</Link>
    </div>
  );
}
