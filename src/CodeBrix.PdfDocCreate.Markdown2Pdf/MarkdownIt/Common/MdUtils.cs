// ============================================================================
// C# port of markdown-it v14.1.0 - lib/common/utils.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// https://github.com/markdown-it/markdown-it
//
// Entity decoding uses CodeBrix.MarkupParse's HtmlEntityProvider in place of the
// upstream "entities" package; Unicode category checks replace the uc.micro regexes.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using CodeBrix.MarkupParse.Html;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

/// <summary>Shared low-level utilities of the markdown-it port.</summary>
public static class MdUtils
{
    /// <summary>
    /// Bounds-safe equivalent of JavaScript's charCodeAt: returns -1 outside the
    /// string instead of throwing, so ported comparisons behave like NaN comparisons.
    /// </summary>
    public static int CharCode(string str, int pos) =>
        pos >= 0 && pos < str.Length ? str[pos] : -1;

    /// <summary>Replaces a token with a sequence of tokens at the given position.</summary>
    public static List<Token> ArrayReplaceAt(List<Token> src, int pos, List<Token> newElements)
    {
        var result = new List<Token>(src.Count - 1 + newElements.Count);
        for (var i = 0; i < pos; i++) { result.Add(src[i]); }
        result.AddRange(newElements);
        for (var i = pos + 1; i < src.Count; i++) { result.Add(src[i]); }
        return result;
    }

    /// <summary>True when the code point may appear in a numeric character reference.</summary>
    public static bool IsValidEntityCode(int c)
    {
        // broken sequence
        if (c >= 0xD800 && c <= 0xDFFF) { return false; }
        // never used
        if (c >= 0xFDD0 && c <= 0xFDEF) { return false; }
        if ((c & 0xFFFF) == 0xFFFF || (c & 0xFFFF) == 0xFFFE) { return false; }
        // control codes
        if (c >= 0x00 && c <= 0x08) { return false; }
        if (c == 0x0B) { return false; }
        if (c >= 0x0E && c <= 0x1F) { return false; }
        if (c >= 0x7F && c <= 0x9F) { return false; }
        // out of range
        if (c > 0x10FFFF) { return false; }
        return true;
    }

    /// <summary>Converts a code point to a string (surrogate pair when above the BMP).</summary>
    public static string FromCodePoint(int c)
    {
        if (c > 0xFFFF)
        {
            c -= 0x10000;
            var surrogate1 = (char)(0xD800 + (c >> 10));
            var surrogate2 = (char)(0xDC00 + (c & 0x3FF));
            return new string(new[] { surrogate1, surrogate2 });
        }
        return ((char)c).ToString();
    }

    private static readonly Regex UnescapeMdRe = new Regex(
        @"\\([!""#$%&'()*+,\-./:;<=>?@[\\\]^_`{|}~])", RegexOptions.Compiled);

