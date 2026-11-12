"use client";

import { useEffect, useState } from "react";
import { adminRequest, formatDateTime } from "@/lib/admin/api";

const PAGE_SIZE = 25;

const SUBSCRIPTION_STATUSES = ["Active", "Notified", "Unsubscribed"];
const NOTIFICATION_STATUSES = ["Pending", "Queued", "Sent", "Delivered", "Read", "Failed"];

function StatusBadge({ status }) {
  const tone = {
    Active: "text-bg-primary",
    Notified: "text-bg-success",
    Unsubscribed: "text-bg-secondary",
    Pending: "text-bg-secondary",
    Queued: "text-bg-info",
    Sent: "text-bg-primary",
    Delivered: "text-bg-success",
    Read: "text-bg-success",
    Failed: "text-bg-danger",
  }[status] || "text-bg-secondary";

  return <span className={`badge ${tone}`}>{status}</span>;
}

export default function AdminBackInStockPage() {
  const [summary, setSummary] = useState(null);
  const [summaryError, setSummaryError] = useState("");
  const [summaryReload, setSummaryReload] = useState(0);

  const [subSearch, setSubSearch] = useState("");
  const [subQuery, setSubQuery] = useState("");
  const [subStatus, setSubStatus] = useState("");
  const [subPage, setSubPage] = useState(1);
  const [subResult, setSubResult] = useState(null);
  const [subError, setSubError] = useState("");

  const [notifStatus, setNotifStatus] = useState("");
  const [notifPage, setNotifPage] = useState(1);
  const [notifResult, setNotifResult] = useState(null);
  const [notifError, setNotifError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest("/back-in-stock/summary")
      .then((data) => active && setSummary(data))
      .catch((err) => active && setSummaryError(err.message));
    return () => { active = false; };
  }, [summaryReload]);

  useEffect(() => {
    let active = true;
    adminRequest("/back-in-stock/subscriptions", { query: { search: subQuery, status: subStatus, page: subPage, pageSize: PAGE_SIZE } })
      .then((data) => active && setSubResult(data))
      .catch((err) => active && setSubError(err.message));
    return () => { active = false; };
  }, [subQuery, subStatus, subPage]);

  useEffect(() => {
    let active = true;
    adminRequest("/back-in-stock/notifications", { query: { status: notifStatus, page: notifPage, pageSize: PAGE_SIZE } })
      .then((data) => active && setNotifResult(data))
      .catch((err) => active && setNotifError(err.message));
    return () => { active = false; };
  }, [notifStatus, notifPage]);

  function applySubSearch(event) {
    event.preventDefault();
    setSubPage(1);
    setSubQuery(subSearch.trim());
  }

  const subTotalPages = subResult ? Math.max(1, Math.ceil(subResult.totalCount / PAGE_SIZE)) : 1;
  const notifTotalPages = notifResult ? Math.max(1, Math.ceil(notifResult.totalCount / PAGE_SIZE)) : 1;

  const stats = summary && [
    ["Active subscriptions", summary.activeSubscriptions],
    ["Notified", summary.notifiedSubscriptions],
    ["Unsubscribed", summary.unsubscribedSubscriptions],
    ["Pending sends", summary.pendingNotifications],
    ["Queued", summary.queuedNotifications],
    ["Sent", summary.sentNotifications],
    ["Delivered", summary.deliveredNotifications],
    ["Read", summary.readNotifications],
    ["Failed", summary.failedNotifications],
  ];

  return (
    <div>
      <h1 className="h4 mb-3">Back in Stock</h1>

      {summaryError && <div className="alert alert-danger">{summaryError}</div>}
      {!summary && !summaryError && <p>Loading...</p>}
      {stats && (
        <div className="row g-3 mb-4">
          {stats.map(([label, value]) => (
            <div key={label} className="col-6 col-md-4 col-xl-3">
              <div className="card h-100">
                <div className="card-body">
                  <div className="small text-muted">{label}</div>
                  <div className="fs-5 fw-semibold">{value}</div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      <div className="d-flex align-items-center justify-content-between mb-2">
        <h2 className="h6 mb-0">Subscriptions</h2>
        <button
          className="btn btn-sm btn-outline-secondary"
          onClick={() => setSummaryReload((n) => n + 1)}
          type="button"
        >
          Refresh stats
        </button>
      </div>

      <div className="d-flex flex-wrap gap-2 mb-3">
        <form onSubmit={applySubSearch} className="d-flex gap-2">
          <input
            className="form-control form-control-sm"
            placeholder="Customer, number, or product"
            value={subSearch}
            onChange={(e) => setSubSearch(e.target.value)}
          />
          <button className="btn btn-outline-dark btn-sm" type="submit">Search</button>
        </form>
        <select
          className="form-select form-select-sm w-auto"
          value={subStatus}
          onChange={(e) => { setSubPage(1); setSubStatus(e.target.value); }}
        >
          <option value="">All statuses</option>
          {SUBSCRIPTION_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {subError && <div className="alert alert-danger">{subError}</div>}
      {!subResult && !subError && <p>Loading...</p>}

      {subResult && (
        <>
          <div className="table-responsive mb-2">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Product</th>
                  <th>Customer</th>
                  <th>WhatsApp number</th>
                  <th>Status</th>
                  <th>Subscribed</th>
                  <th>Notified</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {subResult.items.map((s) => (
                  <tr key={s.id}>
                    <td>{s.productName}{s.variantName ? ` - ${s.variantName}` : ""}</td>
                    <td>{s.customerName}</td>
                    <td>{s.whatsAppNumber}</td>
                    <td><StatusBadge status={s.status} /></td>
                    <td>{formatDateTime(s.subscribedAt)}</td>
                    <td>{formatDateTime(s.notifiedAt)}</td>
                    <td className="text-end">
                      <a className="btn btn-sm btn-success" href={s.whatsAppChatUrl} target="whatsapp-chat" rel="noopener noreferrer">
                        WhatsApp
                      </a>
                    </td>
                  </tr>
                ))}
                {subResult.items.length === 0 && (
                  <tr><td colSpan={7} className="text-muted">No subscriptions match.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <div className="d-flex align-items-center gap-2 mb-4">
            <button className="btn btn-outline-dark btn-sm" disabled={subPage <= 1} onClick={() => setSubPage(subPage - 1)}>Previous</button>
            <span className="small">Page {subPage} of {subTotalPages}</span>
            <button className="btn btn-outline-dark btn-sm" disabled={subPage >= subTotalPages} onClick={() => setSubPage(subPage + 1)}>Next</button>
          </div>
        </>
      )}

      <h2 className="h6 mb-2">Notification history</h2>

      <div className="d-flex flex-wrap gap-2 mb-3">
        <select
          className="form-select form-select-sm w-auto"
          value={notifStatus}
          onChange={(e) => { setNotifPage(1); setNotifStatus(e.target.value); }}
        >
          <option value="">All statuses</option>
          {NOTIFICATION_STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
      </div>

      {notifError && <div className="alert alert-danger">{notifError}</div>}
      {!notifResult && !notifError && <p>Loading...</p>}

      {notifResult && (
        <>
          <div className="table-responsive mb-2">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Product</th>
                  <th>WhatsApp number</th>
                  <th>Status</th>
                  <th className="text-end">Retries</th>
                  <th>Sent</th>
                  <th>Delivered</th>
                  <th>Failure reason</th>
                </tr>
              </thead>
              <tbody>
                {notifResult.items.map((n) => (
                  <tr key={n.id}>
                    <td>{n.productName}</td>
                    <td>{n.whatsAppNumber}</td>
                    <td><StatusBadge status={n.status} /></td>
                    <td className="text-end">{n.retryCount}</td>
                    <td>{formatDateTime(n.sentAt)}</td>
                    <td>{formatDateTime(n.deliveredAt)}</td>
                    <td className="text-danger small">{n.failureReason || "-"}</td>
                  </tr>
                ))}
                {notifResult.items.length === 0 && (
                  <tr><td colSpan={7} className="text-muted">No notifications match.</td></tr>
                )}
              </tbody>
            </table>
          </div>
          <div className="d-flex align-items-center gap-2">
            <button className="btn btn-outline-dark btn-sm" disabled={notifPage <= 1} onClick={() => setNotifPage(notifPage - 1)}>Previous</button>
            <span className="small">Page {notifPage} of {notifTotalPages}</span>
            <button className="btn btn-outline-dark btn-sm" disabled={notifPage >= notifTotalPages} onClick={() => setNotifPage(notifPage + 1)}>Next</button>
          </div>
        </>
      )}
    </div>
  );
}
