using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CodeBrix.PdfDocCreate.Html2Pdf;
using CodeBrix.PdfDocCreate.Markdown2Pdf.Highlighting;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt.Common;
using CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf;

/// <summary>
/// Renders any Markdown (.md) file into a nice-looking, pre-formatted, printable PDF
/// with zero configuration. Markdown parses through the vendored markdown-it port
/// (CommonMark + tables, strikethrough, footnotes, task lists, YAML front matter),
/// converts to HTML styled by a polished built-in stylesheet, and renders through
/// <see cref="HtmlPdfRenderer"/>. Consumers who want a different look call
/// <see cref="GenerateHtml(string, string)"/>, restyle the returned HTML/CSS, and
/// render it with Html2Pdf themselves.
/// </summary>
public sealed class MarkdownPdfRenderer
{
    private static readonly Regex FrontMatterTitleRe = new Regex(
        @"^title\s*:\s*(.+?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex FrontMatterAuthorRe = new Regex(
        @"^author\s*:\s*(.+?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex FirstHeadingRe = new Regex(
        @"<h[1-6][^>]*>(.*?)</h[1-6]>", RegexOptions.Singleline);

    private static readonly Regex TagStripRe = new Regex("<[^>]+>", RegexOptions.Compiled);

    /// <summary>Rendering options; every default is chosen to look good untouched.</summary>
    public MarkdownRenderOptions Options { get; } = new MarkdownRenderOptions();

