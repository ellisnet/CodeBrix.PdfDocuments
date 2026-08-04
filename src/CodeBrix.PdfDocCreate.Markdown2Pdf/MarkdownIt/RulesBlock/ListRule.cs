// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_block/list.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

/// <summary>Bullet and ordered lists.</summary>
internal static class ListRule
{
    /// <summary>Searches `[-+*][\n ]`; returns the position after the marker or -1.</summary>
    private static int SkipBulletListMarker(StateBlock state, int startLine)
    {
        var max = state.EMarks[startLine];
        var pos = state.BMarks[startLine] + state.TShift[startLine];

        var marker = MdUtils.CharCode(state.Src, pos++);
        if (marker != 0x2A /* * */ && marker != 0x2D /* - */ && marker != 0x2B /* + */)
        {
            return -1;
        }

        if (pos < max && !MdUtils.IsSpace(MdUtils.CharCode(state.Src, pos)))
        {
            // " -test " - is not a list item
            return -1;
        }

        return pos;
    }

    /// <summary>Searches `\d+[.)][\n ]`; returns the position after the marker or -1.</summary>
    private static int SkipOrderedListMarker(StateBlock state, int startLine)
    {
        var start = state.BMarks[startLine] + state.TShift[startLine];
        var max = state.EMarks[startLine];
        var pos = start;

        // List marker should have at least 2 chars (digit + dot)
        if (pos + 1 >= max) { return -1; }

        var ch = MdUtils.CharCode(state.Src, pos++);
        if (ch < 0x30 /* 0 */ || ch > 0x39 /* 9 */) { return -1; }

        for (; ; )
        {
            // EOL -> fail
            if (pos >= max) { return -1; }

            ch = MdUtils.CharCode(state.Src, pos++);

            if (ch >= 0x30 /* 0 */ && ch <= 0x39 /* 9 */)
            {
                // no more than 9 digits (prevents integer overflow)
                if (pos - start >= 10) { return -1; }
                continue;
            }

            // found valid marker
            if (ch == 0x29 /* ) */ || ch == 0x2E /* . */) { break; }

            return -1;
        }

        if (pos < max && !MdUtils.IsSpace(MdUtils.CharCode(state.Src, pos)))
        {
            // " 1.test " - is not a list item
            return -1;
        }

        return pos;
    }

