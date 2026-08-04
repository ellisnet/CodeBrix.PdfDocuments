// ============================================================================
// C# port of markdown-it-footnote v4.0.0 (MIT License, copyright (c) 2014-2015
// Vitaly Puzrin, Alex Kocharin). https://github.com/markdown-it/markdown-it-footnote
// ============================================================================

using System.Collections.Generic;
using System.Globalization;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Helpers;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;

/// <summary>
/// Footnotes: [^label] references, [^label]: definitions, and inline ^[notes]; the
/// collected notes render as an ordered list at the end of the document.
/// </summary>
public static class FootnotePlugin
{
    private sealed class FootnoteEntry
    {
        public string Label;
        public int Count;
        public string Content;
        public List<Token> Tokens;
    }

    private sealed class FootnoteState
    {
        public readonly Dictionary<string, int> Refs = new Dictionary<string, int>(System.StringComparer.Ordinal);
        public readonly List<FootnoteEntry> List = new List<FootnoteEntry>();
        public bool HasRefs;
    }

    private static FootnoteState GetState(MdEnv env, bool create)
    {
        if (env.TryGetValue("footnotes", out var existing) && existing is FootnoteState state)
        {
            return state;
        }
        if (!create) { return null; }
        var created = new FootnoteState();
        env["footnotes"] = created;
        return created;
    }

    private static int MetaInt(Token token, string key) =>
        token.Meta != null && token.Meta.TryGetValue(key, out var value) && value is int number ? number : 0;

    private static string MetaLabel(Token token) =>
        token.Meta != null && token.Meta.TryGetValue("label", out var value) ? value as string : null;

    // ---- renderer partials ----------------------------------------------------

    private static string AnchorName(List<Token> tokens, int idx, MdEnv env)
    {
        var n = (MetaInt(tokens[idx], "id") + 1).ToString(CultureInfo.InvariantCulture);
        var prefix = "";
        if (env.TryGetValue("docId", out var docId) && docId is string docIdString)
        {
            prefix = $"-{docIdString}-";
        }
        return prefix + n;
    }

    private static string Caption(List<Token> tokens, int idx)
    {
        var n = (MetaInt(tokens[idx], "id") + 1).ToString(CultureInfo.InvariantCulture);
        var subId = MetaInt(tokens[idx], "subId");
        if (subId > 0) { n += ":" + subId; }
        return "[" + n + "]";
    }

    private static string RenderRef(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self)
    {
        var id = AnchorName(tokens, idx, env);
        var caption = Caption(tokens, idx);
        var refid = id;
        var subId = MetaInt(tokens[idx], "subId");
        if (subId > 0) { refid += ":" + subId; }
        return $"<sup class=\"footnote-ref\"><a href=\"#fn{id}\" id=\"fnref{refid}\">{caption}</a></sup>";
    }

