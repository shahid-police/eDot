namespace Elegance.Infrastructure.Content;

// YouTube serves a standard thumbnail for every public video at this URL with no API key and no
// quota - used whenever an admin hasn't set a custom thumbnail override.
internal static class YouTubeThumbnails
{
    public static string DefaultUrl(string videoId) => $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
}
