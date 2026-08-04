// ============================================================================
// C# port of markdown-it v14.1.0 - lib/common/{html_blocks,html_re}.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System.Text.RegularExpressions;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

/// <summary>Regexps to match HTML elements and the CommonMark html-block tag names.</summary>
internal static class HtmlRe
{
    /// <summary>Valid html block names, per the CommonMark spec.</summary>
    public static readonly string[] BlockNames =
    {
        "address", "article", "aside", "base", "basefont", "blockquote", "body",
        "caption", "center", "col", "colgroup", "dd", "details", "dialog", "dir",
        "div", "dl", "dt", "fieldset", "figcaption", "figure", "footer", "form",
        "frame", "frameset", "h1", "h2", "h3", "h4", "h5", "h6", "head", "header",
        "hr", "html", "iframe", "legend", "li", "link", "main", "menu", "menuitem",
        "nav", "noframes", "ol", "optgroup", "option", "p", "param", "search",
        "section", "summary", "table", "tbody", "td", "tfoot", "th", "thead",
        "title", "tr", "track", "ul",
    };

    private const string AttrName = "[a-zA-Z_:][a-zA-Z0-9:._-]*";
    private const string Unquoted = "[^\"'=<>`\\x00-\\x20]+";
    private const string SingleQuoted = "'[^']*'";
    private const string DoubleQuoted = "\"[^\"]*\"";
    private const string AttrValue = "(?:" + Unquoted + "|" + SingleQuoted + "|" + DoubleQuoted + ")";
    private const string Attribute = "(?:\\s+" + AttrName + "(?:\\s*=\\s*" + AttrValue + ")?)";
    private const string OpenTag = "<[A-Za-z][A-Za-z0-9\\-]*" + Attribute + "*\\s*\\/?>";
    private const string CloseTag = "<\\/[A-Za-z][A-Za-z0-9\\-]*\\s*>";
    private const string Comment = "<!---?>|<!--(?:[^-]|-[^-]|--[^>])*-->";
    private const string Processing = "<[?][\\s\\S]*?[?]>";
    private const string Declaration = "<![A-Za-z][^>]*>";
    private const string Cdata = "<!\\[CDATA\\[[\\s\\S]*?\\]\\]>";

    /// <summary>Matches a complete inline HTML construct at the string start.</summary>
    public static readonly Regex HtmlTagRe = new Regex(
        "^(?:" + OpenTag + "|" + CloseTag + "|" + Comment + "|" + Processing + "|" + Declaration + "|" + Cdata + ")",
        RegexOptions.Compiled);

    /// <summary>Matches an open or close tag at the string start.</summary>
    public static readonly Regex HtmlOpenCloseTagRe = new Regex(
        "^(?:" + OpenTag + "|" + CloseTag + ")", RegexOptions.Compiled);

    /// <summary>The open/close tag pattern followed by whitespace to the line end.</summary>
    public static readonly Regex HtmlOpenCloseTagLineRe = new Regex(
        "^(?:" + OpenTag + "|" + CloseTag + ")\\s*$", RegexOptions.Compiled);
}
