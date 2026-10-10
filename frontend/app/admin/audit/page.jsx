"use client";

import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 50;

export default function AdminAuditPage() {
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest("/audit-logs", { query: { page, pageSize: PAGE_SIZE } })
      .then((data) => active && setResult(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [page]);

  const totalPages = result ? Math.max(1, Math.ceil(result.totalCount / PAGE_SIZE)) : 1;

  return (
    <div>
      <h1 className="h4 mb-3">Audit log</h1>
      {error && <div className="alert alert-danger">{error}</div>}
      {!result && !error && <p>Loading...</p>}

      {result && (
        <>
          <div className="table-responsive">
            <table className="table table-sm">
              <thead>
                <tr><th>When</th><th>Admin</th><th>Action</th><th>Entity</th><th>Summary</th></tr>
              </thead>
              <tbody>
                {result.items.map((entry) => (
                  <tr key={entry.id}>
                    <td>{formatDateTime(entry.createdAt)}</td>
                    <td>{entry.adminEmail}</td>
                    <td>{entry.action}</td>
                    <td>{entry.entityType}{entry.entityId ? ` #${entry.entityId}` : ""}</td>
                    <td className="small">{entry.summary}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="d-flex align-items-center gap-2">
            <button className="btn btn-outline-dark btn-sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>Previous</button>
            <span className="small">Page {page} of {totalPages}</span>
            <button className="btn btn-outline-dark btn-sm" disabled={page >= totalPages} onClick={() => setPage(page + 1)}>Next</button>
          </div>
        </>
      )}
    </div>
  );
}
