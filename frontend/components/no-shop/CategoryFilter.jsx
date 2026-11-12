"use client";

export default function CategoryFilter({ categories, selected, onSelect }) {
  return (
    <div className="no-shop-category-filter d-flex flex-wrap gap-2 mb-4">
      <button
        type="button"
        className={`no-shop-filter-pill${selected === null ? " active" : ""}`}
        onClick={() => onSelect(null)}
      >
        All
      </button>
      {categories.map((c) => (
        <button
          key={c.slug}
          type="button"
          className={`no-shop-filter-pill${selected === c.slug ? " active" : ""}`}
          onClick={() => onSelect(c.slug)}
        >
          {c.name} ({c.videoCount})
        </button>
      ))}
    </div>
  );
}
