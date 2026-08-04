// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_inline/{text,newline,escape,
// backticks,entity,autolink,html_inline,linkify}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
//
// Named-entity decoding routes through CodeBrix.MarkupParse's HTML5 entity
// table instead of the upstream "entities" package.
// ============================================================================

using System.Globalization;
using System.Text.RegularExpressions;
using CodeBrix.MarkupParse.Html;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

/// <summary>Skips runs of plain text characters into the pending buffer.</summary>
internal static class TextRule
{
    // Don't confuse with "Markdown ASCII Punctuation" - '{}$%@~+=:' is reserved for
    // extensions.
    private static bool IsTerminatorChar(int ch)
    {
        switch (ch)
        {
            case 0x0A: case 0x21: case 0x23: case 0x24: case 0x25: case 0x26:
            case 0x2A: case 0x2B: case 0x2D: case 0x3A: case 0x3C: case 0x3D:
            case 0x3E: case 0x40: case 0x5B: case 0x5C: case 0x5D: case 0x5E:
            case 0x5F: case 0x60: case 0x7B: case 0x7D: case 0x7E:
                return true;
            default:
                return false;
        }
    }

    public static bool Rule(StateInline state, bool silent)
    {
        var pos = state.Pos;

        while (pos < state.PosMax && !IsTerminatorChar(state.Src[pos]))
        {
            pos++;
        }

        if (pos == state.Pos) { return false; }

        if (!silent) { state.Pending += state.Src.Substring(state.Pos, pos - state.Pos); }

        state.Pos = pos;

        return true;
    }
}

/// <summary>Processes '\n' (soft and hard breaks).</summary>
internal static class Newline
{
    public static bool Rule(StateInline state, bool silent)
    {
        var pos = state.Pos;

        if (MdUtils.CharCode(state.Src, pos) != 0x0A /* \n */) { return false; }

        var pmax = state.Pending.Length - 1;
        var max = state.PosMax;

        // '  \n' -> hardbreak
        if (!silent)
        {
            if (pmax >= 0 && state.Pending[pmax] == 0x20)
            {
                if (pmax >= 1 && state.Pending[pmax - 1] == 0x20)
                {
                    // Find the whitespace tail of the pending chars.
                    var ws = pmax - 1;
                    while (ws >= 1 && state.Pending[ws - 1] == 0x20) { ws--; }

                    state.Pending = state.Pending.Substring(0, ws);
                    state.Push("hardbreak", "br", 0);
                }
                else
                {
                    state.Pending = state.Pending.Substring(0, state.Pending.Length - 1);
                    state.Push("softbreak", "br", 0);
                }
            }
            else
            {
                state.Push("softbreak", "br", 0);
            }
        }

        pos++;

        // skip heading spaces for next line
        while (pos < max && MdUtils.IsSpace(MdUtils.CharCode(state.Src, pos))) { pos++; }

        state.Pos = pos;
        return true;
    }
}

/// <summary>Processes escaped chars and hardbreaks.</summary>
internal static class EscapeRule
{
    private static readonly bool[] Escaped = BuildEscaped();

    private static bool[] BuildEscaped()
    {
        var escaped = new bool[256];
        foreach (var ch in "\\!\"#$%&'()*+,./:;<=>?@[]^_`{|}~-")
        {
            escaped[ch] = true;
        }
        return escaped;
    }

    public static bool Rule(StateInline state, bool silent)
    {
        var pos = state.Pos;
        var max = state.PosMax;

        if (MdUtils.CharCode(state.Src, pos) != 0x5C /* \ */) { return false; }
        pos++;

        // '\' at the end of the inline block
        if (pos >= max) { return false; }

        var ch1 = MdUtils.CharCode(state.Src, pos);

        if (ch1 == 0x0A)
        {
            if (!silent)
            {
                state.Push("hardbreak", "br", 0);
            }

            pos++;
            // skip leading whitespaces from next line
            while (pos < max)
            {
                ch1 = MdUtils.CharCode(state.Src, pos);
                if (!MdUtils.IsSpace(ch1)) { break; }
                pos++;
            }

            state.Pos = pos;
            return true;
        }

        var escapedStr = state.Src[pos].ToString();

        if (ch1 >= 0xD800 && ch1 <= 0xDBFF && pos + 1 < max)
        {
            var ch2 = MdUtils.CharCode(state.Src, pos + 1);

            if (ch2 >= 0xDC00 && ch2 <= 0xDFFF)
            {
                escapedStr += state.Src[pos + 1];
                pos++;
            }
        }

        var origStr = "\\" + escapedStr;

        if (!silent)
        {
            var token = state.Push("text_special", "", 0);
            token.Content = ch1 < 256 && Escaped[ch1] ? escapedStr : origStr;
            token.Markup = origStr;
            token.Info = "escape";
        }

        state.Pos = pos + 1;
        return true;
    }
}

