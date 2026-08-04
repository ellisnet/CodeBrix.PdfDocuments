using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeBrix.MarkupParse.Dom;
using CodeBrix.PdfDocCreate.Html2Pdf.Css;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// Flattens an element's inline content - text nodes, styled inline elements, links,
/// line breaks, and inline images - into a list of <see cref="InlineRun"/>s with fully
/// resolved fonts and colors. Whitespace collapses per CSS rules unless the computed
/// white-space preserves it.
/// </summary>
internal sealed class InlineExtractor
{
    private static readonly HashSet<string> SkippedElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "link", "meta", "template", "noscript", "head", "title",
    };

    private readonly StyleResolver _resolver;
    private readonly RenderWarnings _warnings;
    private readonly ImageResolver _images;
    private readonly Dictionary<string, string> _faceCache = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly HashSet<string> _unresolvedFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private readonly double _contentWidthPoints;

    public InlineExtractor(
        StyleResolver resolver,
        RenderWarnings warnings,
        ImageResolver images,
        double contentWidthPoints)
    {
        _resolver = resolver;
        _warnings = warnings;
        _images = images;
        _contentWidthPoints = contentWidthPoints;
    }

    /// <summary>Extracts the runs of every inline descendant of a container element.</summary>
    public List<InlineRun> Extract(IElement container, ComputedStyle containerStyle)
    {
        var runs = new List<InlineRun>();
        var lastWasSpace = true;
        ExtractChildren(container, containerStyle, runs, ref lastWasSpace, href: "");
        TrimTrailingSpace(runs);
        return runs;
    }

    /// <summary>
    /// Extracts runs from a sequence of sibling nodes (the "anonymous box" case where
    /// text and inline elements sit directly next to block elements).
    /// </summary>
    public List<InlineRun> ExtractNodes(IEnumerable<INode> nodes, ComputedStyle parentStyle)
    {
        var runs = new List<InlineRun>();
        var lastWasSpace = true;
        foreach (var node in nodes)
        {
            ExtractNode(node, parentStyle, runs, ref lastWasSpace, href: "");
        }
        TrimTrailingSpace(runs);
        return runs;
    }

    /// <summary>Resolves the package font face for a computed style, with caching.</summary>
    public string ResolveFace(ComputedStyle style)
    {
        var key = string.Join("|", style.FontFamilies) + "|" + style.FontWeight + "|" + (style.Italic ? "i" : "n");
        if (_faceCache.TryGetValue(key, out var cached)) { return cached; }

        string face = null;
        foreach (var family in style.FontFamilies)
        {
            face = Html2PdfFonts.TryResolveFaceName(family, style.FontWeight, style.Italic);
            if (face != null) { break; }
        }

        if (face == null)
        {
            foreach (var family in style.FontFamilies)
            {
                if (_unresolvedFamilies.Add(family))
                {
                    _warnings.Add(RenderWarnings.CategoryFont,
                        $"Font family '{family}' does not match any package font; the default sans-serif family was used.");
                }
            }
            face = Html2PdfFonts.TryResolveFaceName("sans-serif", style.FontWeight, style.Italic);
        }

        _faceCache[key] = face;
        return face;
    }

    private void ExtractChildren(IElement element, ComputedStyle style, List<InlineRun> runs, ref bool lastWasSpace, string href)
    {
        foreach (var child in element.ChildNodes)
        {
            ExtractNode(child, style, runs, ref lastWasSpace, href);
        }
    }

    private void ExtractNode(INode node, ComputedStyle parentStyle, List<InlineRun> runs, ref bool lastWasSpace, string href)
    {
        if (node is IText textNode)
        {
            AppendText(textNode.Data, parentStyle, runs, ref lastWasSpace, href);
            return;
        }

        if (node is not IElement element) { return; }
        if (SkippedElements.Contains(element.LocalName)) { return; }

        var style = _resolver.Compute(element, parentStyle);
        if (style.DisplayNone) { return; }

        switch (element.LocalName.ToLowerInvariant())
        {
            case "br":
                TrimTrailingSpace(runs);
                runs.Add(new InlineRun { IsLineBreak = true });
                lastWasSpace = true;
                return;

            case "img":
                AppendImage(element, style, runs, href);
                lastWasSpace = false;
                return;

            case "a":
                var target = element.GetAttribute("href")?.Trim() ?? "";
                ExtractChildren(element, style, runs, ref lastWasSpace, target.Length > 0 ? target : href);
                return;

            case "sup":
                ExtractStyled(element, style, runs, ref lastWasSpace, href, superscript: true, subscript: false);
                return;

            case "sub":
                ExtractStyled(element, style, runs, ref lastWasSpace, href, superscript: false, subscript: true);
                return;

            default:
                var isSuper = style.VerticalAlign == "super";
                var isSub = style.VerticalAlign == "sub";
                if (isSuper || isSub)
                {
                    ExtractStyled(element, style, runs, ref lastWasSpace, href, isSuper, isSub);
                }
                else
                {
                    ExtractChildren(element, style, runs, ref lastWasSpace, href);
                }
                return;
        }
    }

    private void ExtractStyled(IElement element, ComputedStyle style, List<InlineRun> runs, ref bool lastWasSpace, string href, bool superscript, bool subscript)
    {
        var start = runs.Count;
        ExtractChildren(element, style, runs, ref lastWasSpace, href);
        for (var i = start; i < runs.Count; i++)
        {
            runs[i].Superscript = runs[i].Superscript || superscript;
            runs[i].Subscript = runs[i].Subscript || subscript;
        }
    }

    private void AppendText(string data, ComputedStyle style, List<InlineRun> runs, ref bool lastWasSpace, string href)
    {
        if (string.IsNullOrEmpty(data)) { return; }

        var preserve = style.WhiteSpace is "pre" or "pre-wrap";
        var text = ApplyTransform(GlyphSafety.Filter(data, _warnings), style.TextTransform);

        if (preserve)
        {
            var segments = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var i = 0; i < segments.Length; i++)
            {
                if (i > 0) { runs.Add(new InlineRun { IsLineBreak = true }); }
                if (segments[i].Length > 0)
                {
                    runs.Add(CreateTextRun(segments[i].Replace("\t", "    "), style, href));
                }
            }
            lastWasSpace = false;
            return;
        }

        var builder = new System.Text.StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is ' ' or '\t' or '\n' or '\r')
            {
                if (!lastWasSpace) { builder.Append(' '); }
                lastWasSpace = true;
            }
            else
            {
                builder.Append(c);
                lastWasSpace = false;
            }
        }

        if (builder.Length > 0)
        {
            runs.Add(CreateTextRun(builder.ToString(), style, href));
        }
    }

    private InlineRun CreateTextRun(string text, ComputedStyle style, string href) => new InlineRun
    {
        Text = text,
        FaceName = ResolveFace(style),
        SizePoints = style.FontSizePoints,
        TextColor = style.TextColor,
        Underline = style.Underline,
        Strikethrough = style.Strikethrough,
        Href = href,
    };

    private void AppendImage(IElement element, ComputedStyle style, List<InlineRun> runs, string href)
    {
        if (!_images.TryResolve(element.GetAttribute("src"), out var source, out var naturalW, out var naturalH))
        {
            var alt = element.GetAttribute("alt");
            if (!string.IsNullOrWhiteSpace(alt))
            {
                runs.Add(CreateTextRun("[" + GlyphSafety.Filter(alt, _warnings) + "]", style, href));
            }
            return;
        }

        double? width = style.WidthPoints;
        double? height = style.HeightPoints;

        if (width == null && style.WidthPercent != null)
        {
            width = _contentWidthPoints * style.WidthPercent.Value / 100.0;
        }

        if (width == null && TryParsePixelAttribute(element, "width", out var attrWidth)) { width = attrWidth; }
        if (height == null && TryParsePixelAttribute(element, "height", out var attrHeight)) { height = attrHeight; }

        // Fill in the missing dimension from the natural aspect ratio, then cap to the
        // content width preserving that ratio.
        if (width == null && height == null)
        {
            width = naturalW;
            height = naturalH;
        }
        else if (width == null)
        {
            width = naturalH > 0 ? height.Value * naturalW / naturalH : height;
        }
        else if (height == null)
        {
            height = naturalW > 0 ? width.Value * naturalH / naturalW : width;
        }

        if (width.Value > _contentWidthPoints && width.Value > 0)
        {
            var scale = _contentWidthPoints / width.Value;
            width = _contentWidthPoints;
            height = height.Value * scale;
        }

        runs.Add(new InlineRun
        {
            Image = source,
            ImageWidthPoints = width,
            ImageHeightPoints = height,
            Href = href,
        });
    }

    private static bool TryParsePixelAttribute(IElement element, string attribute, out double points)
    {
        points = 0;
        var raw = element.GetAttribute(attribute)?.Trim();
        if (string.IsNullOrEmpty(raw)) { return false; }
        raw = raw.TrimEnd('p', 'x', 'P', 'X');
        if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var px)
            || px <= 0)
        {
            return false;
        }
        points = px * 0.75;
        return true;
    }

    private static string ApplyTransform(string text, string transform) => transform switch
    {
        "uppercase" => text.ToUpperInvariant(),
        "lowercase" => text.ToLowerInvariant(),
        "capitalize" => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text),
        _ => text,
    };

    private static void TrimTrailingSpace(List<InlineRun> runs)
    {
        for (var i = runs.Count - 1; i >= 0; i--)
        {
            var run = runs[i];
            if (run.IsLineBreak || run.Image != null) { return; }
            run.Text = run.Text.TrimEnd(' ');
            if (run.Text.Length > 0) { return; }
            runs.RemoveAt(i);
        }
    }
}
