using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.MarkupParse.Dom;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;
using CodeBrix.PdfDocCreate.Html2Pdf.Css;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// Walks the parsed HTML body and builds the PdfDocCreate document object model from it,
/// applying each element's computed style. The PdfDocCreate renderer then performs all
/// actual layout (line breaking, pagination, tables).
/// </summary>
internal sealed partial class HtmlDocumentComposer
{
    private static readonly HashSet<string> InlineElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a", "abbr", "b", "bdi", "bdo", "big", "br", "cite", "code", "data", "dfn", "em",
        "font", "i", "img", "ins", "kbd", "label", "mark", "q", "s", "samp", "small",
        "span", "strike", "strong", "sub", "sup", "svg", "time", "tt", "u", "var", "wbr",
    };

    private static readonly HashSet<string> IgnoredElements = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "script", "style", "link", "meta", "template", "noscript", "head", "title",
        "base", "iframe", "canvas", "audio", "video", "object", "embed", "map",
        "input", "button", "select", "textarea", "form", "colgroup", "col",
    };

    private readonly Section _section;
    private readonly StyleResolver _resolver;
    private readonly RenderWarnings _warnings;
    private readonly InlineExtractor _inline;
    private readonly ImageResolver _images;
    private readonly MeasureHelper _measure;
    private readonly double _contentWidthPoints;
    private readonly bool _generateOutline;
    private readonly HashSet<string> _bookmarks = new HashSet<string>(StringComparer.Ordinal);

    public HtmlDocumentComposer(
        Section section,
        StyleResolver resolver,
        RenderWarnings warnings,
        ImageResolver images,
        MeasureHelper measure,
        double contentWidthPoints,
        bool generateOutline,
        bool keepUncoveredCharacters = false)
    {
        _section = section;
        _resolver = resolver;
        _warnings = warnings;
        _images = images;
        _measure = measure;
        _contentWidthPoints = contentWidthPoints;
        _generateOutline = generateOutline;
        _inline = new InlineExtractor(resolver, warnings, images, contentWidthPoints, keepUncoveredCharacters);
    }

    /// <summary>Composes the body element's content into the section.</summary>
    public void ComposeBody(IElement body, ComputedStyle bodyStyle)
    {
        var context = new BlockContext();
        ComposeChildren(body, bodyStyle, new BlockTarget(_section), context);
    }

    /// <summary>Resolves the package font face for a computed style (for callers outside the walk).</summary>
    public string ResolveFaceFor(ComputedStyle style) => _inline.ResolveFace(style);

    // ---- targets and flow state ------------------------------------------------

    /// <summary>Where blocks are being emitted: a section or a table cell.</summary>
    private sealed class BlockTarget
    {
        private readonly Section _section;
        private readonly Cell _cell;

        public BlockTarget(Section section) { _section = section; }
        public BlockTarget(Cell cell) { _cell = cell; }

        public bool IsSection => _section != null;

        public Paragraph AddParagraph() => _section != null ? _section.AddParagraph() : _cell.AddParagraph();

        public Table AddTable() => _section.AddTable();
    }

    /// <summary>Per-container flow state: margin collapsing, page breaks, inherited indents.</summary>
    private sealed class BlockContext
    {
        public double PendingBottomMargin;
        public bool PendingPageBreak;
        public double IndentLeft;
        public double IndentRight;

        public BlockContext CreateNested(double extraLeft, double extraRight) => new BlockContext
        {
            PendingBottomMargin = PendingBottomMargin,
            PendingPageBreak = PendingPageBreak,
            IndentLeft = IndentLeft + extraLeft,
            IndentRight = IndentRight + extraRight,
        };

        /// <summary>CSS-style collapse: the gap before a block is max(prev bottom, own top).</summary>
        public double TakeSpaceBefore(double marginTop)
        {
            var space = Math.Max(0, marginTop - PendingBottomMargin) + PendingBottomMargin;
            PendingBottomMargin = 0;
            return space;
        }

        public bool TakePageBreak(bool ownBreak)
        {
            var result = PendingPageBreak || ownBreak;
            PendingPageBreak = false;
            return result;
        }
    }

    // ---- block dispatch ---------------------------------------------------------

    private void ComposeChildren(IElement container, ComputedStyle containerStyle, BlockTarget target, BlockContext context)
    {
        var inlineBuffer = new List<INode>();

        foreach (var node in container.ChildNodes)
        {
            if (node is IText text)
            {
                if (text.Data.Trim().Length > 0 || inlineBuffer.Count > 0) { inlineBuffer.Add(node); }
                continue;
            }

            if (node is not IElement element) { continue; }

            if (IgnoredElements.Contains(element.LocalName))
            {
                WarnIgnored(element.LocalName);
                continue;
            }

            if (InlineElements.Contains(element.LocalName))
            {
                inlineBuffer.Add(node);
                continue;
            }

            FlushInlineBuffer(inlineBuffer, containerStyle, target, context);
            ComposeBlock(element, containerStyle, target, context);
        }

        FlushInlineBuffer(inlineBuffer, containerStyle, target, context);
    }

    private void FlushInlineBuffer(List<INode> buffer, ComputedStyle containerStyle, BlockTarget target, BlockContext context)
    {
        if (buffer.Count == 0) { return; }

        var runs = _inline.ExtractNodes(buffer, containerStyle);
        buffer.Clear();
        if (runs.Count == 0) { return; }

        // Anonymous paragraph: text sitting directly in a container. It uses the
        // container's inherited text properties but no box properties.
        var anonymous = containerStyle.CreateChildBase();
        EmitParagraph(runs, anonymous, target, context, bookmarkId: null, outlineLevel: null,
            spaceAfterOverride: containerStyle.FontSizePoints * 0.55);
    }

    private void ComposeBlock(IElement element, ComputedStyle parentStyle, BlockTarget target, BlockContext context)
    {
        var style = _resolver.Compute(element, parentStyle);
        if (style.DisplayNone) { return; }

        var name = element.LocalName.ToLowerInvariant();
        switch (name)
        {
            case "h1": case "h2": case "h3": case "h4": case "h5": case "h6":
                EmitHeading(element, style, target, context, name[1] - '0');
                return;

            case "p":
            case "address":
            case "summary":
                EmitParagraphElement(element, style, target, context);
                return;

            case "hr":
                EmitRule(style, target, context);
                return;

            case "ul":
            case "ol":
                ComposeList(element, style, target, context, depth: 0);
                return;

            case "dl":
                ComposeDefinitionList(element, style, target, context);
                return;

            case "table":
                ComposeTable(element, style, target, context);
                return;

            case "pre":
                EmitPre(element, style, target, context);
                return;

            case "img":
            case "svg":
                EmitBlockImage(element, style, target, context);
                return;

            case "figure":
            case "blockquote":
            case "div":
            case "section":
            case "article":
            case "main":
            case "header":
            case "footer":
            case "aside":
            case "nav":
            case "details":
            case "figcaption":
            case "body":
                ComposeContainer(element, style, target, context);
                return;

            default:
                // Unknown elements compose as generic containers so their content survives.
                ComposeContainer(element, style, target, context);
                return;
        }
    }

    private void ComposeContainer(IElement element, ComputedStyle style, BlockTarget target, BlockContext context)
    {
        if (NeedsBox(style) && target.IsSection)
        {
            EmitBoxedContainer(element, style, target, context);
            return;
        }

        // A plain container contributes margins/padding as indents and vertical spacing,
        // then its children emit directly.
        var spaceBefore = context.TakeSpaceBefore(style.MarginTop + style.PaddingTop);
        var nested = context.CreateNested(
            style.MarginLeft + style.PaddingLeft,
            style.MarginRight + style.PaddingRight);
        nested.PendingBottomMargin = spaceBefore;
        nested.PendingPageBreak = context.TakePageBreak(style.PageBreakBefore);

        ComposeChildren(element, style, target, nested);

        context.PendingBottomMargin = Math.Max(nested.PendingBottomMargin, style.MarginBottom + style.PaddingBottom);
        context.PendingPageBreak = nested.PendingPageBreak || style.PageBreakAfter;
    }

    private static bool NeedsBox(ComputedStyle style) =>
        !style.BackgroundColor.IsEmpty
        || style.BorderTop.IsVisible
        || style.BorderRight.IsVisible
        || style.BorderBottom.IsVisible
        || style.BorderLeft.IsVisible;

    private void WarnIgnored(string elementName)
    {
        switch (elementName.ToLowerInvariant())
        {
            case "script": case "style": case "link": case "meta": case "head":
            case "title": case "base": case "template": case "noscript":
            case "colgroup": case "col":
                return; // structurally expected; not worth a warning
            default:
                _warnings.Add(RenderWarnings.CategoryHtml, $"The <{elementName}> element is not supported and was skipped.", "html.element.ignored");
                return;
        }
    }

    // ---- simple block emitters --------------------------------------------------

    private void EmitParagraphElement(IElement element, ComputedStyle style, BlockTarget target, BlockContext context)
    {
        var runs = _inline.Extract(element, style);
        if (runs.Count == 0 && style.BackgroundColor.IsEmpty)
        {
            context.PendingBottomMargin = Math.Max(context.PendingBottomMargin, style.MarginBottom);
            return;
        }

        EmitParagraph(runs, style, target, context, GetBookmarkId(element), outlineLevel: null);
    }

    private void EmitHeading(IElement element, ComputedStyle style, BlockTarget target, BlockContext context, int level)
    {
        var runs = _inline.Extract(element, style);
        if (runs.Count == 0) { return; }

        var paragraph = EmitParagraph(runs, style, target, context, GetBookmarkId(element),
            _generateOutline && level <= 6 ? level : (int?)null);
        paragraph.Format.KeepWithNext = true;
    }

    private void EmitRule(ComputedStyle style, BlockTarget target, BlockContext context)
    {
        var paragraph = target.AddParagraph();
        paragraph.Format.Font.Size = 1;
        paragraph.Format.LineSpacingRule = LineSpacingRule.Exactly;
        paragraph.Format.LineSpacing = Unit.FromPoint(1);
        paragraph.Format.SpaceBefore = Unit.FromPoint(context.TakeSpaceBefore(style.MarginTop));
        paragraph.Format.LeftIndent = Unit.FromPoint(context.IndentLeft + style.MarginLeft);
        paragraph.Format.RightIndent = Unit.FromPoint(context.IndentRight + style.MarginRight);
        if (context.TakePageBreak(style.PageBreakBefore)) { paragraph.Format.PageBreakBefore = true; }

        var edge = style.BorderTop.IsVisible ? style.BorderTop : new BorderEdge
        {
            WidthPoints = 0.75,
            LineStyle = "solid",
            LineColor = new Color(0xc9, 0xd1, 0xd9),
        };
        ApplyBorder(paragraph.Format.Borders.Top, edge, style.TextColor);

        context.PendingBottomMargin = style.MarginBottom;
        context.PendingPageBreak = style.PageBreakAfter;
    }

    private void EmitBlockImage(IElement element, ComputedStyle style, BlockTarget target, BlockContext context)
    {
        var runs = new List<InlineRun>();
        var single = new List<INode> { element };
        runs.AddRange(_inline.ExtractNodes(single, style));
        if (runs.Count == 0)
        {
            context.PendingBottomMargin = Math.Max(context.PendingBottomMargin, style.MarginBottom);
            return;
        }

        EmitParagraph(runs, style, target, context, GetBookmarkId(element), outlineLevel: null);
    }

    // ---- shared paragraph emission ----------------------------------------------

    private Paragraph EmitParagraph(
        List<InlineRun> runs,
        ComputedStyle style,
        BlockTarget target,
        BlockContext context,
        string bookmarkId,
        int? outlineLevel,
        double? spaceAfterOverride = null)
    {
        var paragraph = target.AddParagraph();
        var format = paragraph.Format;

        format.Alignment = MapAlignment(style.TextAlign);
        format.LeftIndent = Unit.FromPoint(context.IndentLeft + style.MarginLeft + style.PaddingLeft);
        format.RightIndent = Unit.FromPoint(context.IndentRight + style.MarginRight + style.PaddingRight);
        format.FirstLineIndent = Unit.FromPoint(style.TextIndentPoints);
        format.SpaceBefore = Unit.FromPoint(context.TakeSpaceBefore(style.MarginTop + style.PaddingTop));
        format.SpaceAfter = 0;
        format.WidowControl = true;

        var isPre = style.WhiteSpace is "pre" or "pre-wrap";
        format.LineSpacingRule = isPre ? LineSpacingRule.Exactly : LineSpacingRule.AtLeast;
        format.LineSpacing = Unit.FromPoint(style.ResolvedLineHeightPoints);

        format.Font.Name = _inline.ResolveFace(style);
        format.Font.Size = Unit.FromPoint(style.FontSizePoints);
        format.Font.Color = style.TextColor;

        if (!style.BackgroundColor.IsEmpty) { format.Shading.Color = style.BackgroundColor; }
        ApplyBorders(format.Borders, style);

        if (context.TakePageBreak(style.PageBreakBefore)) { format.PageBreakBefore = true; }

        if (outlineLevel.HasValue)
        {
            format.OutlineLevel = outlineLevel.Value switch
            {
                1 => OutlineLevel.Level1,
                2 => OutlineLevel.Level2,
                3 => OutlineLevel.Level3,
                4 => OutlineLevel.Level4,
                5 => OutlineLevel.Level5,
                _ => OutlineLevel.Level6,
            };
        }

        if (bookmarkId != null && _bookmarks.Add(bookmarkId))
        {
            paragraph.AddBookmark(bookmarkId);
        }

        WriteRuns(paragraph, runs);

        context.PendingBottomMargin = spaceAfterOverride ?? (style.MarginBottom + style.PaddingBottom);
        context.PendingPageBreak = style.PageBreakAfter;
        return paragraph;
    }

    private void WriteRuns(Paragraph paragraph, List<InlineRun> runs)
    {
        foreach (var run in runs)
        {
            if (run.IsLineBreak)
            {
                paragraph.AddLineBreak();
                continue;
            }

            if (run.Image != null)
            {
                var image = run.Href.Length > 0 && IsWebHref(run.Href)
                    ? paragraph.AddHyperlink(run.Href, HyperlinkType.Web).AddImage(run.Image)
                    : paragraph.AddImage(run.Image);
                if (run.ImageWidthPoints.HasValue) { image.Width = Unit.FromPoint(run.ImageWidthPoints.Value); }
                if (run.ImageHeightPoints.HasValue) { image.Height = Unit.FromPoint(run.ImageHeightPoints.Value); }
                image.LockAspectRatio = !(run.ImageWidthPoints.HasValue && run.ImageHeightPoints.HasValue);
                continue;
            }

            if (run.Text.Length == 0) { continue; }

            FormattedText formatted;
            if (run.Href.Length > 0)
            {
                if (run.Href.StartsWith("#", StringComparison.Ordinal) && run.Href.Length > 1)
                {
                    formatted = paragraph
                        .AddHyperlink(run.Href.Substring(1), HyperlinkType.Bookmark)
                        .AddFormattedText(run.Text);
                }
                else if (IsWebHref(run.Href))
                {
                    formatted = paragraph
                        .AddHyperlink(run.Href, HyperlinkType.Web)
                        .AddFormattedText(run.Text);
                }
                else
                {
                    formatted = paragraph.AddFormattedText(run.Text);
                }
            }
            else
            {
                formatted = paragraph.AddFormattedText(run.Text);
            }

            formatted.Font.Name = run.FaceName;
            formatted.Font.Size = Unit.FromPoint(run.SizePoints);
            formatted.Font.Color = run.TextColor;
            if (run.Underline) { formatted.Font.Underline = Underline.Single; }
            if (run.Strikethrough) { formatted.Font.Strikethrough = Strikethrough.Single; }
            if (run.Superscript) { formatted.Font.Superscript = true; }
            if (run.Subscript) { formatted.Font.Subscript = true; }
        }
    }

    private static bool IsWebHref(string href) =>
        href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || href.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);

    private static ParagraphAlignment MapAlignment(string textAlign) => textAlign switch
    {
        "right" => ParagraphAlignment.Right,
        "center" => ParagraphAlignment.Center,
        "justify" => ParagraphAlignment.Justify,
        _ => ParagraphAlignment.Left,
    };

    private static void ApplyBorders(Borders borders, ComputedStyle style)
    {
        if (style.BorderTop.IsVisible) { ApplyBorder(borders.Top, style.BorderTop, style.TextColor); }
        if (style.BorderRight.IsVisible) { ApplyBorder(borders.Right, style.BorderRight, style.TextColor); }
        if (style.BorderBottom.IsVisible) { ApplyBorder(borders.Bottom, style.BorderBottom, style.TextColor); }
        if (style.BorderLeft.IsVisible) { ApplyBorder(borders.Left, style.BorderLeft, style.TextColor); }

        if (style.BorderTop.IsVisible || style.BorderRight.IsVisible
            || style.BorderBottom.IsVisible || style.BorderLeft.IsVisible
            || !style.BackgroundColor.IsEmpty)
        {
            borders.DistanceFromTop = Unit.FromPoint(style.PaddingTop);
            borders.DistanceFromBottom = Unit.FromPoint(style.PaddingBottom);
            borders.DistanceFromLeft = Unit.FromPoint(style.PaddingLeft);
            borders.DistanceFromRight = Unit.FromPoint(style.PaddingRight);
        }
    }

    private static void ApplyBorder(Border border, BorderEdge edge, Color fallbackColor)
    {
        border.Width = Unit.FromPoint(edge.WidthPoints);
        border.Color = edge.LineColor.IsEmpty ? fallbackColor : edge.LineColor;
        border.Style = edge.LineStyle switch
        {
            "dashed" => BorderStyle.DashLargeGap,
            "dotted" => BorderStyle.Dot,
            _ => BorderStyle.Single,
        };
    }

    private string GetBookmarkId(IElement element)
    {
        var id = element.GetAttribute("id")?.Trim();
        return string.IsNullOrEmpty(id) ? null : id;
    }
}
