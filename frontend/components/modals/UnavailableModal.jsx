"use client";

import { useEffect, useState } from "react";
import { useContextElement } from "@/context/Context";
import { subscribeBackInStock } from "@/lib/api/backInStock";

export default function UnavailableModal() {
  const { notifyItem } = useContextElement();
  const [name, setName] = useState("");
  const [whatsApp, setWhatsApp] = useState("+92");
  const [consent, setConsent] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [done, setDone] = useState(false);

  // Reset the form whenever a different product/variant is targeted, so a previous submission's
  // state doesn't leak into the next one.
  useEffect(() => {
    setName("");
    setWhatsApp("+92");
    setConsent(false);
    setError("");
    setDone(false);
  }, [notifyItem?.id, notifyItem?.variantId]);

  async function submit(event) {
    event.preventDefault();
    if (!notifyItem) return;

    setBusy(true);
    setError("");
    try {
      await subscribeBackInStock({
        productId: notifyItem.id,
        productVariantId: notifyItem.variantId ?? null,
        customerName: name,
        whatsAppNumber: whatsApp,
        consentGiven: consent,
      });
      setDone(true);
    } catch (err) {
      setError(err.message || "Something went wrong. Please try again.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div
      className="modal modalCentered fade modal-unavailable"
      id="unavailable"
    >
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <span className="icon-close-popup" data-bs-dismiss="modal">
            <i className="icon-close" />
          </span>
          <div className="tf-product-notify">
            {done ? (
              <>
                <h4 className="title">You&apos;re on the list!</h4>
                <p className="sub-title text-main-6">
                  Thank you! You&apos;re on the list. We&apos;ll notify you on
                  WhatsApp when this product is available again.
                </p>
              </>
            ) : (
              <>
                <h4 className="title">
                  Notify me when it&apos;s back in stock
                </h4>
                <p className="sub-title text-main-6">
                  Leave your WhatsApp number, and we&apos;ll notify you as
                  soon as this product becomes available again.
                </p>
                <form className="style-border form-notify" onSubmit={submit}>
                  <div className="form-content-2">
                    <input
                      type="text"
                      required
                      placeholder="Name"
                      value={name}
                      onChange={(e) => setName(e.target.value)}
                    />
                    <input
                      type="tel"
                      required
                      placeholder="WhatsApp Number"
                      value={whatsApp}
                      onChange={(e) => setWhatsApp(e.target.value)}
                    />
                  </div>
                  <div className="checkbox-wrap mt-3 mb-3">
                    <input
                      id="notifyConsent"
                      type="checkbox"
                      className="tf-check"
                      checked={consent}
                      onChange={(e) => setConsent(e.target.checked)}
                      required
                    />
                    <label htmlFor="notifyConsent" className="text-main-6">
                      I agree to receive a WhatsApp notification from E. when
                      this product is back in stock.
                    </label>
                  </div>
                  {error && <p className="text-danger mb-3">{error}</p>}
                  <button
                    type="submit"
                    className="tf-btn btn-fill-2 w-100 fw-medium animate-btn"
                    disabled={busy}
                  >
                    {busy ? "Submitting..." : "Notify me"}
                  </button>
                </form>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
