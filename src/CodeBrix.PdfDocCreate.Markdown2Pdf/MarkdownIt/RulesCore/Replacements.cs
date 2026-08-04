// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_core/replacements.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;

/// <summary>
/// Simple typographic replacements (typographer mode): (c) (r) (tm), +-, ellipses,
/// repeated punctuation, en/em dashes.
/// </summary>
internal static class Replacements
{
    private static readonly Regex RareRe = new Regex(@"\+-|\.\.|\?\?\?\?|!!!!|,,|--", RegexOptions.Compiled);
    private static readonly Regex ScopedAbbrTestRe = new Regex(@"\((c|tm|r)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ScopedAbbrRe = new Regex(@"\((c|tm|r)\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PlusMinusRe = new Regex(@"\+-", RegexOptions.Compiled);
    private static readonly Regex EllipsisRe = new Regex(@"\.{2,}", RegexOptions.Compiled);
    private static readonly Regex QuestionEllipsisRe = new Regex(@"([?!])…", RegexOptions.Compiled);
    private static readonly Regex RepeatedPunctRe = new Regex(@"([?!]){4,}", RegexOptions.Compiled);
    private static readonly Regex CommasRe = new Regex(@",{2,}", RegexOptions.Compiled);
    private static readonly Regex EmDashRe = new Regex(@"(^|[^-])---(?=[^-]|$)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex EnDashSpaceRe = new Regex(@"(^|\s)--(?=\s|$)", RegexOptions.Compiled | RegexOptions.Multiline);
    private static readonly Regex EnDashRe = new Regex(@"(^|[^-\s])--(?=[^-\s]|$)", RegexOptions.Compiled | RegexOptions.Multiline);

    private static string ScopedReplaceFn(Match match) => match.Groups[1].Value.ToLowerInvariant() switch
    {
        "c" => "©",
        "r" => "®",
        _ => "™",
    };

    private static void ReplaceScoped(List<Token> inlineTokens)
    {
        var insideAutolink = 0;

        for (var i = inlineTokens.Count - 1; i >= 0; i--)
        {
            var token = inlineTokens[i];

            if (token.Type == "text" && insideAutolink == 0)
            {
                token.Content = ScopedAbbrRe.Replace(token.Content, ScopedReplaceFn);
            }

            if (token.Type == "link_open" && token.Info == "auto") { insideAutolink--; }
            if (token.Type == "link_close" && token.Info == "auto") { insideAutolink++; }
        }
    }

    private static void ReplaceRare(List<Token> inlineTokens)
    {
        var insideAutolink = 0;

        for (var i = inlineTokens.Count - 1; i >= 0; i--)
        {
            var token = inlineTokens[i];

            if (token.Type == "text" && insideAutolink == 0 && RareRe.IsMatch(token.Content))
            {
                var content = PlusMinusRe.Replace(token.Content, "±");
                // .., ..., ....... -> … but ?..... & !..... -> ?.. & !..
                content = EllipsisRe.Replace(content, "…");
                content = QuestionEllipsisRe.Replace(content, "$1..");
                content = RepeatedPunctRe.Replace(content, "$1$1$1");
                content = CommasRe.Replace(content, ",");
                content = EmDashRe.Replace(content, "$1—");
                content = EnDashSpaceRe.Replace(content, "$1–");
                content = EnDashRe.Replace(content, "$1–");
                token.Content = content;
            }

            if (token.Type == "link_open" && token.Info == "auto") { insideAutolink--; }
            if (token.Type == "link_close" && token.Info == "auto") { insideAutolink++; }
        }
    }

    public static void Rule(StateCore state)
    {
        if (!state.Md.Options.Typographer) { return; }

        for (var blkIdx = state.Tokens.Count - 1; blkIdx >= 0; blkIdx--)
        {
            if (state.Tokens[blkIdx].Type != "inline") { continue; }

            if (ScopedAbbrTestRe.IsMatch(state.Tokens[blkIdx].Content))
            {
                ReplaceScoped(state.Tokens[blkIdx].Children);
            }

            if (RareRe.IsMatch(state.Tokens[blkIdx].Content))
            {
                ReplaceRare(state.Tokens[blkIdx].Children);
            }
        }
    }
}
