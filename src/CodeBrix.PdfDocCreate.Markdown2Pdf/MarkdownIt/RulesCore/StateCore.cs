// ============================================================================
// C# port of markdown-it v14.1.0 - lib/rules_core/state_core.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Collections.Generic;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;

/// <summary>State of the top-level (core) rule chain.</summary>
public sealed class StateCore
{
    /// <summary>Creates the state for one parse run.</summary>
    public StateCore(string src, MarkdownParser md, MdEnv env)
    {
        Src = src;
        Env = env;
        Md = md;
    }

    /// <summary>The source text (normalized in place by the normalize rule).</summary>
    public string Src { get; set; }

    /// <summary>The environment sandbox of this run.</summary>
    public MdEnv Env { get; }

    /// <summary>The output token stream.</summary>
    public List<Token> Tokens { get; } = new List<Token>();

    /// <summary>True when parsing single-paragraph inline content only.</summary>
    public bool InlineMode { get; set; }

    /// <summary>Link to the parser instance.</summary>
    public MarkdownParser Md { get; }
}
