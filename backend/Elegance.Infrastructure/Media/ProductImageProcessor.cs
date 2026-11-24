using Elegance.Application.Common;
using SkiaSharp;

namespace Elegance.Infrastructure.Media;

public sealed record ProcessedImage(byte[] Bytes, int Width, int Height, string Extension);

// Shared hardened image validation/resize/re-encode pipeline, used by both AdminMediaService
// (publishing a product image) and the vendor submission upload path (private reference images).
// Keeping this in one place means every image that ever reaches disk/storage - public or
// vendor-original - has gone through the same signature check, decompression-bomb guard, and
// re-encode, rather than two copies that could silently drift apart.
public static class ProductImageProcessor
{
    public const long MaxUploadBytes = 8 * 1024 * 1024;
    public const int MaxInputEdge = 10000;
    public const int MaxStoredEdge = 2000;

    public static readonly IReadOnlyDictionary<string, string> AllowedTypes = new Dictionary<string, string>
    {
        ["image/jpeg"] = "jpg",
        ["image/png"] = "png",
        ["image/webp"] = "webp",
    };

    // The client's declared Content-Type is just a request header - trivially spoofable, and no
    // guarantee of what the bytes actually are. Checked against the real file signature before we
    // ever hand the bytes to the image codec.
    private static readonly Dictionary<string, Func<byte[], bool>> SignatureValidators = new()
    {
        ["image/jpeg"] = bytes => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        ["image/png"] = bytes => bytes.Length >= 8 &&
            bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
            bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A,
        ["image/webp"] = bytes => bytes.Length >= 12 &&
            bytes[0] == 'R' && bytes[1] == 'I' && bytes[2] == 'F' && bytes[3] == 'F' &&
            bytes[8] == 'W' && bytes[9] == 'E' && bytes[10] == 'B' && bytes[11] == 'P',
    };

    public static async Task<ProcessedImage> ReadAndProcessAsync(
        Stream content, string contentType, long contentLength, CancellationToken cancellationToken)
    {
        if (!AllowedTypes.TryGetValue(contentType, out var extension))
        {
            throw new ApplicationFailure(FailureKind.Validation, "Upload a JPEG, PNG, or WebP image.");
        }
        if (contentLength <= 0 || contentLength > MaxUploadBytes)
        {
            throw new ApplicationFailure(FailureKind.Validation, $"Images must be {MaxUploadBytes / (1024 * 1024)} MB or smaller.");
        }

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var original = buffer.ToArray();

        if (!SignatureValidators[contentType](original))
        {
            throw new ApplicationFailure(FailureKind.Validation, "That file is not a valid image.");
        }

        // SKCodec only parses the header, so this reports the real pixel dimensions without
        // decoding a single pixel - checked before SKBitmap.Decode below allocates the full pixel
        // buffer, so a small-but-highly-compressed file can't be used to force a huge allocation
        // (a decompression-bomb DoS) before we ever get a chance to reject it.
        using (var codec = SKCodec.Create(new SKMemoryStream(original)))
        {
            if (codec is null)
            {
                throw new ApplicationFailure(FailureKind.Validation, "That file is not a valid image.");
            }
            if (codec.Info.Width > MaxInputEdge || codec.Info.Height > MaxInputEdge)
            {
                throw new ApplicationFailure(FailureKind.Validation, $"Images must be {MaxInputEdge}×{MaxInputEdge} pixels or smaller. Resize it and try again.");
            }
        }

        // SKBitmap.Decode doesn't just return null on unparseable bytes - it can throw
        // (e.g. ArgumentNullException when it can't construct a codec), so both are "not an image".
        SKBitmap? decoded;
        try
        {
            decoded = SKBitmap.Decode(original);
        }
        catch
        {
            decoded = null;
        }
        using var bitmap = decoded ?? throw new ApplicationFailure(FailureKind.Validation, "That file is not a valid image.");

        var longEdge = Math.Max(bitmap.Width, bitmap.Height);
        SKBitmap toEncode = bitmap;
        SKBitmap? resized = null;
        if (longEdge > MaxStoredEdge)
        {
            var scale = (float)MaxStoredEdge / longEdge;
            var newWidth = Math.Max(1, (int)Math.Round(bitmap.Width * scale));
            var newHeight = Math.Max(1, (int)Math.Round(bitmap.Height * scale));
            resized = bitmap.Resize(new SKImageInfo(newWidth, newHeight), SKSamplingOptions.Default);
            if (resized is not null)
            {
                toEncode = resized;
            }
        }

        var format = contentType switch
        {
            "image/jpeg" => SKEncodedImageFormat.Jpeg,
            "image/png" => SKEncodedImageFormat.Png,
            "image/webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Jpeg,
        };

        using var image = SKImage.FromBitmap(toEncode);
        using var encoded = image.Encode(format, 85);
        var bytes = encoded.ToArray();
        var result = new ProcessedImage(bytes, toEncode.Width, toEncode.Height, extension);
        resized?.Dispose();
        return result;
    }
}
