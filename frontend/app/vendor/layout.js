import VendorShell from "@/components/vendor/VendorShell";

export const metadata = {
  title: "Vendor Portal | E.",
  robots: { index: false, follow: false },
};

export default function VendorLayout({ children }) {
  return <VendorShell>{children}</VendorShell>;
}
