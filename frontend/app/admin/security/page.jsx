"use client";

import { useEffect, useState } from "react";
import { adminRequest } from "@/lib/admin/api";

export default function AdminSecurityPage() {
  const [status, setStatus] = useState(null);
  const [error, setError] = useState("");

  // Enrollment flow state.
  const [enrolling, setEnrolling] = useState(false);
  const [enrollPassword, setEnrollPassword] = useState("");
  const [enrollment, setEnrollment] = useState(null); // { manualEntryKey, qrCodePngBase64 }
  const [enrollCode, setEnrollCode] = useState("");
  const [busy, setBusy] = useState(false);

  // Disable flow state.
  const [disabling, setDisabling] = useState(false);
  const [disablePassword, setDisablePassword] = useState("");
  const [disableCode, setDisableCode] = useState("");

  function loadStatus() {
    setError("");
    adminRequest("/2fa/status")
      .then(setStatus)
      .catch((err) => setError(err.message));
  }

  useEffect(loadStatus, []);

  async function startEnrollment(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const result = await adminRequest("/2fa/enroll/start", { method: "POST", body: { password: enrollPassword } });
      setEnrollment(result);
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function confirmEnrollment(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await adminRequest("/2fa/enroll/verify", { method: "POST", body: { code: enrollCode } });
      resetEnrollment();
      loadStatus();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  function resetEnrollment() {
    setEnrolling(false);
    setEnrollPassword("");
    setEnrollment(null);
    setEnrollCode("");
  }

  async function submitDisable(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await adminRequest("/2fa/disable", { method: "POST", body: { password: disablePassword, code: disableCode } });
      setDisabling(false);
      setDisablePassword("");
      setDisableCode("");
      loadStatus();
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  return (
    <div style={{ maxWidth: 480 }}>
      <h1 className="h4 mb-3">Security</h1>
      {error && <div className="alert alert-danger py-2">{error}</div>}

      {!status && !error && <p>Loading...</p>}

      {status && !enrolling && !disabling && (
        <div className="card">
          <div className="card-body d-flex align-items-center justify-content-between">
            <div>
              <div className="fw-medium">Two-factor authentication</div>
              <div className="small text-muted">
                {status.enabled ? "Enabled - a code is required at sign-in." : "Not enabled."}
              </div>
            </div>
            {status.enabled ? (
              <button type="button" className="btn btn-outline-danger btn-sm" onClick={() => setDisabling(true)}>
                Disable
              </button>
            ) : (
              <button type="button" className="btn btn-dark btn-sm" onClick={() => setEnrolling(true)}>
                Enable
              </button>
            )}
          </div>
        </div>
      )}

      {enrolling && !enrollment && (
        <form onSubmit={startEnrollment} className="vstack gap-3">
          <p className="text-muted small">Confirm your password to start enrollment.</p>
          <div>
            <label className="form-label" htmlFor="enroll-password">Password</label>
            <input
              id="enroll-password"
              type="password"
              className="form-control"
              value={enrollPassword}
              onChange={(e) => setEnrollPassword(e.target.value)}
              autoFocus
              required
            />
          </div>
          <div className="d-flex gap-2">
            <button type="submit" className="btn btn-dark" disabled={busy}>
              {busy ? "Starting..." : "Continue"}
            </button>
            <button type="button" className="btn btn-outline-secondary" onClick={resetEnrollment}>
              Cancel
            </button>
          </div>
        </form>
      )}

      {enrollment && (
        <form onSubmit={confirmEnrollment} className="vstack gap-3">
          <p className="text-muted small mb-1">
            Scan this with Google Authenticator (or any TOTP app), then enter the 6-digit code it shows.
          </p>
          <img
            src={`data:image/png;base64,${enrollment.qrCodePngBase64}`}
            alt="Two-factor QR code"
            width={200}
            height={200}
            className="border rounded p-2"
          />
          <details>
            <summary className="small text-muted" style={{ cursor: "pointer" }}>Can&apos;t scan? Enter manually</summary>
            <code className="d-block mt-2 p-2 bg-light rounded small" style={{ wordBreak: "break-all" }}>
              {enrollment.manualEntryKey}
            </code>
          </details>
          <div>
            <label className="form-label" htmlFor="enroll-code">Authentication code</label>
            <input
              id="enroll-code"
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              className="form-control"
              value={enrollCode}
              onChange={(e) => setEnrollCode(e.target.value.replace(/\D/g, ""))}
              required
            />
          </div>
          <div className="d-flex gap-2">
            <button type="submit" className="btn btn-dark" disabled={busy || enrollCode.length !== 6}>
              {busy ? "Activating..." : "Activate"}
            </button>
            <button type="button" className="btn btn-outline-secondary" onClick={resetEnrollment}>
              Cancel
            </button>
          </div>
        </form>
      )}

      {disabling && (
        <form onSubmit={submitDisable} className="vstack gap-3">
          <p className="text-muted small">Confirm your password and a current authentication code to disable two-factor authentication.</p>
          <div>
            <label className="form-label" htmlFor="disable-password">Password</label>
            <input
              id="disable-password"
              type="password"
              className="form-control"
              value={disablePassword}
              onChange={(e) => setDisablePassword(e.target.value)}
              autoFocus
              required
            />
          </div>
          <div>
            <label className="form-label" htmlFor="disable-code">Authentication code</label>
            <input
              id="disable-code"
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              className="form-control"
              value={disableCode}
              onChange={(e) => setDisableCode(e.target.value.replace(/\D/g, ""))}
              required
            />
          </div>
          <div className="d-flex gap-2">
            <button type="submit" className="btn btn-outline-danger" disabled={busy || disableCode.length !== 6}>
              {busy ? "Disabling..." : "Disable two-factor authentication"}
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary"
              onClick={() => {
                setDisabling(false);
                setDisablePassword("");
                setDisableCode("");
              }}
            >
              Cancel
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
