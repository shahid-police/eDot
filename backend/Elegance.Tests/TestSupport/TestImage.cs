using SkiaSharp;

namespace Elegance.Tests.TestSupport;

public static class TestImage
{
    // A tiny, genuinely-valid PNG (passes signature check, SKCodec, and SKBitmap.Decode) so
    // upload-pipeline tests exercise the real validation path rather than a hand-faked byte array.
    public static byte[] ValidPngBytes(int width = 20, int height = 20)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        return encoded.ToArray();
    }
}
