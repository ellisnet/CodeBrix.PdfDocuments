namespace CodeBrix.PdfDocCreate.Html2Pdf;

/// <summary>
/// Options for rendering HTML to PDF. Every value has a sensible default; @page rules
/// in the document's CSS override the page size and margins configured here.
/// </summary>
public sealed class HtmlRenderOptions
{
    /// <summary>Page width in points. The default is US Letter (612 x 792).</summary>
    public double PageWidthPoints { get; set; } = 612;

    /// <summary>Page height in points. The default is US Letter (612 x 792).</summary>
    public double PageHeightPoints { get; set; } = 792;

    /// <summary>Renders with width and height swapped when true.</summary>
    public bool Landscape { get; set; }

    /// <summary>Top page margin in points (default 72 = 1 inch).</summary>
    public double MarginTopPoints { get; set; } = 72;

    /// <summary>Right page margin in points (default 72 = 1 inch).</summary>
    public double MarginRightPoints { get; set; } = 72;

    /// <summary>Bottom page margin in points (default 72 = 1 inch).</summary>
    public double MarginBottomPoints { get; set; } = 72;

    /// <summary>Left page margin in points (default 72 = 1 inch).</summary>
    public double MarginLeftPoints { get; set; } = 72;

    /// <summary>
    /// Centered header text for every page; null renders no header. The tokens
    /// {page}, {pages} and {title} expand to the page number, total page count, and
    /// document title.
    /// </summary>
    public string HeaderText { get; set; }

    /// <summary>
    /// Centered footer text for every page; null renders no footer. Supports the same
    /// tokens as <see cref="HeaderText"/>.
    /// </summary>
    public string FooterText { get; set; }

    /// <summary>
    /// When true, img elements with http(s) sources are downloaded. Off by default -
    /// author-created documents normally reference local files or data: URIs.
    /// </summary>
    public bool AllowRemoteImages { get; set; }

    /// <summary>Builds the PDF outline (bookmark pane) from h1-h6 headings. On by default.</summary>
    public bool GenerateOutline { get; set; } = true;

    /// <summary>
    /// Overrides the document title; when null the HTML title element is used.
    /// </summary>
    public string DocumentTitle { get; set; }

    /// <summary>Document author written to the PDF metadata; optional.</summary>
    public string DocumentAuthor { get; set; }

    /// <summary>Applies a named page size ("letter", "legal", "a4", "a5", ...).</summary>
    public void SetPageSize(string name)
    {
        if (Css.PageStyle.TryGetNamedSize(name, out var width, out var height))
        {
            PageWidthPoints = width;
            PageHeightPoints = height;
        }
        else
        {
            throw new System.ArgumentException($"'{name}' is not a recognized page size name.", nameof(name));
        }
    }

    internal HtmlRenderOptions Clone() => (HtmlRenderOptions)MemberwiseClone();
}
