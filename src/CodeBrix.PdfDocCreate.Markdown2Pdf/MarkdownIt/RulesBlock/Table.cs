// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_block/table.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

/// <summary>GFM table (https://github.github.com/gfm/#tables-extension-).</summary>
internal static class Table
{
    // Limits the amount of empty autocompleted cells in a table
    // (markdown-it issue #1000); 65k can expand user input by a factor of ~x370.
    private const int MaxAutocompletedCells = 0x10000;

    private static readonly Regex AlignRe = new Regex("^:?-+:?$", RegexOptions.Compiled);

    private static string GetLine(StateBlock state, int line)
    {
        var pos = state.BMarks[line] + state.TShift[line];
        var max = state.EMarks[line];
        return state.Src.Substring(pos, max - pos);
    }

    private static List<string> EscapedSplit(string str)
    {
        var result = new List<string>();
        var max = str.Length;

        var pos = 0;
        var ch = MdUtils.CharCode(str, pos);
        var isEscaped = false;
        var lastPos = 0;
        var current = "";

        while (pos < max)
        {
            if (ch == 0x7C /* | */)
            {
                if (!isEscaped)
                {
                    // pipe separating cells, '|'
                    result.Add(current + str.Substring(lastPos, pos - lastPos));
                    current = "";
                    lastPos = pos + 1;
                }
                else
                {
                    // escaped pipe, '\|'
                    current += str.Substring(lastPos, pos - 1 - lastPos);
                    lastPos = pos;
                }
            }

            isEscaped = ch == 0x5C /* \ */;
            pos++;

            ch = MdUtils.CharCode(str, pos);
        }

        result.Add(current + str.Substring(lastPos));

        return result;
    }

    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        // should have at least two lines
        if (startLine + 2 > endLine) { return false; }

        var nextLine = startLine + 1;

