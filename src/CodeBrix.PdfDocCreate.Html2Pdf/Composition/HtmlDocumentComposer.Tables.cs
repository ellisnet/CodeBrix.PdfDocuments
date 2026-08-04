using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.MarkupParse.Dom;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.DocumentObjectModel.Tables;
using CodeBrix.PdfDocCreate.Html2Pdf.Css;
using VerticalAlignment = CodeBrix.PdfDocCreate.DocumentObjectModel.Tables.VerticalAlignment;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

internal sealed partial class HtmlDocumentComposer
{
    private sealed class TableCellInfo
    {
        public IElement Element;
        public ComputedStyle Style;
        public int ColumnIndex;
        public int ColumnSpan = 1;
        public int RowSpan = 1;
        public bool IsHeader;
    }

    private sealed class TableRowInfo
    {
        public readonly List<TableCellInfo> Cells = new List<TableCellInfo>();
        public bool IsHeaderRow;
        public ComputedStyle RowStyle;
    }

    private void ComposeTable(IElement element, ComputedStyle style, BlockTarget target, BlockContext context)
    {
        if (!target.IsSection)
        {
            _warnings.Add(RenderWarnings.CategoryHtml, "A table nested inside another table (or box) is not supported and was skipped.");
            return;
        }

        var caption = element.Children.FirstOrDefault(c => c.LocalName.Equals("caption", StringComparison.OrdinalIgnoreCase));
        var rows = CollectRows(element, style);
        if (rows.Count == 0 || rows.All(r => r.Cells.Count == 0)) { return; }

        var columnCount = ResolveGridPositions(rows);
        if (columnCount == 0) { return; }

        var availableWidth = Math.Max(60, _contentWidthPoints - context.IndentLeft - context.IndentRight
            - style.MarginLeft - style.MarginRight);
        if (style.WidthPoints.HasValue) { availableWidth = Math.Min(availableWidth, style.WidthPoints.Value); }
        else if (style.WidthPercent.HasValue) { availableWidth = availableWidth * style.WidthPercent.Value / 100.0; }

        var columnWidths = ComputeColumnWidths(rows, columnCount, availableWidth);

        if (caption != null)
        {
            ComposeBlockLike(caption, style, target, context);
        }

        // Vertical gap before the table (tables have no SpaceBefore of their own).
        EmitSpacer(target, context.TakeSpaceBefore(style.MarginTop),
            context.TakePageBreak(style.PageBreakBefore));

        var table = target.AddTable();
        table.Borders.Visible = false;
        table.Rows.LeftIndent = Unit.FromPoint(context.IndentLeft + style.MarginLeft);

        // Cell padding comes from the first cell's computed style (per-cell padding is
        // approximated at table granularity - the common case is a uniform stylesheet).
        var referenceCell = rows.SelectMany(r => r.Cells).FirstOrDefault();
        if (referenceCell != null)
        {
            table.TopPadding = Unit.FromPoint(referenceCell.Style.PaddingTop);
            table.BottomPadding = Unit.FromPoint(referenceCell.Style.PaddingBottom);
            table.LeftPadding = Unit.FromPoint(referenceCell.Style.PaddingLeft);
            table.RightPadding = Unit.FromPoint(referenceCell.Style.PaddingRight);
        }

        foreach (var width in columnWidths)
        {
            table.AddColumn(Unit.FromPoint(width));
        }

        var occupancy = new int[rows.Count, columnCount];

        for (var r = 0; r < rows.Count; r++)
        {
            var rowInfo = rows[r];
            var row = table.AddRow();
            if (rowInfo.IsHeaderRow) { row.HeadingFormat = true; }

            var firstCell = rowInfo.Cells.FirstOrDefault();
            if (firstCell?.Style.VerticalAlign is string va)
            {
                row.VerticalAlignment = va switch
                {
                    "top" => VerticalAlignment.Top,
                    "bottom" => VerticalAlignment.Bottom,
                    "middle" or "center" => VerticalAlignment.Center,
                    _ => row.VerticalAlignment,
                };
            }

            foreach (var cellInfo in rowInfo.Cells)
            {
                var cell = row.Cells[cellInfo.ColumnIndex];
                if (cellInfo.ColumnSpan > 1) { cell.MergeRight = cellInfo.ColumnSpan - 1; }
                if (cellInfo.RowSpan > 1) { cell.MergeDown = cellInfo.RowSpan - 1; }

                if (!cellInfo.Style.BackgroundColor.IsEmpty)
                {
                    cell.Shading.Color = cellInfo.Style.BackgroundColor;
                }

                if (cellInfo.Style.BorderTop.IsVisible) { ApplyBorder(cell.Borders.Top, cellInfo.Style.BorderTop, cellInfo.Style.TextColor); }
                if (cellInfo.Style.BorderRight.IsVisible) { ApplyBorder(cell.Borders.Right, cellInfo.Style.BorderRight, cellInfo.Style.TextColor); }
                if (cellInfo.Style.BorderBottom.IsVisible) { ApplyBorder(cell.Borders.Bottom, cellInfo.Style.BorderBottom, cellInfo.Style.TextColor); }
                if (cellInfo.Style.BorderLeft.IsVisible) { ApplyBorder(cell.Borders.Left, cellInfo.Style.BorderLeft, cellInfo.Style.TextColor); }

                var cellTarget = new BlockTarget(cell);
                var cellContext = new BlockContext();
                ComposeChildren(cellInfo.Element, cellInfo.Style, cellTarget, cellContext);
            }
        }

        context.PendingBottomMargin = style.MarginBottom;
        context.PendingPageBreak = style.PageBreakAfter;
    }