    /// <summary>
    /// Renders a Markdown file to a PDF file. When <paramref name="outputPdfPath"/> is
    /// null the PDF is written next to the source file with a .pdf extension.
    /// </summary>
    public MarkdownRenderResult RenderFile(string markdownFilePath, string outputPdfPath = null)
    {
        if (string.IsNullOrWhiteSpace(markdownFilePath))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(markdownFilePath));
        }

        var fullPath = Path.GetFullPath(markdownFilePath);
        var markdown = File.ReadAllText(fullPath);
        var baseDirectory = Path.GetDirectoryName(fullPath);
        outputPdfPath ??= Path.ChangeExtension(fullPath, ".pdf");

        return Render(markdown, baseDirectory, outputPdfPath, Path.GetFileNameWithoutExtension(fullPath));
    }

    /// <summary>Renders a Markdown string to a PDF file.</summary>
    public MarkdownRenderResult RenderMarkdown(string markdown, string outputPdfPath, string baseDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(outputPdfPath))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(outputPdfPath));
        }

        return Render(markdown, baseDirectory, outputPdfPath, fallbackTitle: null);
    }

    /// <summary>Renders a Markdown string and returns the PDF as bytes.</summary>
    public MarkdownRenderResult RenderMarkdownToBytes(string markdown, string baseDirectory = null) =>
        Render(markdown, baseDirectory, outputPdfPath: null, fallbackTitle: null);

    /// <summary>
    /// Converts a Markdown file to ready-to-render HTML/CSS without producing a PDF -
    /// the restyling hand-off point.
    /// </summary>
    public MarkdownHtmlResult GenerateHtmlFromFile(string markdownFilePath)
    {
        var fullPath = Path.GetFullPath(markdownFilePath);
        return GenerateHtml(
            File.ReadAllText(fullPath),
            Path.GetDirectoryName(fullPath),
            Path.GetFileNameWithoutExtension(fullPath));
    }

    /// <summary>
    /// Converts a Markdown string to ready-to-render HTML/CSS without producing a PDF.
    /// </summary>
    public MarkdownHtmlResult GenerateHtml(string markdown, string baseDirectory = null) =>
        GenerateHtml(markdown, baseDirectory, fallbackTitle: null);

    private MarkdownHtmlResult GenerateHtml(string markdown, string baseDirectory, string fallbackTitle)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        string frontMatter = null;
        var md = CreateParser(text => frontMatter = text);

        var bodyHtml = md.Render(markdown);
        var title = InferTitle(frontMatter, bodyHtml, fallbackTitle);

        return new MarkdownHtmlResult(bodyHtml, DefaultTheme.Css, title, baseDirectory);
    }

    private MarkdownRenderResult Render(string markdown, string baseDirectory, string outputPdfPath, string fallbackTitle)
    {
        ArgumentNullException.ThrowIfNull(markdown);

        string frontMatter = null;
        var md = CreateParser(text => frontMatter = text);
        var bodyHtml = md.Render(markdown);
        var title = InferTitle(frontMatter, bodyHtml, fallbackTitle);
        var author = InferAuthor(frontMatter);

        var html = BuildHtmlDocument(bodyHtml, DefaultTheme.Css, title);

        var htmlRenderer = new HtmlPdfRenderer();
        htmlRenderer.Options.SetPageSize(Options.PageSize);
        htmlRenderer.Options.AllowRemoteImages = Options.AllowRemoteImages;
        htmlRenderer.Options.FooterText = Options.FooterText;
        htmlRenderer.Options.GenerateOutline = true;
        htmlRenderer.Options.DocumentTitle = title;
        if (!string.IsNullOrWhiteSpace(author)) { htmlRenderer.Options.DocumentAuthor = author; }
        htmlRenderer.Options.MarginTopPoints = 66;
        htmlRenderer.Options.MarginBottomPoints = 72;

        var result = outputPdfPath != null
            ? htmlRenderer.RenderHtml(html, outputPdfPath, baseDirectory)
            : htmlRenderer.RenderHtmlToBytes(html, baseDirectory);

        return new MarkdownRenderResult(
            result.OutputFilePath, result.PdfBytes, result.PageCount, title, result.Warnings);
    }

    /// <summary>
    /// Builds a complete HTML document from body markup, a stylesheet, and a title -
    /// public so consumers restyling the generated output can reassemble the document.
    /// </summary>
    public static string BuildHtmlDocument(string bodyHtml, string css, string title)
    {
        var builder = new StringBuilder(bodyHtml.Length + css.Length + 256);
        builder.Append("<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"utf-8\">\n");
        builder.Append("<title>").Append(MdUtils.EscapeHtml(title ?? "")).Append("</title>\n");
        builder.Append("<style>\n").Append(css).Append("\n</style>\n</head>\n<body>\n");
        builder.Append(bodyHtml);
        builder.Append("</body>\n</html>\n");
        return builder.ToString();
    }

    private static MarkdownParser CreateParser(Action<string> frontMatterCallback)
    {
        var md = new MarkdownParser(MarkdownPreset.Default, options =>
        {
            // Embedded HTML flows into Html2Pdf, which renders its documented subset.
            options.Html = true;
            options.Highlight = (content, langName, _) => CodeHighlighter.Highlight(content, langName);
        });

        md.Use(FootnotePlugin.Apply);
        md.Use(TaskListPlugin.Apply);
        FrontMatterPlugin.Apply(md, frontMatterCallback);

        return md;
    }

    private static string InferTitle(string frontMatter, string bodyHtml, string fallbackTitle)
    {
        if (!string.IsNullOrEmpty(frontMatter))
        {
            var match = FrontMatterTitleRe.Match(frontMatter);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim().Trim('"', '\'');
            }
        }

        var heading = FirstHeadingRe.Match(bodyHtml);
        if (heading.Success)
        {
            var text = TagStripRe.Replace(heading.Groups[1].Value, "").Trim();
            text = System.Net.WebUtility.HtmlDecode(text);
            if (text.Length > 0)
            {
                return text.Length > 120 ? text.Substring(0, 120) : text;
            }
        }

        if (!string.IsNullOrWhiteSpace(fallbackTitle))
        {
            return fallbackTitle.Replace('_', ' ').Replace('-', ' ').Trim();
        }

        return "Document";
    }

    private static string InferAuthor(string frontMatter)
    {
        if (string.IsNullOrEmpty(frontMatter)) { return null; }
        var match = FrontMatterAuthorRe.Match(frontMatter);
        return match.Success ? match.Groups[1].Value.Trim().Trim('"', '\'') : null;
    }
}
