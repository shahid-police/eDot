import Footer1 from "@/components/footers/Footer1";
import Header2 from "@/components/headers/Header2";
import Topbar1 from "@/components/headers/Topbar1";
import Link from "next/link";
import { notFound } from "next/navigation";
import NoShopVideoDetail from "@/components/no-shop/NoShopVideoDetail";
import { loadNoShopVideoBySlug, loadNoShopVideos } from "@/lib/api/noshop";
import { absoluteUrl } from "@/lib/siteUrl";

// Escapes "</script" so the JSON payload can't prematurely close the script tag it's embedded
// in - JSON.stringify alone doesn't escape "<", only quotes/backslashes/control characters.
function toJsonLd(value) {
  return JSON.stringify(value).replace(/</g, "\\u003c");
}

function videoJsonLd(video) {
  return {
    "@context": "https://schema.org",
    "@type": "VideoObject",
    name: video.title,
    description: video.description ?? video.seoDescription ?? video.title,
    thumbnailUrl: [video.thumbnailUrl],
    uploadDate: video.publishedAt ?? undefined,
    embedUrl: `https://www.youtube-nocookie.com/embed/${video.youTubeVideoId}`,
    url: absoluteUrl(`/no-shop/${video.slug}`),
  };
}

const DEFAULT_METADATA = {
  title: "No Shop || E.",
  description: "No pressure to shop. Just a place to smile.",
};

export async function generateMetadata({ params }) {
  const { slug } = await params;
  const video = await loadNoShopVideoBySlug(slug);
  if (!video) {
    return DEFAULT_METADATA;
  }

  const title = video.seoTitle ?? `${video.title} || No Shop || E.`;
  const description = video.seoDescription ?? video.description ?? DEFAULT_METADATA.description;
  const url = `/no-shop/${video.slug}`;

  return {
    title,
    description,
    alternates: { canonical: url },
    openGraph: {
      title,
      description,
      url,
      type: "video.other",
      images: [{ url: video.thumbnailUrl }],
    },
    twitter: {
      card: "summary_large_image",
      title,
      description,
      images: [video.thumbnailUrl],
    },
  };
}

export default async function NoShopVideoPage({ params }) {
  const { slug } = await params;
  const video = await loadNoShopVideoBySlug(slug);

  if (!video) {
    notFound();
  }

  const related = await loadNoShopVideos({ category: video.categorySlug, pageSize: 4 });
  const relatedVideos = related.items.filter((v) => v.slug !== video.slug).slice(0, 3);

  return (
    <>
      <script
        type="application/ld+json"
        // eslint-disable-next-line react/no-danger
        dangerouslySetInnerHTML={{ __html: toJsonLd(videoJsonLd(video)) }}
      />
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
                <li>
                  <Link href={`/no-shop`} className="text-main-4 link-secondary">
                    No Shop
                  </Link>
                </li>
                <li className="br-line w-12 bg-main" />
                <li>{video.categoryName}</li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <NoShopVideoDetail key={video.id} video={video} relatedVideos={relatedVideos} />

      <Footer1 />
    </>
  );
}
