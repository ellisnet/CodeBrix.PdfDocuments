// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_core/{normalize,block,inline,
// linkify,text_join}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Text.RegularExpressions;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;

/// <summary>Normalizes the input string (line endings, NULL characters).</summary>
internal static class Normalize
{
    private static readonly Regex NewlinesRe = new Regex("\r\n?|\n", RegexOptions.Compiled);
    private static readonly Regex NullRe = new Regex("\0", RegexOptions.Compiled);

    public static void Rule(StateCore state)
    {
        var str = NewlinesRe.Replace(state.Src, "\n");
        str = NullRe.Replace(str, "�");
        state.Src = str;
    }
}

/// <summary>Runs the block parser (or wraps the source as one inline token).</summary>
internal static class BlockRule
{
    public static void Rule(StateCore state)
    {
        if (state.InlineMode)
        {
            var token = new Token("inline", "", 0)
            {
                Content = state.Src,
                Map = new[] { 0, 1 },
                Children = new System.Collections.Generic.List<Token>(),
            };
            state.Tokens.Add(token);
        }
        else
        {
            state.Md.Block.Parse(state.Src, state.Md, state.Env, state.Tokens);
        }
    }
}

/// <summary>Parses the content of every inline token.</summary>
internal static class InlineRule
{
    public static void Rule(StateCore state)
    {
        foreach (var token in state.Tokens)
        {
            if (token.Type == "inline")
            {
                state.Md.Inline.Parse(token.Content, state.Md, state.Env, token.Children);
            }
        }
    }
}

/// <summary>
/// Upstream replaces link-like text with link nodes via linkify-it. The linkify-it
/// library is not part of this port, so the rule is inert; the option is accepted for
/// compatibility.
/// </summary>
internal static class LinkifyRule
{
    public static void Rule(StateCore state)
    {
        if (!state.Md.Options.Linkify) { return; }
        // linkify-it is not ported - bare-URL autolinking does not occur.
    }
}

/// <summary>
/// Joins raw text tokens with the rest of the text. text_special tokens (escape
/// sequences) become plain text first, then adjacent text nodes collapse.
/// </summary>
internal static class TextJoin
{
    public static void Rule(StateCore state)
    {
        var blockTokens = state.Tokens;

        foreach (var blockToken in blockTokens)
        {
            if (blockToken.Type != "inline") { continue; }

            var tokens = blockToken.Children;
            var max = tokens.Count;

            for (var curr = 0; curr < max; curr++)
            {
                if (tokens[curr].Type == "text_special") { tokens[curr].Type = "text"; }
            }

            int last;
            int position;
            for (position = last = 0; position < max; position++)
            {
                if (tokens[position].Type == "text"
                    && position + 1 < max
                    && tokens[position + 1].Type == "text")
                {
                    // collapse two adjacent text nodes
                    tokens[position + 1].Content = tokens[position].Content + tokens[position + 1].Content;
                }
                else
                {
                    if (position != last) { tokens[last] = tokens[position]; }
                    last++;
                }
            }

            if (position != last)
            {
                tokens.RemoveRange(last, tokens.Count - last);
            }
        }
    }
}