    private List<TableRowInfo> CollectRows(IElement table, ComputedStyle tableStyle)
    {
        var rows = new List<TableRowInfo>();

        void AddRowsFrom(IElement parent, ComputedStyle parentStyle, bool headerSection)
        {
            foreach (var tr in parent.Children.Where(c => c.LocalName.Equals("tr", StringComparison.OrdinalIgnoreCase)))
            {
                var rowStyle = _resolver.Compute(tr, parentStyle);
                if (rowStyle.DisplayNone) { continue; }

                var rowInfo = new TableRowInfo { RowStyle = rowStyle, IsHeaderRow = headerSection };
                foreach (var cellElement in tr.Children.Where(c =>
                             c.LocalName.Equals("td", StringComparison.OrdinalIgnoreCase)
                             || c.LocalName.Equals("th", StringComparison.OrdinalIgnoreCase)))
                {
                    var cellStyle = _resolver.Compute(cellElement, rowStyle);
                    if (cellStyle.DisplayNone) { continue; }

                    rowInfo.Cells.Add(new TableCellInfo
                    {
                        Element = cellElement,
                        Style = cellStyle,
                        ColumnSpan = ParseSpan(cellElement, "colspan"),
                        RowSpan = ParseSpan(cellElement, "rowspan"),
                        IsHeader = cellElement.LocalName.Equals("th", StringComparison.OrdinalIgnoreCase),
                    });
                }

                if (rowInfo.Cells.Count > 0) { rows.Add(rowInfo); }
            }
        }

        foreach (var sectionName in new[] { "thead", "tbody", "tfoot" })
        {
            foreach (var section in table.Children.Where(c => c.LocalName.Equals(sectionName, StringComparison.OrdinalIgnoreCase)))
            {
                var sectionStyle = _resolver.Compute(section, tableStyle);
                AddRowsFrom(section, sectionStyle, sectionName == "thead");
            }

            if (sectionName == "thead")
            {
                // Direct tr children (no explicit sections) come after any thead rows.
                AddRowsFrom(table, tableStyle, headerSection: false);
            }
        }

        // A table without thead whose first row is entirely th cells is a header row.
        if (rows.Count > 0 && !rows[0].IsHeaderRow && rows[0].Cells.All(c => c.IsHeader))
        {
            rows[0].IsHeaderRow = true;
        }

        return rows;
    }

    private static int ParseSpan(IElement element, string attribute)
    {
        var raw = element.GetAttribute(attribute);
        return int.TryParse(raw, out var span) && span > 1 ? Math.Min(span, 50) : 1;
    }

