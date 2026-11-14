import AccountSidebar from "@/components/account/AccountSidebar";
import MyAccount from "@/components/account/MyAccount";
import Footer1 from "@/components/footers/Footer1";
import Header2 from "@/components/headers/Header2";
import Link from "next/link";
import React from "react";
export const metadata = {
  title: "Account || E.",
  description: "The Choice of Elegant Women, Every Day",
};
export default function page() {
  return (
    <>
      <Header2 parentClass="tf-header style-2" />
      <section className="flat-spacing-2 pb-0">
        <div className="container">
          <div className="page-title">
            <div className="breadcrumbs">
              <ul className="bread-wrap">
                <li>
                  <Link href={`/`} className="text-main-4 link-secondary">
                    Home
                  </Link>
                </li>
                <li className="br-line w-12 bg-main" />
                <li>Account</li>
              </ul>
              <h1 className="heading fw-normal text-uppercase">my account</h1>
            </div>
          </div>
        </div>
      </section>
      <div className="flat-spacing">
        <div className="container">
          <div className="row">
            <div className="col-lg-4">
              <div className="sidebar-account-wrap sidebar-content-wrap sticky-top d-lg-block d-none">
                <ul className="my-account-nav">
                  <AccountSidebar />
                </ul>
              </div>
            </div>
            <div className="col-lg-8">
              <MyAccount />
            </div>
          </div>
        </div>
      </div>

      <Footer1 />
    </>
  );
}
