"use client";
import { allProducts } from "@/data/products";
// import { openCartModal } from "@/utlis/openCartModal";
import { apiGet } from "@/lib/api/client";

import React, { useEffect } from "react";
import { useContext, useState } from "react";
const dataContext = React.createContext();
export const useContextElement = () => {
  return useContext(dataContext);
};

export default function Context({ children }) {
  const [cartProducts, setCartProducts] = useState([]);
  const [quickViewItem, setQuickViewItem] = useState(allProducts[0]);
  // The product (and optional variant) the "Notify me when available" trigger was clicked for -
  // null until a trigger sets it, so UnavailableModal knows what it's subscribing the customer to.
  const [notifyItem, setNotifyItem] = useState(null);
  // The store's own WhatsApp number (backend-configured, admin can change it without a frontend
  // deploy) - fetched once here rather than per product card, since every card in a grid needs it.
  const [storeWhatsAppNumber, setStoreWhatsAppNumber] = useState("");
  const [totalPrice, setTotalPrice] = useState(0);

  useEffect(() => {
    apiGet("/api/v1/store-info", { revalidate: 3600 })
      .then((data) => data?.whatsAppNumber && setStoreWhatsAppNumber(data.whatsAppNumber))
      .catch(() => {
        // No manual WhatsApp link without a number - the product-card action simply hides itself.
      });
  }, []);
  useEffect(() => {
    const subtotal = cartProducts.reduce((accumulator, product) => {
      return accumulator + product.quantity * product.price;
    }, 0);
    setTotalPrice(subtotal);
  }, [cartProducts]);

  const isAddedToCartProducts = (id) => {
    if (cartProducts.filter((elm) => elm.id == id)[0]) {
      return true;
    }
    return false;
  };
  const addProductToCart = (id, qty, isModal = true) => {
    if (!isAddedToCartProducts(id)) {
      const item = {
        ...allProducts.filter((elm) => elm.id == id)[0],
        quantity: qty ? qty : 1,
      };
      setCartProducts((pre) => [...pre, item]);
      if (isModal) {
        // openCartModal();
      }
    }
  };

  const updateQuantity = (id, qty) => {
    if (isAddedToCartProducts(id) && qty >= 1) {
      let item = cartProducts.filter((elm) => elm.id == id)[0];
      let items = [...cartProducts];
      const itemIndex = items.indexOf(item);

      item.quantity = qty / 1;
      items[itemIndex] = item;
      setCartProducts(items);
    }
  };

  useEffect(() => {
    localStorage.removeItem("cartList");
  }, []);

  const contextElement = {
    cartProducts,
    setCartProducts,
    totalPrice,
    addProductToCart,
    isAddedToCartProducts,
    quickViewItem,
    setQuickViewItem,
    notifyItem,
    setNotifyItem,
    storeWhatsAppNumber,
    updateQuantity,
  };
  return (
    <dataContext.Provider value={contextElement}>
      {children}
    </dataContext.Provider>
  );
}
