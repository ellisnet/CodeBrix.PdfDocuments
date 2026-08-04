namespace CodeBrix.PdfDocCreate.Markdown2Pdf;

/// <summary>
/// The few knobs Markdown-to-PDF rendering exposes. The design goal is zero
/// configuration: every default produces a nice-looking, pre-formatted, printable PDF.
/// Consumers who want real styling control should use <c>GenerateHtml</c> and restyle
/// the generated HTML/CSS before handing it to Html2Pdf.
/// </summary>
public sealed class MarkdownRenderOptions
{
    /// <summary>Named page size: "letter" (default), "legal", "a4", "a5", ...</summary>
    public string PageSize { get; set; } = "letter";

    /// <summary>
    /// When true, img references with http(s) sources are downloaded. Off by default.
    /// </summary>
    public bool AllowRemoteImages { get; set; }

    /// <summary>
    /// Footer template with {page}, {pages} and {title} tokens. The default renders
    /// centered "page / pages" numbers; set to null for no footer.
    /// </summary>
    public string FooterText { get; set; } = "{page} / {pages}";

    internal MarkdownRenderOptions Clone() => (MarkdownRenderOptions)MemberwiseClone();
}
