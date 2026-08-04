// ============================================================================
// C# port of markdown-it v14.1.0 - lib/presets/{default,commonmark,zero}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>Parser/renderer options of a <see cref="MarkdownParser"/> instance.</summary>
public sealed class MarkdownItOptions
{
    /// <summary>Enable HTML tags in source.</summary>
    public bool Html { get; set; }

    /// <summary>Use '/' to close single tags (&lt;br /&gt;).</summary>
    public bool XhtmlOut { get; set; }

    /// <summary>Convert '\n' in paragraphs into &lt;br&gt;.</summary>
    public bool Breaks { get; set; }

    /// <summary>CSS language prefix for fenced blocks.</summary>
    public string LangPrefix { get; set; } = "language-";

    /// <summary>Autoconvert URL-like text to links (requires a linkifier; off by default).</summary>
    public bool Linkify { get; set; }

    /// <summary>Enable language-neutral replacements and quote beautification.</summary>
    public bool Typographer { get; set; }

    /// <summary>Double + single quote replacement pairs for the typographer.</summary>
    public string Quotes { get; set; } = "“”‘’"; /* “”‘’ */

    /// <summary>
    /// Highlighter for fenced code: (content, langName, langAttrs) =&gt; escaped HTML.
    /// Return null/empty to fall back to plain escaping; a result starting with
    /// &lt;pre skips the internal wrapper.
    /// </summary>
    public Func<string, string, string, string> Highlight { get; set; }

    /// <summary>Internal protection: recursion limit.</summary>
    public int MaxNesting { get; set; } = 100;
}

/// <summary>The configuration presets a parser can start from.</summary>
public enum MarkdownPreset
{
    /// <summary>The markdown-it "default" preset: all rules on, HTML off.</summary>
    Default,

    /// <summary>Strict CommonMark mode: only the CommonMark rules, HTML on.</summary>
    CommonMark,

    /// <summary>Nothing enabled; configure rules manually.</summary>
    Zero,
}
