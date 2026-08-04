// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_block/{code,fence,heading,
// lheading,hr,paragraph}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

/// <summary>Code block (4 spaces padded).</summary>
internal static class Code
{
    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        if (state.SCount[startLine] - state.BlkIndent < 4) { return false; }

        var nextLine = startLine + 1;
        var last = nextLine;

        while (nextLine < endLine)
        {
            if (state.IsEmpty(nextLine))
            {
                nextLine++;
                continue;
            }

            if (state.SCount[nextLine] - state.BlkIndent >= 4)
            {
                nextLine++;
                last = nextLine;
                continue;
            }
            break;
        }

        state.Line = last;

        var token = state.Push("code_block", "code", 0);
        token.Content = state.GetLines(startLine, last, 4 + state.BlkIndent, false) + "\n";
        token.Map = new[] { startLine, state.Line };

        return true;
    }
}

/// <summary>Fences (``` lang, ~~~ lang).</summary>
internal static class Fence
{
    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var pos = state.BMarks[startLine] + state.TShift[startLine];
        var max = state.EMarks[startLine];

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }

        if (pos + 3 > max) { return false; }

        var marker = MdUtils.CharCode(state.Src, pos);
        if (marker != 0x7E /* ~ */ && marker != 0x60 /* ` */) { return false; }

        // scan marker length
        var mem = pos;
        pos = state.SkipChars(pos, marker);

        var len = pos - mem;
        if (len < 3) { return false; }

        var markup = state.Src.Substring(mem, pos - mem);
        var params_ = state.Src.Substring(pos, max - pos);

        if (marker == 0x60 /* ` */ && params_.IndexOf((char)marker) >= 0) { return false; }

        // Since start is found, we can report success here in validation mode
        if (silent) { return true; }

        // search end of block
        var nextLine = startLine;
        var haveEndMarker = false;

        for (; ; )
        {
            nextLine++;
            if (nextLine >= endLine)
            {
                // unclosed block is autoclosed by end of document (or parent)
                break;
            }

            pos = mem = state.BMarks[nextLine] + state.TShift[nextLine];
            max = state.EMarks[nextLine];

            if (pos < max && state.SCount[nextLine] < state.BlkIndent)
            {
                // non-empty line with negative indent should stop the list
                break;
            }

            if (MdUtils.CharCode(state.Src, pos) != marker) { continue; }

            if (state.SCount[nextLine] - state.BlkIndent >= 4)
            {
                // closing fence should be indented less than 4 spaces
                continue;
            }

            pos = state.SkipChars(pos, marker);

            // closing code fence must be at least as long as the opening one
            if (pos - mem < len) { continue; }

            // make sure tail has spaces only
            pos = state.SkipSpaces(pos);
            if (pos < max) { continue; }

            haveEndMarker = true;
            break;
        }

        // If a fence has heading spaces, they should be removed from its inner block
        len = state.SCount[startLine];

        state.Line = nextLine + (haveEndMarker ? 1 : 0);

        var token = state.Push("fence", "code", 0);
        token.Info = params_;
        token.Content = state.GetLines(startLine + 1, nextLine, len, true);
        token.Markup = markup;
        token.Map = new[] { startLine, state.Line };

        return true;
    }
}

/// <summary>Heading (#, ##, ...).</summary>
internal static class Heading
{
    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var pos = state.BMarks[startLine] + state.TShift[startLine];
        var max = state.EMarks[startLine];

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }

        var ch = MdUtils.CharCode(state.Src, pos);
        if (ch != 0x23 /* # */ || pos >= max) { return false; }

        // count heading level
        var level = 1;
        ch = MdUtils.CharCode(state.Src, ++pos);
        while (ch == 0x23 /* # */ && pos < max && level <= 6)
        {
            level++;
            ch = MdUtils.CharCode(state.Src, ++pos);
        }

        if (level > 6 || (pos < max && !MdUtils.IsSpace(ch))) { return false; }

        if (silent) { return true; }

        // Cut tails like '    ###  ' from the end of the string
        max = state.SkipSpacesBack(max, pos);
        var tmp = state.SkipCharsBack(max, 0x23, pos); // #
        if (tmp > pos && MdUtils.IsSpace(MdUtils.CharCode(state.Src, tmp - 1)))
        {
            max = tmp;
        }

        state.Line = startLine + 1;

        var tokenO = state.Push("heading_open", "h" + level, 1);
        tokenO.Markup = "########".Substring(0, level);
        tokenO.Map = new[] { startLine, state.Line };

        var tokenI = state.Push("inline", "", 0);
        tokenI.Content = state.Src.Substring(pos, max - pos).Trim();
        tokenI.Map = new[] { startLine, state.Line };
        tokenI.Children = new System.Collections.Generic.List<Token>();

        var tokenC = state.Push("heading_close", "h" + level, -1);
        tokenC.Markup = "########".Substring(0, level);

        return true;
    }
}

