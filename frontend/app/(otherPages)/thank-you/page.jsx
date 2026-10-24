import Footer1 from "@/components/footers/Footer1";
import Header1 from "@/components/headers/Header1";
import Topbar1 from "@/components/headers/Topbar1";
import Order from "@/components/otherPages/Order";
import React from "react";
export const metadata = {
  title: "Shop Order || E.",
  description: "The Choice of Elegant Women, Every Day",
};
export default function page() {
  return (
    <>
      <Topbar1 />
      <Header1 />
      <Order />
      <Footer1 />
    </>
  );
}
