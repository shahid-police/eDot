import React from "react";
import Image from "next/image";
import CurrencySelect from "../common/CurrencySelect";
import LanguageSelect from "../common/LanguageSelect";

export default function Footer3() {
  return (
    <footer className="tf-footer">
      <div className="footer-bottom">
        <div className="container">
          <div className="footer-bottom-wrap">
            <div className="footer-bar-language">
              <div className="tf-currencies">
                <CurrencySelect />
              </div>
              <div className="tf-languages">
                <LanguageSelect />
              </div>
            </div>
            <p className="text-nocopy">All Rights Reserved 2025 E.</p>
            <ul className="paymend-method-list">
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/am-ex.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/apple-pay.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/dinner.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/discover.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/gg-pay.svg"
                    width={38}
                    height={24}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/master-2.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/master.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/shop-pay.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/unicon-pay.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    src="/images/payment/visa.svg"
                    width={40}
                    height={25}
                  />
                </a>
              </li>
            </ul>
          </div>
        </div>
      </div>
    </footer>
  );
}
