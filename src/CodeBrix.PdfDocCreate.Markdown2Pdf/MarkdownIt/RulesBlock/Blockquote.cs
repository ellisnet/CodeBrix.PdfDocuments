// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_block/blockquote.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

/// <summary>Block quotes.</summary>
internal static class Blockquote
{
    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var pos = state.BMarks[startLine] + state.TShift[startLine];
        var max = state.EMarks[startLine];

        var oldLineMax = state.LineMax;

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }

        // check the block quote marker
        if (MdUtils.CharCode(state.Src, pos) != 0x3E /* > */) { return false; }

        // we know that it's going to be a valid blockquote, so no point trying to find
        // the end of it in silent mode
        if (silent) { return true; }

        var oldBMarks = new List<int>();
        var oldBSCount = new List<int>();
        var oldSCount = new List<int>();
        var oldTShift = new List<int>();

        var terminatorRules = state.Md.Block.Ruler.GetRules("blockquote");

        var oldParentType = state.ParentType;
        state.ParentType = "blockquote";
        var lastLineEmpty = false;
        int nextLine;

        // Search the end of the block. It ends with an empty line outside, an empty
        // line inside, or another tag.
        for (nextLine = startLine; nextLine < endLine; nextLine++)
        {
            // check if it's outdented, i.e. inside a list item and indented less than
            // said list item
            var isOutdented = state.SCount[nextLine] < state.BlkIndent;

            pos = state.BMarks[nextLine] + state.TShift[nextLine];
            max = state.EMarks[nextLine];

            if (pos >= max)
            {
                // Case 1: line is not inside the blockquote, and this line is empty.
                break;
            }

            if (MdUtils.CharCode(state.Src, pos++) == 0x3E /* > */ && !isOutdented)
            {
                // This line is inside the blockquote.

                // set offset past spaces and ">"
                var initial = state.SCount[nextLine] + 1;
                bool spaceAfterMarker;
                var adjustTab = false;

                // skip one optional space after '>'
                if (MdUtils.CharCode(state.Src, pos) == 0x20 /* space */)
                {
                    // ' >   test ' -> position start of line here
                    pos++;
                    initial++;
                    spaceAfterMarker = true;
                }
                else if (MdUtils.CharCode(state.Src, pos) == 0x09 /* tab */)
                {
                    spaceAfterMarker = true;

                    if ((state.BsCount[nextLine] + initial) % 4 == 3)
                    {
                        // '  >\t  test ' -> position start of line here (tab width 1)
                        pos++;
                        initial++;
                    }
                    else
                    {
                        // ' >\t  test ' -> position start of line here + shift bsCount
                        // slightly to make extra space appear
                        adjustTab = true;
                    }
                }
                else
                {
                    spaceAfterMarker = false;
                }

                var offset = initial;
                oldBMarks.Add(state.BMarks[nextLine]);
                state.BMarks[nextLine] = pos;

                while (pos < max)
                {
                    var ch = MdUtils.CharCode(state.Src, pos);

                    if (MdUtils.IsSpace(ch))
                    {
                        if (ch == 0x09)
                        {
                            offset += 4 - (offset + state.BsCount[nextLine] + (adjustTab ? 1 : 0)) % 4;
                        }
                        else
                        {
                            offset++;
                        }
                    }
                    else
                    {
                        break;
                    }

                    pos++;
                }

                lastLineEmpty = pos >= max;

                oldBSCount.Add(state.BsCount[nextLine]);
                state.BsCount[nextLine] = state.SCount[nextLine] + 1 + (spaceAfterMarker ? 1 : 0);

                oldSCount.Add(state.SCount[nextLine]);
                state.SCount[nextLine] = offset - initial;

                oldTShift.Add(state.TShift[nextLine]);
                state.TShift[nextLine] = pos - state.BMarks[nextLine];
                continue;
            }

            // Case 2: line is not inside the blockquote, and the last line was empty.
            if (lastLineEmpty) { break; }

            // Case 3: another tag found.
            var terminate = false;
            foreach (var terminator in terminatorRules)
            {
                if (terminator(state, nextLine, endLine, true))
                {
                    terminate = true;
                    break;
                }
            }

            if (terminate)
            {
                // "hard termination mode" for paragraphs: when terminated by another
                // tag, paragraphs must not look below nextLine for continuations
                state.LineMax = nextLine;

                if (state.BlkIndent != 0)
                {
                    // re-calculate offsets to appear as if the indent wasn't changed
                    oldBMarks.Add(state.BMarks[nextLine]);
                    oldBSCount.Add(state.BsCount[nextLine]);
                    oldTShift.Add(state.TShift[nextLine]);
                    oldSCount.Add(state.SCount[nextLine]);
                    state.SCount[nextLine] -= state.BlkIndent;
                }

                break;
            }

            oldBMarks.Add(state.BMarks[nextLine]);
            oldBSCount.Add(state.BsCount[nextLine]);
            oldTShift.Add(state.TShift[nextLine]);
            oldSCount.Add(state.SCount[nextLine]);

            // A negative indentation means that this is a paragraph continuation.
            state.SCount[nextLine] = -1;
        }

        var oldIndent = state.BlkIndent;
        state.BlkIndent = 0;

        var tokenO = state.Push("blockquote_open", "blockquote", 1);
        tokenO.Markup = ">";
        var lines = new[] { startLine, 0 };
        tokenO.Map = lines;

        state.Md.Block.Tokenize(state, startLine, nextLine);

        var tokenC = state.Push("blockquote_close", "blockquote", -1);
        tokenC.Markup = ">";

        state.LineMax = oldLineMax;
        state.ParentType = oldParentType;
        lines[1] = state.Line;

        // Restore the original line caches.
        for (var i = 0; i < oldTShift.Count; i++)
        {
            state.BMarks[i + startLine] = oldBMarks[i];
            state.TShift[i + startLine] = oldTShift[i];
            state.SCount[i + startLine] = oldSCount[i];
            state.BsCount[i + startLine] = oldBSCount[i];
        }
        state.BlkIndent = oldIndent;

        return true;
    }
}
