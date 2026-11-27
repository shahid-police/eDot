"use client";
import Link from "next/link";

// Small inline icons (star/wallet/diamond/crown) - the icon font has no matching glyphs, so these
// are hand-drawn to keep the chip row dependency-free.
const ICONS = {
  any: (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" strokeLinecap="round">
      <path d="M12 2.5l2.6 5.6 6.1.6-4.6 4.1 1.3 6-5.4-3.1-5.4 3.1 1.3-6-4.6-4.1 6.1-.6z" />
    </svg>
  ),
  budget: (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" strokeLinecap="round">
      <rect x="2.5" y="6.5" width="19" height="13" rx="2" />
      <path d="M2.5 10.2h19" />
      <circle cx="16.8" cy="14.7" r="1.3" fill="currentColor" stroke="none" />
    </svg>
  ),
  premium: (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" strokeLinecap="round">
      <path d="M12 2.8l5.4 5.4L12 21.2 6.6 8.2z" />
      <path d="M6.3 8.2h11.4" />
    </svg>
  ),
  luxury: (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinejoin="round" strokeLinecap="round">
      <path d="M3 8.3l4 2.9 5-6 5 6 4-2.9-2.1 10.9H5.1z" />
    </svg>
  ),
};

const CHIPS = [
  { value: "", label: "Any Item", icon: "any" },
  { value: "Budget", label: "Budget", icon: "budget" },
  { value: "Premium", label: "Premium", icon: "premium" },
  { value: "Luxury", label: "Luxury", icon: "luxury" },
];

// Navigation chips that drive server-side filtering via the `preference` query param - clicking
// one is a real link to a new URL, not a client-side re-filter of already-fetched products.
export default function ShoppingPreferenceChips({ active = "", basePath = "/shop-default", className = "", elevated = false }) {
  return (
    <div
      className={`d-flex flex-wrap justify-content-center gap-2 gap-sm-3 ${className}`}
      role="navigation"
      aria-label="Shop by preference"
    >
      <style jsx>{`
        .preference-chip:hover {
          transform: translateY(-2px);
        }
      `}</style>
      {CHIPS.map((chip) => {
        const selected = (active || "") === chip.value;
        const href = chip.value ? `${basePath}?preference=${chip.value}` : basePath;
        return (
          <Link
            key={chip.label}
            href={href}
            className="d-inline-flex align-items-center gap-2 preference-chip"
            style={{
              padding: "11px 22px",
              borderRadius: 999,
              border: selected ? "1px solid var(--primary)" : "1px solid rgba(60, 36, 20, 0.12)",
              background: selected ? "var(--primary)" : "#fff",
              color: selected ? "#fff" : "var(--main)",
              fontSize: 14,
              fontWeight: 600,
              letterSpacing: 0.2,
              textDecoration: "none",
              whiteSpace: "nowrap",
              boxShadow: selected
                ? "0 10px 24px rgba(174, 135, 62, 0.35)"
                : elevated
                ? "0 10px 24px rgba(20, 12, 6, 0.12)"
                : "0 2px 8px rgba(20, 12, 6, 0.06)",
              transition: "transform 0.15s ease, box-shadow 0.15s ease",
            }}
          >
            {ICONS[chip.icon]}
            {chip.label}
          </Link>
        );
      })}
    </div>
  );
}
