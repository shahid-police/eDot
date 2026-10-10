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

  async function submit(event) {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      const session = await adminRequest("/auth/login", { method: "POST", body: { email, password } });
      setAdminToken(session.accessToken);
      router.replace("/admin");
    } catch (err) {
      setError(err.message);
      setBusy(false);
    }
  }

  return (
    <div className="container py-5" style={{ maxWidth: 420 }}>
      <h1 className="h4 mb-4">Admin sign in</h1>
      <form onSubmit={submit} className="vstack gap-3">
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
