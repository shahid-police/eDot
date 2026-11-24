"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { clearVendorToken, getVendorToken, vendorRequest } from "@/lib/vendor/api";

const NAV = [
  { href: "/vendor/dashboard", label: "Dashboard" },
  { href: "/vendor/products", label: "My Products" },
];

export default function VendorShell({ children }) {
  const pathname = usePathname();
  const router = useRouter();
  const [ready, setReady] = useState(false);
  const isLogin = pathname === "/vendor/login";

  useEffect(() => {
    if (!isLogin && !getVendorToken()) {
      router.replace("/vendor/login");
      return;
    }
    setReady(true);
  }, [isLogin, pathname, router]);

  if (isLogin) {
    return children;
  }

  if (!ready) {
    return null;
  }

  async function signOut() {
    try {
      await vendorRequest("/auth/logout", { method: "POST" });
    } catch {
      // Sign out locally regardless of whether the server call succeeded.
    }
    clearVendorToken();
    router.replace("/vendor/login");
  }

  return (
    <div className="container-fluid py-3 px-3 px-lg-4 vendor-shell">
      <nav className="d-flex flex-wrap align-items-center gap-3 border-bottom pb-3 mb-4">
        <strong className="me-2">Vendor Portal</strong>
        {NAV.map((item) => {
          const active = pathname.startsWith(item.href);
          return (
            <Link key={item.href} href={item.href} className={`btn btn-sm ${active ? "btn-dark" : "btn-outline-dark"}`}>
              {item.label}
            </Link>
          );
        })}
        <button type="button" className="btn btn-sm btn-outline-secondary ms-auto" onClick={signOut}>
          Sign out
        </button>
      </nav>
      {children}
    </div>
  );
}
