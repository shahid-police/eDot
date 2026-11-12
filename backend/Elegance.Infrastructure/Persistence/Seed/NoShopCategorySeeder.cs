using Elegance.Domain.Content;
using Microsoft.EntityFrameworkCore;

namespace Elegance.Infrastructure.Persistence.Seed;

/// <summary>
/// The baseline "No Shop" category taxonomy from the feature spec. Always runs in Development
/// (like DevelopmentSeeder's product categories) since these are the real categories this feature
/// is built around, not placeholder/demo data. Idempotent and upserts by slug: a category that
/// already exists (including one an admin has since edited) is left untouched by a re-run, so
/// this is also safe to run again after the table already has real admin-managed content.
/// </summary>
public static class NoShopCategorySeeder
{
    private sealed record CategorySpec(string Slug, string Name, string Description, int SortOrder);

    private static readonly CategorySpec[] CategorySpecs =
    [
        new("just-for-laughs", "Just for Laughs", "Funny, sarcastic and relatable moments about jewellery, shopping and everyday life.", 1),
        new("jewellery-fashion", "Jewellery & Fashion", "Styling tips, trends and fashion-forward jewellery content.", 2),
        new("shopping-drama", "Shopping Drama", "The highs, lows and hilarious chaos of a shopping trip.", 3),
        new("relationship-humour", "Relationship Humour", "Jewellery, gifting and relationships - the funny side.", 4),
        new("girls-talk", "Girls' Talk", "Relatable, girl-to-girl conversations and moments.", 5),
        new("brand-stories", "Brand Stories", "Behind the scenes at E. - craftsmanship, people and process.", 6),
        new("e-promotions", "E. Promotions", "New collection announcements, offers and seasonal campaigns.", 7),
        new("trending-videos", "Trending Videos", "Whatever's getting the most love from our audience right now.", 8),
    ];

    public static async Task SeedAsync(EleganceDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var existingSlugs = await dbContext.NoShopCategories
            .Select(c => c.Slug)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var missing = CategorySpecs.Where(spec => !existingSlugs.Contains(spec.Slug));

        foreach (var spec in missing)
        {
            dbContext.NoShopCategories.Add(new NoShopCategory
            {
                Name = spec.Name,
                Slug = spec.Slug,
                Description = spec.Description,
                SortOrder = spec.SortOrder,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
