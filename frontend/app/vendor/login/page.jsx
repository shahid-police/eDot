"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { vendorRequest, setVendorToken } from "@/lib/vendor/api";

const STAGE = { PASSWORD: "password", ENROLL: "enroll", VERIFY: "verify" };

export default function VendorLoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  const [stage, setStage] = useState(STAGE.PASSWORD);
  const [pendingToken, setPendingToken] = useState(null);
  const [enrollment, setEnrollment] = useState(null); // { manualEntryKey, qrCodePngBase64 }
  const [code, setCode] = useState("");

  function finishLogin(session) {
    setVendorToken(session.accessToken);
    router.replace("/vendor/dashboard");
  }

  async function submitPassword(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const result = await vendorRequest("/auth/login", { method: "POST", body: { email, password } });
      setPendingToken(result.twoFactor.pendingToken);
      if (result.twoFactor.requiresEnrollment) {
        const enrollResult = await vendorRequest("/auth/2fa/enroll/start", {
          method: "POST",
          body: { pendingToken: result.twoFactor.pendingToken },
        });
        setEnrollment(enrollResult);
        setStage(STAGE.ENROLL);
      } else {
        setStage(STAGE.VERIFY);
      }
    } catch (err) {
      setError(err.message);
    } finally {
      setBusy(false);
    }
  }

  async function submitEnrollCode(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const session = await vendorRequest("/auth/2fa/enroll/confirm", {
        method: "POST",
        body: { pendingToken, code },
      });
      finishLogin(session);
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  async function submitVerifyCode(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const session = await vendorRequest("/auth/2fa/verify", { method: "POST", body: { pendingToken, code } });
      finishLogin(session);
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  function backToSignIn() {
    setStage(STAGE.PASSWORD);
    setPendingToken(null);
    setEnrollment(null);
    setCode("");
    setError("");
  }

  if (stage === STAGE.ENROLL && enrollment) {
    return (
      <div className="container py-5" style={{ maxWidth: 440 }}>
        <h1 className="h4 mb-2">Set up two-factor authentication</h1>
        <p className="text-muted mb-3">
          This is required before you can access the vendor portal. Scan the QR code with an authenticator app
          (Google Authenticator, Authy, etc.), then enter the 6-digit code it shows.
        </p>
        <img
          src={`data:image/png;base64,${enrollment.qrCodePngBase64}`}
          alt="Two-factor authentication QR code"
          className="border rounded mb-2"
          width={200}
          height={200}
        />
        <p className="small text-muted mb-3">
          Can&apos;t scan it? Enter this key manually: <code>{enrollment.manualEntryKey}</code>
        </p>
        <form onSubmit={submitEnrollCode} className="vstack gap-3">
          <div>
            <label className="form-label" htmlFor="vendor-2fa-enroll-code">Authentication code</label>
            <input
              id="vendor-2fa-enroll-code"
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
            {busy ? "Verifying..." : "Confirm and sign in"}
          </button>
          <button type="button" className="btn btn-link btn-sm" onClick={backToSignIn}>Back to sign in</button>
        </form>
      </div>
    );
  }

  if (stage === STAGE.VERIFY) {
    return (
      <div className="container py-5" style={{ maxWidth: 420 }}>
        <h1 className="h4 mb-2">Two-factor verification</h1>
        <p className="text-muted mb-4">Enter the 6-digit code from your authenticator app.</p>
        <form onSubmit={submitVerifyCode} className="vstack gap-3">
          <div>
            <label className="form-label" htmlFor="vendor-2fa-code">Authentication code</label>
            <input
              id="vendor-2fa-code"
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
          <button type="button" className="btn btn-link btn-sm" onClick={backToSignIn}>Back to sign in</button>
        </form>
      </div>
    );
  }

  return (
    <div className="container py-5" style={{ maxWidth: 420 }}>
      <h1 className="h4 mb-4">Vendor sign in</h1>
      <form onSubmit={submitPassword} className="vstack gap-3">
        <div>
          <label className="form-label" htmlFor="vendor-email">Email</label>
          <input id="vendor-email" type="email" className="form-control" value={email} onChange={(e) => setEmail(e.target.value)} required />
        </div>
        <div>
          <label className="form-label" htmlFor="vendor-password">Password</label>
          <input id="vendor-password" type="password" className="form-control" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>
        {error && <div className="alert alert-danger py-2 mb-0">{error}</div>}
        <button type="submit" className="btn btn-dark" disabled={busy}>
          {busy ? "Signing in..." : "Sign in"}
        </button>
      </form>
    </div>
  );
}
