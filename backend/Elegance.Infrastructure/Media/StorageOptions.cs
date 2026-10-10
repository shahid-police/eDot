namespace Elegance.Infrastructure.Media;

public class StorageOptions
{
    public const string SectionName = "Storage";

    // "Local" writes under the API's wwwroot/uploads and serves them from the API itself.
    // Use that only for development. "R2" uploads to a Cloudflare R2 bucket through the
    // S3-compatible API and is what production should run.
    public string Provider { get; set; } = "Local";

    // The origin files are served from: the API's own base URL for Local, or the bucket's
    // public/CDN domain for R2. Required either way.
    public string PublicBaseUrl { get; set; } = "";

    public R2Options R2 { get; set; } = new();
}

public class R2Options
{
    public string AccountId { get; set; } = "";
    public string AccessKey { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public string Bucket { get; set; } = "";
}
