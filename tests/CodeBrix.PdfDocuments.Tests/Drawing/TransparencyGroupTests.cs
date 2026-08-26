using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Drawing;

/// <summary>
/// Transparency groups (XForm.MakeTransparencyGroup + XGraphics.DrawTransparencyGroup) and
/// the multi-stop shading brush, checked on rasterized pages.
/// </summary>
public class TransparencyGroupTests
{
    private static byte[] Save(PdfDocument document)
    {
        using var stream = new MemoryStream();
        document.Save(stream);
        return stream.ToArray();
    }

    private static async Task<CodeBrix.Imaging.Image<Rgba32>> Rasterize(byte[] pdf)
    {
        using var rasterizer = new PageRasterizer { Dpi = 72 };
        using var raster = await rasterizer.RasterizeToImage(pdf, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        return raster.CloneAs<Rgba32>();
    }

    [Fact]
    public async Task a_group_with_opacity_composites_its_overlapping_content_once()
    {
        //Arrange - two overlapping red squares in a half-opaque group over white.
        var document = new PdfDocument();
        var page = document.AddPage();
        page.Width = 200;
        page.Height = 100;
        using var gfx = XGraphics.FromPdfPage(page);
        var form = new XForm(document, new XRect(0, 0, 200, 100));
        form.MakeTransparencyGroup();
        using (var formGfx = XGraphics.FromForm(form))
        {
            formGfx.DrawRectangle(XBrushes.Red, new XRect(10, 10, 100, 80));
            formGfx.DrawRectangle(XBrushes.Red, new XRect(80, 10, 100, 80));
        }

        //Act
        gfx.DrawTransparencyGroup(form, new XRect(0, 0, 200, 100), 0.5, XBlendMode.Normal);
        var pdf = Save(document);

        //Assert - the overlap (x 80..110) is the same pink as the single-coverage areas.
        using var rgba = await Rasterize(pdf);
        var single = rgba[40, 50];
        var overlap = rgba[95, 50];
        single.R.Should().BeGreaterThan(200);
        single.G.Should().BeInRange(100, 160);
        overlap.G.Should().BeInRange(100, 160);
        Math.Abs(single.G - overlap.G).Should().BeLessThanOrEqualTo(6);
        Encoding.Latin1.GetString(pdf).Should().Contain("/Transparency");
    }

    [Fact]
    public async Task a_group_with_a_multiply_blend_mode_multiplies_into_the_backdrop()
    {
        //Arrange - cyan multiplied over yellow is green.
        var document = new PdfDocument();
        var page = document.AddPage();
        page.Width = 200;
        page.Height = 100;
        using var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(255, 255, 255, 0)), new XRect(0, 0, 140, 100));
        var form = new XForm(document, new XRect(0, 0, 200, 100));
        form.MakeTransparencyGroup();
        using (var formGfx = XGraphics.FromForm(form))
        {
            formGfx.DrawRectangle(new XSolidBrush(XColor.FromArgb(255, 0, 255, 255)), new XRect(60, 0, 140, 100));
        }

        //Act
        gfx.DrawTransparencyGroup(form, new XRect(0, 0, 200, 100), 1.0, XBlendMode.Multiply);
        var pdf = Save(document);

        //Assert
        using var rgba = await Rasterize(pdf);
        var overlap = rgba[100, 50];
        overlap.G.Should().BeGreaterThan(200);
        overlap.R.Should().BeLessThan(60);
        overlap.B.Should().BeLessThan(60);
        rgba[20, 50].B.Should().BeLessThan(60);
        rgba[180, 50].R.Should().BeLessThan(60);
        Encoding.Latin1.GetString(pdf).Should().Contain("/BM /Multiply");
    }

    [Fact]
    public async Task a_shading_brush_with_three_stops_and_a_brush_matrix_paints_a_stitched_gradient()
    {
        //Arrange - red, green, blue across a 200pt rect; the brush is defined on a unit
        // square and scaled to the rect by its transform.
        var document = new PdfDocument();
        var page = document.AddPage();
        page.Width = 200;
        page.Height = 100;
        using var gfx = XGraphics.FromPdfPage(page);
        var brush = new XShadingBrush(new XPoint(0, 0), new XPoint(1, 0), new[]
        {
            new XGradientStop(0, XColors.Red),
            new XGradientStop(0.5, XColor.FromArgb(255, 0, 255, 0)),
            new XGradientStop(1, XColors.Blue),
        });
        brush.Transform = new XMatrix(200, 0, 0, 100, 0, 0);

        //Act
        gfx.DrawRectangle(brush, new XRect(0, 0, 200, 100));
        var pdf = Save(document);

        //Assert
        using var rgba = await Rasterize(pdf);
        rgba[4, 50].R.Should().BeGreaterThan(200);
        rgba[100, 50].G.Should().BeGreaterThan(150);
        rgba[100, 50].R.Should().BeLessThan(110);
        rgba[195, 50].B.Should().BeGreaterThan(200);
        var text = Encoding.Latin1.GetString(pdf);
        text.Should().Contain("/FunctionType 3");
    }
}
