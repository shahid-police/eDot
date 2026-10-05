"use client";
import React from "react";
import Link from "next/link";
import { useServerCompare } from "@/lib/compare/useServerCompare";
import { formatPrice } from "@/lib/format/price";

export default function Compare() {
  const { compare, items, remove, clear } = useServerCompare("compare");

  return (
    <div className="modal modalCentered fade modal-compare" id="compare">
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content">
          <span className="icon-close-popup" data-bs-dismiss="modal">
            <i className="icon-close" />
          </span>
          <div className="modal-heading">
            <h3 className="title fw-normal text-uppercase">compare products</h3>
          </div>
          <div className="modal-body main-list-clear">
            <div className="tf-compare-inner">
              <div className="tf-compare-list list-empty">
                {compare !== null && items.length === 0 && (
                  <p className="text-empty">Your compare is currently empty</p>
                )}
                {items.map((item) => (
                  <div key={item.id} className="tf-compare-item card_product--V01 file-delete">
                    <div className="card_product-wrapper aspect-ratio-1">
                      <span className="remove icon-close" role="button" onClick={() => remove(item.id)} />
                      <Link href={`/product-default/${item.slug}`} className="product-img">
                        {item.imageUrl && (
                          <img src={item.imageUrl} alt={item.name} className="lazyload img-product" width={714} height={900} />
                        )}
                      </Link>
                    </div>
                    <div className="card_product-info text-center">
                      <Link href={`/product-default/${item.slug}`} className="name-product link text-line-clamp-2">
                        {item.name}
                      </Link>
                      <div className="price-wrap justify-content-center">
                        <span className="price-new">{formatPrice((item.salePrice ?? item.price))}</span>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
            <div className="group-btn">
              <Link href="/compare" className="tf-btn btn-fill animate-btn fw-medium">
                COMPARE <span className="count-item-compare">({items.length})</span>
              </Link>
              <button type="button" onClick={clear} className="tf-btn fw-medium clear-list-empty">
                <span>CLEAR ALL</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
