import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";
import Order from "@/components/otherPages/Order";
import React from "react";
export const metadata = {
  title: "Shop Order || E.",
  description: "The Choice of Elegant Women, Every Day",
};
export default function page() {
  return (
    <>
      <Header2 parentClass="tf-header style-2 line-bt-2" />
      <Order />
      <Footer2 />
    </>
  );
}
