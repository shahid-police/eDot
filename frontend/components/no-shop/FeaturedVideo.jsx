"use client";

export default function FeaturedVideo({ video, onPlay }) {
  if (!video) return null;

  return (
    <section className="flat-spacing-2 pt-0 pb-0">
      <div className="container">
        <div
          className="article-blog hover-img no-shop-featured"
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
              <img src={video.thumbnailUrl} alt={video.title} />
            </div>
            <div className="entry_tag">
              <span className="name-tag text-caption">{video.categoryName}</span>
              <span className="name-tag text-caption">Featured</span>
            </div>
            <span className="no-shop-play-icon no-shop-play-icon-lg" aria-hidden="true" />
          </div>
          <div className="blog-content">
            <h3 className="h4 mb-2">{video.title}</h3>
            {video.description && <p className="text-main-4 text-line-clamp-2 mb-0">{video.description}</p>}
          </div>
        </div>
      </div>
    </section>
  );
}
