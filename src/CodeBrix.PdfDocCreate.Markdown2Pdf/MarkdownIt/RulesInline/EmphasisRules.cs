// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_inline/{emphasis,strikethrough,
// balance_pairs,fragments_join}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

/// <summary>Processes *this* and _that_.</summary>
internal static class Emphasis
{
    /// <summary>Inserts each marker as a separate text token and records delimiters.</summary>
    public static bool Tokenize(StateInline state, bool silent)
    {
        var start = state.Pos;
        var marker = MdUtils.CharCode(state.Src, start);

        if (silent) { return false; }

        if (marker != 0x5F /* _ */ && marker != 0x2A /* * */) { return false; }

        var scanned = state.ScanDelims(state.Pos, marker == 0x2A);

        for (var i = 0; i < scanned.Length; i++)
        {
            var token = state.Push("text", "", 0);
            token.Content = ((char)marker).ToString();

            state.Delimiters.Add(new Delimiter
            {
                Marker = marker,
                Length = scanned.Length,
                Token = state.Tokens.Count - 1,
                End = -1,
                Open = scanned.CanOpen,
                Close = scanned.CanClose,
            });
        }

        state.Pos += scanned.Length;

        return true;
    }

    private static void Process(StateInline state, List<Delimiter> delimiters)
    {
        var max = delimiters.Count;

        for (var i = max - 1; i >= 0; i--)
        {
            var startDelim = delimiters[i];

            if (startDelim.Marker != 0x5F /* _ */ && startDelim.Marker != 0x2A /* * */)
            {
                continue;
            }

            // Process only opening markers
            if (startDelim.End == -1) { continue; }

            var endDelim = delimiters[startDelim.End];

            // If the previous delimiter has the same marker and is adjacent, merge into
            // one strong delimiter: <em><em>x</em></em> -> <strong>x</strong>
            var isStrong = i > 0
                && delimiters[i - 1].End == startDelim.End + 1
                && delimiters[i - 1].Marker == startDelim.Marker
                && delimiters[i - 1].Token == startDelim.Token - 1
                && delimiters[startDelim.End + 1].Token == endDelim.Token + 1;

            var ch = ((char)startDelim.Marker).ToString();

            var tokenO = state.Tokens[startDelim.Token];
            tokenO.Type = isStrong ? "strong_open" : "em_open";
            tokenO.Tag = isStrong ? "strong" : "em";
            tokenO.Nesting = 1;
            tokenO.Markup = isStrong ? ch + ch : ch;
            tokenO.Content = "";

            var tokenC = state.Tokens[endDelim.Token];
            tokenC.Type = isStrong ? "strong_close" : "em_close";
            tokenC.Tag = isStrong ? "strong" : "em";
            tokenC.Nesting = -1;
            tokenC.Markup = isStrong ? ch + ch : ch;
            tokenC.Content = "";

            if (isStrong)
            {
                state.Tokens[delimiters[i - 1].Token].Content = "";
                state.Tokens[delimiters[startDelim.End + 1].Token].Content = "";
                i--;
            }
        }
    }

    /// <summary>Walks the delimiter lists and replaces text tokens with tags.</summary>
    public static void PostProcess(StateInline state)
    {
        Process(state, state.Delimiters);

        foreach (var meta in state.TokensMeta)
        {
            if (meta?.Delimiters != null)
            {
                Process(state, meta.Delimiters);
            }
        }
    }
}

/// <summary>~~strike through~~.</summary>
internal static class Strikethrough
{
    /// <summary>Inserts each marker pair as a separate text token and records delimiters.</summary>
    public static bool Tokenize(StateInline state, bool silent)
    {
        var start = state.Pos;
        var marker = MdUtils.CharCode(state.Src, start);

        if (silent) { return false; }

        if (marker != 0x7E /* ~ */) { return false; }

        var scanned = state.ScanDelims(state.Pos, true);
        var len = scanned.Length;
        var ch = ((char)marker).ToString();

        if (len < 2) { return false; }

        Token token;

        if (len % 2 != 0)
        {
            token = state.Push("text", "", 0);
            token.Content = ch;
            len--;
        }

        for (var i = 0; i < len; i += 2)
        {
            token = state.Push("text", "", 0);
            token.Content = ch + ch;

            state.Delimiters.Add(new Delimiter
            {
                Marker = marker,
                Length = 0, // disable "rule of 3" length checks meant for emphasis
                Token = state.Tokens.Count - 1,
                End = -1,
                Open = scanned.CanOpen,
                Close = scanned.CanClose,
            });
        }

        state.Pos += scanned.Length;

        return true;
    }

    private static void Process(StateInline state, List<Delimiter> delimiters)
    {
        Token token;
        var loneMarkers = new List<int>();
        var max = delimiters.Count;

        for (var i = 0; i < max; i++)
        {
            var startDelim = delimiters[i];

            if (startDelim.Marker != 0x7E /* ~ */) { continue; }
            if (startDelim.End == -1) { continue; }

            var endDelim = delimiters[startDelim.End];

            token = state.Tokens[startDelim.Token];
            token.Type = "s_open";
            token.Tag = "s";
            token.Nesting = 1;
            token.Markup = "~~";
            token.Content = "";

            token = state.Tokens[endDelim.Token];
            token.Type = "s_close";
            token.Tag = "s";
            token.Nesting = -1;
            token.Markup = "~~";
            token.Content = "";

            if (state.Tokens[endDelim.Token - 1].Type == "text"
                && state.Tokens[endDelim.Token - 1].Content == "~")
            {
                loneMarkers.Add(endDelim.Token - 1);
            }
        }

        // An odd marker sequence splits like `~~~~~` -> `~` + `~~` + `~~`, leaving one
        // marker at the start; move those markers after subsequent s_close tags.
        while (loneMarkers.Count > 0)
        {
            var i = loneMarkers[loneMarkers.Count - 1];
            loneMarkers.RemoveAt(loneMarkers.Count - 1);
            var j = i + 1;

            while (j < state.Tokens.Count && state.Tokens[j].Type == "s_close")
            {
                j++;
            }

            j--;

            if (i != j)
            {
                token = state.Tokens[j];
                state.Tokens[j] = state.Tokens[i];
                state.Tokens[i] = token;
            }
        }
    }

