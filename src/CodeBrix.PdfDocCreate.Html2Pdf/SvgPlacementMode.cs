namespace CodeBrix.PdfDocCreate.Html2Pdf;

/// <summary>
/// How SVG images (referenced files, data: URIs, and inline svg elements) are placed
/// into the PDF.
/// </summary>
public enum SvgPlacementMode
{
    /// <summary>
    /// The default. The SVG's drawing commands are written into the page as PDF vector
    /// operators - paths, strokes, dashes, clips, transforms, text outlines, linear,
    /// radial and focal gradients (as shadings, any number of stops), group opacity and
    /// blend modes (as transparency groups) - so the picture stays sharp at any zoom and
    /// adds no image to the file. A construct PDF cannot express (an image filter such as
    /// a blur, a colour filter, a Porter-Duff compositing mode other than source-over, a
    /// "plus" or "modulate" blend, a difference clip, a cropped or translucent image, a
    /// repeating or reflecting gradient, a gradient whose stops differ in opacity, or a
    /// pattern fill) is rasterized on its own at
    /// <see cref="HtmlRenderOptions.SvgRasterScale"/>, embedded as a transparent PNG, and
    /// reported with the warning code <c>image.svg.rasterized</c>; everything else on
    /// the page stays vector.
    /// </summary>
    Vector = 0,

    /// <summary>
    /// The whole SVG is rasterized in managed code to a transparent PNG at
    /// <see cref="HtmlRenderOptions.SvgRasterScale"/> times its natural CSS-pixel size and
    /// embedded as a bitmap - the placement every version before the vector route used.
    /// </summary>
    Raster = 1,
}
