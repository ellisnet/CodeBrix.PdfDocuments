// ============================================================================
// C# port of markdown-it v14.1.0 - lib/parser_block.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>Block rule delegate: returns true when the rule consumed input.</summary>
public delegate bool BlockRuleFn(StateBlock state, int startLine, int endLine, bool silent);

/// <summary>Block-level tokenizer.</summary>
public sealed class ParserBlock
{
    /// <summary>Creates the block chain with the default rules.</summary>
    public ParserBlock()
    {
        // Second array element: list of rules which can be terminated by this one.
        Ruler.Push("table", Table.Rule, new[] { "paragraph", "reference" });
        Ruler.Push("code", Code.Rule);
        Ruler.Push("fence", Fence.Rule, new[] { "paragraph", "reference", "blockquote", "list" });
        Ruler.Push("blockquote", Blockquote.Rule, new[] { "paragraph", "reference", "blockquote", "list" });
        Ruler.Push("hr", Hr.Rule, new[] { "paragraph", "reference", "blockquote", "list" });
        Ruler.Push("list", ListRule.Rule, new[] { "paragraph", "reference", "blockquote" });
        Ruler.Push("reference", Reference.Rule);
        Ruler.Push("html_block", HtmlBlock.Rule, new[] { "paragraph", "reference", "blockquote" });
        Ruler.Push("heading", Heading.Rule, new[] { "paragraph", "reference", "blockquote" });
        Ruler.Push("lheading", LHeading.Rule);
        Ruler.Push("paragraph", Paragraph.Rule);
    }

    /// <summary>Keeps the configuration of block rules.</summary>
    public Ruler<BlockRuleFn> Ruler { get; } = new Ruler<BlockRuleFn>();

    /// <summary>Generates tokens for the input range.</summary>
    public void Tokenize(StateBlock state, int startLine, int endLine)
    {
        var rules = Ruler.GetRules("");
        var len = rules.Length;
        var maxNesting = state.Md.Options.MaxNesting;
        var line = startLine;
        var hasEmptyLines = false;

        while (line < endLine)
        {
            state.Line = line = state.SkipEmptyLines(line);
            if (line >= endLine) { break; }

            // Termination condition for nested calls (blockquotes & lists).
            if (state.SCount[line] < state.BlkIndent) { break; }

            // If nesting level exceeded - skip tail to the end.
            if (state.Level >= maxNesting)
            {
                state.Line = endLine;
                break;
            }

            // Try all possible rules. On success, a rule updates state.Line and
            // state.Tokens, then returns true.
            var prevLine = state.Line;
            var ok = false;

            for (var i = 0; i < len; i++)
            {
                ok = rules[i](state, line, endLine, false);
                if (ok)
                {
                    if (prevLine >= state.Line)
                    {
                        throw new InvalidOperationException("block rule didn't increment state.line");
                    }
                    break;
                }
            }

            // this can only happen if the user disables the paragraph rule
            if (!ok) { throw new InvalidOperationException("none of the block rules matched"); }

            // set state.Tight if we had an empty line before the current tag, i.e. the
            // latest empty line should not count
            state.Tight = !hasEmptyLines;

            // paragraph might "eat" one newline after it in nested lists
            if (state.IsEmpty(state.Line - 1)) { hasEmptyLines = true; }

            line = state.Line;

            if (line < endLine && state.IsEmpty(line))
            {
                hasEmptyLines = true;
                line++;
                state.Line = line;
            }
        }
    }

    /// <summary>Processes an input string and pushes block tokens into outTokens.</summary>
    public void Parse(string src, MarkdownParser md, MdEnv env, List<Token> outTokens)
    {
        if (string.IsNullOrEmpty(src)) { return; }

        var state = new StateBlock(src, md, env, outTokens);
        Tokenize(state, state.Line, state.LineMax);
    }
}
