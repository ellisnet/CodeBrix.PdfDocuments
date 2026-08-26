using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// Gives SVG text the same per-glyph font fallback HTML text has, plus the warning
/// for what nothing covers. In one pass over the SVG markup: characters the resolved
/// face lacks but a fallback family (e.g. Noto Music) covers are wrapped in a tspan
/// naming that family, so the rasterizer draws them with it; characters no registered
/// font covers stay put - they render as missing-glyph shapes - and raise a
/// structured warning per code point with an occurrence count, so every coverage gap
/// is baselined instead of invisible. Font selection mirrors
/// <see cref="SvgFontResolution"/> exactly (attribute-based, with inheritance; the
/// supported SVG dialect has no CSS). The same walk records every font the text asks
/// for - family list, weight and style, fallback families included - so the SVG
/// engine's registry can be filled with exactly those faces.
/// </summary>
internal static class SvgTextFallback
{
    /// <summary>
    /// Processes the SVG markup, returning the (possibly rewritten) bytes to render.
    /// Never throws: markup this pass cannot parse is returned unchanged and
    /// contributes no warnings (the rasterizer deals with it on its own terms).
    /// </summary>
    public static byte[] Process(byte[] svgBytes, string reference, RenderWarnings warnings, ISet<SvgFontRequest> fontRequests)
    {
        XDocument document;
        try
        {
            using var stream = new MemoryStream(svgBytes, writable: false);
            document = XDocument.Load(stream, LoadOptions.PreserveWhitespace);
        }
        catch (Exception ex) when (ex is XmlException or IOException or ArgumentException)
        {
            return svgBytes;
        }

        if (document.Root == null) { return svgBytes; }

        var changed = ProcessElement(document.Root, "serif", 400, false, insideText: false, reference, warnings, fontRequests);
        if (!changed) { return svgBytes; }

        using var output = new MemoryStream();
        document.Save(output, SaveOptions.DisableFormatting);
        return output.ToArray();
    }

    private static bool ProcessElement(
        XElement element,
        string fontFamily,
        int weight,
        bool italic,
        bool insideText,
        string reference,
        RenderWarnings warnings,
        ISet<SvgFontRequest> fontRequests)
    {
        fontFamily = element.Attribute("font-family")?.Value ?? fontFamily;
        weight = ParseWeight(element.Attribute("font-weight")?.Value, weight);
        italic = ParseItalic(element.Attribute("font-style")?.Value, italic);
        insideText = insideText || element.Name.LocalName.Equals("text", StringComparison.OrdinalIgnoreCase);

        var changed = false;
        if (insideText)
        {
            var textNodes = new List<XText>();
            foreach (var node in element.Nodes())
            {
                if (node is XText textNode) { textNodes.Add(textNode); }
            }

            foreach (var textNode in textNodes)
            {
                changed |= ProcessTextNode(textNode, element.Name.Namespace, fontFamily, weight, italic, reference, warnings, fontRequests);
            }
        }

        foreach (var child in new List<XElement>(element.Elements()))
        {
            changed |= ProcessElement(child, fontFamily, weight, italic, insideText, reference, warnings, fontRequests);
        }

        return changed;
    }

    private static bool ProcessTextNode(
        XText textNode,
        XNamespace ns,
        string fontFamily,
        int weight,
        bool italic,
        string reference,
        RenderWarnings warnings,
        ISet<SvgFontRequest> fontRequests)
    {
        var text = textNode.Value;
        if (string.IsNullOrWhiteSpace(text)) { return false; }

        fontRequests?.Add(new SvgFontRequest(fontFamily, weight, italic));

        var primaryFace = SvgFontResolution.ResolveFaceNameOrDefault(fontFamily, weight, italic);
        var coverage = Html2PdfFonts.TryGetFaceCoverage(primaryFace);
        if (coverage == null) { return false; }

        // Segments alternate between the primary face (FamilyName null) and fallback
        // families; whitespace joins whichever segment is open.
        var segments = new List<(string FamilyName, string Text)>();
        var current = new System.Text.StringBuilder();
        string currentFamily = null;
        var anyFallback = false;

        void Flush()
        {
            if (current.Length > 0)
            {
                segments.Add((currentFamily, current.ToString()));
                current.Clear();
            }
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var codePoint = (int)c;
            var isPair = false;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(c, text[i + 1]);
                isPair = true;
            }

            string targetFamily;
            if (c <= ' ' || coverage.Covers(codePoint))
            {
                targetFamily = c <= ' ' ? currentFamily : null;
            }
            else
            {
                var (fallbackFamily, _) = Html2PdfFonts.TryResolveFallback(codePoint, weight, italic);
                targetFamily = fallbackFamily;
                if (fallbackFamily != null)
                {
                    anyFallback = true;
                    fontRequests?.Add(new SvgFontRequest(fallbackFamily, weight, italic));
                }
                else
                {
                    warnings.Add(RenderWarnings.CategoryFont,
                        $"SVG text in '{reference}' contains characters no registered font covers (first seen: U+{codePoint:X4}); they render as missing-glyph shapes.",
                        "font.svg-text.notdef", codePoint);
                }
            }

            if (!string.Equals(targetFamily, currentFamily, StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                currentFamily = targetFamily;
            }

            current.Append(c);
            if (isPair)
            {
                current.Append(text[i + 1]);
                i++;
            }
        }

        Flush();

        if (!anyFallback) { return false; }

        var replacement = new List<object>();
        foreach (var (familyName, segmentText) in segments)
        {
            if (familyName == null)
            {
                replacement.Add(new XText(segmentText));
            }
            else
            {
                replacement.Add(new XElement(ns + "tspan",
                    new XAttribute("font-family", familyName),
                    segmentText));
            }
        }

        textNode.ReplaceWith(replacement.ToArray());
        return true;
    }

    private static int ParseWeight(string value, int inherited)
    {
        if (string.IsNullOrWhiteSpace(value)) { return inherited; }

        return value.Trim().ToLowerInvariant() switch
        {
            "normal" => 400,
            "bold" => 700,
            "bolder" => Math.Min(inherited + 300, 900),
            "lighter" => Math.Max(inherited - 300, 100),
            var text => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric)
                ? Math.Clamp(numeric, 100, 900)
                : inherited,
        };
    }

    private static bool ParseItalic(string value, bool inherited)
    {
        if (string.IsNullOrWhiteSpace(value)) { return inherited; }

        return value.Trim().ToLowerInvariant() switch
        {
            "italic" or "oblique" => true,
            "normal" => false,
            _ => inherited,
        };
    }
}
