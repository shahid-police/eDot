"use client";

function formatVideoType(type) {
  return type.replace(/([a-z])([A-Z])/g, "$1 $2");
}

export default function VideoCard({ video, onPlay }) {
  return (
    <div
      className="article-blog hover-img no-shop-video-card"
      role="button"
      tabIndex={0}
      onClick={() => onPlay(video)}
      onKeyDown={(e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          onPlay(video);
        }
      }}
    >
      <div className="entry_image">
        <div className="image img-style">
          <img src={video.thumbnailUrl} alt={video.title} loading="lazy" />
        </div>
        <div className="entry_tag">
          <span className="name-tag text-caption">{video.categoryName}</span>
          {video.videoType !== "Entertainment" && (
            <span className="name-tag text-caption">{formatVideoType(video.videoType)}</span>
          )}
        </div>
        <span className="no-shop-play-icon" aria-hidden="true" />
      </div>
      <div className="blog-content">
        <h6 className="text-line-clamp-2 mb-1">{video.title}</h6>
        {video.description && <p className="text-main-6 text-line-clamp-2 small mb-0">{video.description}</p>}
      </div>
    </div>
  );
}
