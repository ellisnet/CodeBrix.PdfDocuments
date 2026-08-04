// ============================================================================
// C# port of markdown-it v14.1.0 - lib/renderer.mjs
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>Renderer rule delegate.</summary>
public delegate string RendererRuleFn(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self);

/// <summary>
/// Generates HTML from a parsed token stream. Each instance has an independent copy of
/// rules which can be replaced or extended (that is how plugins add token types).
/// </summary>
public sealed class Renderer
{
    private static readonly Regex FenceInfoSplitRe = new Regex(@"(\s+)", RegexOptions.Compiled);

    /// <summary>Creates a renderer with the default rules.</summary>
    public Renderer()
    {
        Rules["code_inline"] = RenderCodeInline;
        Rules["code_block"] = RenderCodeBlock;
        Rules["fence"] = RenderFence;
        Rules["image"] = RenderImage;
        Rules["hardbreak"] = RenderHardbreak;
        Rules["softbreak"] = RenderSoftbreak;
        Rules["text"] = RenderText;
        Rules["html_block"] = RenderHtmlBlock;
        Rules["html_inline"] = RenderHtmlInline;
    }

    /// <summary>Render rules by token type; can be updated and extended.</summary>
    public Dictionary<string, RendererRuleFn> Rules { get; } =
        new Dictionary<string, RendererRuleFn>(StringComparer.Ordinal);

    private static string RenderCodeInline(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self)
    {
        var token = tokens[idx];
        return "<code" + self.RenderAttrs(token) + ">" + MdUtils.EscapeHtml(token.Content) + "</code>";
    }

    private static string RenderCodeBlock(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self)
    {
        var token = tokens[idx];
        return "<pre" + self.RenderAttrs(token) + "><code>" + MdUtils.EscapeHtml(token.Content) + "</code></pre>\n";
    }

    private static string RenderFence(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self)
    {
        var token = tokens[idx];
        var info = token.Info.Length > 0 ? MdUtils.UnescapeAll(token.Info).Trim() : "";
        var langName = "";
        var langAttrs = "";

        if (info.Length > 0)
        {
            var arr = FenceInfoSplitRe.Split(info);
            langName = arr[0];
            langAttrs = string.Concat(arr.Length > 2 ? string.Join("", arr, 2, arr.Length - 2) : "");
        }

        string highlighted;
        if (options.Highlight != null)
        {
            var result = options.Highlight(token.Content, langName, langAttrs);
            highlighted = string.IsNullOrEmpty(result) ? MdUtils.EscapeHtml(token.Content) : result;
        }
        else
        {
            highlighted = MdUtils.EscapeHtml(token.Content);
        }

        if (highlighted.StartsWith("<pre", StringComparison.Ordinal))
        {
            return highlighted + "\n";
        }

        // If a language exists, inject the class gently, without modifying the token.
        if (info.Length > 0)
        {
            var i = token.AttrIndex("class");
            var tmpAttrs = token.Attrs != null ? new List<string[]>(token.Attrs) : new List<string[]>();

            if (i < 0)
            {
                tmpAttrs.Add(new[] { "class", options.LangPrefix + langName });
            }
            else
            {
                tmpAttrs[i] = new[] { tmpAttrs[i][0], tmpAttrs[i][1] + " " + options.LangPrefix + langName };
            }

            // Fake token just to render attributes.
            var tmpToken = new Token("", "", 0) { Attrs = tmpAttrs };
            return "<pre><code" + self.RenderAttrs(tmpToken) + ">" + highlighted + "</code></pre>\n";
        }

        return "<pre><code" + self.RenderAttrs(token) + ">" + highlighted + "</code></pre>\n";
    }

    private static string RenderImage(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self)
    {
        var token = tokens[idx];

        // The "alt" attr MUST be set, even if empty - replace content with the value.
        token.Attrs[token.AttrIndex("alt")][1] = self.RenderInlineAsText(token.Children, options, env);

        return self.RenderToken(tokens, idx, options);
    }

