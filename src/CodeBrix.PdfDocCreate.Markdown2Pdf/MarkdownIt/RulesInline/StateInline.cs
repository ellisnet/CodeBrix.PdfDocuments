// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_inline/state_inline.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesInline;

/// <summary>Inline parser state.</summary>
public sealed class StateInline
{
    /// <summary>Creates the inline state for one paragraph's content.</summary>
    public StateInline(string src, MarkdownParser md, MdEnv env, List<Token> outTokens)
    {
        Src = src;
        Env = env;
        Md = md;
        Tokens = outTokens;
        TokensMeta = new List<TokenMeta>(outTokens.Count);
        for (var i = 0; i < outTokens.Count; i++) { TokensMeta.Add(null); }
        PosMax = Src.Length;
    }

    /// <summary>The source text of the inline fragment.</summary>
    public string Src { get; set; }

    /// <summary>The environment sandbox of this run.</summary>
    public MdEnv Env { get; }

    /// <summary>Link to the parser instance.</summary>
    public MarkdownParser Md { get; }

    /// <summary>The output token stream.</summary>
    public List<Token> Tokens { get; }

    /// <summary>Per-token metadata parallel to <see cref="Tokens"/> (delimiter lists).</summary>
    public List<TokenMeta> TokensMeta { get; }

    /// <summary>Current parse position.</summary>
    public int Pos { get; set; }

    /// <summary>Parse limit (usually the source length; links shrink it temporarily).</summary>
    public int PosMax { get; set; }

    /// <summary>Current nesting level.</summary>
    public int Level { get; set; }

    /// <summary>Accumulated plain text awaiting a flush into a text token.</summary>
    public string Pending { get; set; } = "";

    /// <summary>Nesting level the pending text belongs to.</summary>
    public int PendingLevel { get; set; }

    /// <summary>Stores start:end pairs; useful for backtrack optimization of pair parses.</summary>
    public Dictionary<int, int> Cache { get; } = new Dictionary<int, int>();

    /// <summary>List of emphasis-like delimiters for the current tag.</summary>
    public List<Delimiter> Delimiters { get; set; } = new List<Delimiter>();

    private readonly Stack<List<Delimiter>> _prevDelimiters = new Stack<List<Delimiter>>();

    /// <summary>backtick length =&gt; last seen position.</summary>
    public Dictionary<int, int> Backticks { get; } = new Dictionary<int, int>();

    /// <summary>True once the whole source was scanned for backtick closers.</summary>
    public bool BackticksScanned { get; set; }

    /// <summary>Counter used to disable inline linkify execution inside links.</summary>
    public int LinkLevel { get; set; }

    /// <summary>Flushes pending text.</summary>
    public Token PushPending()
    {
        var token = new Token("text", "", 0)
        {
            Content = Pending,
            Level = PendingLevel,
        };
        Tokens.Add(token);
        TokensMeta.Add(null);
        Pending = "";
        return token;
    }

    /// <summary>Pushes a new token, flushing pending text first.</summary>
    public Token Push(string type, string tag, int nesting)
    {
        if (Pending.Length > 0) { PushPending(); }

        var token = new Token(type, tag, nesting);
        TokenMeta tokenMeta = null;

        if (nesting < 0)
        {
            // closing tag
            Level--;
            Delimiters = _prevDelimiters.Pop();
        }

        token.Level = Level;

        if (nesting > 0)
        {
            // opening tag
            Level++;
            _prevDelimiters.Push(Delimiters);
            Delimiters = new List<Delimiter>();
            tokenMeta = new TokenMeta { Delimiters = Delimiters };
        }

        PendingLevel = Level;
        Tokens.Add(token);
        TokensMeta.Add(tokenMeta);
        return token;
    }

    /// <summary>
    /// Scans a sequence of emphasis-like markers and determines whether it can open
    /// and/or close an emphasis sequence.
    /// </summary>
    public ScanDelimsResult ScanDelims(int start, bool canSplitWord)
    {
        var max = PosMax;
        var marker = MdUtils.CharCode(Src, start);

        // treat beginning of the line as a whitespace
        var lastChar = start > 0 ? Src[start - 1] : 0x20;

        var pos = start;
        while (pos < max && Src[pos] == marker) { pos++; }

        var count = pos - start;

        // treat end of the line as a whitespace
        var nextChar = pos < max ? Src[pos] : 0x20;

        var isLastPunctChar = MdUtils.IsMdAsciiPunct(lastChar) || MdUtils.IsPunctChar((char)lastChar);
        var isNextPunctChar = MdUtils.IsMdAsciiPunct(nextChar) || MdUtils.IsPunctChar((char)nextChar);

        var isLastWhiteSpace = MdUtils.IsWhiteSpace(lastChar);
        var isNextWhiteSpace = MdUtils.IsWhiteSpace(nextChar);

        var leftFlanking = !isNextWhiteSpace && (!isNextPunctChar || isLastWhiteSpace || isLastPunctChar);
        var rightFlanking = !isLastWhiteSpace && (!isLastPunctChar || isNextWhiteSpace || isNextPunctChar);

        var canOpen = leftFlanking && (canSplitWord || !rightFlanking || isLastPunctChar);
        var canClose = rightFlanking && (canSplitWord || !leftFlanking || isNextPunctChar);

        return new ScanDelimsResult(canOpen, canClose, count);
    }
}
