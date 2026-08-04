using System;
using System.IO;
using System.Linq;
using CodeBrix.MarkupParse.Dom;
using CodeBrix.MarkupParse.Html.Dom;
using CodeBrix.MarkupParse.Html.Parser;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.Html2Pdf.Composition;
using CodeBrix.PdfDocCreate.Html2Pdf.Css;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.PdfDocCreate.Rendering;
using Document = CodeBrix.PdfDocCreate.DocumentObjectModel.Document;

namespace CodeBrix.PdfDocCreate.Html2Pdf;

/// <summary>
/// Renders author-created HTML with CSS styling into PDF documents. HTML is parsed with
/// CodeBrix.MarkupParse; the documented CSS dialect (inline styles, style blocks, and
/// linked local stylesheets) is applied with real selector matching, cascade and
/// inheritance; and the result is composed onto the CodeBrix.PdfDocCreate document
/// object model, whose renderer performs all layout. All text renders with the
/// CodeBrix.Platform.Fonts package fonts, so output is identical on every platform.
/// </summary>
public sealed class HtmlPdfRenderer
{
    /// <summary>Rendering options; modify before calling a render method.</summary>
    public HtmlRenderOptions Options { get; } = new HtmlRenderOptions();

    /// <summary>
    /// Renders an HTML file to a PDF file. Relative resource references (linked
    /// stylesheets, images) resolve against the HTML file's directory.
    /// </summary>
    public HtmlRenderResult RenderFile(string htmlFilePath, string outputPdfPath)
    {
        if (string.IsNullOrWhiteSpace(htmlFilePath))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(htmlFilePath));
        }

        var html = File.ReadAllText(htmlFilePath);
        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(htmlFilePath));
        return Render(html, baseDirectory, outputPdfPath);
    }

    /// <summary>
    /// Renders an HTML string to a PDF file. <paramref name="baseDirectory"/> anchors
    /// relative resource references; it defaults to the current directory.
    /// </summary>
    public HtmlRenderResult RenderHtml(string html, string outputPdfPath, string baseDirectory = null)
    {
        if (string.IsNullOrWhiteSpace(outputPdfPath))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(outputPdfPath));
        }

        return Render(html, baseDirectory, outputPdfPath);
    }

    /// <summary>
    /// Renders an HTML string and returns the PDF as a byte array (no file is written).
    /// </summary>
    public HtmlRenderResult RenderHtmlToBytes(string html, string baseDirectory = null) =>
        Render(html, baseDirectory, outputPdfPath: null);

    private HtmlRenderResult Render(string html, string baseDirectory, string outputPdfPath)
    {
        ArgumentNullException.ThrowIfNull(html);

        Html2PdfFonts.EnsureRegistered();
        if (Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false) == null)
        {
            throw new InvalidOperationException(
                "The CodeBrix.Platform.Fonts packages were not found next to the application. " +
                "They are normally copied to '<application base directory>/CodeBrix.Platform.Fonts.<Name>/Fonts/' " +
                "at build time by the CodeBrix.PdfDocCreate.Html2Pdf package's build targets. " +
                "If the fonts live elsewhere, call Html2PdfFonts.AddFontDirectory before rendering.");
        }

        ImageResolver.EnsureImagingBackend();

        var options = Options.Clone();
        var warnings = new RenderWarnings();
        var resolvedBase = string.IsNullOrWhiteSpace(baseDirectory)
            ? Directory.GetCurrentDirectory()
            : baseDirectory;

        var parser = new HtmlParser();
        var dom = parser.ParseDocument(html);

        var resolver = new StyleResolver(warnings);
        resolver.AddStylesheet(DefaultStylesheet.Css, isDefaultSheet: true);
        CollectAuthorStylesheets(dom, resolver, resolvedBase, warnings);

        var document = new Document();
        var title = options.DocumentTitle
            ?? dom.Title?.Trim()
            ?? "";
        if (title.Length > 0) { document.Info.Title = title; }
        if (!string.IsNullOrWhiteSpace(options.DocumentAuthor)) { document.Info.Author = options.DocumentAuthor; }

        var metaAuthor = dom.QuerySelectorAll("meta[name='author']")
            .Select(m => m.GetAttribute("content"))
            .FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));
        if (string.IsNullOrWhiteSpace(options.DocumentAuthor) && !string.IsNullOrWhiteSpace(metaAuthor))
        {
            document.Info.Author = metaAuthor.Trim();
        }

        var section = document.AddSection();
        ApplyPageSetup(section, options, resolver.Page);

        var pageWidth = section.PageSetup.PageWidth.Point;
        var contentWidth = pageWidth
            - section.PageSetup.LeftMargin.Point
            - section.PageSetup.RightMargin.Point;

        // Compute the root and body styles top-down so inherited values flow correctly.
        var rootStyle = ComputedStyle.CreateRoot();
        var htmlElement = dom.DocumentElement;
        var htmlStyle = htmlElement != null ? resolver.Compute(htmlElement, rootStyle) : rootStyle;
        var body = dom.Body ?? htmlElement;
        var bodyStyle = body != null ? resolver.Compute(body, htmlStyle) : htmlStyle;

        var images = new ImageResolver(resolvedBase, options.AllowRemoteImages, warnings);

        using (var measure = new MeasureHelper())
        {
            var composer = new HtmlDocumentComposer(
                section, resolver, warnings, images, measure,
                contentWidth, options.GenerateOutline);

            SetNormalStyle(document, bodyStyle, composer);
            AddPageFurniture(section, options, title, bodyStyle, composer);

            if (body != null)
            {
                composer.ComposeBody(body, bodyStyle);
            }

            var renderer = new PdfDocumentRenderer(unicode: true) { Document = document };
            renderer.RenderDocument();

            if (outputPdfPath != null)
            {
                var fullPath = Path.GetFullPath(outputPdfPath);
                var directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory)) { Directory.CreateDirectory(directory); }
                renderer.PdfDocument.Save(fullPath);
                return new HtmlRenderResult(fullPath, null, renderer.PdfDocument.PageCount, title, warnings);
            }

            using (var stream = new MemoryStream())
            {
                renderer.PdfDocument.Save(stream);
                return new HtmlRenderResult(null, stream.ToArray(), renderer.PdfDocument.PageCount, title, warnings);
            }
        }
    }

    private static void CollectAuthorStylesheets(IHtmlDocument dom, StyleResolver resolver, string baseDirectory, RenderWarnings warnings)
    {
        // Document order: link and style elements contribute in the order they appear.
        foreach (var element in dom.QuerySelectorAll("link[rel='stylesheet'], style"))
        {
            if (element.LocalName.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                resolver.AddStylesheet(element.TextContent, isDefaultSheet: false);
                continue;
            }

            var href = element.GetAttribute("href")?.Trim();
            if (string.IsNullOrEmpty(href)) { continue; }

            if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                warnings.Add(RenderWarnings.CategoryCss,
                    $"Remote stylesheet '{href}' was skipped; only local stylesheet files are supported.");
                continue;
            }

            var path = Path.IsPathRooted(href)
                ? href
                : Path.Combine(baseDirectory, href.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                var unescaped = Uri.UnescapeDataString(path);
                if (File.Exists(unescaped)) { path = unescaped; }
            }

            if (!File.Exists(path))
            {
                warnings.Add(RenderWarnings.CategoryCss, $"Linked stylesheet '{href}' was not found and was skipped.");
                continue;
            }

            resolver.AddStylesheet(File.ReadAllText(path), isDefaultSheet: false);
        }
    }

    private static void ApplyPageSetup(Section section, HtmlRenderOptions options, PageStyle page)
    {
        var width = page.PageWidthPoints ?? options.PageWidthPoints;
        var height = page.PageHeightPoints ?? options.PageHeightPoints;
        var landscape = page.Landscape ?? options.Landscape;
        if (landscape && height > width)
        {
            (width, height) = (height, width);
        }

        section.PageSetup.PageWidth = Unit.FromPoint(width);
        section.PageSetup.PageHeight = Unit.FromPoint(height);
        section.PageSetup.TopMargin = Unit.FromPoint(page.MarginTopPoints ?? options.MarginTopPoints);
        section.PageSetup.RightMargin = Unit.FromPoint(page.MarginRightPoints ?? options.MarginRightPoints);
        section.PageSetup.BottomMargin = Unit.FromPoint(page.MarginBottomPoints ?? options.MarginBottomPoints);
        section.PageSetup.LeftMargin = Unit.FromPoint(page.MarginLeftPoints ?? options.MarginLeftPoints);
    }

    private static void SetNormalStyle(Document document, ComputedStyle bodyStyle, HtmlDocumentComposer composer)
    {
        var normal = document.Styles["Normal"];
        normal.Font.Name = composer.ResolveFaceFor(bodyStyle);
        normal.Font.Size = Unit.FromPoint(bodyStyle.FontSizePoints);
        normal.Font.Color = bodyStyle.TextColor;
    }

    private static void AddPageFurniture(Section section, HtmlRenderOptions options, string title, ComputedStyle bodyStyle, HtmlDocumentComposer composer)
    {
        if (!string.IsNullOrWhiteSpace(options.HeaderText))
        {
            WriteFurniture(section.Headers.Primary.AddParagraph(), options.HeaderText, title, bodyStyle, composer);
        }

        if (!string.IsNullOrWhiteSpace(options.FooterText))
        {
            WriteFurniture(section.Footers.Primary.AddParagraph(), options.FooterText, title, bodyStyle, composer);
        }
    }

    private static void WriteFurniture(Paragraph paragraph, string template, string title, ComputedStyle bodyStyle, HtmlDocumentComposer composer)
    {
        paragraph.Format.Alignment = ParagraphAlignment.Center;
        paragraph.Format.Font.Name = composer.ResolveFaceFor(bodyStyle);
        paragraph.Format.Font.Size = Unit.FromPoint(Math.Max(6.0, bodyStyle.FontSizePoints * 0.8));
        paragraph.Format.Font.Color = new Color(0x6a, 0x6a, 0x6a);

        var remaining = template.Replace("{title}", title ?? "");
        while (remaining.Length > 0)
        {
            var pageIndex = remaining.IndexOf("{page}", StringComparison.Ordinal);
            var pagesIndex = remaining.IndexOf("{pages}", StringComparison.Ordinal);

            int tokenIndex;
            bool isPages;
            if (pageIndex < 0 && pagesIndex < 0)
            {
                paragraph.AddText(remaining);
                break;
            }
            if (pagesIndex >= 0 && (pageIndex < 0 || pagesIndex < pageIndex))
            {
                tokenIndex = pagesIndex;
                isPages = true;
            }
            else
            {
                tokenIndex = pageIndex;
                isPages = false;
            }

            if (tokenIndex > 0) { paragraph.AddText(remaining.Substring(0, tokenIndex)); }
            if (isPages)
            {
                paragraph.AddNumPagesField();
                remaining = remaining.Substring(tokenIndex + "{pages}".Length);
            }
            else
            {
                paragraph.AddPageField();
                remaining = remaining.Substring(tokenIndex + "{page}".Length);
            }
        }
    }
}
