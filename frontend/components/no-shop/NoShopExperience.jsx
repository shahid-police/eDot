"use client";

import { useEffect, useRef, useState } from "react";
import { loadNoShopVideos } from "@/lib/api/noshop";
import CategoryFilter from "./CategoryFilter";
import VideoCard from "./VideoCard";

const PAGE_SIZE = 12;

export default function NoShopExperience({ initialVideos, initialTotalCount, categories, onPlay }) {
  const [category, setCategory] = useState(null);
  const [videos, setVideos] = useState(initialVideos);
  const [totalCount, setTotalCount] = useState(initialTotalCount);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(false);
  const skippedFirstRun = useRef(false);

  useEffect(() => {
    // The server already rendered page 1 / all categories - don't re-fetch the exact same thing
    // on mount, only once the admin actually changes a filter or page.
    if (!skippedFirstRun.current) {
      skippedFirstRun.current = true;
      return;
    }
    let active = true;
    setLoading(true);
    loadNoShopVideos({ category: category ?? undefined, page, pageSize: PAGE_SIZE }).then((result) => {
      if (!active) return;
      setVideos(result.items);
      setTotalCount(result.totalCount);
      setLoading(false);
    });
    return () => {
      active = false;
    };
  }, [category, page]);

  function selectCategory(slug) {
    setCategory(slug);
    setPage(1);
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <section className="flat-spacing">
      <div className="container">
        <CategoryFilter categories={categories} selected={category} onSelect={selectCategory} />

        {loading && <p className="text-muted">Loading...</p>}
        {!loading && videos.length === 0 && (
          <p className="text-muted">No videos here yet - check back soon.</p>
        )}

        <div className="tf-grid-layout sm-col-2 md-col-3">
          {videos.map((v) => (
            <VideoCard key={v.id} video={v} onPlay={onPlay} />
          ))}
        </div>

        {totalPages > 1 && (
          <div className="d-flex align-items-center gap-3 justify-content-center mt-4">
            <button className="tf-btn-line" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>
              Prev
            </button>
            <span className="small">
              Page {page} of {totalPages}
            </span>
            <button className="tf-btn-line" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>
              Next
            </button>
          </div>
        )}
      </div>
    </section>
  );
}