    /// <summary>Walks the delimiter lists and replaces text tokens with tags.</summary>
    public static void PostProcess(StateInline state)
    {
        Process(state, state.Delimiters);

        foreach (var meta in state.TokensMeta)
        {
            if (meta?.Delimiters != null)
            {
                Process(state, meta.Delimiters);
            }
        }
    }
}

/// <summary>For each opening emphasis-like marker finds a matching closing one.</summary>
internal static class BalancePairs
{
    private static void ProcessDelimiters(List<Delimiter> delimiters)
    {
        var max = delimiters.Count;
        if (max == 0) { return; }

        var openersBottom = new Dictionary<int, int[]>();

        // headerIdx is the first delimiter of the current delimiter run
        var headerIdx = 0;
        var lastTokenIdx = -2; // needs any value lower than -1
        var jumps = new List<int>();

        for (var closerIdx = 0; closerIdx < max; closerIdx++)
        {
            var closer = delimiters[closerIdx];

            jumps.Add(0);

            // markers belong to the same delimiter run when tokens are adjacent and
            // markers are equal
            if (delimiters[headerIdx].Marker != closer.Marker || lastTokenIdx != closer.Token - 1)
            {
                headerIdx = closerIdx;
            }

            lastTokenIdx = closer.Token;

            if (!closer.Close) { continue; }

            if (!openersBottom.ContainsKey(closer.Marker))
            {
                openersBottom[closer.Marker] = new[] { -1, -1, -1, -1, -1, -1 };
            }

            var minOpenerIdx = openersBottom[closer.Marker][(closer.Open ? 3 : 0) + (closer.Length % 3)];

            var openerIdx = headerIdx - jumps[headerIdx] - 1;

            var newMinOpenerIdx = openerIdx;

            for (; openerIdx > minOpenerIdx; openerIdx -= jumps[openerIdx] + 1)
            {
                var opener = delimiters[openerIdx];

                if (opener.Marker != closer.Marker) { continue; }

                if (opener.Open && opener.End < 0)
                {
                    var isOddMatch = false;

                    // From the spec: if one of the delimiters can both open and close,
                    // the sum of the lengths of the runs must not be a multiple of 3
                    // unless both lengths are multiples of 3.
                    if ((opener.Close || closer.Open)
                        && (opener.Length + closer.Length) % 3 == 0
                        && (opener.Length % 3 != 0 || closer.Length % 3 != 0))
                    {
                        isOddMatch = true;
                    }

                    if (!isOddMatch)
                    {
                        // If the previous delimiter cannot be an opener, safely skip
                        // the entire sequence in future checks (linear complexity).
                        var lastJump = openerIdx > 0 && !delimiters[openerIdx - 1].Open
                            ? jumps[openerIdx - 1] + 1
                            : 0;

                        jumps[closerIdx] = closerIdx - openerIdx + lastJump;
                        jumps[openerIdx] = lastJump;

                        closer.Open = false;
                        opener.End = closerIdx;
                        opener.Close = false;
                        newMinOpenerIdx = -1;
                        // treat the next token as the start of a run (optimizes the
                        // **<...>**a**<...>** pathological case)
                        lastTokenIdx = -2;
                        break;
                    }
                }
            }

            if (newMinOpenerIdx != -1)
            {
                // Set the lower bound for future lookups after a failed match
                // (linear-complexity requirement).
                openersBottom[closer.Marker][(closer.Open ? 3 : 0) + (closer.Length % 3)] = newMinOpenerIdx;
            }
        }
    }

    public static void Rule(StateInline state)
    {
        ProcessDelimiters(state.Delimiters);

        foreach (var meta in state.TokensMeta)
        {
            if (meta?.Delimiters != null)
            {
                ProcessDelimiters(meta.Delimiters);
            }
        }
    }
}

/// <summary>
/// Cleans up tokens after emphasis/strikethrough post-processing: merges adjacent text
/// nodes and re-calculates all token levels.
/// </summary>
internal static class FragmentsJoin
{
    public static void Rule(StateInline state)
    {
        int curr, last;
        var level = 0;
        var tokens = state.Tokens;
        var max = state.Tokens.Count;

        for (curr = last = 0; curr < max; curr++)
        {
            // re-calculate levels after emphasis/strikethrough turned some text nodes
            // into opening/closing tags
            if (tokens[curr].Nesting < 0) { level--; } // closing tag
            tokens[curr].Level = level;
            if (tokens[curr].Nesting > 0) { level++; } // opening tag

            if (tokens[curr].Type == "text"
                && curr + 1 < max
                && tokens[curr + 1].Type == "text")
            {
                // collapse two adjacent text nodes
                tokens[curr + 1].Content = tokens[curr].Content + tokens[curr + 1].Content;
            }
            else
            {
                if (curr != last) { tokens[last] = tokens[curr]; }
                last++;
            }
        }

        if (curr != last)
        {
            tokens.RemoveRange(last, tokens.Count - last);
        }
    }
}