    private static void MarkTightParagraphs(StateBlock state, int idx)
    {
        var level = state.Level + 2;

        for (int i = idx + 2, l = state.Tokens.Count - 2; i < l; i++)
        {
            if (state.Tokens[i].Level == level && state.Tokens[i].Type == "paragraph_open")
            {
                state.Tokens[i + 2].Hidden = true;
                state.Tokens[i].Hidden = true;
                i += 2;
            }
        }
    }

    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var nextLine = startLine;
        var tight = true;

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[nextLine] - state.BlkIndent >= 4) { return false; }

        // Special case: deeply indented bullet under a list is a paragraph continuation
        if (state.ListIndent >= 0
            && state.SCount[nextLine] - state.ListIndent >= 4
            && state.SCount[nextLine] < state.BlkIndent)
        {
            return false;
        }

        var isTerminatingParagraph = false;

        // limit conditions when a list can interrupt a paragraph (validation mode only)
        if (silent && state.ParentType == "paragraph")
        {
            // The next list item should still terminate the previous list item.
            if (state.SCount[nextLine] >= state.BlkIndent)
            {
                isTerminatingParagraph = true;
            }
        }

        // Detect list type and position after the marker
        bool isOrdered;
        long markerValue = 0;
        var start = 0;
        int posAfterMarker;

        if ((posAfterMarker = SkipOrderedListMarker(state, nextLine)) >= 0)
        {
            isOrdered = true;
            start = state.BMarks[nextLine] + state.TShift[nextLine];
            markerValue = long.Parse(
                state.Src.Substring(start, posAfterMarker - 1 - start),
                CultureInfo.InvariantCulture);

            // A new ordered list right after a paragraph should start with 1.
            if (isTerminatingParagraph && markerValue != 1) { return false; }
        }
        else if ((posAfterMarker = SkipBulletListMarker(state, nextLine)) >= 0)
        {
            isOrdered = false;
        }
        else
        {
            return false;
        }

        // A new unordered list right after a paragraph: first line must not be empty.
        if (isTerminatingParagraph
            && state.SkipSpaces(posAfterMarker) >= state.EMarks[nextLine])
        {
            return false;
        }

        // For validation mode we can terminate immediately.
        if (silent) { return true; }

        // Terminate list on style change; remember the first marker to compare.
        var markerCharCode = MdUtils.CharCode(state.Src, posAfterMarker - 1);

        // Start list
        var listTokIdx = state.Tokens.Count;
        Token token;

        if (isOrdered)
        {
            token = state.Push("ordered_list_open", "ol", 1);
            if (markerValue != 1)
            {
                token.Attrs = new List<string[]>
                {
                    new[] { "start", markerValue.ToString(CultureInfo.InvariantCulture) },
                };
            }
        }
        else
        {
            token = state.Push("bullet_list_open", "ul", 1);
        }

        var listLines = new[] { nextLine, 0 };
        token.Map = listLines;
        token.Markup = ((char)markerCharCode).ToString();

        // Iterate list items
        var prevEmptyEnd = false;
        var terminatorRules = state.Md.Block.Ruler.GetRules("list");

        var oldParentType = state.ParentType;
        state.ParentType = "list";

        while (nextLine < endLine)
        {
            var pos = posAfterMarker;
            var max = state.EMarks[nextLine];

            var initial = state.SCount[nextLine] + posAfterMarker
                - (state.BMarks[nextLine] + state.TShift[nextLine]);
            var offset = initial;

            while (pos < max)
            {
                var ch = MdUtils.CharCode(state.Src, pos);

                if (ch == 0x09) { offset += 4 - (offset + state.BsCount[nextLine]) % 4; }
                else if (ch == 0x20) { offset++; }
                else { break; }

                pos++;
            }

            var contentStart = pos;
            int indentAfterMarker;

            if (contentStart >= max)
            {
                // trimming space in "-    \n  3" case, indent is 1 here
                indentAfterMarker = 1;
            }
            else
            {
                indentAfterMarker = offset - initial;
            }

            // With more than 4 spaces the indent is 1 (the rest is indented code)
            if (indentAfterMarker > 4) { indentAfterMarker = 1; }

            // "  -  test" - total length of this thing
            var indent = initial + indentAfterMarker;

            // Run subparser and write tokens
            token = state.Push("list_item_open", "li", 1);
            token.Markup = ((char)markerCharCode).ToString();
            var itemLines = new[] { nextLine, 0 };
            token.Map = itemLines;
            if (isOrdered)
            {
                token.Info = state.Src.Substring(start, posAfterMarker - 1 - start);
            }

            // change current state, restore after the parser subcall
            var oldTight = state.Tight;
            var oldTShift = state.TShift[nextLine];
            var oldSCount = state.SCount[nextLine];
            var oldListIndent = state.ListIndent;
            state.ListIndent = state.BlkIndent;
            state.BlkIndent = indent;

            state.Tight = true;
            state.TShift[nextLine] = contentStart - state.BMarks[nextLine];
            state.SCount[nextLine] = offset;

            if (contentStart >= max && state.IsEmpty(nextLine + 1))
            {
                // workaround: list item is empty and the list terminates before "foo"
                state.Line = Math.Min(state.Line + 2, endLine);
            }
            else
            {
                state.Md.Block.Tokenize(state, nextLine, endLine);
            }

            // If any list item is loose, mark the list as loose
            if (!state.Tight || prevEmptyEnd)
            {
                tight = false;
            }

            // An item becomes loose if it finishes with an empty line, but we filter
            // the last element because it means the list finished
            prevEmptyEnd = (state.Line - nextLine) > 1 && state.IsEmpty(state.Line - 1);

            state.BlkIndent = state.ListIndent;
            state.ListIndent = oldListIndent;
            state.TShift[nextLine] = oldTShift;
            state.SCount[nextLine] = oldSCount;
            state.Tight = oldTight;

            token = state.Push("list_item_close", "li", -1);
            token.Markup = ((char)markerCharCode).ToString();

            nextLine = state.Line;
            itemLines[1] = nextLine;

            if (nextLine >= endLine) { break; }

            // Check whether the list is terminated or continued.
            if (state.SCount[nextLine] < state.BlkIndent) { break; }

            // if it's indented more than 3 spaces, it should be a code block
            if (state.SCount[nextLine] - state.BlkIndent >= 4) { break; }

            // fail if terminating block found
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

            // fail if list has another type
            if (isOrdered)
            {
                posAfterMarker = SkipOrderedListMarker(state, nextLine);
                if (posAfterMarker < 0) { break; }
                start = state.BMarks[nextLine] + state.TShift[nextLine];
            }
            else
            {
                posAfterMarker = SkipBulletListMarker(state, nextLine);
                if (posAfterMarker < 0) { break; }
            }

            if (markerCharCode != MdUtils.CharCode(state.Src, posAfterMarker - 1)) { break; }
        }

        // Finalize list
        token = isOrdered
            ? state.Push("ordered_list_close", "ol", -1)
            : state.Push("bullet_list_close", "ul", -1);
        token.Markup = ((char)markerCharCode).ToString();

        listLines[1] = nextLine;
        state.Line = nextLine;

        state.ParentType = oldParentType;

        // mark paragraphs tight if needed
        if (tight)
        {
            MarkTightParagraphs(state, listTokIdx);
        }

        return true;
    }
}
