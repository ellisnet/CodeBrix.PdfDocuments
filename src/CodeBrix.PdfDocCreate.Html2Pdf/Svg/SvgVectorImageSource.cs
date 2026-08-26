using System;
using System.IO;
using CodeBrix.PdfDocuments.Drawing;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// An SVG placed as vectors: the document object model lays it out at its natural size
/// (or the size CSS asked for), and at render time <see cref="Draw"/> writes the display
/// list into the page through <see cref="SvgVectorEmitter"/>. No bitmap is created for
/// the picture itself; only a part PDF cannot express is rasterized, by the emitter.
/// </summary>
internal sealed class SvgVectorImageSource : ImageSource.IVectorImageSource
{
    private readonly LoadedSvg _svg;
    private readonly string _reference;
    private readonly double _rasterScale;
    private readonly RenderWarnings _warnings;

    public SvgVectorImageSource(LoadedSvg svg, string name, string reference, double rasterScale, RenderWarnings warnings)
    {
        _svg = svg ?? throw new ArgumentNullException(nameof(svg));
        Name = name;
        _reference = reference;
        _rasterScale = rasterScale;
        _warnings = warnings;
    }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public double WidthPoints => _svg.NaturalWidthPoints;

    /// <inheritdoc />
    public double HeightPoints => _svg.NaturalHeightPoints;

    /// <inheritdoc />
    public int Width => Math.Max(1, (int)Math.Round(WidthPoints));

    /// <inheritdoc />
    public int Height => Math.Max(1, (int)Math.Round(HeightPoints));

    /// <inheritdoc />
    public bool Transparent => true;

    /// <inheritdoc />
    public void Draw(XGraphics graphics, XRect destination)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        var picture = _svg.Picture;
        var cull = picture.CullRect;
        if (cull.Width <= 0 || cull.Height <= 0 || destination.Width <= 0 || destination.Height <= 0) { return; }

        // Map the picture's own bounds onto the placed rectangle, and never paint outside it.
        graphics.IntersectClip(destination);
        graphics.TranslateTransform(destination.X, destination.Y);
        graphics.ScaleTransform(destination.Width / cull.Width, destination.Height / cull.Height);
        graphics.TranslateTransform(-cull.Left, -cull.Top);

        var state = graphics.Save();
        try
        {
            new SvgVectorEmitter(graphics, picture, _reference, _rasterScale, _warnings, _svg.FontMap).Emit();
        }
        catch (Exception ex)
        {
            // The renderer above would paint a grey "image could not be read" box and say
            // nothing. Say what happened, and place the whole picture as a raster instead -
            // whatever vector content already landed is the same geometry underneath.
            graphics.Restore(state);
            _warnings?.Add(RenderWarnings.CategoryImage,
                $"SVG image '{_reference}' could not be written as vectors ({ex.GetType().Name}: {ex.Message}); the whole picture was rasterized instead.",
                "image.svg.rasterized");
            DrawWholeRaster(graphics, cull);
        }
    }

    private void DrawWholeRaster(XGraphics graphics, CodeBrix.Imaging.Drawing.NoSkia.DrawingRect cull)
    {
        byte[] png;
        try
        {
            var scale = Math.Min(_rasterScale, 10000.0 / Math.Max(cull.Width, cull.Height));
            png = _svg.Document.RasterizeToPng((float)scale);
        }
        catch (Exception ex)
        {
            _warnings?.Add(RenderWarnings.CategoryImage,
                $"SVG image '{_reference}' could not be rasterized either ({ex.GetType().Name}) and was skipped.", "image.svg.failed");
            return;
        }

        using var image = XImage.FromStream(() => new MemoryStream(png, writable: false));
        graphics.DrawImage(image, new XRect(cull.Left, cull.Top, cull.Width, cull.Height));
    }

    /// <inheritdoc />
    public void SaveAsJpeg(MemoryStream ms) => throw new NotSupportedException("A vector image source has no pixels.");

    /// <inheritdoc />
    public void SaveAsPdfBitmap(MemoryStream ms) => throw new NotSupportedException("A vector image source has no pixels.");

    /// <inheritdoc />
    public void Dispose()
    {
        // The loaded document owns nothing that needs releasing; the picture stays
        // valid for the life of the render.
    }
}
