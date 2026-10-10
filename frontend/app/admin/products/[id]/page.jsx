"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import ProductForm, { fromApi } from "@/components/admin/ProductForm";
import { adminRequest } from "@/lib/admin/api";

export default function EditProductPage() {
  const { id } = useParams();
  const [product, setProduct] = useState(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let active = true;
    adminRequest(`/products/${id}`)
      .then((data) => active && setProduct(data))
      .catch((err) => active && setError(err.message));
    return () => { active = false; };
  }, [id]);

  if (error) return <div className="alert alert-danger">{error}</div>;
  if (!product) return <p>Loading...</p>;

  return <ProductForm key={product.id} productId={product.id} initial={fromApi(product)} />;
}
