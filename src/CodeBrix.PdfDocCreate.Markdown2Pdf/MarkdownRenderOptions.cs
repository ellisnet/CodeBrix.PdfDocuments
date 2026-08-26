using CodeBrix.PdfDocCreate.Html2Pdf;

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

    /// <summary>
    /// How SVG images are placed: as PDF vector content (the default) or as rasterized
    /// bitmaps. Forwarded to Html2Pdf; see <see cref="SvgPlacementMode"/>.
    /// </summary>
    public SvgPlacementMode SvgPlacement { get; set; } = SvgPlacementMode.Vector;

    /// <summary>
    /// Scale factor for rasterizing SVG content, relative to the SVG's natural CSS-pixel
    /// size (default 2.0, roughly 192 DPI at natural size): the whole picture in raster
    /// placement, only the parts PDF cannot express in vector placement. Forwarded to
    /// Html2Pdf.
    /// </summary>
    public double SvgRasterScale { get; set; } = 2.0;

    /// <summary>
    /// When false (the default), characters no registered font covers are removed with
    /// a warning; when true they are kept and render as visible missing-glyph shapes.
    /// Forwarded to Html2Pdf.
    /// </summary>
    public bool KeepUncoveredCharacters { get; set; }

    internal MarkdownRenderOptions Clone() => (MarkdownRenderOptions)MemberwiseClone();
}
