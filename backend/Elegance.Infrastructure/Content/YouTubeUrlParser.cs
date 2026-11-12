using System.Text.RegularExpressions;
using Elegance.Application.Common;

namespace Elegance.Infrastructure.Content;

// Extracts and validates a YouTube video ID from whatever an admin pastes in: a bare ID, a
// standard watch URL, a youtu.be short link, a Shorts URL, or an embed URL. Never trusts the
// client - the extracted ID is always re-validated against YouTube's own ID shape before use.
public static class YouTubeUrlParser
{
    private static readonly Regex IdPattern = new("^[a-zA-Z0-9_-]{11}$", RegexOptions.Compiled);

    public static string ExtractVideoId(string url)
    {
        var trimmed = url.Trim();

        if (IdPattern.IsMatch(trimmed))
        {
            return trimmed;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
        {
            var host = uri.Host.ToLowerInvariant();

            if (host.EndsWith("youtu.be", StringComparison.Ordinal))
            {
                var id = uri.AbsolutePath.Trim('/');
                if (IdPattern.IsMatch(id))
                {
                    return id;
                }
            }
            else if (host.EndsWith("youtube.com", StringComparison.Ordinal) || host.EndsWith("youtube-nocookie.com", StringComparison.Ordinal))
            {
                foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = pair.Split('=', 2);
                    if (parts.Length == 2 && parts[0] == "v" && IdPattern.IsMatch(parts[1]))
                    {
                        return parts[1];
                    }
                }

                var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (segments.Length >= 2 && segments[0] is "shorts" or "embed" or "live" && IdPattern.IsMatch(segments[1]))
                {
                    return segments[1];
                }
            }
        }

        throw new ApplicationFailure(FailureKind.Validation, "Enter a valid YouTube URL (watch, youtu.be, Shorts, or embed) or an 11-character video ID.");
    }
}
