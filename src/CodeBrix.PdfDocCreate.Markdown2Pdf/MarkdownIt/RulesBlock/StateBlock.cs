// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_block/state_block.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using System.Text;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesBlock;

/// <summary>Block parser state.</summary>
public sealed class StateBlock
{
    /// <summary>Creates the block state and computes the per-line offset caches.</summary>
    public StateBlock(string src, MarkdownParser md, MdEnv env, List<Token> tokens)
    {
        Src = src;
        Md = md;
        Env = env;
        Tokens = tokens;

        // Create caches; generate line markers.
        var s = Src;
        var indentFound = false;

        for (int start = 0, pos = 0, indent = 0, offset = 0, len = s.Length; pos < len; pos++)
        {
            var ch = s[pos];

            if (!indentFound)
            {
                if (MdUtils.IsSpace(ch))
                {
                    indent++;
                    if (ch == 0x09) { offset += 4 - offset % 4; }
                    else { offset++; }
                    continue;
                }
                indentFound = true;
            }

            if (ch == 0x0A || pos == len - 1)
            {
                if (ch != 0x0A) { pos++; }
                BMarks.Add(start);
                EMarks.Add(pos);
                TShift.Add(indent);
                SCount.Add(offset);
                BsCount.Add(0);

                indentFound = false;
                indent = 0;
                offset = 0;
                start = pos + 1;
            }
        }

        // Push fake entry to simplify cache bounds checks.
        BMarks.Add(s.Length);
        EMarks.Add(s.Length);
        TShift.Add(0);
        SCount.Add(0);
        BsCount.Add(0);

        LineMax = BMarks.Count - 1; // don't count last fake line
    }

    /// <summary>The source text.</summary>
    public string Src { get; set; }

    /// <summary>Link to the parser instance.</summary>
    public MarkdownParser Md { get; }

    /// <summary>The environment sandbox of this run.</summary>
    public MdEnv Env { get; }

    /// <summary>The output token stream.</summary>
    public List<Token> Tokens { get; }

    /// <summary>Line begin offsets for fast jumps.</summary>
    public List<int> BMarks { get; } = new List<int>();

    /// <summary>Line end offsets for fast jumps.</summary>
    public List<int> EMarks { get; } = new List<int>();

    /// <summary>Offsets of the first non-space characters (tabs not expanded).</summary>
    public List<int> TShift { get; } = new List<int>();

    /// <summary>Indents for each line (tabs expanded).</summary>
    public List<int> SCount { get; } = new List<int>();

    /// <summary>
    /// Virtual spaces (tabs expanded) between the beginning of each line and its real
    /// beginning - exists because blockquotes patch BMarks losing information.
    /// </summary>
    public List<int> BsCount { get; } = new List<int>();

    /// <summary>Required block content indent (e.g. positioned after a list marker).</summary>
    public int BlkIndent { get; set; }

    /// <summary>Line index in src.</summary>
    public int Line { get; set; }

    /// <summary>Lines count.</summary>
    public int LineMax { get; set; }

    /// <summary>Loose/tight mode for lists.</summary>
    public bool Tight { get; set; }

    /// <summary>Indent of the current dd block (-1 if there isn't any).</summary>
    public int DdIndent { get; set; } = -1;

    /// <summary>Indent of the current list block (-1 if there isn't any).</summary>
    public int ListIndent { get; set; } = -1;

    /// <summary>'blockquote', 'list', 'root', 'paragraph' or 'reference'.</summary>
    public string ParentType { get; set; } = "root";

    /// <summary>Current nesting level.</summary>
    public int Level { get; set; }

    /// <summary>Pushes a new token to the stream.</summary>
    public Token Push(string type, string tag, int nesting)
    {
        var token = new Token(type, tag, nesting) { Block = true };

        if (nesting < 0) { Level--; } // closing tag
        token.Level = Level;
        if (nesting > 0) { Level++; } // opening tag

        Tokens.Add(token);
        return token;
    }

    /// <summary>True when the line contains only whitespace.</summary>
    public bool IsEmpty(int line) => BMarks[line] + TShift[line] >= EMarks[line];

    /// <summary>Returns the index of the first non-empty line at or after <paramref name="from"/>.</summary>
    public int SkipEmptyLines(int from)
    {
        for (var max = LineMax; from < max; from++)
        {
            if (BMarks[from] + TShift[from] < EMarks[from]) { break; }
        }
        return from;
    }

    /// <summary>Skips spaces from the given position.</summary>
    public int SkipSpaces(int pos)
    {
        for (var max = Src.Length; pos < max; pos++)
        {
            if (!MdUtils.IsSpace(Src[pos])) { break; }
        }
        return pos;
    }

    /// <summary>Skips spaces from the given position in reverse.</summary>
    public int SkipSpacesBack(int pos, int min)
    {
        if (pos <= min) { return pos; }
        while (pos > min)
        {
            if (!MdUtils.IsSpace(Src[--pos])) { return pos + 1; }
        }
        return pos;
    }

    /// <summary>Skips the given char code from the given position.</summary>
    public int SkipChars(int pos, int code)
    {
        for (var max = Src.Length; pos < max; pos++)
        {
            if (Src[pos] != code) { break; }
        }
        return pos;
    }

    /// <summary>Skips the given char code in reverse from the given position - 1.</summary>
    public int SkipCharsBack(int pos, int code, int min)
    {
        if (pos <= min) { return pos; }
        while (pos > min)
        {
            if (code != Src[--pos]) { return pos + 1; }
        }
        return pos;
    }

    /// <summary>Cuts a range of lines from the source.</summary>
    public string GetLines(int begin, int end, int indent, bool keepLastLF)
    {
        if (begin >= end) { return ""; }

        var queue = new string[end - begin];

        for (int i = 0, line = begin; line < end; line++, i++)
        {
            var lineIndent = 0;
            var lineStart = BMarks[line];
            var first = lineStart;
            int last;

            if (line + 1 < end || keepLastLF)
            {
                // No need for bounds check because of the fake entry on the tail.
                last = EMarks[line] + 1;
            }
            else
            {
                last = EMarks[line];
            }

            while (first < last && lineIndent < indent)
            {
                var ch = Src[first];

                if (MdUtils.IsSpace(ch))
                {
                    if (ch == 0x09) { lineIndent += 4 - (lineIndent + BsCount[line]) % 4; }
                    else { lineIndent++; }
                }
                else if (first - lineStart < TShift[line])
                {
                    // patched tShift masked characters to look like spaces (blockquotes, list markers)
                    lineIndent++;
                }
                else
                {
                    break;
                }

                first++;
            }

            if (lineIndent > indent)
            {
                // partially expanding tabs in code blocks, e.g. '\t\tfoobar' with
                // indent=2 becomes '  \tfoobar'
                queue[i] = new string(' ', lineIndent - indent) + Src.Substring(first, last - first);
            }
            else
            {
                queue[i] = Src.Substring(first, last - first);
            }
        }

        return string.Concat(queue);
    }
}
