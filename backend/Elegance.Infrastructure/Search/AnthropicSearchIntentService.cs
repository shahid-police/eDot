using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elegance.Application.Categories;
using Elegance.Application.Search;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elegance.Infrastructure.Search;

// Calls the Anthropic Messages API with a forced tool call so the model can only ever return
// data matching ProductSearchSpec's shape - never free-form text, never SQL. The result is still
// treated as untrusted input: every field is re-validated against real category/enum data before
// it is handed to ProductService. Any failure (disabled, no key, network, bad response) degrades
// to a plain keyword search rather than breaking the search request.
public class AnthropicSearchIntentService(
    HttpClient httpClient,
    IOptions<AiSearchOptions> options,
    ICategoryService categoryService,
    ILogger<AnthropicSearchIntentService> logger) : ISearchIntentService
{
    private static readonly string[] AllowedGenders = ["Women", "Men", "Unisex"];

    public async Task<ProductSearchSpec> InterpretAsync(string query, CancellationToken cancellationToken = default)
    {
        var aiOptions = options.Value;

        IReadOnlyList<CategoryDto> categories;
        try
        {
            categories = await categoryService.GetCategoriesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load categories for search intent, continuing without category matching");
            categories = [];
        }

        // Used both when the AI isn't configured and as the safety net if the AI call itself
        // fails below - still understands price phrasing and category names, just without the
        // semantic/synonym matching an LLM provides.
        var fallback = HeuristicSearchParser.Parse(query, categories.Select(c => (c.Slug, c.Name)).ToArray());

        if (!aiOptions.Enabled || string.IsNullOrWhiteSpace(aiOptions.ApiKey))
        {
            return fallback;
        }

        try
        {
            var categorySlugs = categories.Select(c => c.Slug).ToArray();

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v1/messages")
            {
                Content = new StringContent(
                    BuildRequestBody(query, categorySlugs, aiOptions.Model).ToJsonString(),
                    Encoding.UTF8,
                    "application/json"),
            };
            httpRequest.Headers.Add("x-api-key", aiOptions.ApiKey);
            httpRequest.Headers.Add("anthropic-version", "2023-06-01");

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(aiOptions.TimeoutSeconds));

            using var response = await httpClient.SendAsync(httpRequest, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("AI search intent call failed with status {Status}", response.StatusCode);
                return fallback;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeoutCts.Token);

            var toolUseBlock = document.RootElement
                .GetProperty("content")
                .EnumerateArray()
                .FirstOrDefault(block => block.TryGetProperty("type", out var type) && type.GetString() == "tool_use");

            if (toolUseBlock.ValueKind != JsonValueKind.Object || !toolUseBlock.TryGetProperty("input", out var input))
            {
                return fallback;
            }

            return ParseSpec(input, categorySlugs);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AI search intent interpretation failed, falling back to keyword search");
            return fallback;
        }
    }

    private static JsonObject BuildRequestBody(string query, IReadOnlyList<string> categorySlugs, string model)
    {
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                ["categorySlug"] = new JsonObject
                {
                    ["type"] = new JsonArray("string", "null"),
                    ["description"] = "Best-matching category slug from the allowed list, or null if none clearly applies.",
                },
                ["minPrice"] = new JsonObject { ["type"] = new JsonArray("number", "null") },
                ["maxPrice"] = new JsonObject { ["type"] = new JsonArray("number", "null") },
                ["material"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                ["color"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                ["occasion"] = new JsonObject { ["type"] = new JsonArray("string", "null") },
                ["gender"] = new JsonObject
                {
                    ["type"] = new JsonArray("string", "null"),
                    ["description"] = "One of: Women, Men, Unisex. Null if not implied.",
                },
                ["keywords"] = new JsonObject
                {
                    ["type"] = "array",
                    ["items"] = new JsonObject { ["type"] = "string" },
                    ["description"] = "Descriptive words that don't map to another field (e.g. elegant, lightweight, minimalist).",
                },
            },
            ["required"] = new JsonArray(),
        };

        var systemPrompt =
            "You extract structured product search filters from a customer's natural-language query for an online " +
            "Pakistani jewellery store. Prices are in PKR (Pakistani Rupees). " +
            $"Allowed category slugs: {string.Join(", ", categorySlugs)}. Only set categorySlug to one of these exact " +
            "values, or null if none clearly applies - never invent a slug. " +
            "Only extract a value the customer actually stated or clearly implied; never invent a price, material, " +
            "color, or occasion that wasn't mentioned.";

        return new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = 1024,
            ["system"] = systemPrompt,
            ["tools"] = new JsonArray(new JsonObject
            {
                ["name"] = "extract_search_filters",
                ["description"] = "Extract structured product search filters from the customer's query.",
                ["input_schema"] = schema,
            }),
            ["tool_choice"] = new JsonObject { ["type"] = "tool", ["name"] = "extract_search_filters" },
            ["messages"] = new JsonArray(new JsonObject { ["role"] = "user", ["content"] = query }),
        };
    }

    private static ProductSearchSpec ParseSpec(JsonElement input, IReadOnlyList<string> validSlugs)
    {
        var spec = new ProductSearchSpec();

        var slug = GetOptionalString(input, "categorySlug");
        if (slug is not null && validSlugs.Contains(slug, StringComparer.OrdinalIgnoreCase))
        {
            spec.CategorySlug = slug;
        }

        if (input.TryGetProperty("minPrice", out var minEl) && minEl.ValueKind == JsonValueKind.Number)
        {
            spec.MinPrice = minEl.GetDecimal();
        }

        if (input.TryGetProperty("maxPrice", out var maxEl) && maxEl.ValueKind == JsonValueKind.Number)
        {
            spec.MaxPrice = maxEl.GetDecimal();
        }

        spec.Material = GetOptionalString(input, "material");
        spec.Color = GetOptionalString(input, "color");
        spec.Occasion = GetOptionalString(input, "occasion");

        var gender = GetOptionalString(input, "gender");
        if (gender is not null && AllowedGenders.Contains(gender, StringComparer.OrdinalIgnoreCase))
        {
            spec.Gender = gender;
        }

        if (input.TryGetProperty("keywords", out var keywordsEl) && keywordsEl.ValueKind == JsonValueKind.Array)
        {
            spec.Keywords = keywordsEl.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString()!)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(10)
                .ToList();
        }

        return spec;
    }

    private static string? GetOptionalString(JsonElement input, string propertyName)
    {
        if (input.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
