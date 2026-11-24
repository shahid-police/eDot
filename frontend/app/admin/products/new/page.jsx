"use client";

import { Suspense, useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import ProductForm, { EMPTY_PRODUCT } from "@/components/admin/ProductForm";
import { adminRequest } from "@/lib/admin/api";

// When reached as /admin/products/new?fromSubmission=<id> (the "Create Product" action on an
// approved vendor submission), pre-fills Name/Category/Cost price from that submission and, once
// the product is created through the normal unmodified flow below, links the submission to it -
// the SKU/price/description/publication pipeline here is exactly the same one every other
// product goes through.
function NewProductPageInner() {
  const searchParams = useSearchParams();
  const fromSubmission = searchParams.get("fromSubmission");
  const [initial, setInitial] = useState(fromSubmission ? null : EMPTY_PRODUCT);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!fromSubmission) return;
    adminRequest(`/vendor-submissions/${fromSubmission}`)
      .then((submission) => {
        setInitial({
          ...EMPTY_PRODUCT,
          name: submission.productName,
          categoryId: String(submission.categoryId),
          costPrice: String(submission.wholesalePrice),
        });
      })
      .catch((err) => setError(err.message));
  }, [fromSubmission]);

  async function handleCreated(created) {
    await adminRequest(`/vendor-submissions/${fromSubmission}/link-product`, {
      method: "POST",
      body: { productId: created.id },
    });
  }

  if (fromSubmission && error) {
    return <div className="container py-4"><div className="alert alert-danger">{error}</div></div>;
  }
  if (fromSubmission && !initial) {
    return <div className="container py-4">Loading submission...</div>;
  }

  return (
    <>
      {fromSubmission && (
        <div className="container pt-4">
          <div className="alert alert-info py-2">
            Creating this product from vendor submission #{fromSubmission}. Fill in pricing, description, and
            images below, then save - the submission will be linked to the product automatically.
          </div>
        </div>
      )}
      <ProductForm initial={initial} onCreated={fromSubmission ? handleCreated : null} />
    </>
  );
}

export default function NewProductPage() {
  return (
    <Suspense fallback={<div className="container py-4">Loading...</div>}>
      <NewProductPageInner />
    </Suspense>
  );
}
