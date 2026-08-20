// ============================================================================
// C# port of markdown-it v14.1.0 - lib/index.mjs (the MarkdownIt main class)
// markdown-it copyright (c) 2014 Vitaly Puzrin, Alex Kocharin. MIT License.
// https://github.com/markdown-it/markdown-it
//
// Differences from upstream: hostname punycoding uses System.Globalization
// IdnMapping instead of punycode.js, and the linkify-it dependency is not
// ported - the Linkify option is accepted but bare-URL autolinking is inert.
// The data: image allow-list is widened from upstream's gif/png/jpeg/webp to
// every format the Html2Pdf render pipeline can embed (the CodeBrix.Imaging
// decoder set plus SVG).
// ============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.RulesCore;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;

/// <summary>
/// The main markdown parser/renderer class (markdown-it's MarkdownIt), producing HTML
/// from CommonMark + GFM-style markdown.
/// </summary>
public sealed class MarkdownParser
{
    private static readonly Regex BadProtoRe = new Regex("^(vbscript|javascript|file|data):", RegexOptions.Compiled);
    private static readonly Regex GoodDataRe = new Regex(
        "^data:image\\/(gif|png|jpeg|webp|bmp|x-windows-bmp|tiff|x-tga|x-targa|x-portable-pixmap|x-portable-graymap|x-portable-bitmap|x-portable-anymap|svg\\+xml)[;,]",
        RegexOptions.Compiled);
    private static readonly string[] RecodeHostnameFor = { "http:", "https:", "mailto:" };

    /// <summary>Creates a parser with the given preset and optional option overrides.</summary>
    public MarkdownParser(MarkdownPreset preset = MarkdownPreset.Default, Action<MarkdownItOptions> configureOptions = null)
    {
        Inline = new ParserInline();
        Block = new ParserBlock();
        Core = new ParserCore();
        Renderer = new Renderer();

        ValidateLink = DefaultValidateLink;
        NormalizeLink = DefaultNormalizeLink;
        NormalizeLinkText = DefaultNormalizeLinkText;

        Configure(preset);
        configureOptions?.Invoke(Options);
    }

    /// <summary>The inline tokenizer; add rules here when writing plugins.</summary>
    public ParserInline Inline { get; }

    /// <summary>The block tokenizer; add rules here when writing plugins.</summary>
    public ParserBlock Block { get; }

    /// <summary>The top-level chain executor.</summary>
    public ParserCore Core { get; }

    /// <summary>The HTML renderer; replace rules here to modify output.</summary>
    public Renderer Renderer { get; }

    /// <summary>Parser options.</summary>
    public MarkdownItOptions Options { get; } = new MarkdownItOptions();

    /// <summary>
    /// Link validation function. CommonMark allows too much in links, so javascript:,
    /// vbscript:, file: and most data: schemas are disabled by default.
    /// </summary>
    public Func<string, bool> ValidateLink { get; set; }

    /// <summary>Encodes a link url to machine-readable format (url-encoding, punycode).</summary>
    public Func<string, string> NormalizeLink { get; set; }

    /// <summary>Decodes a link url to a human-readable format.</summary>
    public Func<string, string> NormalizeLinkText { get; set; }

    private static bool DefaultValidateLink(string url)
    {
        // the url should be normalized at this point, and existing entities decoded
        var str = url.Trim().ToLowerInvariant();
        return BadProtoRe.IsMatch(str) ? GoodDataRe.IsMatch(str) : true;
    }

    private static string DefaultNormalizeLink(string url)
    {
        var parsed = MdUrl.Parse(url, slashesDenoteHost: true);

        if (!string.IsNullOrEmpty(parsed.Hostname)
            && (parsed.Protocol == null || Array.IndexOf(RecodeHostnameFor, parsed.Protocol) >= 0))
        {
            try
            {
                parsed.Hostname = new IdnMapping().GetAscii(parsed.Hostname);
            }
            catch (ArgumentException) { /* keep the original hostname */ }
        }

        return MdUrl.Encode(MdUrl.Format(parsed));
    }

    private static string DefaultNormalizeLinkText(string url)
    {
        var parsed = MdUrl.Parse(url, slashesDenoteHost: true);

        if (!string.IsNullOrEmpty(parsed.Hostname)
            && (parsed.Protocol == null || Array.IndexOf(RecodeHostnameFor, parsed.Protocol) >= 0))
        {
            try
            {
                parsed.Hostname = new IdnMapping().GetUnicode(parsed.Hostname);
            }
            catch (ArgumentException) { /* keep the original hostname */ }
        }

        // '%' is added to the exclude list (markdown-it issue #720)
        return MdUrl.Decode(MdUrl.Format(parsed), MdUrl.DecodeDefaultChars + "%");
    }