    private static string RenderHardbreak(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        options.XhtmlOut ? "<br />\n" : "<br>\n";

    private static string RenderSoftbreak(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        options.Breaks ? (options.XhtmlOut ? "<br />\n" : "<br>\n") : "\n";

    private static string RenderText(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        MdUtils.EscapeHtml(tokens[idx].Content);

    private static string RenderHtmlBlock(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        tokens[idx].Content;

    private static string RenderHtmlInline(List<Token> tokens, int idx, MarkdownItOptions options, MdEnv env, Renderer self) =>
        tokens[idx].Content;

    /// <summary>Renders token attributes to a string.</summary>
    public string RenderAttrs(Token token)
    {
        if (token.Attrs == null) { return ""; }

        var result = new StringBuilder();
        foreach (var attr in token.Attrs)
        {
            result.Append(' ').Append(MdUtils.EscapeHtml(attr[0])).Append("=\"").Append(MdUtils.EscapeHtml(attr[1])).Append('"');
        }
        return result.ToString();
    }

    /// <summary>Default token renderer; can be overridden by rules.</summary>
    public string RenderToken(List<Token> tokens, int idx, MarkdownItOptions options)
    {
        var token = tokens[idx];
        var result = "";

        // Tight list paragraphs
        if (token.Hidden) { return ""; }

        // Insert a newline between a hidden paragraph and a subsequent opening
        // block-level tag.
        if (token.Block && token.Nesting != -1 && idx > 0 && tokens[idx - 1].Hidden)
        {
            result += "\n";
        }

        // Add token name, e.g. `<img`
        result += (token.Nesting == -1 ? "</" : "<") + token.Tag;

        // Encode attributes, e.g. `<img src="foo"`
        result += RenderAttrs(token);

        // Add a slash for self-closing tags, e.g. `<img src="foo" /`
        if (token.Nesting == 0 && options.XhtmlOut) { result += " /"; }

        // Check if we need to add a newline after this tag
        var needLf = false;
        if (token.Block)
        {
            needLf = true;

            if (token.Nesting == 1 && idx + 1 < tokens.Count)
            {
                var nextToken = tokens[idx + 1];

                if (nextToken.Type == "inline" || nextToken.Hidden)
                {
                    // Block-level tag containing an inline tag.
                    needLf = false;
                }
                else if (nextToken.Nesting == -1 && nextToken.Tag == token.Tag)
                {
                    // Opening tag + closing tag of the same type, e.g. `<li></li>`.
                    needLf = false;
                }
            }
        }

        result += needLf ? ">\n" : ">";
        return result;
    }

    /// <summary>Renders the children of a single inline token.</summary>
    public string RenderInline(List<Token> tokens, MarkdownItOptions options, MdEnv env)
    {
        var result = new StringBuilder();

        for (var i = 0; i < tokens.Count; i++)
        {
            if (Rules.TryGetValue(tokens[i].Type, out var rule))
            {
                result.Append(rule(tokens, i, options, env, this));
            }
            else
            {
                result.Append(RenderToken(tokens, i, options));
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Special kludge for image alt attributes: renders inline content with markup
    /// stripped, as the CommonMark spec requires.
    /// </summary>
    public string RenderInlineAsText(List<Token> tokens, MarkdownItOptions options, MdEnv env)
    {
        if (tokens == null) { return ""; }

        var result = new StringBuilder();
        foreach (var token in tokens)
        {
            switch (token.Type)
            {
                case "text":
                    result.Append(token.Content);
                    break;
                case "image":
                    result.Append(RenderInlineAsText(token.Children, options, env));
                    break;
                case "html_inline":
                case "html_block":
                    result.Append(token.Content);
                    break;
                case "softbreak":
                case "hardbreak":
                    result.Append('\n');
                    break;
            }
        }
        return result.ToString();
    }

    /// <summary>Takes a token stream and generates HTML.</summary>
    public string Render(List<Token> tokens, MarkdownItOptions options, MdEnv env)
    {
        var result = new StringBuilder();

        for (var i = 0; i < tokens.Count; i++)
        {
            var type = tokens[i].Type;

            if (type == "inline")
            {
                result.Append(RenderInline(tokens[i].Children, options, env));
            }
            else if (Rules.TryGetValue(type, out var rule))
            {
                result.Append(rule(tokens, i, options, env, this));
            }
            else
            {
                result.Append(RenderToken(tokens, i, options));
            }
        }

        return result.ToString();
    }
}
