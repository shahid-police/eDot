"use client";
import React from "react";
import Link from "next/link";
import { useServerCompare } from "@/lib/compare/useServerCompare";

const money = (value) => `$${value.toFixed(2)}`;

export default function Compare() {
  const { compare, items, error, remove, clear } = useServerCompare();

  if (compare === null) {
    return (
      <div className="flat-spacing-10">
        <div className="container">
          <p>Loading your comparison...</p>
        </div>
      </div>
    );
  }

  if (items.length === 0) {
    return (
      <div className="flat-spacing-10">
        <div className="container text-center">
          <p>You have no products to compare yet.</p>
          <Link href="/shop-default" className="tf-btn btn-fill animate-btn">
            Explore products
          </Link>
        </div>
      </div>
    );
  }

  const rows = [
    ["Price", (item) => (item.salePrice != null ? `${money(item.salePrice)} (was ${money(item.price)})` : money(item.price))],
    ["Availability", (item) => (item.inStock ? "In stock" : "Out of stock")],
    ["Category", (item) => item.categoryName],
    ["SKU", (item) => item.sku],
    ["Material", (item) => item.material ?? "-"],
    ["Colour", (item) => item.color ?? "-"],
  ];

  return (
    <div className="flat-spacing-10">
      <div className="container">
        {error && <p className="text-danger">{error}</p>}
        <div className="tf-table-compare">
          <table>
            <thead>
              <tr className="compare-row">
                <th className="compare-col" />
                {items.map((item) => (
                  <th key={item.id} className="compare-col compare-head">
                    <div className="compare-item">
                      <div className="item_image">
                        {item.imageUrl && (
                          <img src={item.imageUrl} alt={item.name} width={714} height={900} />
                        )}
                        <span className="remove icon-close" role="button" onClick={() => remove(item.id)} />
                      </div>
                      <Link href={`/product-default/${item.slug}`} className="link name-product">
                        {item.name}
                      </Link>
                    </div>
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map(([title, value]) => (
                <tr key={title} className="compare-row">
                  <td className="compare-col compare-title">{title}</td>
                  {items.map((item) => (
                    <td key={item.id} className="compare-col compare-value">
                      {value(item)}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="group-btn mt-3">
          <button type="button" className="tf-btn fw-medium" onClick={clear}>
            Clear all
          </button>
        </div>
      </div>
    </div>
  );
}
