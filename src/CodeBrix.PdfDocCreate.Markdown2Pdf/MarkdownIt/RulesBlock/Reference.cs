// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_block/reference.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Helpers;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

/// <summary>Link reference definitions ([label]: destination 'title').</summary>
internal static class Reference
{
    public static bool Rule(StateBlock state, int startLine, int _endLine, bool silent)
    {
        var pos = state.BMarks[startLine] + state.TShift[startLine];
        var max = state.EMarks[startLine];
        var nextLine = startLine + 1;

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }

        if (MdUtils.CharCode(state.Src, pos) != 0x5B /* [ */) { return false; }

        string GetNextLine(int lineNo)
        {
            var endLine = state.LineMax;

            if (lineNo >= endLine || state.IsEmpty(lineNo))
            {
                // empty line or end of input
                return null;
            }

            var isContinuation = false;

            // this would be a code block normally, but after paragraph it's considered
            // a lazy continuation regardless of what's there
            if (state.SCount[lineNo] - state.BlkIndent > 3) { isContinuation = true; }

            // quirk for blockquotes: this line should already be checked by that rule
            if (state.SCount[lineNo] < 0) { isContinuation = true; }

            if (!isContinuation)
            {
                var terminatorRules = state.Md.Block.Ruler.GetRules("reference");
                var oldParentType = state.ParentType;
                state.ParentType = "reference";

                // Some tags can terminate paragraph without empty line.
                var terminate = false;
                foreach (var terminator in terminatorRules)
                {
                    if (terminator(state, lineNo, endLine, true))
                    {
                        terminate = true;
                        break;
                    }
                }

                state.ParentType = oldParentType;
                if (terminate)
                {
                    // terminated by another block
                    return null;
                }
            }

            var linePos = state.BMarks[lineNo] + state.TShift[lineNo];
            var lineMax = state.EMarks[lineNo];

            // lineMax + 1 explicitly includes the newline
            var end = System.Math.Min(lineMax + 1, state.Src.Length);
            return state.Src.Substring(linePos, end - linePos);
        }

        var str = state.Src.Substring(pos, System.Math.Min(max + 1, state.Src.Length) - pos);

        max = str.Length;
        var labelEnd = -1;

        for (pos = 1; pos < max; pos++)
        {
            var ch = MdUtils.CharCode(str, pos);
            if (ch == 0x5B /* [ */)
            {
                return false;
            }
            if (ch == 0x5D /* ] */)
            {
                labelEnd = pos;
                break;
            }
            if (ch == 0x0A /* \n */)
            {
                var lineContent = GetNextLine(nextLine);
                if (lineContent != null)
                {
                    str += lineContent;
                    max = str.Length;
                    nextLine++;
                }
            }
            else if (ch == 0x5C /* \ */)
            {
                pos++;
                if (pos < max && MdUtils.CharCode(str, pos) == 0x0A)
                {
                    var lineContent = GetNextLine(nextLine);
                    if (lineContent != null)
                    {
                        str += lineContent;
                        max = str.Length;
                        nextLine++;
                    }
                }
            }
        }

        if (labelEnd < 0 || MdUtils.CharCode(str, labelEnd + 1) != 0x3A /* : */) { return false; }

        // [label]:   destination   'title'
        //         ^^^ skip optional whitespace here
        for (pos = labelEnd + 2; pos < max; pos++)
        {
            var ch = MdUtils.CharCode(str, pos);
            if (ch == 0x0A)
            {
                var lineContent = GetNextLine(nextLine);
                if (lineContent != null)
                {
                    str += lineContent;
                    max = str.Length;
                    nextLine++;
                }
            }
            else if (MdUtils.IsSpace(ch))
            {
                // skip
            }
            else
            {
                break;
            }
        }

        // [label]:   destination   'title'
        //            ^^^^^^^^^^^ parse this
        var destRes = LinkHelpers.ParseLinkDestination(str, pos, max);
        if (!destRes.Ok) { return false; }

        var href = state.Md.NormalizeLink(destRes.Str);
        if (!state.Md.ValidateLink(href)) { return false; }

        pos = destRes.Pos;

        // save cursor state; we could be required to roll back later
        var destEndPos = pos;
        var destEndLineNo = nextLine;

        // [label]:   destination   'title'
        //                       ^^^ skipping those spaces
        var start = pos;
        for (; pos < max; pos++)
        {
            var ch = MdUtils.CharCode(str, pos);
            if (ch == 0x0A)
            {
                var lineContent = GetNextLine(nextLine);
                if (lineContent != null)
                {
                    str += lineContent;
                    max = str.Length;
                    nextLine++;
                }
            }
            else if (MdUtils.IsSpace(ch))
            {
                // skip
            }
            else
            {
                break;
            }
        }

        // [label]:   destination   'title'
        //                          ^^^^^^^ parse this
        var titleRes = LinkHelpers.ParseLinkTitle(str, pos, max);
        while (titleRes.CanContinue)
        {
            var lineContent = GetNextLine(nextLine);
            if (lineContent == null) { break; }
            str += lineContent;
            pos = max;
            max = str.Length;
            nextLine++;
            titleRes = LinkHelpers.ParseLinkTitle(str, pos, max, titleRes);
        }

        string title;
        if (pos < max && start != pos && titleRes.Ok)
        {
            title = titleRes.Str;
            pos = titleRes.Pos;
        }
        else
        {
            title = "";
            pos = destEndPos;
            nextLine = destEndLineNo;
        }

        // skip trailing spaces until the rest of the line
        while (pos < max)
        {
            if (!MdUtils.IsSpace(MdUtils.CharCode(str, pos))) { break; }
            pos++;
        }

        if (pos < max && MdUtils.CharCode(str, pos) != 0x0A && title.Length > 0)
        {
            // garbage at the end of the line after the title, but it could still be a
            // valid reference if we roll back
            title = "";
            pos = destEndPos;
            nextLine = destEndLineNo;
            while (pos < max)
            {
                if (!MdUtils.IsSpace(MdUtils.CharCode(str, pos))) { break; }
                pos++;
            }
        }

        if (pos < max && MdUtils.CharCode(str, pos) != 0x0A)
        {
            // garbage at the end of the line
            return false;
        }

        var label = MdUtils.NormalizeReference(str.Substring(1, labelEnd - 1));
        if (label.Length == 0)
        {
            // CommonMark 0.20 disallows empty labels
            return false;
        }

        // A reference can not terminate anything; this check is for safety only.
        if (silent) { return true; }

        var references = state.Env.References;
        if (references == null)
        {
            references = new Dictionary<string, LinkReference>(System.StringComparer.Ordinal);
            state.Env.References = references;
        }
        if (!references.ContainsKey(label))
        {
            references[label] = new LinkReference { Title = title, Href = href };
        }

        state.Line = nextLine;
        return true;
    }
}