/// <summary>Setext heading (---, ===).</summary>
internal static class LHeading
{
    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var terminatorRules = state.Md.Block.Ruler.GetRules("paragraph");

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }

        var oldParentType = state.ParentType;
        state.ParentType = "paragraph"; // use paragraph to match terminatorRules

        // jump line-by-line until empty one or EOF
        var level = 0;
        var marker = -1;
        var nextLine = startLine + 1;

        for (; nextLine < endLine && !state.IsEmpty(nextLine); nextLine++)
        {
            // this would be a code block normally, but after paragraph it's considered
            // a lazy continuation regardless of what's there
            if (state.SCount[nextLine] - state.BlkIndent > 3) { continue; }

            // Check for underline in setext header
            if (state.SCount[nextLine] >= state.BlkIndent)
            {
                var pos = state.BMarks[nextLine] + state.TShift[nextLine];
                var max = state.EMarks[nextLine];

                if (pos < max)
                {
                    marker = MdUtils.CharCode(state.Src, pos);

                    if (marker == 0x2D /* - */ || marker == 0x3D /* = */)
                    {
                        pos = state.SkipChars(pos, marker);
                        pos = state.SkipSpaces(pos);

                        if (pos >= max)
                        {
                            level = marker == 0x3D /* = */ ? 1 : 2;
                            break;
                        }
                    }
                }
            }

            // quirk for blockquotes: this line should already be checked by that rule
            if (state.SCount[nextLine] < 0) { continue; }

            // Some tags can terminate paragraph without empty line.
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
        }

        if (level == 0)
        {
            // Didn't find valid underline
            return false;
        }

        var content = state.GetLines(startLine, nextLine, state.BlkIndent, false).Trim();

        state.Line = nextLine + 1;

        var tokenO = state.Push("heading_open", "h" + level, 1);
        tokenO.Markup = ((char)marker).ToString();
        tokenO.Map = new[] { startLine, state.Line };

        var tokenI = state.Push("inline", "", 0);
        tokenI.Content = content;
        tokenI.Map = new[] { startLine, state.Line - 1 };
        tokenI.Children = new System.Collections.Generic.List<Token>();

        var tokenC = state.Push("heading_close", "h" + level, -1);
        tokenC.Markup = ((char)marker).ToString();

        state.ParentType = oldParentType;

        return true;
    }
}

/// <summary>Horizontal rule.</summary>
internal static class Hr
{
    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var max = state.EMarks[startLine];

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }

        var pos = state.BMarks[startLine] + state.TShift[startLine];
        var marker = MdUtils.CharCode(state.Src, pos++);

        // Check hr marker
        if (marker != 0x2A /* * */ && marker != 0x2D /* - */ && marker != 0x5F /* _ */)
        {
            return false;
        }

        // markers can be mixed with spaces, but there should be at least 3 of them
        var cnt = 1;
        while (pos < max)
        {
            var ch = MdUtils.CharCode(state.Src, pos++);
            if (ch != marker && !MdUtils.IsSpace(ch)) { return false; }
            if (ch == marker) { cnt++; }
        }

        if (cnt < 3) { return false; }

        if (silent) { return true; }

        state.Line = startLine + 1;

        var token = state.Push("hr", "hr", 0);
        token.Map = new[] { startLine, state.Line };
        token.Markup = new string((char)marker, cnt);

        return true;
    }
}

/// <summary>Paragraph.</summary>
internal static class Paragraph
{
    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var terminatorRules = state.Md.Block.Ruler.GetRules("paragraph");
        var oldParentType = state.ParentType;
        var nextLine = startLine + 1;
        state.ParentType = "paragraph";

        // jump line-by-line until empty one or EOF
        for (; nextLine < endLine && !state.IsEmpty(nextLine); nextLine++)
        {
            // this would be a code block normally, but after paragraph it's considered
            // a lazy continuation regardless of what's there
            if (state.SCount[nextLine] - state.BlkIndent > 3) { continue; }

            // quirk for blockquotes: this line should already be checked by that rule
            if (state.SCount[nextLine] < 0) { continue; }

            // Some tags can terminate paragraph without empty line.
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
        }

        var content = state.GetLines(startLine, nextLine, state.BlkIndent, false).Trim();

        state.Line = nextLine;

        var tokenO = state.Push("paragraph_open", "p", 1);
        tokenO.Map = new[] { startLine, state.Line };

        var tokenI = state.Push("inline", "", 0);
        tokenI.Content = content;
        tokenI.Map = new[] { startLine, state.Line };
        tokenI.Children = new System.Collections.Generic.List<Token>();

        state.Push("paragraph_close", "p", -1);

        state.ParentType = oldParentType;

        return true;
    }
}
