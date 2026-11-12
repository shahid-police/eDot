import Footer1 from "@/components/footers/Footer1";
import Header2 from "@/components/headers/Header2";
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
      <Header2 parentClass="tf-header style-2 line-bt-2" />
      <Order />
      <Footer1 />
    </>
  );
}
