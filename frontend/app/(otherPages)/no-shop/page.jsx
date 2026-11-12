import Footer1 from "@/components/footers/Footer1";
import Header2 from "@/components/headers/Header2";
import Topbar1 from "@/components/headers/Topbar1";
import Link from "next/link";
import NoShopPageClient from "@/components/no-shop/NoShopPageClient";
import { loadFeaturedNoShopVideos, loadNoShopCategories, loadNoShopVideos } from "@/lib/api/noshop";

const TITLE = "No Shop || E.";
const DESCRIPTION =
  "No pressure to shop. Just a place to smile - funny videos, brand stories and a little sparkle from E.";

export const metadata = {
  title: TITLE,
  description: DESCRIPTION,
  alternates: { canonical: "/no-shop" },
  openGraph: {
    title: TITLE,
    description: DESCRIPTION,
    url: "/no-shop",
    type: "website",
  },
  twitter: {
    card: "summary_large_image",
    title: TITLE,
    description: DESCRIPTION,
  },
};

const PAGE_SIZE = 12;

export default async function NoShopPage() {
  const [featured, categories, videos] = await Promise.all([
    loadFeaturedNoShopVideos(1),
    loadNoShopCategories(),
    loadNoShopVideos({ pageSize: PAGE_SIZE }),
  ]);

  return (
    <>
      <Topbar1 parentClass="tf-topbar bg-dark-olive" />
      <Header2 parentClass="tf-header style-2" />
      <section className="flat-spacing-2 pb-0">
        <div className="container">
          <div className="page-title">
            <div className="breadcrumbs">
              <ul className="bread-wrap">
                <li>
                  <Link href={`/`} className="text-main-4 link-secondary">
                    Home
                  </Link>
                </li>
                <li className="br-line w-12 bg-main" />
                <li>No Shop</li>
              </ul>
              <h1 className="heading fw-normal text-uppercase">No Shop</h1>
            </div>
            <div className="box-text">
              <p className="text-main-4">
                Because life isn&apos;t always about shopping. No pressure to buy anything here -
                <br className="d-none d-xl-block" />
                just a place to smile, laugh, and discover the lighter side of E.
              </p>
            </div>
          </div>
        </div>
      </section>

      <NoShopPageClient
        featuredVideo={featured[0] ?? null}
        initialVideos={videos.items}
        initialTotalCount={videos.totalCount}
        categories={categories}
      />

      <Footer1 />
    </>
  );
}
