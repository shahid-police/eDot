"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { clearAdminToken, getAdminToken } from "@/lib/admin/api";

const NAV = [
  { href: "/admin", label: "Dashboard" },
  { href: "/admin/orders", label: "Orders" },
  { href: "/admin/products", label: "Products" },
  { href: "/admin/inventory", label: "Inventory" },
  { href: "/admin/customers", label: "Customers" },
  { href: "/admin/audit", label: "Audit log" },
];

export default function AdminShell({ children }) {
  const pathname = usePathname();
  const router = useRouter();
  const [ready, setReady] = useState(false);
  const isLogin = pathname === "/admin/login";

  useEffect(() => {
    if (!isLogin && !getAdminToken()) {
      router.replace("/admin/login");
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

  function signOut() {
    clearAdminToken();
    router.replace("/admin/login");
  }

  return (
    <div className="container-fluid py-3 px-3 px-lg-4 admin-shell">
      <nav className="d-flex flex-wrap align-items-center gap-3 border-bottom pb-3 mb-4">
        <strong className="me-2">E. Admin</strong>
        {NAV.map((item) => {
          const active = item.href === "/admin" ? pathname === "/admin" : pathname.startsWith(item.href);
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
