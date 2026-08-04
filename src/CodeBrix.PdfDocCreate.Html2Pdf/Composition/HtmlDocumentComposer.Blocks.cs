using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CodeBrix.MarkupParse.Dom;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;
using CodeBrix.PdfDocCreate.Html2Pdf.Css;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

internal sealed partial class HtmlDocumentComposer
{
    // ---- lists ------------------------------------------------------------------

    private const double ListMarkerWidthPoints = 14.0;
    private const double ListIndentStepPoints = 16.0;

    private void ComposeList(IElement listElement, ComputedStyle listStyle, BlockTarget target, BlockContext context, int depth)
    {
        var ordered = listElement.LocalName.Equals("ol", StringComparison.OrdinalIgnoreCase);
        var counter = 1;
        var startAttribute = listElement.GetAttribute("start");
        if (ordered && int.TryParse(startAttribute, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
        {
            counter = start;
        }

        var listContext = context.CreateNested(
            listStyle.MarginLeft + listStyle.PaddingLeft,
            listStyle.MarginRight + listStyle.PaddingRight);
        listContext.PendingBottomMargin = context.TakeSpaceBefore(listStyle.MarginTop);
        listContext.PendingPageBreak = context.TakePageBreak(listStyle.PageBreakBefore);

        foreach (var child in listElement.Children)
        {
            if (!child.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
            {
                if (child.LocalName is "ul" or "ol")
                {
                    // A nested list placed directly inside ul/ol (missing li wrapper).
                    var nestedStyle = _resolver.Compute(child, listStyle);
                    ComposeList(child, nestedStyle, target, listContext, depth + 1);
                }
                continue;
            }

            var itemStyle = _resolver.Compute(child, listStyle);
            if (itemStyle.DisplayNone) { continue; }

            var marker = BuildMarker(itemStyle.ListStyleType, ordered, depth, counter);
            if (ordered) { counter++; }

            ComposeListItem(child, itemStyle, target, listContext, depth, marker);
        }

        context.PendingBottomMargin = Math.Max(listContext.PendingBottomMargin, listStyle.MarginBottom);
        context.PendingPageBreak = listContext.PendingPageBreak || listStyle.PageBreakAfter;
    }

    private void ComposeListItem(IElement item, ComputedStyle itemStyle, BlockTarget target, BlockContext listContext, int depth, string marker)
    {
        var itemIndent = depth * ListIndentStepPoints + ListMarkerWidthPoints;
        var firstEmitted = false;
        var inlineBuffer = new List<INode>();

        void FlushItemBuffer()
        {
            if (inlineBuffer.Count == 0) { return; }
            var runs = _inline.ExtractNodes(inlineBuffer, itemStyle);
            inlineBuffer.Clear();
            if (runs.Count == 0) { return; }

            EmitListParagraph(runs, itemStyle, target, listContext, itemIndent,
                firstEmitted ? null : marker);
            firstEmitted = true;
        }

        foreach (var node in item.ChildNodes)
        {
            if (node is IText text)
            {
                if (text.Data.Trim().Length > 0 || inlineBuffer.Count > 0) { inlineBuffer.Add(node); }
                continue;
            }

            if (node is not IElement element) { continue; }
            if (IgnoredElements.Contains(element.LocalName)) { WarnIgnored(element.LocalName); continue; }

            if (InlineElements.Contains(element.LocalName))
            {
                inlineBuffer.Add(node);
                continue;
            }

            FlushItemBuffer();

            if (element.LocalName is "ul" or "ol")
            {
                var nestedStyle = _resolver.Compute(element, itemStyle);
                if (!firstEmitted)
                {
                    // A marker with no leading text still deserves its bullet line.
                    EmitListParagraph(new List<InlineRun>(), itemStyle, target, listContext, itemIndent, marker);
                    firstEmitted = true;
                }
                ComposeList(element, nestedStyle, target, listContext, depth + 1);
                continue;
            }

            // Any other block inside the item: indent it to the item's text column.
            var blockContext = listContext.CreateNested(itemIndent, 0);
            blockContext.PendingBottomMargin = listContext.PendingBottomMargin;
            blockContext.PendingPageBreak = listContext.PendingPageBreak;
            if (!firstEmitted)
            {
                EmitListParagraph(new List<InlineRun>(), itemStyle, target, listContext, itemIndent, marker);
                firstEmitted = true;
            }
            ComposeBlock(element, itemStyle, target, blockContext);
            listContext.PendingBottomMargin = blockContext.PendingBottomMargin;
            listContext.PendingPageBreak = blockContext.PendingPageBreak;
        }

        FlushItemBuffer();

        if (!firstEmitted)
        {
            EmitListParagraph(new List<InlineRun>(), itemStyle, target, listContext, itemIndent, marker);
        }

        listContext.PendingBottomMargin = Math.Max(listContext.PendingBottomMargin, itemStyle.MarginBottom);
    }

    private void EmitListParagraph(List<InlineRun> runs, ComputedStyle itemStyle, BlockTarget target, BlockContext listContext, double itemIndent, string marker)
    {
        var paragraph = target.AddParagraph();
        var format = paragraph.Format;

        var leftIndent = listContext.IndentLeft + itemIndent;
        format.LeftIndent = Unit.FromPoint(leftIndent);
        format.RightIndent = Unit.FromPoint(listContext.IndentRight);
        format.SpaceBefore = Unit.FromPoint(listContext.TakeSpaceBefore(itemStyle.MarginTop));
        format.SpaceAfter = 0;
        format.Alignment = MapAlignment(itemStyle.TextAlign);
        format.LineSpacingRule = LineSpacingRule.AtLeast;
        format.LineSpacing = Unit.FromPoint(itemStyle.ResolvedLineHeightPoints);
        format.WidowControl = true;
        format.Font.Name = _inline.ResolveFace(itemStyle);
        format.Font.Size = Unit.FromPoint(itemStyle.FontSizePoints);
        format.Font.Color = itemStyle.TextColor;

        if (listContext.TakePageBreak(false)) { format.PageBreakBefore = true; }

        if (marker != null)
        {
            format.FirstLineIndent = Unit.FromPoint(-ListMarkerWidthPoints);
            format.AddTabStop(Unit.FromPoint(leftIndent), TabAlignment.Left);

            var markerText = paragraph.AddFormattedText(marker);
            markerText.Font.Name = format.Font.Name;
            markerText.Font.Size = Unit.FromPoint(itemStyle.FontSizePoints);
            markerText.Font.Color = itemStyle.TextColor;
            paragraph.AddTab();
        }

        WriteRuns(paragraph, runs);
        listContext.PendingBottomMargin = itemStyle.MarginBottom;
    }

    private static string BuildMarker(string listStyleType, bool ordered, int depth, int counter)
    {
        var type = listStyleType;
        if (string.IsNullOrEmpty(type))
        {
            // Ordered lists stay decimal at every depth (the rendering readers
            // expect from Markdown); unordered markers vary by depth.
            type = ordered
                ? "decimal"
                : (depth % 3) switch { 0 => "disc", 1 => "circle", _ => "square" };
        }

        switch (type)
        {
            case "none": return "";
            case "disc": return "•";
            case "circle": return "◦";
            case "square": return "▪";
            case "decimal": return counter.ToString(CultureInfo.InvariantCulture) + ".";
            case "decimal-leading-zero":
                return counter.ToString("D2", CultureInfo.InvariantCulture) + ".";
            case "lower-alpha": case "lower-latin": return ToAlpha(counter, lower: true) + ".";
            case "upper-alpha": case "upper-latin": return ToAlpha(counter, lower: false) + ".";
            case "lower-roman": return ToRoman(counter).ToLowerInvariant() + ".";
            case "upper-roman": return ToRoman(counter) + ".";
            default:
                return ordered ? counter.ToString(CultureInfo.InvariantCulture) + "." : "•";
        }
    }

    private static string ToAlpha(int value, bool lower)
    {
        if (value < 1) { value = 1; }
        var result = "";
        while (value > 0)
        {
            value--;
            result = (char)((lower ? 'a' : 'A') + (value % 26)) + result;
            value /= 26;
        }
        return result;
    }

    private static string ToRoman(int value)
    {
        if (value < 1 || value > 3999) { return value.ToString(CultureInfo.InvariantCulture); }
        var numerals = new[]
        {
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
            (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I"),
        };
        var result = "";
        foreach (var (number, numeral) in numerals)
        {
            while (value >= number)
            {
                result += numeral;
                value -= number;
            }
        }
        return result;
    }

    // ---- definition lists -------------------------------------------------------

    private void ComposeDefinitionList(IElement element, ComputedStyle listStyle, BlockTarget target, BlockContext context)
    {
        var listContext = context.CreateNested(
            listStyle.MarginLeft + listStyle.PaddingLeft,
            listStyle.MarginRight + listStyle.PaddingRight);
        listContext.PendingBottomMargin = context.TakeSpaceBefore(listStyle.MarginTop);
        listContext.PendingPageBreak = context.TakePageBreak(listStyle.PageBreakBefore);

        foreach (var child in element.Children)
        {
            if (child.LocalName is not ("dt" or "dd")) { continue; }
            ComposeBlockLike(child, listStyle, target, listContext);
        }

        context.PendingBottomMargin = Math.Max(listContext.PendingBottomMargin, listStyle.MarginBottom);
        context.PendingPageBreak = listContext.PendingPageBreak || listStyle.PageBreakAfter;
    }

    /// <summary>Composes an element that behaves like a paragraph container (dt/dd/caption).</summary>
    private void ComposeBlockLike(IElement element, ComputedStyle parentStyle, BlockTarget target, BlockContext context)
    {
        var style = _resolver.Compute(element, parentStyle);
        if (style.DisplayNone) { return; }

        if (element.Children.Any(c => !InlineElements.Contains(c.LocalName) && !IgnoredElements.Contains(c.LocalName)))
        {
            ComposeContainer(element, style, target, context);
            return;
        }

        EmitParagraphElement(element, style, target, context);
    }

    // ---- pre blocks -------------------------------------------------------------

    private void EmitPre(IElement element, ComputedStyle style, BlockTarget target, BlockContext context)
    {
        var runs = _inline.Extract(element, style);

        // Split the run flow into physical lines at the preserved line breaks.
        var lines = new List<List<InlineRun>> { new List<InlineRun>() };
        foreach (var run in runs)
        {
            if (run.IsLineBreak) { lines.Add(new List<InlineRun>()); }
            else { lines[lines.Count - 1].Add(run); }
        }

        while (lines.Count > 0 && lines[^1].Count == 0) { lines.RemoveAt(lines.Count - 1); }
        while (lines.Count > 0 && lines[0].Count == 0) { lines.RemoveAt(0); }
        if (lines.Count == 0) { return; }

        if (!target.IsSection)
        {
            // Inside a table cell: emit the lines as plain code paragraphs.
            foreach (var line in lines)
            {
                EmitPreLine(line, style, target.AddParagraph());
            }
            context.PendingBottomMargin = style.MarginBottom;
            return;
        }

        // Vertical gap and page break precede the box (tables have no SpaceBefore).
        EmitSpacer(target, context.TakeSpaceBefore(style.MarginTop),
            context.TakePageBreak(style.PageBreakBefore));

        // One single-column row per line so long blocks break cleanly across pages
        // while the box (background + border) stays visually continuous.
        var table = target.AddTable();
        var width = Math.Max(40, _contentWidthPoints - context.IndentLeft - context.IndentRight
            - style.MarginLeft - style.MarginRight);
        table.AddColumn(Unit.FromPoint(width));
        table.Rows.LeftIndent = Unit.FromPoint(context.IndentLeft + style.MarginLeft);
        table.TopPadding = 0;
        table.BottomPadding = 0;
        table.LeftPadding = Unit.FromPoint(style.PaddingLeft);
        table.RightPadding = Unit.FromPoint(style.PaddingRight);

        for (var i = 0; i < lines.Count; i++)
        {
            var row = table.AddRow();
            if (!style.BackgroundColor.IsEmpty) { row.Shading.Color = style.BackgroundColor; }

            var cell = row.Cells[0];
            if (style.BorderLeft.IsVisible) { ApplyBorder(cell.Borders.Left, style.BorderLeft, style.TextColor); }
            if (style.BorderRight.IsVisible) { ApplyBorder(cell.Borders.Right, style.BorderRight, style.TextColor); }
            if (i == 0 && style.BorderTop.IsVisible) { ApplyBorder(cell.Borders.Top, style.BorderTop, style.TextColor); }
            if (i == lines.Count - 1 && style.BorderBottom.IsVisible) { ApplyBorder(cell.Borders.Bottom, style.BorderBottom, style.TextColor); }

            if (i == 0) { row.TopPadding = Unit.FromPoint(style.PaddingTop); }
            if (i == lines.Count - 1) { row.BottomPadding = Unit.FromPoint(style.PaddingBottom); }

            EmitPreLine(lines[i], style, cell.AddParagraph());
        }

        context.PendingBottomMargin = style.MarginBottom;
        context.PendingPageBreak = style.PageBreakAfter;
    }

    private void EmitPreLine(List<InlineRun> line, ComputedStyle style, Paragraph paragraph)
    {
        paragraph.Format.LineSpacingRule = LineSpacingRule.Exactly;
        paragraph.Format.LineSpacing = Unit.FromPoint(style.ResolvedLineHeightPoints);
        paragraph.Format.SpaceBefore = 0;
        paragraph.Format.SpaceAfter = 0;
        paragraph.Format.Font.Name = _inline.ResolveFace(style);
        paragraph.Format.Font.Size = Unit.FromPoint(style.FontSizePoints);
        paragraph.Format.Font.Color = style.TextColor;

        if (line.Count == 0)
        {
            paragraph.AddText(" ");
            return;
        }

        // CodeBrix.PdfDocCreate collapses a paragraph's leading spaces; convert the
        // first run's leading indentation to non-breaking spaces so code stays aligned.
        var first = line[0];
        if (first.Text.Length > 0 && first.Text[0] == ' ')
        {
            var indent = 0;
            while (indent < first.Text.Length && first.Text[indent] == ' ') { indent++; }
            first.Text = new string(' ', indent) + first.Text.Substring(indent);
        }

        WriteRuns(paragraph, line);
    }

    /// <summary>
    /// CodeBrix.PdfDocCreate tables have no vertical spacing (or page-break flag) of
    /// their own, so a tiny invisible paragraph added immediately BEFORE the table
    /// carries both.
    /// </summary>
    private void EmitSpacer(BlockTarget target, double points, bool pageBreak)
    {
        if (points <= 0 && !pageBreak) { return; }
        var spacer = target.AddParagraph();
        spacer.Format.Font.Size = 1;
        spacer.Format.LineSpacingRule = LineSpacingRule.Exactly;
        spacer.Format.LineSpacing = Unit.FromPoint(0.5);
        spacer.Format.SpaceBefore = 0;
        spacer.Format.SpaceAfter = Unit.FromPoint(Math.Max(0, points - 0.5));
        if (pageBreak) { spacer.Format.PageBreakBefore = true; }
    }

    // ---- boxed containers -------------------------------------------------------

    private void EmitBoxedContainer(IElement element, ComputedStyle style, BlockTarget target, BlockContext context)
    {
        EmitSpacer(target, context.TakeSpaceBefore(style.MarginTop),
            context.TakePageBreak(style.PageBreakBefore));

        var table = target.AddTable();
        var width = Math.Max(40, _contentWidthPoints - context.IndentLeft - context.IndentRight
            - style.MarginLeft - style.MarginRight);
        table.AddColumn(Unit.FromPoint(width));
        table.Rows.LeftIndent = Unit.FromPoint(context.IndentLeft + style.MarginLeft);
        table.LeftPadding = Unit.FromPoint(Math.Max(style.PaddingLeft, 2));
        table.RightPadding = Unit.FromPoint(Math.Max(style.PaddingRight, 2));
        table.TopPadding = 0;
        table.BottomPadding = 0;

        // Row per top-level child block so the box can break across pages.
        var blocks = SplitIntoRowBlocks(element);
        for (var i = 0; i < blocks.Count; i++)
        {
            var row = table.AddRow();
            if (!style.BackgroundColor.IsEmpty) { row.Shading.Color = style.BackgroundColor; }
            var cell = row.Cells[0];

            if (style.BorderLeft.IsVisible) { ApplyBorder(cell.Borders.Left, style.BorderLeft, style.TextColor); }
            if (style.BorderRight.IsVisible) { ApplyBorder(cell.Borders.Right, style.BorderRight, style.TextColor); }
            if (i == 0 && style.BorderTop.IsVisible) { ApplyBorder(cell.Borders.Top, style.BorderTop, style.TextColor); }
            if (i == blocks.Count - 1 && style.BorderBottom.IsVisible) { ApplyBorder(cell.Borders.Bottom, style.BorderBottom, style.TextColor); }
            if (i == 0) { row.TopPadding = Unit.FromPoint(Math.Max(style.PaddingTop, 2)); }
            if (i == blocks.Count - 1) { row.BottomPadding = Unit.FromPoint(Math.Max(style.PaddingBottom, 2)); }

            var cellTarget = new BlockTarget(cell);
            var cellContext = new BlockContext();

            foreach (var node in blocks[i])
            {
                if (node is IElement blockElement
                    && !InlineElements.Contains(blockElement.LocalName)
                    && node is not IText)
                {
                    ComposeBlock(blockElement, style, cellTarget, cellContext);
                }
                else
                {
                    var runs = _inline.ExtractNodes(new[] { node }, style);
                    if (runs.Count > 0)
                    {
                        EmitParagraph(runs, style.CreateChildBase(), cellTarget, cellContext, null, null);
                    }
                }
            }
        }

        context.PendingBottomMargin = style.MarginBottom;
        context.PendingPageBreak = style.PageBreakAfter;
    }

    /// <summary>
    /// Groups an element's child nodes into row-sized chunks: each block child is its
    /// own chunk, consecutive inline content forms one chunk.
    /// </summary>
    private List<List<INode>> SplitIntoRowBlocks(IElement element)
    {
        var blocks = new List<List<INode>>();
        List<INode> inlineChunk = null;

        foreach (var node in element.ChildNodes)
        {
            var isInline = node is IText
                || (node is IElement e && InlineElements.Contains(e.LocalName));

            if (node is IText t && t.Data.Trim().Length == 0 && inlineChunk == null) { continue; }
            if (node is IElement ig && IgnoredElements.Contains(ig.LocalName)) { continue; }

            if (isInline)
            {
                inlineChunk ??= new List<INode>();
                inlineChunk.Add(node);
            }
            else
            {
                if (inlineChunk != null) { blocks.Add(inlineChunk); inlineChunk = null; }
                blocks.Add(new List<INode> { node });
            }
        }

        if (inlineChunk != null) { blocks.Add(inlineChunk); }
        if (blocks.Count == 0) { blocks.Add(new List<INode>()); }
        return blocks;
    }
}
