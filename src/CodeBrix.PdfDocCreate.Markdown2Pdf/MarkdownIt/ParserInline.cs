// ============================================================================
// C# port of markdown-it v14.1.0 - lib/parser_inline.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>Inline rule delegate: returns true when the rule consumed input.</summary>
public delegate bool InlineRuleFn(StateInline state, bool silent);

/// <summary>Inline post-processing (ruler2) rule delegate.</summary>
public delegate void InlineRule2Fn(StateInline state);

/// <summary>Tokenizes paragraph content.</summary>
public sealed class ParserInline
{
    /// <summary>Creates the inline chains with the default rules.</summary>
    public ParserInline()
    {
        Ruler.Push("text", TextRule.Rule);
        Ruler.Push("linkify", LinkifyInline.Rule);
        Ruler.Push("newline", Newline.Rule);
        Ruler.Push("escape", EscapeRule.Rule);
        Ruler.Push("backticks", Backticks.Rule);
        Ruler.Push("strikethrough", Strikethrough.Tokenize);
        Ruler.Push("emphasis", Emphasis.Tokenize);
        Ruler.Push("link", LinkRule.Rule);
        Ruler.Push("image", ImageRule.Rule);
        Ruler.Push("autolink", Autolink.Rule);
        Ruler.Push("html_inline", HtmlInline.Rule);
        Ruler.Push("entity", Entity.Rule);

        // The rule2 ruleset was created specifically for emphasis/strikethrough
        // post-processing; don't use it for anything except pairs.
        Ruler2.Push("balance_pairs", BalancePairs.Rule);
        Ruler2.Push("strikethrough", Strikethrough.PostProcess);
        Ruler2.Push("emphasis", Emphasis.PostProcess);
        // rules for pairs separate '**' into its own text tokens, which may be left
        // unused - the rule below merges unused segments back with the rest of the text
        Ruler2.Push("fragments_join", FragmentsJoin.Rule);
    }

    /// <summary>Keeps the configuration of inline rules.</summary>
    public Ruler<InlineRuleFn> Ruler { get; } = new Ruler<InlineRuleFn>();

    /// <summary>Second ruler, used for post-processing (emphasis-like rules).</summary>
    public Ruler<InlineRule2Fn> Ruler2 { get; } = new Ruler<InlineRule2Fn>();

    /// <summary>
    /// Skips a single token by running all rules in validation mode; used by
    /// link-label parsing.
    /// </summary>
    public void SkipToken(StateInline state)
    {
        var pos = state.Pos;
        var rules = Ruler.GetRules("");
        var len = rules.Length;
        var maxNesting = state.Md.Options.MaxNesting;
        var cache = state.Cache;

        if (cache.TryGetValue(pos, out var cached))
        {
            state.Pos = cached;
            return;
        }

        var ok = false;

        if (state.Level < maxNesting)
        {
            for (var i = 0; i < len; i++)
            {
                // Increment state.Level and decrement it later to limit recursion.
                // Harmless here because no tokens are created.
                state.Level++;
                ok = rules[i](state, true);
                state.Level--;

                if (ok)
                {
                    if (pos >= state.Pos) { throw new InvalidOperationException("inline rule didn't increment state.pos"); }
                    break;
                }
            }
        }
        else
        {
            // Too much nesting; just skip until the end of the paragraph.
            state.Pos = state.PosMax;
        }

        if (!ok) { state.Pos++; }
        cache[pos] = state.Pos;
    }

    /// <summary>Generates tokens for the input range.</summary>
    public void Tokenize(StateInline state)
    {
        var rules = Ruler.GetRules("");
        var len = rules.Length;
        var end = state.PosMax;
        var maxNesting = state.Md.Options.MaxNesting;

        while (state.Pos < end)
        {
            // Try all possible rules. On success, a rule updates state.Pos and
            // state.Tokens, then returns true.
            var prevPos = state.Pos;
            var ok = false;

            if (state.Level < maxNesting)
            {
                for (var i = 0; i < len; i++)
                {
                    ok = rules[i](state, false);
                    if (ok)
                    {
                        if (prevPos >= state.Pos) { throw new InvalidOperationException("inline rule didn't increment state.pos"); }
                        break;
                    }
                }
            }

            if (ok)
            {
                if (state.Pos >= end) { break; }
                continue;
            }

            state.Pending += state.Src[state.Pos++];
        }

        if (state.Pending.Length > 0) { state.PushPending(); }
    }

    /// <summary>Processes an input string and pushes inline tokens into outTokens.</summary>
    public void Parse(string str, MarkdownParser md, MdEnv env, List<Token> outTokens)
    {
        var state = new StateInline(str, md, env, outTokens);

        Tokenize(state);

        foreach (var rule in Ruler2.GetRules(""))
        {
            rule(state);
        }
    }
}
