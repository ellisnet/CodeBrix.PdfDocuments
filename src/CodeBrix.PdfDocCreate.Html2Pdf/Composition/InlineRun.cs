using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocuments.Drawing;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// One inline run of a paragraph flow: a piece of styled text, a hard line break, or an
/// inline image.
/// </summary>
internal sealed class InlineRun
{
    public string Text { get; set; } = "";

    /// <summary>Registered package font face the run renders with.</summary>
    public string FaceName { get; set; }

    public double SizePoints { get; set; }

    public Color TextColor { get; set; }

    public bool Underline { get; set; }

    public bool Strikethrough { get; set; }

    public bool Superscript { get; set; }

    public bool Subscript { get; set; }

    /// <summary>Hyperlink target; empty for plain runs. "#name" targets a bookmark.</summary>
    public string Href { get; set; } = "";

    public bool IsLineBreak { get; set; }

    /// <summary>Inline image payload; when set the run renders as an image.</summary>
    public ImageSource.IImageSource Image { get; set; }

    public double? ImageWidthPoints { get; set; }

    public double? ImageHeightPoints { get; set; }
}
