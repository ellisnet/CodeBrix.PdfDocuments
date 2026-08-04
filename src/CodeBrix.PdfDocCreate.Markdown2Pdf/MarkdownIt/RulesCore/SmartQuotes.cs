// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_core/smartquotes.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using System.Text.RegularExpressions;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;

/// <summary>Converts straight quotation marks to typographic ones (typographer mode).</summary>
internal static class SmartQuotes
{
    private static readonly Regex QuoteTestRe = new Regex("['\"]", RegexOptions.Compiled);
    private static readonly Regex QuoteRe = new Regex("['\"]", RegexOptions.Compiled);
    private const string Apostrophe = "’";

    private sealed class QuoteItem
    {
        public int Token;
        public int Pos;
        public bool Single;
        public int Level;
    }

    private static string ReplaceAt(string str, int index, string ch) =>
        str.Substring(0, index) + ch + str.Substring(index + 1);

    private static void ProcessInlines(List<Token> tokens, StateCore state)
    {
        var stack = new List<QuoteItem>();

        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            var thisLevel = tokens[i].Level;

            int j;
            for (j = stack.Count - 1; j >= 0; j--)
            {
                if (stack[j].Level <= thisLevel) { break; }
            }
            if (stack.Count > j + 1) { stack.RemoveRange(j + 1, stack.Count - (j + 1)); }

            if (token.Type != "text") { continue; }

            var text = token.Content;
            var pos = 0;
            var max = text.Length;

            while (pos < max)
            {
                var t = QuoteRe.Match(text, pos);
                if (!t.Success) { break; }

                var canOpen = true;
                var canClose = true;
                pos = t.Index + 1;
                var isSingle = t.Value == "'";

                // Find the previous character; default to space at line start.
                var lastChar = 0x20;
                if (t.Index - 1 >= 0)
                {
                    lastChar = text[t.Index - 1];
                }
                else
                {
                    for (j = i - 1; j >= 0; j--)
                    {
                        if (tokens[j].Type == "softbreak" || tokens[j].Type == "hardbreak") { break; }
                        if (string.IsNullOrEmpty(tokens[j].Content)) { continue; }
                        lastChar = tokens[j].Content[tokens[j].Content.Length - 1];
                        break;
                    }
                }

                // Find the next character; default to space at line end.
                var nextChar = 0x20;
                if (pos < max)
                {
                    nextChar = text[pos];
                }
                else
                {
                    for (j = i + 1; j < tokens.Count; j++)
                    {
                        if (tokens[j].Type == "softbreak" || tokens[j].Type == "hardbreak") { break; }
                        if (string.IsNullOrEmpty(tokens[j].Content)) { continue; }
                        nextChar = tokens[j].Content[0];
                        break;
                    }
                }

                var isLastPunctChar = MdUtils.IsMdAsciiPunct(lastChar) || MdUtils.IsPunctChar((char)lastChar);
                var isNextPunctChar = MdUtils.IsMdAsciiPunct(nextChar) || MdUtils.IsPunctChar((char)nextChar);
                var isLastWhiteSpace = MdUtils.IsWhiteSpace(lastChar);
                var isNextWhiteSpace = MdUtils.IsWhiteSpace(nextChar);

                if (isNextWhiteSpace)
                {
                    canOpen = false;
                }
                else if (isNextPunctChar && !(isLastWhiteSpace || isLastPunctChar))
                {
                    canOpen = false;
                }

                if (isLastWhiteSpace)
                {
                    canClose = false;
                }
                else if (isLastPunctChar && !(isNextWhiteSpace || isNextPunctChar))
                {
                    canClose = false;
                }

                if (nextChar == 0x22 /* " */ && t.Value == "\""
                    && lastChar >= 0x30 && lastChar <= 0x39)
                {
                    // special case: 1"" - count first quote as an inch
                    canClose = canOpen = false;
                }

                if (canOpen && canClose)
                {
                    // Replace quotes in the middle of punctuation sequences, but not in
                    // the middle of words.
                    canOpen = isLastPunctChar;
                    canClose = isNextPunctChar;
                }

                if (!canOpen && !canClose)
                {
                    // middle of word
                    if (isSingle)
                    {
                        token.Content = ReplaceAt(token.Content, t.Index, Apostrophe);
                        text = token.Content;
                        max = text.Length;
                    }
                    continue;
                }

                var matchedInStack = false;
                if (canClose)
                {
                    // this could be a closing quote; rewind the stack for a match
                    for (j = stack.Count - 1; j >= 0; j--)
                    {
                        var item = stack[j];
                        if (stack[j].Level < thisLevel) { break; }
                        if (item.Single == isSingle && stack[j].Level == thisLevel)
                        {
                            string openQuote;
                            string closeQuote;
                            var quotes = state.Md.Options.Quotes;
                            if (isSingle)
                            {
                                openQuote = quotes[2].ToString();
                                closeQuote = quotes[3].ToString();
                            }
                            else
                            {
                                openQuote = quotes[0].ToString();
                                closeQuote = quotes[1].ToString();
                            }

                            // replace token.Content before tokens[item.Token].Content -
                            // if they point at the same token, replacing in the other
                            // order could shift indices when quote length != 1
                            token.Content = ReplaceAt(token.Content, t.Index, closeQuote);
                            tokens[item.Token].Content = ReplaceAt(tokens[item.Token].Content, item.Pos, openQuote);

                            pos += closeQuote.Length - 1;
                            if (item.Token == i) { pos += openQuote.Length - 1; }

                            text = token.Content;
                            max = text.Length;

                            stack.RemoveRange(j, stack.Count - j);
                            matchedInStack = true;
                            break;
                        }
                    }
                }

                if (matchedInStack) { continue; }

                if (canOpen)
                {
                    stack.Add(new QuoteItem { Token = i, Pos = t.Index, Single = isSingle, Level = thisLevel });
                }
                else if (canClose && isSingle)
                {
                    token.Content = ReplaceAt(token.Content, t.Index, Apostrophe);
                    text = token.Content;
                    max = text.Length;
                }
            }
        }
    }

    public static void Rule(StateCore state)
    {
        if (!state.Md.Options.Typographer) { return; }

        for (var blkIdx = state.Tokens.Count - 1; blkIdx >= 0; blkIdx--)
        {
            if (state.Tokens[blkIdx].Type != "inline"
                || !QuoteTestRe.IsMatch(state.Tokens[blkIdx].Content))
            {
                continue;
            }

            ProcessInlines(state.Tokens[blkIdx].Children, state);
        }
    }
}
