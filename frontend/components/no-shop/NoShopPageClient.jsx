"use client";

import { useState } from "react";
import FeaturedVideo from "./FeaturedVideo";
import NoShopExperience from "./NoShopExperience";
import VideoModal from "./VideoModal";

// Owns the single shared video modal so clicking either the featured video or any grid card
// opens the same player instead of each section managing its own.
export default function NoShopPageClient({ featuredVideo, initialVideos, initialTotalCount, categories }) {
  const [activeVideo, setActiveVideo] = useState(null);

  return (
    <>
      <FeaturedVideo video={featuredVideo} onPlay={setActiveVideo} />
      <NoShopExperience
        initialVideos={initialVideos}
        initialTotalCount={initialTotalCount}
        categories={categories}
        onPlay={setActiveVideo}
      />
      <VideoModal video={activeVideo} onClose={() => setActiveVideo(null)} />
    </>
  );
}