    /// <summary>
    /// Assigns each cell its grid column index, honoring colspan and rowspan, and
    /// returns the table's column count.
    /// </summary>
    private static int ResolveGridPositions(List<TableRowInfo> rows)
    {
        // pendingRowSpans[column] = rows still covered by a rowspan from above.
        var pending = new Dictionary<int, int>();
        var columnCount = 0;

        foreach (var row in rows)
        {
            var column = 0;
            foreach (var cell in row.Cells)
            {
                while (pending.TryGetValue(column, out var remaining) && remaining > 0) { column++; }
                cell.ColumnIndex = column;
                column += cell.ColumnSpan;
            }

            columnCount = Math.Max(columnCount, column);
            foreach (var key in pending.Keys.ToList())
            {
                if (pending[key] > 0) { pending[key]--; }
                columnCount = Math.Max(columnCount, key + 1);
            }

            foreach (var cell in row.Cells.Where(c => c.RowSpan > 1))
            {
                for (var span = 0; span < cell.ColumnSpan; span++)
                {
                    pending[cell.ColumnIndex + span] = cell.RowSpan - 1;
                }
            }
        }

        // Clamp spans that run past the real grid.
        foreach (var row in rows)
        {
            foreach (var cell in row.Cells)
            {
                if (cell.ColumnIndex + cell.ColumnSpan > columnCount)
                {
                    cell.ColumnSpan = Math.Max(1, columnCount - cell.ColumnIndex);
                }
            }
        }

        return columnCount;
    }

    private double[] ComputeColumnWidths(List<TableRowInfo> rows, int columnCount, double availableWidth)
    {
        var maxNatural = new double[columnCount];
        var minNatural = new double[columnCount];
        var explicitWidth = new double?[columnCount];
        const double CellChrome = 12.0; // padding + border allowance

        foreach (var row in rows)
        {
            foreach (var cell in row.Cells)
            {
                var face = _inline.ResolveFace(cell.Style);
                var size = cell.Style.FontSizePoints;
                var text = cell.Element.TextContent ?? "";

                var longestLine = 0.0;
                var longestWord = 0.0;
                foreach (var line in text.Split('\n'))
                {
                    var collapsed = string.Join(" ", line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                    if (collapsed.Length == 0) { continue; }
                    longestLine = Math.Max(longestLine, _measure.MeasureWidth(collapsed, face, size));
                    foreach (var word in collapsed.Split(' '))
                    {
                        longestWord = Math.Max(longestWord, _measure.MeasureWidth(word, face, size));
                    }
                }

                // Very long content should wrap rather than claim the whole page.
                longestLine = Math.Min(longestLine, availableWidth * 0.75);

                var perColumnMax = (longestLine + CellChrome) / cell.ColumnSpan;
                var perColumnMin = (longestWord + CellChrome) / cell.ColumnSpan;

                for (var span = 0; span < cell.ColumnSpan && cell.ColumnIndex + span < columnCount; span++)
                {
                    var index = cell.ColumnIndex + span;
                    maxNatural[index] = Math.Max(maxNatural[index], perColumnMax);
                    minNatural[index] = Math.Max(minNatural[index], perColumnMin);
                }

                if (cell.ColumnSpan == 1 && cell.ColumnIndex < columnCount)
                {
                    if (cell.Style.WidthPoints.HasValue)
                    {
                        explicitWidth[cell.ColumnIndex] = cell.Style.WidthPoints.Value;
                    }
                    else if (cell.Style.WidthPercent.HasValue)
                    {
                        explicitWidth[cell.ColumnIndex] = availableWidth * cell.Style.WidthPercent.Value / 100.0;
                    }
                }
            }
        }

        var widths = new double[columnCount];
        for (var i = 0; i < columnCount; i++)
        {
            widths[i] = explicitWidth[i] ?? Math.Max(maxNatural[i], 24.0);
        }

        var total = widths.Sum();
        if (total > availableWidth)
        {
            // Shrink proportionally, but never below the minimum content width (which
            // itself shrinks proportionally as a last resort).
            var scale = availableWidth / total;
            for (var i = 0; i < columnCount; i++)
            {
                widths[i] = Math.Max(explicitWidth[i].HasValue ? widths[i] * scale : Math.Max(widths[i] * scale, Math.Min(minNatural[i], availableWidth / columnCount)), 20.0);
            }

            total = widths.Sum();
            if (total > availableWidth)
            {
                var finalScale = availableWidth / total;
                for (var i = 0; i < columnCount; i++) { widths[i] *= finalScale; }
            }
        }

        return widths;
    }
}