    private static readonly Regex UnescapeAllRe = new Regex(
        @"\\([!""#$%&'()*+,\-./:;<=>?@[\\\]^_`{|}~])|&([a-z#][a-z0-9]{1,31});",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DigitalEntityTestRe = new Regex(
        "^#((?:x[a-f0-9]{1,8}|[0-9]{1,8}))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string ReplaceEntityPattern(string match, string name)
    {
        if (name.Length > 0 && name[0] == '#' && DigitalEntityTestRe.IsMatch(name))
        {
            var code = name.Length > 1 && (name[1] == 'x' || name[1] == 'X')
                ? int.Parse(name.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture)
                : int.Parse(name.Substring(1), CultureInfo.InvariantCulture);

            return IsValidEntityCode(code) ? FromCodePoint(code) : match;
        }

        // Named entity: resolve through MarkupParse's HTML5 entity table. The lookup
        // name carries the trailing semicolon, matching the provider's convention.
        var decoded = HtmlEntityProvider.Resolver.GetSymbol(name + ";");
        return string.IsNullOrEmpty(decoded) ? match : decoded;
    }

    /// <summary>Removes backslash escapes from markdown punctuation.</summary>
    public static string UnescapeMd(string str)
    {
        if (str.IndexOf('\\') < 0) { return str; }
        return UnescapeMdRe.Replace(str, "$1");
    }

    /// <summary>Removes backslash escapes and resolves HTML entities.</summary>
    public static string UnescapeAll(string str)
    {
        if (str.IndexOf('\\') < 0 && str.IndexOf('&') < 0) { return str; }

        return UnescapeAllRe.Replace(str, match =>
        {
            if (match.Groups[1].Success) { return match.Groups[1].Value; }
            return ReplaceEntityPattern(match.Value, match.Groups[2].Value);
        });
    }

    /// <summary>Escapes &amp;, &lt;, &gt; and &quot; for HTML output.</summary>
    public static string EscapeHtml(string str)
    {
        if (str.IndexOfAny(new[] { '&', '<', '>', '"' }) < 0) { return str; }

        var builder = new System.Text.StringBuilder(str.Length + 16);
        foreach (var c in str)
        {
            switch (c)
            {
                case '&': builder.Append("&amp;"); break;
                case '<': builder.Append("&lt;"); break;
                case '>': builder.Append("&gt;"); break;
                case '"': builder.Append("&quot;"); break;
                default: builder.Append(c); break;
            }
        }
        return builder.ToString();
    }

    private static readonly Regex EscapeReRe = new Regex(@"[.?*+^$[\]\\(){}|-]", RegexOptions.Compiled);

    /// <summary>Escapes regular-expression metacharacters.</summary>
    public static string EscapeRE(string str) => EscapeReRe.Replace(str, @"\$&");

    /// <summary>True for space or tab.</summary>
    public static bool IsSpace(int code) => code == 0x09 || code == 0x20;

    /// <summary>True for Unicode Zs category or [\t\f\v\r\n].</summary>
    public static bool IsWhiteSpace(int code)
    {
        if (code >= 0x2000 && code <= 0x200A) { return true; }
        switch (code)
        {
            case 0x09: // \t
            case 0x0A: // \n
            case 0x0B: // \v
            case 0x0C: // \f
            case 0x0D: // \r
            case 0x20:
            case 0xA0:
            case 0x1680:
            case 0x202F:
            case 0x205F:
            case 0x3000:
                return true;
            default:
                return false;
        }
    }

    /// <summary>True for Unicode punctuation (P) or symbol (S) characters.</summary>
    public static bool IsPunctChar(char ch)
    {
        switch (CharUnicodeInfo.GetUnicodeCategory(ch))
        {
            case UnicodeCategory.ConnectorPunctuation:
            case UnicodeCategory.DashPunctuation:
            case UnicodeCategory.OpenPunctuation:
            case UnicodeCategory.ClosePunctuation:
            case UnicodeCategory.InitialQuotePunctuation:
            case UnicodeCategory.FinalQuotePunctuation:
            case UnicodeCategory.OtherPunctuation:
            case UnicodeCategory.MathSymbol:
            case UnicodeCategory.CurrencySymbol:
            case UnicodeCategory.ModifierSymbol:
            case UnicodeCategory.OtherSymbol:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Markdown ASCII punctuation characters (per the CommonMark spec).</summary>
    public static bool IsMdAsciiPunct(int ch)
    {
        switch (ch)
        {
            case 0x21: case 0x22: case 0x23: case 0x24: case 0x25: case 0x26: case 0x27:
            case 0x28: case 0x29: case 0x2A: case 0x2B: case 0x2C: case 0x2D: case 0x2E:
            case 0x2F: case 0x3A: case 0x3B: case 0x3C: case 0x3D: case 0x3E: case 0x3F:
            case 0x40: case 0x5B: case 0x5C: case 0x5D: case 0x5E: case 0x5F: case 0x60:
            case 0x7B: case 0x7C: case 0x7D: case 0x7E:
                return true;
            default:
                return false;
        }
    }

    private static readonly Regex WhitespaceRunRe = new Regex(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Unifies [reference labels]: trims, collapses whitespace, and case-folds via
    /// lower-then-upper casing (which normalizes letter variants like ϴ/θ/ϑ).
    /// </summary>
    public static string NormalizeReference(string str)
    {
        str = WhitespaceRunRe.Replace(str.Trim(), " ");

        // JavaScript's toUpperCase applies full Unicode case mapping (ß -> SS), while
        // .NET's invariant mapping is simple (ß stays ß). Mirror the JS behaviour so
        // [ẞ], [ß] and [SS] labels unify the way upstream markdown-it unifies them.
        if (str.IndexOf('ẞ') >= 0) { str = str.Replace('ẞ', 'ß'); }
        str = str.ToLowerInvariant();
        if (str.IndexOf('ß') >= 0) { str = str.Replace("ß", "ss"); }

        return str.ToUpperInvariant();
    }
}
