using System.Globalization;
using System.Text.RegularExpressions;
using Elegance.Application.Search;

namespace Elegance.Infrastructure.Search;

// A non-AI parser used whenever the LLM is unavailable or not configured, so the most common
// query shapes - a price ceiling/floor and a category name - still work correctly without an
// API key, instead of degrading all the way to an unconstrained substring keyword dump.
internal static class HeuristicSearchParser
{
    private static readonly Regex MaxPriceRegex = new(
        @"\b(?:under|below|less\s+than|up\s+to)\b\s*(?:rs\.?|pkr)?\s*([\d,]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex MinPriceRegex = new(
        @"\b(?:over|above|more\s+than)\b\s*(?:rs\.?|pkr)?\s*([\d,]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "for", "with", "and", "or", "of", "to", "in", "is", "are",
        "me", "show", "find", "looking", "want", "need", "rs", "pkr",
    };

    public static ProductSearchSpec Parse(string query, IReadOnlyList<(string Slug, string Name)> categories)
    {
        var spec = new ProductSearchSpec();
        var remaining = query;

        var maxMatch = MaxPriceRegex.Match(remaining);
        if (maxMatch.Success && TryParsePrice(maxMatch.Groups[1].Value, out var maxPrice))
        {
            spec.MaxPrice = maxPrice;
            remaining = MaxPriceRegex.Replace(remaining, " ");
        }

        var minMatch = MinPriceRegex.Match(remaining);
        if (minMatch.Success && TryParsePrice(minMatch.Groups[1].Value, out var minPrice))
        {
            spec.MinPrice = minPrice;
            remaining = MinPriceRegex.Replace(remaining, " ");
        }

        string? bestSlug = null;
        var bestWords = Array.Empty<string>();
        var bestScore = 0;

        foreach (var (slug, name) in categories)
        {
            var nameWords = name
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToArray();
            var matchedWords = nameWords
                .Where(w => Regex.IsMatch(remaining, $@"\b{Regex.Escape(w)}\b", RegexOptions.IgnoreCase))
                .ToArray();

            if (matchedWords.Length > bestScore)
            {
                bestScore = matchedWords.Length;
                bestSlug = slug;
                bestWords = matchedWords;
            }
        }

        // Require at least two shared words (e.g. "party" + "wear"), so a single generic word
        // like "jewellery" alone doesn't pin every search to whichever category happens to list
        // it first.
        if (bestScore >= 2)
        {
            spec.CategorySlug = bestSlug;
            foreach (var word in bestWords)
            {
                remaining = Regex.Replace(remaining, $@"\b{Regex.Escape(word)}\b", " ", RegexOptions.IgnoreCase);
            }
        }

        spec.Keywords = remaining
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .Take(10)
            .ToList();

        return spec;
    }

    private static bool TryParsePrice(string raw, out decimal price) =>
        decimal.TryParse(raw.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out price);
}