    /// <summary>Applies a configuration preset (options + enabled rule sets).</summary>
    public MarkdownParser Configure(MarkdownPreset preset)
    {
        switch (preset)
        {
            case MarkdownPreset.Default:
                // All rules enabled (the registration order), default options.
                break;

            case MarkdownPreset.CommonMark:
                Options.Html = true;
                Options.XhtmlOut = true;
                Options.MaxNesting = 20;
                Core.Ruler.EnableOnly(new[] { "normalize", "block", "inline", "text_join" });
                Block.Ruler.EnableOnly(new[]
                {
                    "blockquote", "code", "fence", "heading", "hr", "html_block",
                    "lheading", "list", "reference", "paragraph",
                });
                Inline.Ruler.EnableOnly(new[]
                {
                    "autolink", "backticks", "emphasis", "entity", "escape",
                    "html_inline", "image", "link", "newline", "text",
                });
                Inline.Ruler2.EnableOnly(new[] { "balance_pairs", "emphasis", "fragments_join" });
                break;

            case MarkdownPreset.Zero:
                Options.MaxNesting = 20;
                Core.Ruler.EnableOnly(new[] { "normalize", "block", "inline", "text_join" });
                Block.Ruler.EnableOnly(new[] { "paragraph" });
                Inline.Ruler.EnableOnly(new[] { "text" });
                Inline.Ruler2.EnableOnly(new[] { "balance_pairs", "fragments_join" });
                break;
        }

        return this;
    }

    /// <summary>
    /// Enables rules by name across all chains; throws for unknown names unless
    /// <paramref name="ignoreInvalid"/> is set.
    /// </summary>
    public MarkdownParser Enable(IEnumerable<string> list, bool ignoreInvalid = false)
    {
        var names = new List<string>(list);
        var result = new List<string>();

        result.AddRange(Core.Ruler.Enable(names, true));
        result.AddRange(Block.Ruler.Enable(names, true));
        result.AddRange(Inline.Ruler.Enable(names, true));
        result.AddRange(Inline.Ruler2.Enable(names, true));

        var missed = names.FindAll(name => !result.Contains(name));
        if (missed.Count > 0 && !ignoreInvalid)
        {
            throw new InvalidOperationException("MarkdownIt. Failed to enable unknown rule(s): " + string.Join(",", missed));
        }

        return this;
    }

    /// <summary>The same as <see cref="Enable"/>, but turns the specified rules off.</summary>
    public MarkdownParser Disable(IEnumerable<string> list, bool ignoreInvalid = false)
    {
        var names = new List<string>(list);
        var result = new List<string>();

        result.AddRange(Core.Ruler.Disable(names, true));
        result.AddRange(Block.Ruler.Disable(names, true));
        result.AddRange(Inline.Ruler.Disable(names, true));
        result.AddRange(Inline.Ruler2.Disable(names, true));

        var missed = names.FindAll(name => !result.Contains(name));
        if (missed.Count > 0 && !ignoreInvalid)
        {
            throw new InvalidOperationException("MarkdownIt. Failed to disable unknown rule(s): " + string.Join(",", missed));
        }

        return this;
    }

    /// <summary>Loads a plugin (sugar for calling it with this instance).</summary>
    public MarkdownParser Use(Action<MarkdownParser> plugin)
    {
        plugin(this);
        return this;
    }

    /// <summary>
    /// Parses the input string and returns the list of block tokens (the "inline" token
    /// type carries its inline tokens in Children).
    /// </summary>
    public List<Token> Parse(string src, MdEnv env)
    {
        ArgumentNullException.ThrowIfNull(src);

        var state = new StateCore(src, this, env ?? new MdEnv());
        Core.Process(state);
        return state.Tokens;
    }

    /// <summary>Renders a markdown string into HTML.</summary>
    public string Render(string src, MdEnv env = null)
    {
        env ??= new MdEnv();
        return Renderer.Render(Parse(src, env), Options, env);
    }

    /// <summary>The same as <see cref="Parse"/> but skips all block rules.</summary>
    public List<Token> ParseInline(string src, MdEnv env)
    {
        var state = new StateCore(src, this, env ?? new MdEnv()) { InlineMode = true };
        Core.Process(state);
        return state.Tokens;
    }

    /// <summary>Renders single-paragraph content without the paragraph wrap.</summary>
    public string RenderInline(string src, MdEnv env = null)
    {
        env ??= new MdEnv();
        return Renderer.Render(ParseInline(src, env), Options, env);
    }
}
