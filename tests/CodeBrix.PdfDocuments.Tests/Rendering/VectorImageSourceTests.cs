using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.Rendering;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Rendering;

/// <summary>
/// The vector-image seam: a document-object-model image whose source draws itself into
/// the page instead of supplying pixels.
/// </summary>
public class VectorImageSourceTests
{
    /// <summary>A 100pt x 50pt drawing that fills its placed rectangle with red.</summary>
    private sealed class RedBoxSource : ImageSource.IVectorImageSource
    {
        public int DrawCalls;
        public XRect LastDestination;

        public string Name => "red-box";
        public int Width => 100;
        public int Height => 50;
        public bool Transparent => true;
        public double WidthPoints => 100;
        public double HeightPoints => 50;

        public void Draw(XGraphics graphics, XRect destination)
        {
            DrawCalls++;
            LastDestination = destination;
            graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(255, 255, 0, 0)), destination);
        }

        public void SaveAsJpeg(MemoryStream ms) => throw new NotSupportedException();
        public void SaveAsPdfBitmap(MemoryStream ms) => throw new NotSupportedException();
        public void Dispose() { }
    }

    private static byte[] RenderToBytes(Document document)
    {
        var renderer = new PdfDocumentRenderer(unicode: true) { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream);
        return stream.ToArray();
    }

    private static async Task<(int MinX, int MinY, int Width, int Height)> FindRed(byte[] pdfBytes)
    {
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(pdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var p = rgba[x, y];
                if (p.R > 180 && p.G < 100 && p.B < 100)
                {
                    minX = Math.Min(minX, x); minY = Math.Min(minY, y);
                    maxX = Math.Max(maxX, x); maxY = Math.Max(maxY, y);
                }
            }
        }

        return maxX >= minX ? (minX, minY, maxX - minX + 1, maxY - minY + 1) : (-1, -1, 0, 0);
    }

    [Fact]
    public async Task a_vector_source_is_laid_out_at_its_natural_size_and_drawn_once()
    {
        //Arrange
        var document = new Document();
        var section = document.AddSection();
        section.PageSetup.LeftMargin = Unit.FromPoint(72);
        section.PageSetup.TopMargin = Unit.FromPoint(72);
        var source = new RedBoxSource();
        section.AddParagraph().AddImage(source);

        //Act
        var pdf = RenderToBytes(document);

        //Assert - 100pt x 50pt at 96 DPI is 133 x 67 pixels, at the 1-inch margins.
        source.DrawCalls.Should().Be(1);
        source.LastDestination.Width.Should().BeApproximately(100, 0.01);
        source.LastDestination.Height.Should().BeApproximately(50, 0.01);
        var red = await FindRed(pdf);
        red.Width.Should().BeInRange(131, 135);
        red.Height.Should().BeInRange(65, 69);
        red.MinX.Should().BeInRange(94, 98);
        System.Text.RegularExpressions.Regex.IsMatch(Encoding.Latin1.GetString(pdf), @"/Subtype\s*/Image\b").Should().BeFalse();
    }

    [Fact]
    public async Task a_vector_source_honours_an_explicit_width_and_keeps_its_aspect()
    {
        //Arrange
        var document = new Document();
        var section = document.AddSection();
        var source = new RedBoxSource();
        var image = section.AddParagraph().AddImage(source);
        image.Width = Unit.FromPoint(200);

        //Act
        var pdf = RenderToBytes(document);

        //Assert - the aspect ratio is locked by default: 200 x 100pt.
        source.LastDestination.Width.Should().BeApproximately(200, 0.01);
        source.LastDestination.Height.Should().BeApproximately(100, 0.01);
        var red = await FindRed(pdf);
        red.Width.Should().BeInRange(264, 270);
        red.Height.Should().BeInRange(131, 136);
    }
}
