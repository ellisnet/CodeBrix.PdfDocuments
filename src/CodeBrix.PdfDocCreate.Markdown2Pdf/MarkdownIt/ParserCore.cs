// ============================================================================
// C# port of markdown-it v14.1.0 - lib/parser_core.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>Core rule delegate.</summary>
public delegate void CoreRule(StateCore state);

/// <summary>
/// Top-level rules executor: glues the block/inline parsers and does intermediate
/// transformations.
/// </summary>
public sealed class ParserCore
{
    /// <summary>Creates the core chain with the default rules.</summary>
    public ParserCore()
    {
        Ruler.Push("normalize", Normalize.Rule);
        Ruler.Push("block", BlockRule.Rule);
        Ruler.Push("inline", InlineRule.Rule);
        Ruler.Push("linkify", LinkifyRule.Rule);
        Ruler.Push("replacements", Replacements.Rule);
        Ruler.Push("smartquotes", SmartQuotes.Rule);
        // text_join finds text_special tokens (for escape sequences) and joins them
        // with the rest of the text
        Ruler.Push("text_join", TextJoin.Rule);
    }

    /// <summary>Keeps the configuration of core rules.</summary>
    public Ruler<CoreRule> Ruler { get; } = new Ruler<CoreRule>();

    /// <summary>Executes the core chain rules.</summary>
    public void Process(StateCore state)
    {
        foreach (var rule in Ruler.GetRules(""))
        {
            rule(state);
        }
    }
}
