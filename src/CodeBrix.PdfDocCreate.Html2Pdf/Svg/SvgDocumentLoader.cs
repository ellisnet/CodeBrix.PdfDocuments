using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CodeBrix.Imaging.Drawing.NoSkia;
using CodeBrix.Imaging.Drawing.NoSkia.Svg;
using CodeBrix.SvgParse;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// An SVG document loaded for placement: its compiled display list and the size it is
/// placed at when nothing else decides.
/// </summary>
internal sealed class LoadedSvg
{
    internal LoadedSvg(DrawingSvg document, SvgFontMap fontMap, double naturalWidthPoints, double naturalHeightPoints)
    {
        Document = document;
        FontMap = fontMap;
        NaturalWidthPoints = naturalWidthPoints;
        NaturalHeightPoints = naturalHeightPoints;
    }

    /// <summary>The loaded document; its Picture is the display list.</summary>
    public DrawingSvg Document { get; }

    /// <summary>The faces the document's text resolved to, for embedding real text.</summary>
    public SvgFontMap FontMap { get; }

    /// <summary>The display list.</summary>
    public DrawingPicture Picture => Document.Picture;

    /// <summary>The natural placed width, in points.</summary>
    public double NaturalWidthPoints { get; }

    /// <summary>The natural placed height, in points.</summary>
    public double NaturalHeightPoints { get; }
}

/// <summary>
/// Sniffs, loads and sizes SVG content through the fully managed SVG engine
/// (CodeBrix.Imaging.Drawing.NoSkia): no GPU, window system, native library or system
/// font is involved, so behavior is identical on every operating system.
/// </summary>
internal static class SvgDocumentLoader
{
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
    /// Loads SVG markup: runs the per-glyph font fallback pre-pass, registers the fonts
    /// the text asks for, compiles the document, and reports the engine's own warnings
    /// under Html2Pdf codes. Returns null - after warning - when the content cannot be
    /// placed; never throws.
    /// </summary>
    public static LoadedSvg Load(byte[] svgBytes, string reference, RenderWarnings warnings)
    {
        // Per-glyph font fallback for SVG text: characters the styled face lacks are
        // wrapped in tspans naming the covering fallback family; what nothing covers
        // renders as missing-glyph shapes and warns. The same pass records every font
        // the text asks for, which is what the engine's registry is filled with.
        var fontRequests = new HashSet<SvgFontRequest>();
        svgBytes = SvgTextFallback.Process(svgBytes, reference, warnings, fontRequests);

        DrawingSvg svg;
        SvgFontMap fontMap;
        try
        {
            svg = new DrawingSvg();
            fontMap = SvgFontBridge.Register(svg.Fonts, fontRequests);
            // Plain runs, deliberately: the engine's per-code-point emission rounds its
            // positions to whole user units, which at an engraving's 2.5-unit font size puts
            // two letters on one spot. A run carries its anchor-resolved origin and the
            // page lays the glyphs out from the same font file, so nothing is lost.
            svg.TextEmission = DrawingSvgTextEmission.Runs;

            bool loaded;
            using (var stream = new MemoryStream(svgBytes, writable: false))
            {
                loaded = svg.Load(stream);
            }

            if (!loaded || svg.Picture == null)
            {
                warnings.Add(RenderWarnings.CategoryImage,
                    $"SVG image '{reference}' could not be parsed and was skipped.", "image.svg.failed");
                return null;
            }
        }
        catch (Exception ex)
        {
            warnings.Add(RenderWarnings.CategoryImage,
                $"SVG image '{reference}' could not be rendered and was skipped ({ex.GetType().Name}).", "image.svg.failed");
            return null;
        }

        var bounds = svg.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            warnings.Add(RenderWarnings.CategoryImage,
                $"SVG image '{reference}' has no positive-size drawing area and was skipped.", "image.svg.failed");
            return null;
        }

        ReportEngineWarnings(svg, reference, warnings);

        // Natural size: the declared physical width/height when the root carries absolute
        // units (an engraving declares millimetres), otherwise the engine's CSS-pixel
        // bounds at 0.75 pt per pixel. The declared size is exact, where the pixel bounds
        // are rounded to whole pixels.
        double naturalWidth, naturalHeight;
        if (TryToPoints(svg.DeclaredWidth, out naturalWidth) && TryToPoints(svg.DeclaredHeight, out naturalHeight)
            && naturalWidth > 0 && naturalHeight > 0)
        {
            return new LoadedSvg(svg, fontMap, naturalWidth, naturalHeight);
        }

        return new LoadedSvg(svg, fontMap, bounds.Width * 0.75, bounds.Height * 0.75);
    }

    /// <summary>
    /// Converts a declared root length to points when its unit is absolute; false for
    /// relative or missing units (em, ex, %, none).
    /// </summary>
    internal static bool TryToPoints(SvgUnit unit, out double points)
    {
        points = 0;
        switch (unit.Type)
        {
            case SvgUnitType.Millimeter: points = unit.Value * 72.0 / 25.4; return true;
            case SvgUnitType.Centimeter: points = unit.Value * 72.0 / 2.54; return true;
            case SvgUnitType.Inch: points = unit.Value * 72.0; return true;
            case SvgUnitType.Point: points = unit.Value; return true;
            case SvgUnitType.Pica: points = unit.Value * 12.0; return true;
            case SvgUnitType.Pixel:
            case SvgUnitType.User:
                points = unit.Value * 0.75; return true;
            default:
                return false;
        }
    }

    private static void ReportEngineWarnings(DrawingSvg svg, string reference, RenderWarnings warnings)
    {
        foreach (var warning in svg.Warnings)
        {
            var code = warning.Kind switch
            {
                DrawingSvgWarningKind.UnsupportedFilterPrimitive => "image.svg.filter-unsupported",
                DrawingSvgWarningKind.TurbulenceDropped => "image.svg.filter-unsupported",
                DrawingSvgWarningKind.GlyphIdTextRunUnsupported => "image.svg.text-unsupported",
                DrawingSvgWarningKind.TextOnPathUnsupported => "image.svg.text-unsupported",
                DrawingSvgWarningKind.NoFontsRegistered => "image.svg.fonts-missing",
                _ => "image.svg.degraded",
            };
            warnings.Add(RenderWarnings.CategoryImage, $"SVG image '{reference}': {warning.Message}", code);
        }
    }
}
