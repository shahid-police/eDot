namespace Elegance.Infrastructure.Search;

public class AiSearchOptions
{
    public const string SectionName = "AiSearch";

    // Lets the feature be switched off (e.g. no key configured yet) without erroring -
    // InterpretAsync degrades to a plain keyword search instead.
    public bool Enabled { get; set; } = true;
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-haiku-5-5";
    public string BaseUrl { get; set; } = "https://api.anthropic.com";
    public int TimeoutSeconds { get; set; } = 15;
}