    private static string RenderBlockOpen(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        (options.XhtmlOut ? "<hr class=\"footnotes-sep\" />\n" : "<hr class=\"footnotes-sep\">\n")
        + "<section class=\"footnotes\">\n<ol class=\"footnotes-list\">\n";

    private static string RenderBlockClose(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        "</ol>\n</section>\n";

    private static string RenderOpen(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self)
    {
        var id = AnchorName(tokens, idx, env);
        var subId = MetaInt(tokens[idx], "subId");
        if (subId > 0) { id += ":" + subId; }
        return $"<li id=\"fn{id}\" class=\"footnote-item\">";
    }

    private static string RenderClose(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        "</li>\n";

    private static string RenderAnchor(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self)
    {
        var id = AnchorName(tokens, idx, env);
        var subId = MetaInt(tokens[idx], "subId");
        if (subId > 0) { id += ":" + subId; }
        // U+21A9 + variation selector to prevent emoji presentation
        return $" <a href=\"#fnref{id}\" class=\"footnote-backref\">↩︎</a>";
    }

    /// <summary>Installs the plugin into a parser.</summary>
    public static void Apply(MarkdownParser md)
    {
        md.Renderer.Rules["footnote_ref"] = RenderRef;
        md.Renderer.Rules["footnote_block_open"] = RenderBlockOpen;
        md.Renderer.Rules["footnote_block_close"] = RenderBlockClose;
        md.Renderer.Rules["footnote_open"] = RenderOpen;
        md.Renderer.Rules["footnote_close"] = RenderClose;
        md.Renderer.Rules["footnote_anchor"] = RenderAnchor;

        md.Block.Ruler.Before("reference", "footnote_def", FootnoteDef, new[] { "paragraph", "reference" });
        md.Inline.Ruler.After("image", "footnote_inline", FootnoteInline);
        md.Inline.Ruler.After("footnote_inline", "footnote_ref", FootnoteRef);
        md.Core.Ruler.After("inline", "footnote_tail", FootnoteTail);
    }

    // ---- block rule: footnote definition --------------------------------------

    private static bool FootnoteDef(StateBlock state, int startLine, int endLine, bool silent)
    {
        var start = state.BMarks[startLine] + state.TShift[startLine];
        var max = state.EMarks[startLine];

        // line should be at least 5 chars - "[^x]:"
        if (start + 4 > max) { return false; }

        if (MdUtils.CharCode(state.Src, start) != 0x5B /* [ */) { return false; }
        if (MdUtils.CharCode(state.Src, start + 1) != 0x5E /* ^ */) { return false; }

        int pos;
        for (pos = start + 2; pos < max; pos++)
        {
            if (MdUtils.CharCode(state.Src, pos) == 0x20) { return false; }
            if (MdUtils.CharCode(state.Src, pos) == 0x5D /* ] */) { break; }
        }

        if (pos == start + 2) { return false; } // no empty footnote labels
        if (pos + 1 >= max || MdUtils.CharCode(state.Src, ++pos) != 0x3A /* : */) { return false; }
        if (silent) { return true; }
        pos++;

        var footnotes = GetState(state.Env, create: true);
        footnotes.HasRefs = true;
        var label = state.Src.Substring(start + 2, pos - 2 - (start + 2));
        footnotes.Refs[":" + label] = -1;

        var tokenFrefO = new Token("footnote_reference_open", "", 1)
        {
            Meta = new Dictionary<string, object> { ["label"] = label },
            Level = state.Level++,
        };
        state.Tokens.Add(tokenFrefO);

        var oldBMark = state.BMarks[startLine];
        var oldTShift = state.TShift[startLine];
        var oldSCount = state.SCount[startLine];
        var oldParentType = state.ParentType;

        var posAfterColon = pos;
        var initial = state.SCount[startLine] + pos - (state.BMarks[startLine] + state.TShift[startLine]);
        var offset = initial;

        while (pos < max)
        {
            var ch = MdUtils.CharCode(state.Src, pos);

            if (MdUtils.IsSpace(ch))
            {
                if (ch == 0x09) { offset += 4 - offset % 4; }
                else { offset++; }
            }
            else
            {
                break;
            }

            pos++;
        }

        state.TShift[startLine] = pos - posAfterColon;
        state.SCount[startLine] = offset - initial;

        state.BMarks[startLine] = posAfterColon;
        state.BlkIndent += 4;
        state.ParentType = "footnote";

        if (state.SCount[startLine] < state.BlkIndent)
        {
            state.SCount[startLine] += state.BlkIndent;
        }

        state.Md.Block.Tokenize(state, startLine, endLine);

        state.ParentType = oldParentType;
        state.BlkIndent -= 4;
        state.TShift[startLine] = oldTShift;
        state.SCount[startLine] = oldSCount;
        state.BMarks[startLine] = oldBMark;

        var tokenFrefC = new Token("footnote_reference_close", "", -1)
        {
            Level = --state.Level,
        };
        state.Tokens.Add(tokenFrefC);

        return true;
    }

    // ---- inline rule: inline footnotes ^[...] ---------------------------------

    private static bool FootnoteInline(StateInline state, bool silent)
    {
        var max = state.PosMax;
        var start = state.Pos;

        if (start + 2 >= max) { return false; }
        if (MdUtils.CharCode(state.Src, start) != 0x5E /* ^ */) { return false; }
        if (MdUtils.CharCode(state.Src, start + 1) != 0x5B /* [ */) { return false; }

        var labelStart = start + 2;
        var labelEnd = LinkHelpers.ParseLinkLabel(state, start + 1);

        // parser failed to find ']', so it's not a valid note
        if (labelEnd < 0) { return false; }

        if (!silent)
        {
            var footnotes = GetState(state.Env, create: true);
            var footnoteId = footnotes.List.Count;
            var tokens = new List<Token>();

            state.Md.Inline.Parse(
                state.Src.Substring(labelStart, labelEnd - labelStart),
                state.Md, state.Env, tokens);

            var token = state.Push("footnote_ref", "", 0);
            token.Meta = new Dictionary<string, object> { ["id"] = footnoteId };

            footnotes.List.Add(new FootnoteEntry
            {
                Content = state.Src.Substring(labelStart, labelEnd - labelStart),
                Tokens = tokens,
            });
        }

        state.Pos = labelEnd + 1;
        state.PosMax = max;
        return true;
    }

    // ---- inline rule: footnote references [^...] ------------------------------

    private static bool FootnoteRef(StateInline state, bool silent)
    {
        var max = state.PosMax;
        var start = state.Pos;

        // should be at least 4 chars - "[^x]"
        if (start + 3 > max) { return false; }

        var footnotes = GetState(state.Env, create: false);
        if (footnotes == null || !footnotes.HasRefs) { return false; }
        if (MdUtils.CharCode(state.Src, start) != 0x5B /* [ */) { return false; }
        if (MdUtils.CharCode(state.Src, start + 1) != 0x5E /* ^ */) { return false; }

        int pos;
        for (pos = start + 2; pos < max; pos++)
        {
            if (MdUtils.CharCode(state.Src, pos) == 0x20) { return false; }
            if (MdUtils.CharCode(state.Src, pos) == 0x0A) { return false; }
            if (MdUtils.CharCode(state.Src, pos) == 0x5D /* ] */) { break; }
        }

        if (pos == start + 2) { return false; } // no empty footnote labels
        if (pos >= max) { return false; }
        pos++;

        var label = state.Src.Substring(start + 2, pos - 1 - (start + 2));
        if (!footnotes.Refs.TryGetValue(":" + label, out var refId)) { return false; }

        if (!silent)
        {
            int footnoteId;

            if (refId < 0)
            {
                footnoteId = footnotes.List.Count;
                footnotes.List.Add(new FootnoteEntry { Label = label });
                footnotes.Refs[":" + label] = footnoteId;
            }
            else
            {
                footnoteId = refId;
            }

            var footnoteSubId = footnotes.List[footnoteId].Count;
            footnotes.List[footnoteId].Count++;

            var token = state.Push("footnote_ref", "", 0);
            token.Meta = new Dictionary<string, object>
            {
                ["id"] = footnoteId,
                ["subId"] = footnoteSubId,
                ["label"] = label,
            };
        }

        state.Pos = pos;
        state.PosMax = max;
        return true;
    }

    // ---- core rule: glue footnote tokens to the end of the stream --------------

    private static void FootnoteTail(StateCore state)
    {
        var insideRef = false;
        List<Token> current = null;
        string currentLabel = null;
        var refTokens = new Dictionary<string, List<Token>>(System.StringComparer.Ordinal);

        var footnotes = GetState(state.Env, create: false);
        if (footnotes == null) { return; }

        var filtered = new List<Token>(state.Tokens.Count);
        foreach (var tok in state.Tokens)
        {
            if (tok.Type == "footnote_reference_open")
            {
                insideRef = true;
                current = new List<Token>();
                currentLabel = MetaLabel(tok);
                continue;
            }
            if (tok.Type == "footnote_reference_close")
            {
                insideRef = false;
                refTokens[":" + currentLabel] = current;
                continue;
            }
            if (insideRef) { current.Add(tok); }
            else { filtered.Add(tok); }
        }
        state.Tokens.Clear();
        state.Tokens.AddRange(filtered);

        if (footnotes.List.Count == 0) { return; }
        var list = footnotes.List;

        state.Tokens.Add(new Token("footnote_block_open", "", 1));

        for (var i = 0; i < list.Count; i++)
        {
            var tokenFo = new Token("footnote_open", "", 1)
            {
                Meta = new Dictionary<string, object> { ["id"] = i, ["label"] = list[i].Label },
            };
            state.Tokens.Add(tokenFo);

            List<Token> tokens = null;
            if (list[i].Tokens != null)
            {
                tokens = new List<Token>();

                var tokenPo = new Token("paragraph_open", "p", 1) { Block = true };
                tokens.Add(tokenPo);

                var tokenI = new Token("inline", "", 0)
                {
                    Children = list[i].Tokens,
                    Content = list[i].Content,
                };
                tokens.Add(tokenI);

                var tokenPc = new Token("paragraph_close", "p", -1) { Block = true };
                tokens.Add(tokenPc);
            }
            else if (list[i].Label != null)
            {
                refTokens.TryGetValue(":" + list[i].Label, out tokens);
            }

            if (tokens != null) { state.Tokens.AddRange(tokens); }

            Token lastParagraph = null;
            if (state.Tokens[state.Tokens.Count - 1].Type == "paragraph_close")
            {
                lastParagraph = state.Tokens[state.Tokens.Count - 1];
                state.Tokens.RemoveAt(state.Tokens.Count - 1);
            }

            var t = list[i].Count > 0 ? list[i].Count : 1;
            for (var j = 0; j < t; j++)
            {
                var tokenA = new Token("footnote_anchor", "", 0)
                {
                    Meta = new Dictionary<string, object>
                    {
                        ["id"] = i,
                        ["subId"] = j,
                        ["label"] = list[i].Label,
                    },
                };
                state.Tokens.Add(tokenA);
            }

            if (lastParagraph != null)
            {
                state.Tokens.Add(lastParagraph);
            }

            state.Tokens.Add(new Token("footnote_close", "", -1));
        }

        state.Tokens.Add(new Token("footnote_block_close", "", -1));
    }
}
