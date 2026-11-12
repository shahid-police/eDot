import LayoutWrapper from "@/components/common/LayoutWrapper";
import { SITE_URL } from "@/lib/siteUrl";

import "../public/scss/main.scss";
import "photoswipe/dist/photoswipe.css";
import ScrollTop from "@/components/common/ScrollTop";
import AuthBootstrap from "@/components/common/AuthBootstrap";
import Toolbar from "@/components/common/Toolbar";
import Context from "@/context/Context";
import Compare from "@/components/modals/Compare";
import Login from "@/components/modals/Login";
import MobileMenu from "@/components/modals/MobileMenu";
import QuickView from "@/components/modals/QuickView";
import Register from "@/components/modals/Register";
import ResetPass from "@/components/modals/ResetPass";
import Search from "@/components/modals/Search";
import ShoppingCart from "@/components/modals/ShoppingCart";
import SizeGuide from "@/components/modals/SizeGuide";
import AskQuestion from "@/components/modals/AskQuestion";
import Delivery from "@/components/modals/Delivery";
import Engrave from "@/components/modals/Engrave";
import Pickup from "@/components/modals/Pickup";
import ProductDes from "@/components/modals/ProductDes";
import UnavailableModal from "@/components/modals/UnavailableModal";

// Lets every page's `metadata`/`generateMetadata` use relative image/URL values (e.g. a video
// thumbnail path) and have Next.js resolve them to absolute URLs for Open Graph/Twitter tags and
// canonical links, instead of each page needing to build an absolute URL itself.
export const metadata = {
  metadataBase: new URL(SITE_URL),
};

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        <LayoutWrapper>
          <Context>
            <div id="wrapper">{children}</div>
            <Toolbar />

            <AuthBootstrap />
            <ScrollTop />
            <Compare />
            <Login />
            <MobileMenu />

            <QuickView />
            <Register />
            <ResetPass />
            <Search />
            <ShoppingCart />
            <SizeGuide />
            <AskQuestion />
            <Delivery />
            <Engrave />
            <Pickup />
            <ProductDes />
            <UnavailableModal />
          </Context>
        </LayoutWrapper>
      </body>
    </html>
  );
}
