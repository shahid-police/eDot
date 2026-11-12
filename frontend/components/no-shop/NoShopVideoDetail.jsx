"use client";

import Link from "next/link";
import { useState } from "react";

export default function NoShopVideoDetail({ video, relatedVideos }) {
  const [copied, setCopied] = useState(false);
  const [playing, setPlaying] = useState(false);
  const youTubeUrl = `https://www.youtube.com/watch?v=${video.youTubeVideoId}`;

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(window.location.href);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  async function shareVideo() {
    if (navigator.share) {
      try {
        await navigator.share({ title: video.title, url: window.location.href });
        return;
      } catch {
        // Cancelled or unsupported - fall back to copy-link.
      }
    }
    copyLink();
  }

  return (
    <section className="flat-spacing">
      <div className="container">
        <div className="row g-4">
          <div className="col-lg-8">
            <div className="ratio ratio-16x9 mb-3">
              {playing ? (
                <iframe
                  src={`https://www.youtube-nocookie.com/embed/${video.youTubeVideoId}?autoplay=1`}
                  title={video.title}
                  allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                  allowFullScreen
                />
              ) : (
                <button
                  type="button"
                  className="no-shop-detail-player-thumb"
                  onClick={() => setPlaying(true)}
                  aria-label={`Play ${video.title}`}
                >
                  <img src={video.thumbnailUrl} alt={video.title} />
                  <span className="no-shop-play-icon no-shop-play-icon-lg" aria-hidden="true" />
                </button>
              )}
            </div>
            <h1 className="h4 mb-2">{video.title}</h1>
            {video.description && <p className="text-main-4 mb-3">{video.description}</p>}
            <div className="d-flex flex-wrap gap-2">
              <button type="button" className="tf-btn-line" onClick={shareVideo}>
                Share
              </button>
              <button type="button" className="tf-btn-line" onClick={copyLink}>
                {copied ? "Link copied!" : "Copy link"}
              </button>
              <a href={youTubeUrl} target="_blank" rel="noreferrer" className="tf-btn-line">
                Watch on YouTube
              </a>
              <Link href="/no-shop" className="tf-btn-line">
                Back to No Shop
              </Link>
            </div>
          </div>

          {relatedVideos.length > 0 && (
            <div className="col-lg-4">
              <h2 className="h6 mb-3">More from {video.categoryName}</h2>
              <div className="vstack gap-3">
                {relatedVideos.map((v) => (
                  <Link key={v.id} href={`/no-shop/${v.slug}`} className="d-flex gap-2 link no-shop-related-item">
                    <img src={v.thumbnailUrl} alt={v.title} width={96} className="rounded" style={{ objectFit: "cover" }} />
                    <span className="small text-line-clamp-2">{v.title}</span>
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </section>
  );
}
