import Header1 from "@/components/headers/Header1";
import Topbar1 from "@/components/headers/Topbar1";
import React from "react";
import Link from "next/link";
import Image from "next/image";
import CountdownTimer from "@/components/common/Countdown";
import CommingSoonForm from "@/components/otherPages/CommingSoonForm";
export const metadata = {
  title: "Comming Soon || E.",
  description: "The Choice of Elegant Women, Every Day",
};
export default function page() {
  return (
    <>
      <Topbar1 />
      <Header1 parentClass="tf-header" />
      <div className="wg-coming">
        <div className="image">
          <Image
            src="/images/section/coming-soon.jpg"
            alt="Coming"
            className="lazyload"
            width={2879}
            height={1401}
          />
        </div>
        <div className="content">
          <h1 className="head">Coming Soon!</h1>
          <p className="sub-head">
            We couldn’t find the page you were looking for. We suggest you
            return to homepage
          </p>
          <div className="countdown-V06">
            <div className="js-countdown">
              <CountdownTimer style={2} />
            </div>
          </div>
          <CommingSoonForm />
          <Link href={`/`} className="tf-btn btn-fill fw-medium animate-btn">
            RETURN TO HOMEPAGE
          </Link>
        </div>
      </div>
    </>
  );
}
