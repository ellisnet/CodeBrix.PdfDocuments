// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_block/html_block.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Text.RegularExpressions;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

/// <summary>HTML block.</summary>
internal static class HtmlBlock
{
    private sealed class HtmlSequence
    {
        public Regex Open;
        public Regex Close;
        public bool CanTerminateParagraph;
    }

    // Opening and corresponding closing sequences for html tags; the flag defines
    // whether the sequence can terminate a paragraph.
    private static readonly HtmlSequence[] HtmlSequences =
    {
        new HtmlSequence
        {
            Open = new Regex("^<(script|pre|style|textarea)(?=(\\s|>|$))", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            Close = new Regex("</(script|pre|style|textarea)>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            CanTerminateParagraph = true,
        },
        new HtmlSequence
        {
            Open = new Regex("^<!--", RegexOptions.Compiled),
            Close = new Regex("-->", RegexOptions.Compiled),
            CanTerminateParagraph = true,
        },
        new HtmlSequence
        {
            Open = new Regex("^<\\?", RegexOptions.Compiled),
            Close = new Regex("\\?>", RegexOptions.Compiled),
            CanTerminateParagraph = true,
        },
        new HtmlSequence
        {
            Open = new Regex("^<![A-Z]", RegexOptions.Compiled),
            Close = new Regex(">", RegexOptions.Compiled),
            CanTerminateParagraph = true,
        },
        new HtmlSequence
        {
            Open = new Regex("^<!\\[CDATA\\[", RegexOptions.Compiled),
            Close = new Regex("\\]\\]>", RegexOptions.Compiled),
            CanTerminateParagraph = true,
        },
        new HtmlSequence
        {
            Open = new Regex("^</?(" + string.Join("|", HtmlRe.BlockNames) + ")(?=(\\s|/?>|$))", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            Close = new Regex("^$", RegexOptions.Compiled),
            CanTerminateParagraph = true,
        },
        new HtmlSequence
        {
            Open = HtmlRe.HtmlOpenCloseTagLineRe,
            Close = new Regex("^$", RegexOptions.Compiled),
            CanTerminateParagraph = false,
        },
    };

    public static bool Rule(StateBlock state, int startLine, int endLine, bool silent)
    {
        var pos = state.BMarks[startLine] + state.TShift[startLine];
        var max = state.EMarks[startLine];

        // if it's indented more than 3 spaces, it should be a code block
        if (state.SCount[startLine] - state.BlkIndent >= 4) { return false; }

        if (!state.Md.Options.Html) { return false; }

        if (MdUtils.CharCode(state.Src, pos) != 0x3C /* < */) { return false; }

        var lineText = state.Src.Substring(pos, max - pos);

        var i = 0;
        for (; i < HtmlSequences.Length; i++)
        {
            if (HtmlSequences[i].Open.IsMatch(lineText)) { break; }
        }
        if (i == HtmlSequences.Length) { return false; }

        if (silent)
        {
            // true when this sequence can be a terminator
            return HtmlSequences[i].CanTerminateParagraph;
        }

        var nextLine = startLine + 1;

        // We detected an HTML block; roll down till the block end.
        if (!HtmlSequences[i].Close.IsMatch(lineText))
        {
            for (; nextLine < endLine; nextLine++)
            {
                if (state.SCount[nextLine] < state.BlkIndent) { break; }

                pos = state.BMarks[nextLine] + state.TShift[nextLine];
                max = state.EMarks[nextLine];
                lineText = state.Src.Substring(pos, max - pos);

                if (HtmlSequences[i].Close.IsMatch(lineText))
                {
                    if (lineText.Length != 0) { nextLine++; }
                    break;
                }
            }
        }

        state.Line = nextLine;

        var token = state.Push("html_block", "", 0);
        token.Map = new[] { startLine, nextLine };
        token.Content = state.GetLines(startLine, nextLine, state.BlkIndent, true);

        return true;
    }
}
