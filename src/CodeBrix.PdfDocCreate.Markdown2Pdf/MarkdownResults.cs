using CodeBrix.PdfDocCreate.Html2Pdf;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf;

/// <summary>The outcome of a Markdown-to-PDF render.</summary>
public sealed class MarkdownRenderResult
{
    internal MarkdownRenderResult(string outputFilePath, byte[] pdfBytes, int pageCount, string title, RenderWarnings warnings)
    {
        OutputFilePath = outputFilePath;
        PdfBytes = pdfBytes;
        PageCount = pageCount;
        Title = title;
        Warnings = warnings;
    }

    /// <summary>Path of the written PDF file; null when the render produced bytes.</summary>
    public string OutputFilePath { get; }

    /// <summary>The PDF content; null when the render wrote to a file.</summary>
    public byte[] PdfBytes { get; }

    /// <summary>Number of pages in the rendered document.</summary>
    public int PageCount { get; }

    /// <summary>The document title that was inferred and written to the PDF metadata.</summary>
    public string Title { get; }

    /// <summary>Non-fatal issues collected while rendering.</summary>
    public RenderWarnings Warnings { get; }
}

/// <summary>
/// The ready-to-render HTML/CSS generated from a Markdown document - the hand-off point
/// for consumers who want to restyle the output before rendering it with
/// <see cref="HtmlPdfRenderer"/>.
/// </summary>
public sealed class MarkdownHtmlResult
{
    internal MarkdownHtmlResult(string bodyHtml, string css, string title, string baseDirectory)
    {
        BodyHtml = bodyHtml;
        Css = css;
        Title = title;
        BaseDirectory = baseDirectory;
    }

    /// <summary>The HTML rendered from the Markdown content (body markup only).</summary>
    public string BodyHtml { get; }

    /// <summary>The default stylesheet, written in the Html2Pdf CSS dialect.</summary>
    public string Css { get; }

    /// <summary>The inferred document title.</summary>
    public string Title { get; }

    /// <summary>
    /// The directory relative image references resolve against (the .md file's folder),
    /// or null when the source was a string.
    /// </summary>
    public string BaseDirectory { get; }

    /// <summary>The complete HTML document (head, style block, body) ready for Html2Pdf.</summary>
    public string ToHtmlDocument() => MarkdownPdfRenderer.BuildHtmlDocument(BodyHtml, Css, Title);

    /// <summary>The complete HTML document with a replacement stylesheet.</summary>
    public string ToHtmlDocument(string replacementCss) =>
        MarkdownPdfRenderer.BuildHtmlDocument(BodyHtml, replacementCss, Title);
}
