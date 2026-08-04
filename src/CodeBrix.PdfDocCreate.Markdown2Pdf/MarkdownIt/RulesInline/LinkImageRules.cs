// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_inline/{link,image}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Helpers;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

/// <summary>Processes [link](&lt;to&gt; "stuff").</summary>
internal static class LinkRule
{
    public static bool Rule(StateInline state, bool silent)
    {
        string label = null;
        var href = "";
        var title = "";
        var parseReference = true;

        if (MdUtils.CharCode(state.Src, state.Pos) != 0x5B /* [ */) { return false; }

        var oldPos = state.Pos;
        var max = state.PosMax;
        var labelStart = state.Pos + 1;
        var labelEnd = LinkHelpers.ParseLinkLabel(state, state.Pos, true);

        // parser failed to find ']', so it's not a valid link
        if (labelEnd < 0) { return false; }

        var pos = labelEnd + 1;
        if (pos < max && MdUtils.CharCode(state.Src, pos) == 0x28 /* ( */)
        {
            // Inline link

            // might have found a valid shortcut link, disable reference parsing
            parseReference = false;

            // [link](  <href>  "title"  )
            //        ^^ skipping these spaces
            pos++;
            for (; pos < max; pos++)
            {
                var code = MdUtils.CharCode(state.Src, pos);
                if (!MdUtils.IsSpace(code) && code != 0x0A) { break; }
            }
            if (pos >= max) { return false; }

            // parsing link destination
            var res = LinkHelpers.ParseLinkDestination(state.Src, pos, state.PosMax);
            if (res.Ok)
            {
                href = state.Md.NormalizeLink(res.Str);
                if (state.Md.ValidateLink(href))
                {
                    pos = res.Pos;
                }
                else
                {
                    href = "";
                }

                // skipping spaces after destination
                var start = pos;
                for (; pos < max; pos++)
                {
                    var code = MdUtils.CharCode(state.Src, pos);
                    if (!MdUtils.IsSpace(code) && code != 0x0A) { break; }
                }

                // parsing link title
                var titleRes = LinkHelpers.ParseLinkTitle(state.Src, pos, state.PosMax);
                if (pos < max && start != pos && titleRes.Ok)
                {
                    title = titleRes.Str;
                    pos = titleRes.Pos;

                    // skipping spaces after title
                    for (; pos < max; pos++)
                    {
                        var code = MdUtils.CharCode(state.Src, pos);
                        if (!MdUtils.IsSpace(code) && code != 0x0A) { break; }
                    }
                }
            }

            if (pos >= max || MdUtils.CharCode(state.Src, pos) != 0x29 /* ) */)
            {
                // parsing a valid shortcut link failed, fallback to reference
                parseReference = true;
            }
            pos++;
        }

        if (parseReference)
        {
            // Link reference
            var references = state.Env.References;
            if (references == null) { return false; }

            if (pos < max && MdUtils.CharCode(state.Src, pos) == 0x5B /* [ */)
            {
                var start = pos + 1;
                pos = LinkHelpers.ParseLinkLabel(state, pos);
                if (pos >= 0)
                {
                    label = state.Src.Substring(start, pos - start);
                    pos++;
                }
                else
                {
                    pos = labelEnd + 1;
                }
            }
            else
            {
                pos = labelEnd + 1;
            }

            // covers collapsed reference links and shortcut reference links
            if (string.IsNullOrEmpty(label))
            {
                label = state.Src.Substring(labelStart, labelEnd - labelStart);
            }

            if (!references.TryGetValue(MdUtils.NormalizeReference(label), out var reference))
            {
                state.Pos = oldPos;
                return false;
            }
            href = reference.Href;
            title = reference.Title;
        }

        // We found the end of the link and know it's valid; call the tokenizer.
        if (!silent)
        {
            state.Pos = labelStart;
            state.PosMax = labelEnd;

            var tokenO = state.Push("link_open", "a", 1);
            var attrs = new List<string[]> { new[] { "href", href } };
            tokenO.Attrs = attrs;
            if (!string.IsNullOrEmpty(title))
            {
                attrs.Add(new[] { "title", title });
            }

            state.LinkLevel++;
            state.Md.Inline.Tokenize(state);
            state.LinkLevel--;

            state.Push("link_close", "a", -1);
        }

        state.Pos = pos;
        state.PosMax = max;
        return true;
    }
}

