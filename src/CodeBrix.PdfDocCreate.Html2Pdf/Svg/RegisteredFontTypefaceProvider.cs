using System;
using System.Collections.Concurrent;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.SkiaSvg.TypefaceProviders;
using SkiaSharp;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// Resolves SVG text typefaces from the registered document fonts - package fonts plus
/// anything added through Html2PdfFonts - using the same family matching and CSS-style
/// weight/style face selection the HTML text layer uses. Generic families (serif,
/// sans-serif and its SVG "sans" spelling, monospace) map to the default text
/// families; a family no registered font provides yields no typeface, so system fonts
/// are never consulted.
/// </summary>
internal sealed class RegisteredFontTypefaceProvider : ITypefaceProvider
{
    private static readonly ConcurrentDictionary<string, SKTypeface> TypefaceCache =
        new ConcurrentDictionary<string, SKTypeface>(StringComparer.Ordinal);

    /// <inheritdoc />
    public SKTypeface FromFamilyName(
        string fontFamily,
        SKFontStyleWeight fontWeight,
        SKFontStyleWidth fontWidth,
        SKFontStyleSlant fontSlant)
    {
        if (string.IsNullOrWhiteSpace(fontFamily)) { return null; }

        var italic = fontSlant != SKFontStyleSlant.Upright;
        var faceName = SvgFontResolution.TryResolveFaceName(fontFamily, (int)fontWeight, italic);
        if (faceName == null) { return null; }

        var filePath = Html2PdfFonts.TryGetFaceFilePath(faceName);
        if (filePath == null) { return null; }

        return TypefaceCache.GetOrAdd(filePath, static path => SKTypeface.FromFile(path));
    }
}