/// <summary>Parses backticks (inline code).</summary>
internal static class Backticks
{
    private static readonly Regex NewlineRe = new Regex("\n", RegexOptions.Compiled);
    private static readonly Regex StripSpaceRe = new Regex("^ (.+) $", RegexOptions.Compiled | RegexOptions.Singleline);

    public static bool Rule(StateInline state, bool silent)
    {
        var pos = state.Pos;
        var ch = MdUtils.CharCode(state.Src, pos);

        if (ch != 0x60 /* ` */) { return false; }

        var start = pos;
        pos++;
        var max = state.PosMax;

        // scan marker length
        while (pos < max && state.Src[pos] == 0x60 /* ` */) { pos++; }

        var marker = state.Src.Substring(start, pos - start);
        var openerLength = marker.Length;

        if (state.BackticksScanned
            && (state.Backticks.TryGetValue(openerLength, out var lastSeen) ? lastSeen : 0) <= start)
        {
            if (!silent) { state.Pending += marker; }
            state.Pos += openerLength;
            return true;
        }

        var matchEnd = pos;
        int matchStart;

        // Nothing in the cache; scan until the end (or until a marker is found)
        while ((matchStart = state.Src.IndexOf('`', matchEnd)) != -1)
        {
            matchEnd = matchStart + 1;

            // scan marker length
            while (matchEnd < max && state.Src[matchEnd] == 0x60 /* ` */) { matchEnd++; }

            var closerLength = matchEnd - matchStart;

            if (closerLength == openerLength)
            {
                // Found matching closer length.
                if (!silent)
                {
                    var token = state.Push("code_inline", "code", 0);
                    token.Markup = marker;
                    var content = NewlineRe.Replace(state.Src.Substring(pos, matchStart - pos), " ");
                    token.Content = StripSpaceRe.Replace(content, "$1");
                }
                state.Pos = matchEnd;
                return true;
            }

            // Different length found; cache it as the upper limit for that closer.
            state.Backticks[closerLength] = matchStart;
        }

        // Scanned through the end, didn't find anything
        state.BackticksScanned = true;

        if (!silent) { state.Pending += marker; }
        state.Pos += openerLength;
        return true;
    }
}

