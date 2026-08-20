using System;
using System.IO;
using System.Text;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.SkiaSvg;
using SkiaSharp;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// Rasterizes SVG content to a transparent PNG for embedding. Rendering is a pure CPU
/// raster surface - no GPU, window system, or system font is involved - so behavior is
/// identical on Windows, macOS and Linux, headless or not.
/// </summary>
internal static class SvgImageRasterizer
{
    // Keeps a pathological viewBox from allocating an absurd raster.
    private const int MaxPixelsPerSide = 10000;

    // SkiaSvg documents its operations as not thread-safe; one SVG rasterizes at a time.
    private static readonly object RenderSync = new object();

    private static readonly RegisteredFontTypefaceProvider TypefaceProvider =
        new RegisteredFontTypefaceProvider();

    /// <summary>
    /// Sniffs whether the bytes are SVG markup: optionally a UTF-8 BOM, whitespace,
    /// comments, an XML declaration or doctype, then an svg root element. No raster
    /// format can match, because none starts with '&lt;'.
    /// </summary>
    public static bool LooksLikeSvg(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 5) { return false; }

        var probeLength = Math.Min(bytes.Length, 1024);
        var offset = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        string prefix;
        try
        {
            prefix = Encoding.UTF8.GetString(bytes, offset, probeLength - offset);
        }
        catch (ArgumentException)
        {
            return false;
        }

        var trimmed = prefix.TrimStart();
        if (trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase)) { return true; }
        if (trimmed.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("<!--", StringComparison.Ordinal)
            || trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.IndexOf("<svg", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        return false;
    }

    /// <summary>
    /// Renders the SVG to PNG bytes at <paramref name="scale"/> times its natural size,
    /// with a transparent background. The natural size (in points, 1 CSS px = 0.75 pt)
    /// comes from the SVG's own width/height/viewBox, so the raster scale never leaks
    /// into layout. Throws when the content is not renderable; the caller converts
    /// failures to warnings. A missing SkiaSharp native surfaces as
    /// <see cref="SkiaNativeLibraryMissingException"/> so the reason is legible from the
    /// message rather than buried in a type-initializer chain.
    /// </summary>
    public static byte[] RasterizeToPng(byte[] svgBytes, double scale, out double naturalWidthPoints, out double naturalHeightPoints)
    {
        try
        {
            return Rasterize(svgBytes, scale, out naturalWidthPoints, out naturalHeightPoints);
        }
        catch (Exception ex) when (SkiaNativeLibrary.IsMissingNativeLibrary(ex))
        {
            throw new SkiaNativeLibraryMissingException(ex);
        }
    }

    private static byte[] Rasterize(byte[] svgBytes, double scale, out double naturalWidthPoints, out double naturalHeightPoints)
    {
        lock (RenderSync)
        {
            using var stream = new MemoryStream(svgBytes, writable: false);
            using var svg = new SKSvg();
            ApplyTypefaceProviders(svg);
            svg.Load(stream);
            var picture = svg.Picture;
            if (picture == null)
            {
                throw new InvalidOperationException("The SVG content produced no drawable picture.");
            }

            var bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                throw new InvalidOperationException("The SVG content has no positive-size drawing area.");
            }

            naturalWidthPoints = bounds.Width * 0.75;
            naturalHeightPoints = bounds.Height * 0.75;

            var effectiveScale = Math.Min(scale, MaxPixelsPerSide / Math.Max(bounds.Width, bounds.Height));
            var pixelWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width * effectiveScale));
            var pixelHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height * effectiveScale));

            var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);
            canvas.Scale((float)effectiveScale);
            // CullRect does not necessarily start at the origin; translate so content
            // whose bounds are offset is not clipped.
            canvas.Translate(-bounds.Left, -bounds.Top);
            canvas.DrawPicture(picture);
            canvas.Flush();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }

    /// <summary>
    /// Replaces the SVG engine's typeface chain with the registered document fonts
    /// (package fonts plus anything added through Html2PdfFonts), so SVG text renders
    /// with exactly the fonts the rest of the document uses - never system fonts.
    /// Callers hold <see cref="RenderSync"/>.
    /// </summary>
    private static void ApplyTypefaceProviders(SKSvg svg)
    {
        Html2PdfFonts.EnsureRegistered();
        svg.Settings.TypefaceProviders.Clear();
        svg.Settings.TypefaceProviders.Add(TypefaceProvider);
    }
}
