"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { adminRequest, setAdminToken } from "@/lib/admin/api";

export default function AdminLoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  // Set once a password-only login comes back with a two-factor challenge instead of a session.
  const [pendingToken, setPendingToken] = useState(null);
  const [code, setCode] = useState("");

  async function submitPassword(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const result = await adminRequest("/auth/login", { method: "POST", body: { email, password } });
      if (result.session) {
        setAdminToken(result.session.accessToken);
        router.replace("/admin");
        return;
      }
      setPendingToken(result.twoFactor.pendingToken);
      setBusy(false);
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  async function submitCode(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const session = await adminRequest("/auth/2fa/verify", { method: "POST", body: { pendingToken, code } });
      setAdminToken(session.accessToken);
      router.replace("/admin");
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  if (pendingToken) {
    return (
      <div className="container py-5" style={{ maxWidth: 420 }}>
        <h1 className="h4 mb-2">Two-factor verification</h1>
        <p className="text-muted mb-4">Enter the 6-digit code from your authenticator app.</p>
        <form onSubmit={submitCode} className="vstack gap-3">
          <div>
            <label className="form-label" htmlFor="admin-2fa-code">Authentication code</label>
            <input
              id="admin-2fa-code"
              type="text"
              inputMode="numeric"
              autoComplete="one-time-code"
              maxLength={6}
              className="form-control"
              value={code}
              onChange={(e) => setCode(e.target.value.replace(/\D/g, ""))}
              autoFocus
              required
            />
          </div>
          {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
          <button type="submit" className="btn btn-dark" disabled={busy || code.length !== 6}>
            {busy ? "Verifying..." : "Verify"}
          </button>
          <button
            type="button"
            className="btn btn-link btn-sm"
            onClick={() => {
              setPendingToken(null);
              setCode("");
              setError("");
            }}
          >
            Back to sign in
          </button>
        </form>
      </div>
    );
  }

  return (
    <div className="container py-5" style={{ maxWidth: 420 }}>
      <h1 className="h4 mb-4">Admin sign in</h1>
      <form onSubmit={submitPassword} className="vstack gap-3">
        <div>
          <label className="form-label" htmlFor="admin-email">Email</label>
          <input id="admin-email" type="email" className="form-control" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <div>
          <label className="form-label" htmlFor="admin-password">Password</label>
          <input id="admin-password" type="password" className="form-control" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>
        {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
        <button type="submit" className="btn btn-dark" disabled={busy}>
          {busy ? "Signing in..." : "Sign in"}
        </button>
      </form>
    </div>
  );
}
