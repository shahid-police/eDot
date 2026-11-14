import React from "react";

export default function Footer2() {
  return (
    <footer className="tf-footer style-2 bg-dark-brown">
      <div className="container">
        <div className="d-flex justify-content-center" style={{ padding: "48px 0" }}>
          <ul className="tf-social-icon style-white">
            <li>
              <a
                href="https://www.facebook.com/"
                target="_blank"
                className="social-facebook"
              >
                <span className="icon">
                  <i className="icon-facebook" />
                </span>
              </a>
            </li>
            <li>
              <a
                href="https://www.instagram.com/"
                target="_blank"
                className="social-instagram"
              >
                <span className="icon">
                  <i className="icon-instagram" />
                </span>
              </a>
            </li>
            <li>
              <a href="https://x.com/" target="_blank" className="social-x">
                <span className="icon">
                  <i className="icon-x" />
                </span>
              </a>
            </li>
            <li>
              <a
                href="https://www.snapchat.com/"
                target="_blank"
                className="social-snapchat"
              >
                <span className="icon">
                  <i className="icon-snapchat" />
                </span>
              </a>
            </li>
          </ul>
        </div>
      </div>
    </footer>
  );
}
