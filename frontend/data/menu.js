export const productDetailPages = [
  {
    heading: "PRODUCT LAYOUT",
    links: [
      { href: "/product-default/2", label: "Product Default" },
      { href2: "/product-default/2", label: "Product Style 1" },
      { href: "/product-style-02/2", label: "Product Style 2" },
      { href: "/product-style-03/2", label: "Product Style 3" },
      { href: "/product-style-04/2", label: "Product Style 4", badge: "Hot" },
      {
        href: "/product-style-05/2",
        label: "Product Style 5",
        badge: "Trending",
        badgeType: "type-trend",
      },
      { href: "/product-style-04-no-size/2", label: "Product No Size" },
    ],
  },
  {
    heading: "PRODUCT DESCRIPTION",
    links: [
      {
        href: "/product-description-side-dropdown/2",
        label: "Description Side Dropdown",
      },
      {
        href: "/product-description-full-accordion/2",
        label: "Description Full Accordions",
      },
      {
        href: "/product-description-side-drawer/2",
        label: "Description Side Drawer",
      },
    ],
  },
  {
    heading: "PRODUCT DETAILS",
    links: [
      { href: "/product-inner-zoom/2", label: "Product Inner Zoom" },
      {
        href: "/product-inner-circle-zoom/2",
        label: "Product Inner Circle Zoom",
      },
      { href: "/product-no-zoom/2", label: "Product No Zoom" },
      {
        href: "/product-video/2",
        label: "Product Video",
        badge: "Trending",
        badgeType: "type-trend",
      },
      { href: "/product-3d/2", label: "Product 3D/AR" },
    ],
  },
  {
    heading: "PRODUCT FEATURES",
    links: [
      { href: "/product-pickup-available/2", label: "Pickup Available" },
      { href: "/product-swatch-image/2", label: "Swatch Image" },
      {
        href: "/product-together/2",
        label: "Buy Together",
        badge: "New",
        badgeType: "type-new",
      },
      { href: "/product-size-guide/2", label: "Size Guide" },
      { href: "/product-engrave-option/2", label: "Engrave Option" },
    ],
  },
];

export const otherPages = [
  { href: "/about-us", label: "About Us" },
  { href: "/contact-us", label: "Contact Us" },
  { href: "/our-store", label: "Store Location" },
  { href: "/account-page", label: "My Account" },
  { href: "/faq", label: "FAQ" },
  { href: "/shop-cart", label: "Cart Page" },
  { href: "/before-you-leave", label: "Before You Leave" },
  { href: "/product-popup", label: "Products Popup" },
  { href: "/newspaper-popup", label: "Newspaper Popup" },
  { href: "/404", label: "404" },
  { href: "/coming-soon", label: "Coming Soon!" },
];

export const blogMenuLinks = [
  { href: "/blog-list-style-1", label: "Blog List Style 1" },
  { href: "/blog-list-style-2", label: "Blog List Style 2" },
  { href: "/blog-single/1", label: "Single Post" },
];

export const accountLinks = [
  { href: "/account-page", text: "Dashboard", isActive: true },
  { href: "/account-orders-empty", text: "My Orders", isActive: false },
  { href: "/wishlist", text: "My Wishlist", isActive: false },
  { href: "/account-addresses", text: "Addresses", isActive: false },
  { href: "/", text: "Log Out", isActive: false },
];
