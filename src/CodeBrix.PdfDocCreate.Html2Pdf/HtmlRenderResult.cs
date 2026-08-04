namespace CodeBrix.PdfDocCreate.Html2Pdf;

/// <summary>The outcome of an HTML-to-PDF render.</summary>
public sealed class HtmlRenderResult
{
    internal HtmlRenderResult(string outputFilePath, byte[] pdfBytes, int pageCount, string title, RenderWarnings warnings)
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

    /// <summary>The document title that was written to the PDF metadata.</summary>
    public string Title { get; }

    /// <summary>
    /// Non-fatal issues encountered while rendering (unsupported CSS, missing images,
    /// unresolved fonts, removed characters). Empty on a perfectly clean render.
    /// </summary>
    public RenderWarnings Warnings { get; }
}