/// <summary>Processes ![image](&lt;src&gt; "title").</summary>
internal static class ImageRule
{
    public static bool Rule(StateInline state, bool silent)
    {
        string label = null;
        var href = "";
        var title = "";
        var oldPos = state.Pos;
        var max = state.PosMax;

        if (MdUtils.CharCode(state.Src, state.Pos) != 0x21 /* ! */) { return false; }
        if (MdUtils.CharCode(state.Src, state.Pos + 1) != 0x5B /* [ */) { return false; }

        var labelStart = state.Pos + 2;
        var labelEnd = LinkHelpers.ParseLinkLabel(state, state.Pos + 1, false);

        // parser failed to find ']', so it's not a valid link
        if (labelEnd < 0) { return false; }

        var pos = labelEnd + 1;
        if (pos < max && MdUtils.CharCode(state.Src, pos) == 0x28 /* ( */)
        {
            // Inline link

            // skipping spaces after '('
            pos++;
            for (; pos < max; pos++)
            {
                var code = MdUtils.CharCode(state.Src, pos);
                if (!MdUtils.IsSpace(code) && code != 0x0A) { break; }
            }
            if (pos >= max) { return false; }

            // parsing link destination
            var res = LinkHelpers.ParseLinkDestination(state.Src, pos, state.PosMax);
            if (res.Ok)
            {
                href = state.Md.NormalizeLink(res.Str);
                if (state.Md.ValidateLink(href))
                {
                    pos = res.Pos;
                }
                else
                {
                    href = "";
                }
            }

            // skipping spaces after destination
            var start = pos;
            for (; pos < max; pos++)
            {
                var code = MdUtils.CharCode(state.Src, pos);
                if (!MdUtils.IsSpace(code) && code != 0x0A) { break; }
            }

            // parsing link title
            var titleRes = LinkHelpers.ParseLinkTitle(state.Src, pos, state.PosMax);
            if (pos < max && start != pos && titleRes.Ok)
            {
                title = titleRes.Str;
                pos = titleRes.Pos;

                // skipping spaces after title
                for (; pos < max; pos++)
                {
                    var code = MdUtils.CharCode(state.Src, pos);
                    if (!MdUtils.IsSpace(code) && code != 0x0A) { break; }
                }
            }
            else
            {
                title = "";
            }

            if (pos >= max || MdUtils.CharCode(state.Src, pos) != 0x29 /* ) */)
            {
                state.Pos = oldPos;
                return false;
            }
            pos++;
        }
        else
        {
            // Link reference
            var references = state.Env.References;
            if (references == null) { return false; }

            if (pos < max && MdUtils.CharCode(state.Src, pos) == 0x5B /* [ */)
            {
                var start = pos + 1;
                pos = LinkHelpers.ParseLinkLabel(state, pos);
                if (pos >= 0)
                {
                    label = state.Src.Substring(start, pos - start);
                    pos++;
                }
                else
                {
                    pos = labelEnd + 1;
                }
            }
            else
            {
                pos = labelEnd + 1;
            }

            // covers collapsed reference links and shortcut reference links
            if (string.IsNullOrEmpty(label))
            {
                label = state.Src.Substring(labelStart, labelEnd - labelStart);
            }

            if (!references.TryGetValue(MdUtils.NormalizeReference(label), out var reference))
            {
                state.Pos = oldPos;
                return false;
            }
            href = reference.Href;
            title = reference.Title;
        }

        // We found the end of the link and know it's valid.
        if (!silent)
        {
            var content = state.Src.Substring(labelStart, labelEnd - labelStart);

            var tokens = new List<Token>();
            state.Md.Inline.Parse(content, state.Md, state.Env, tokens);

            var token = state.Push("image", "img", 0);
            var attrs = new List<string[]> { new[] { "src", href }, new[] { "alt", "" } };
            token.Attrs = attrs;
            token.Children = tokens;
            token.Content = content;

            if (!string.IsNullOrEmpty(title))
            {
                attrs.Add(new[] { "title", title });
            }
        }

        state.Pos = pos;
        state.PosMax = max;
        return true;
    }
}