/// <summary>Processes html entities: &amp;#123;, &amp;#xAF;, &amp;quot;, ...</summary>
internal static class Entity
{
    private static readonly Regex DigitalRe = new Regex("^&#((?:x[a-f0-9]{1,6}|[0-9]{1,7}));", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex NamedRe = new Regex("^&([a-z][a-z0-9]{1,31});", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool Rule(StateInline state, bool silent)
    {
        var pos = state.Pos;
        var max = state.PosMax;

        if (MdUtils.CharCode(state.Src, pos) != 0x26 /* & */) { return false; }

        if (pos + 1 >= max) { return false; }

        var ch = MdUtils.CharCode(state.Src, pos + 1);

        if (ch == 0x23 /* # */)
        {
            var match = DigitalRe.Match(state.Src.Substring(pos));
            if (match.Success)
            {
                if (!silent)
                {
                    var digits = match.Groups[1].Value;
                    var code = char.ToLowerInvariant(digits[0]) == 'x'
                        ? int.Parse(digits.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                        : int.Parse(digits, CultureInfo.InvariantCulture);

                    var token = state.Push("text_special", "", 0);
                    token.Content = MdUtils.IsValidEntityCode(code)
                        ? MdUtils.FromCodePoint(code)
                        : MdUtils.FromCodePoint(0xFFFD);
                    token.Markup = match.Value;
                    token.Info = "entity";
                }
                state.Pos += match.Length;
                return true;
            }
        }
        else
        {
            var match = NamedRe.Match(state.Src.Substring(pos));
            if (match.Success)
            {
                var decoded = HtmlEntityProvider.Resolver.GetSymbol(match.Groups[1].Value + ";");
                if (!string.IsNullOrEmpty(decoded))
                {
                    if (!silent)
                    {
                        var token = state.Push("text_special", "", 0);
                        token.Content = decoded;
                        token.Markup = match.Value;
                        token.Info = "entity";
                    }
                    state.Pos += match.Length;
                    return true;
                }
            }
        }

        return false;
    }
}

/// <summary>Processes autolinks: '&lt;protocol:...&gt;'.</summary>
internal static class Autolink
{
    private static readonly Regex EmailRe = new Regex(
        @"^([a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*)$",
        RegexOptions.Compiled);

    private static readonly Regex AutolinkRe = new Regex(
        "^([a-zA-Z][a-zA-Z0-9+.-]{1,31}):([^<>\\x00-\\x20]*)$", RegexOptions.Compiled);

    public static bool Rule(StateInline state, bool silent)
    {
        var pos = state.Pos;

        if (MdUtils.CharCode(state.Src, pos) != 0x3C /* < */) { return false; }

        var start = state.Pos;
        var max = state.PosMax;

        for (; ; )
        {
            if (++pos >= max) { return false; }

            var ch = MdUtils.CharCode(state.Src, pos);

            if (ch == 0x3C /* < */) { return false; }
            if (ch == 0x3E /* > */) { break; }
        }

        var url = state.Src.Substring(start + 1, pos - start - 1);

        if (AutolinkRe.IsMatch(url))
        {
            var fullUrl = state.Md.NormalizeLink(url);
            if (!state.Md.ValidateLink(fullUrl)) { return false; }

            if (!silent)
            {
                var tokenO = state.Push("link_open", "a", 1);
                tokenO.Attrs = new System.Collections.Generic.List<string[]> { new[] { "href", fullUrl } };
                tokenO.Markup = "autolink";
                tokenO.Info = "auto";

                var tokenT = state.Push("text", "", 0);
                tokenT.Content = state.Md.NormalizeLinkText(url);

                var tokenC = state.Push("link_close", "a", -1);
                tokenC.Markup = "autolink";
                tokenC.Info = "auto";
            }

            state.Pos += url.Length + 2;
            return true;
        }

        if (EmailRe.IsMatch(url))
        {
            var fullUrl = state.Md.NormalizeLink("mailto:" + url);
            if (!state.Md.ValidateLink(fullUrl)) { return false; }

            if (!silent)
            {
                var tokenO = state.Push("link_open", "a", 1);
                tokenO.Attrs = new System.Collections.Generic.List<string[]> { new[] { "href", fullUrl } };
                tokenO.Markup = "autolink";
                tokenO.Info = "auto";

                var tokenT = state.Push("text", "", 0);
                tokenT.Content = state.Md.NormalizeLinkText(url);

                var tokenC = state.Push("link_close", "a", -1);
                tokenC.Markup = "autolink";
                tokenC.Info = "auto";
            }

            state.Pos += url.Length + 2;
            return true;
        }

        return false;
    }
}

/// <summary>Processes inline html tags.</summary>
internal static class HtmlInline
{
    private static readonly Regex LinkOpenRe = new Regex("^<a[>\\s]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LinkCloseRe = new Regex("^</a\\s*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static bool IsLetter(int ch)
    {
        var lc = ch | 0x20; // to lower case
        return lc >= 0x61 /* a */ && lc <= 0x7A /* z */;
    }

    public static bool Rule(StateInline state, bool silent)
    {
        if (!state.Md.Options.Html) { return false; }

        // Check start
        var max = state.PosMax;
        var pos = state.Pos;
        if (MdUtils.CharCode(state.Src, pos) != 0x3C /* < */ || pos + 2 >= max)
        {
            return false;
        }

        // Quick fail on second char
        var ch = MdUtils.CharCode(state.Src, pos + 1);
        if (ch != 0x21 /* ! */ && ch != 0x3F /* ? */ && ch != 0x2F /* / */ && !IsLetter(ch))
        {
            return false;
        }

        var match = HtmlRe.HtmlTagRe.Match(state.Src.Substring(pos));
        if (!match.Success) { return false; }

        if (!silent)
        {
            var token = state.Push("html_inline", "", 0);
            token.Content = match.Value;

            if (LinkOpenRe.IsMatch(token.Content)) { state.LinkLevel++; }
            if (LinkCloseRe.IsMatch(token.Content)) { state.LinkLevel--; }
        }
        state.Pos += match.Length;
        return true;
    }
}

/// <summary>
/// Upstream linkifies bare URLs via linkify-it, which is not part of this port; the
/// rule never matches.
/// </summary>
internal static class LinkifyInline
{
    public static bool Rule(StateInline state, bool silent) => false;
}
