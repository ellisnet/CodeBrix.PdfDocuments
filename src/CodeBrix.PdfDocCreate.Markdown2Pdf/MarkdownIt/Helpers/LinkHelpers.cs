// ============================================================================
// C# port of markdown-it v14.1.0 - lib/helpers/{parse_link_label,
// parse_link_destination,parse_link_title}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Helpers;

/// <summary>Result of <see cref="LinkHelpers.ParseLinkDestination"/>.</summary>
public sealed class LinkDestinationResult
{
    /// <summary>True when a valid destination was parsed.</summary>
    public bool Ok { get; set; }

    /// <summary>Position of the first character after the destination.</summary>
    public int Pos { get; set; }

    /// <summary>The unescaped destination.</summary>
    public string Str { get; set; } = "";
}

/// <summary>Result of <see cref="LinkHelpers.ParseLinkTitle"/>.</summary>
public sealed class LinkTitleResult
{
    /// <summary>True when this is a valid link title.</summary>
    public bool Ok { get; set; }

    /// <summary>True when this link title can be continued on the next line.</summary>
    public bool CanContinue { get; set; }

    /// <summary>When Ok, the position of the first character after the closing marker.</summary>
    public int Pos { get; set; }

    /// <summary>When Ok, the unescaped title.</summary>
    public string Str { get; set; } = "";

    /// <summary>Expected closing marker character code.</summary>
    public int Marker { get; set; }
}

/// <summary>Link component parsers, also useful for plugins.</summary>
public static class LinkHelpers
{
    /// <summary>
    /// Parses a link label; assumes the first character ("[") already matched. Returns
    /// the end of the label or -1.
    /// </summary>
    public static int ParseLinkLabel(StateInline state, int start, bool disableNested = false)
    {
        var max = state.PosMax;
        var oldPos = state.Pos;
        var found = false;

        state.Pos = start + 1;
        var level = 1;

        while (state.Pos < max)
        {
            var marker = MdUtils.CharCode(state.Src, state.Pos);
            if (marker == 0x5D /* ] */)
            {
                level--;
                if (level == 0)
                {
                    found = true;
                    break;
                }
            }

            var prevPos = state.Pos;
            state.Md.Inline.SkipToken(state);
            if (marker == 0x5B /* [ */)
            {
                if (prevPos == state.Pos - 1)
                {
                    // increase level if we find text `[` that is not part of any token
                    level++;
                }
                else if (disableNested)
                {
                    state.Pos = oldPos;
                    return -1;
                }
            }
        }

        var labelEnd = found ? state.Pos : -1;

        // restore old state
        state.Pos = oldPos;

        return labelEnd;
    }

    /// <summary>Parses a link destination.</summary>
    public static LinkDestinationResult ParseLinkDestination(string str, int start, int max)
    {
        var pos = start;
        var result = new LinkDestinationResult();

        if (MdUtils.CharCode(str, pos) == 0x3C /* < */)
        {
            pos++;
            while (pos < max)
            {
                var code = MdUtils.CharCode(str, pos);
                if (code == 0x0A /* \n */) { return result; }
                if (code == 0x3C /* < */) { return result; }
                if (code == 0x3E /* > */)
                {
                    result.Pos = pos + 1;
                    result.Str = MdUtils.UnescapeAll(str.Substring(start + 1, pos - start - 1));
                    result.Ok = true;
                    return result;
                }
                if (code == 0x5C /* \ */ && pos + 1 < max)
                {
                    pos += 2;
                    continue;
                }

                pos++;
            }

            // no closing '>'
            return result;
        }

        var level = 0;
        while (pos < max)
        {
            var code = MdUtils.CharCode(str, pos);

            if (code == 0x20) { break; }

            // ascii control characters
            if (code < 0x20 || code == 0x7F) { break; }

            if (code == 0x5C /* \ */ && pos + 1 < max)
            {
                if (MdUtils.CharCode(str, pos + 1) == 0x20) { break; }
                pos += 2;
                continue;
            }

            if (code == 0x28 /* ( */)
            {
                level++;
                if (level > 32) { return result; }
            }

            if (code == 0x29 /* ) */)
            {
                if (level == 0) { break; }
                level--;
            }

            pos++;
        }

        if (start == pos) { return result; }
        if (level != 0) { return result; }

        result.Str = MdUtils.UnescapeAll(str.Substring(start, pos - start));
        result.Pos = pos;
        result.Ok = true;
        return result;
    }

    /// <summary>
    /// Parses a link title within [start, max], or continues a previous parse when
    /// <paramref name="prevState"/> is supplied (for multiline reference titles).
    /// </summary>
    public static LinkTitleResult ParseLinkTitle(string str, int start, int max, LinkTitleResult prevState = null)
    {
        var pos = start;
        var state = new LinkTitleResult();

        if (prevState != null)
        {
            // continuation of a previous ParseLinkTitle call on the next line
            state.Str = prevState.Str;
            state.Marker = prevState.Marker;
        }
        else
        {
            if (pos >= max) { return state; }

            var marker = MdUtils.CharCode(str, pos);
            if (marker != 0x22 /* " */ && marker != 0x27 /* ' */ && marker != 0x28 /* ( */) { return state; }

            start++;
            pos++;

            // if the opening marker is "(", the closing one is ")"
            if (marker == 0x28) { marker = 0x29; }

            state.Marker = marker;
        }

        while (pos < max)
        {
            var code = MdUtils.CharCode(str, pos);
            if (code == state.Marker)
            {
                state.Pos = pos + 1;
                state.Str += MdUtils.UnescapeAll(str.Substring(start, pos - start));
                state.Ok = true;
                return state;
            }
            if (code == 0x28 /* ( */ && state.Marker == 0x29 /* ) */)
            {
                return state;
            }
            if (code == 0x5C /* \ */ && pos + 1 < max)
            {
                pos++;
            }

            pos++;
        }

        // no closing marker found, but the title may continue on the next line
        state.CanContinue = true;
        state.Str += MdUtils.UnescapeAll(str.Substring(start, pos - start));
        return state;
    }
}
