"use client";

import { useEffect, useState } from "react";

// Not driven by Bootstrap's modal JS (no data-bs-toggle) - this is a small self-contained
// React-state modal so the No Shop page doesn't need another globally-mounted modal registered
// in the root layout. Reuses the theme's own modal/close-icon classes for visual consistency.
export default function VideoModal({ video, onClose }) {
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    if (!video) return;
    function handleKeyDown(e) {
      if (e.key === "Escape") onClose();
    }
    document.addEventListener("keydown", handleKeyDown);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = "";
    };
  }, [video, onClose]);

  useEffect(() => {
    setCopied(false);
  }, [video?.slug]);

  if (!video) return null;

  const shareUrl = typeof window !== "undefined" ? `${window.location.origin}/no-shop/${video.slug}` : `/no-shop/${video.slug}`;
  const youTubeUrl = `https://www.youtube.com/watch?v=${video.youTubeVideoId}`;

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(shareUrl);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  async function shareVideo() {
    if (navigator.share) {
      try {
        await navigator.share({ title: video.title, url: shareUrl });
        return;
      } catch {
        // User cancelled or share failed silently - fall through to copy-link.
      }
    }
    copyLink();
  }

  return (
    <>
      <div className="modal-backdrop fade show" onClick={onClose} />
      <div className="modal modalCentered fade show d-block" tabIndex={-1} role="dialog">
        <div className="modal-dialog modal-dialog-centered modal-lg">
          <div className="modal-content">
            <span className="icon-close icon-close-popup" onClick={onClose} role="button" aria-label="Close" />
            <div className="ratio ratio-16x9">
              <iframe
                src={`https://www.youtube-nocookie.com/embed/${video.youTubeVideoId}?autoplay=1`}
                title={video.title}
                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture"
                allowFullScreen
              />
            </div>
            <div className="p-3">
              <h5 className="mb-1">{video.title}</h5>
              {video.description && <p className="text-main-4 mb-2">{video.description}</p>}
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
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
