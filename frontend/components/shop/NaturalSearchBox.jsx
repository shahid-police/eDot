"use client";
import { useState } from "react";
import { searchProductsNatural } from "@/lib/api/search";

const EXAMPLE_PROMPTS = [
  "Elegant gold-plated earrings under Rs. 3,000",
  "Bridal necklace set with kundan work",
  "Lightweight daily wear jewellery",
  "Party wear jewellery under Rs. 2,500",
];

const FILTER_LABELS = {
  categorySlug: "Category",
  minPrice: "Min price",
  maxPrice: "Max price",
  material: "Material",
  color: "Color",
  occasion: "Occasion",
  gender: "For",
};

function formatFilterValue(key, value) {
  if (key === "categorySlug") {
    return String(value).replace(/-/g, " ");
  }
  if (key === "minPrice" || key === "maxPrice") {
    return `Rs. ${Number(value).toLocaleString()}`;
  }
  return value;
}

export default function NaturalSearchBox({ onResults, onClear }) {
  const [query, setQuery] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [appliedFilters, setAppliedFilters] = useState(null);
  const [resultCount, setResultCount] = useState(null);

  async function runSearch(text) {
    const trimmed = text.trim();
    if (!trimmed) return;

    setLoading(true);
    setError(null);

    try {
      const result = await searchProductsNatural(trimmed);
      setAppliedFilters(result.appliedFilters);
      setResultCount(result.totalCount);
      onResults(result.products);
    } catch (err) {
      console.error("Natural language search failed:", err);
      setError(
        "Something went wrong understanding that search. Please try rephrasing, or browse the categories above."
      );
    } finally {
      setLoading(false);
    }
  }

  function handleSubmit(e) {
    e.preventDefault();
    runSearch(query);
  }

  function handleExampleClick(example) {
    setQuery(example);
    runSearch(example);
  }

  function handleClear() {
    setQuery("");
    setAppliedFilters(null);
    setResultCount(null);
    setError(null);
    onClear();
  }

  const activeFilterEntries = appliedFilters
    ? Object.entries(appliedFilters).filter(
        ([key, value]) =>
          key !== "keywords" && value !== null && value !== undefined && value !== ""
      )
    : [];

  return (
    <div className="ai-search-box">
      <form className="ai-search-form" onSubmit={handleSubmit}>
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Describe what you're looking for... e.g. &quot;elegant gold earrings for a wedding under Rs. 3,000&quot;"
          aria-label="Search products using natural language"
        />
        <button type="submit" disabled={loading || !query.trim()}>
          {loading ? "Searching..." : "Search"}
        </button>
        {(appliedFilters || query) && !loading && (
          <button type="button" className="ai-search-clear" onClick={handleClear}>
            Clear
          </button>
        )}
      </form>

      {!appliedFilters && !loading && (
        <div className="ai-search-examples">
          <span>Try:</span>
          {EXAMPLE_PROMPTS.map((example) => (
            <button key={example} type="button" onClick={() => handleExampleClick(example)}>
              {example}
            </button>
          ))}
        </div>
      )}

      {error && <p className="ai-search-error">{error}</p>}

      {resultCount !== null && !loading && (
        <div className="ai-search-applied">
          {activeFilterEntries.length > 0 && (
            <>
              <span>Understood as:</span>
              {activeFilterEntries.map(([key, value]) => (
                <span key={key} className="ai-search-chip">
                  {FILTER_LABELS[key]}: {formatFilterValue(key, value)}
                </span>
              ))}
            </>
          )}
          <span className="ai-search-count">
            {resultCount} {resultCount === 1 ? "product" : "products"} found
          </span>
        </div>
      )}
    </div>
  );
}
