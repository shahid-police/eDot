import React from "react";
import Image from "next/image";
import CurrencySelect from "../common/CurrencySelect";
import LanguageSelect from "../common/LanguageSelect";

export default function Footer1() {
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
                    width={40}
                    height={25}
                    src="/images/payment/am-ex.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/apple-pay.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/dinner.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/discover.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={38}
                    height={24}
                    src="/images/payment/gg-pay.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/master-2.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/master.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/shop-pay.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/unicon-pay.svg"
                  />
                </a>
              </li>
              <li>
                <a href="#">
                  <Image
                    alt="Paymend Method"
                    width={40}
                    height={25}
                    src="/images/payment/visa.svg"
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
