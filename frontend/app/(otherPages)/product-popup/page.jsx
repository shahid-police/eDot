import Features from "@/components/common/Features";
import Footer2 from "@/components/footers/Footer2";
import Header2 from "@/components/headers/Header2";
import Banner from "@/components/homes/home-1/Banner";
import Collections from "@/components/homes/home-1/Collections";
import Hero from "@/components/homes/home-1/Hero";
import Products1 from "@/components/homes/home-1/Products1";
import Products2 from "@/components/homes/home-1/Products2";
import Products3 from "@/components/homes/home-1/Products3";
import Testimonials from "@/components/homes/home-1/Testimonials";
import TextBanner from "@/components/homes/home-1/TextBanner";
import TextSlider from "@/components/common/TextSlider";
import ProductsModal from "@/components/modals/ProductsModal";

export const metadata = {
  title: "Home || E.",
  description: "The Choice of Elegant Women, Every Day",
};
export default function Home() {
  return (
    <>
      <div className="bg-surface">
        <div id="wrapper">
          <Header2 parentClass="tf-header style-2 line-bt-2" />
          <Hero />
          <TextBanner />
          <Products1 />
          <Products2 />
          <Collections />
          <TextSlider />
          <Products3 />
          <Testimonials />
          <Banner />
          <Features />
          <Footer2 />
          <ProductsModal />
        </div>
      </div>
    </>
  );
}