        if (state.SCount[nextLine] < state.BlkIndent) { return false; }

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[nextLine] - state.BlkIndent >= 4) { return false; }

        // first character of the second line should be '|', '-', ':', and no other
        // characters are allowed but spaces (equivalent of /^[-:|][-:|\s]*$/)
        var pos = state.BMarks[nextLine] + state.TShift[nextLine];
        if (pos >= state.EMarks[nextLine]) { return false; }

        var firstCh = MdUtils.CharCode(state.Src, pos++);
        if (firstCh != 0x7C /* | */ && firstCh != 0x2D /* - */ && firstCh != 0x3A /* : */) { return false; }

        if (pos >= state.EMarks[nextLine]) { return false; }

        var secondCh = MdUtils.CharCode(state.Src, pos++);
        if (secondCh != 0x7C /* | */ && secondCh != 0x2D /* - */ && secondCh != 0x3A /* : */
            && !MdUtils.IsSpace(secondCh))
        {
            return false;
        }

        // if the first character is '-', the second must not be a space
        // (parsing ambiguity with list)
        if (firstCh == 0x2D /* - */ && MdUtils.IsSpace(secondCh)) { return false; }

        while (pos < state.EMarks[nextLine])
        {
            var ch = MdUtils.CharCode(state.Src, pos);
            if (ch != 0x7C /* | */ && ch != 0x2D /* - */ && ch != 0x3A /* : */ && !MdUtils.IsSpace(ch))
            {
                return false;
            }
            pos++;
        }

        var lineText = GetLine(state, startLine + 1);
        var columnsRaw = lineText.Split('|');
        var aligns = new List<string>();
        for (var i = 0; i < columnsRaw.Length; i++)
        {
            var t = columnsRaw[i].Trim();
            if (t.Length == 0)
            {
                // allow empty columns before and after table, but not in between
                if (i == 0 || i == columnsRaw.Length - 1) { continue; }
                return false;
            }

            if (!AlignRe.IsMatch(t)) { return false; }
            if (t[t.Length - 1] == ':')
            {
                aligns.Add(t[0] == ':' ? "center" : "right");
            }
            else if (t[0] == ':')
            {
                aligns.Add("left");
            }
            else
            {
                aligns.Add("");
            }
        }

        lineText = GetLine(state, startLine).Trim();
        if (lineText.IndexOf('|') == -1) { return false; }
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }
        var columns = EscapedSplit(lineText);
        if (columns.Count > 0 && columns[0].Length == 0) { columns.RemoveAt(0); }
        if (columns.Count > 0 && columns[columns.Count - 1].Length == 0) { columns.RemoveAt(columns.Count - 1); }

        // the header row defines the column count of the entire table, and the align
        // row must match it exactly (other rows can differ)
        var columnCount = columns.Count;
        if (columnCount == 0 || columnCount != aligns.Count) { return false; }

        if (silent) { return true; }

        var oldParentType = state.ParentType;
        state.ParentType = "table";

        // use the 'blockquote' terminator list because it's the most similar to tables
        var terminatorRules = state.Md.Block.Ruler.GetRules("blockquote");

        var tokenTo = state.Push("table_open", "table", 1);
        var tableLines = new[] { startLine, 0 };
        tokenTo.Map = tableLines;

        var tokenTho = state.Push("thead_open", "thead", 1);
        tokenTho.Map = new[] { startLine, startLine + 1 };

        var tokenHtro = state.Push("tr_open", "tr", 1);
        tokenHtro.Map = new[] { startLine, startLine + 1 };

        for (var i = 0; i < columns.Count; i++)
        {
            var tokenHo = state.Push("th_open", "th", 1);
            if (aligns[i].Length > 0)
            {
                tokenHo.Attrs = new List<string[]> { new[] { "style", "text-align:" + aligns[i] } };
            }

            var tokenIl = state.Push("inline", "", 0);
            tokenIl.Content = columns[i].Trim();
            tokenIl.Children = new List<Token>();

            state.Push("th_close", "th", -1);
        }

        state.Push("tr_close", "tr", -1);
        state.Push("thead_close", "thead", -1);

        int[] tbodyLines = null;
        var autocompletedCells = 0;

        for (nextLine = startLine + 2; nextLine < endLine; nextLine++)
        {
            if (state.SCount[nextLine] < state.BlkIndent) { break; }

            var terminate = false;
            foreach (var terminator in terminatorRules)
            {
                if (terminator(state, nextLine, endLine, true))
                {
                    terminate = true;
                    break;
                }
            }

            if (terminate) { break; }
            lineText = GetLine(state, nextLine).Trim();
            if (lineText.Length == 0) { break; }
            if (state.SCount[nextLine] - state.BlkIndent >= 4) { break; }
            columns = EscapedSplit(lineText);
            if (columns.Count > 0 && columns[0].Length == 0) { columns.RemoveAt(0); }
            if (columns.Count > 0 && columns[columns.Count - 1].Length == 0) { columns.RemoveAt(columns.Count - 1); }

            // the autocomplete count can go negative when a row has more columns than
            // the header, which does not affect the intended expansion limit
            autocompletedCells += columnCount - columns.Count;
            if (autocompletedCells > MaxAutocompletedCells) { break; }

            if (nextLine == startLine + 2)
            {
                var tokenTbo = state.Push("tbody_open", "tbody", 1);
                tbodyLines = new[] { startLine + 2, 0 };
                tokenTbo.Map = tbodyLines;
            }

            var tokenTro = state.Push("tr_open", "tr", 1);
            tokenTro.Map = new[] { nextLine, nextLine + 1 };

            for (var i = 0; i < columnCount; i++)
            {
                var tokenTdo = state.Push("td_open", "td", 1);
                if (aligns[i].Length > 0)
                {
                    tokenTdo.Attrs = new List<string[]> { new[] { "style", "text-align:" + aligns[i] } };
                }

                var tokenIl = state.Push("inline", "", 0);
                tokenIl.Content = i < columns.Count ? columns[i].Trim() : "";
                tokenIl.Children = new List<Token>();

                state.Push("td_close", "td", -1);
            }
            state.Push("tr_close", "tr", -1);
        }

        if (tbodyLines != null)
        {
            state.Push("tbody_close", "tbody", -1);
            tbodyLines[1] = nextLine;
        }

        state.Push("table_close", "table", -1);
        tableLines[1] = nextLine;

        state.ParentType = oldParentType;
        state.Line = nextLine;
        return true;
    }
}
