using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// The S2 fidelity features of the vector SVG route: gradients as PDF shadings (with the
/// brush-space matrix that keeps a bounding-box gradient's direction on a non-square
/// shape), group opacity and blend modes as transparency groups, and the cases that
/// still have to fall back to a raster. Pages render at 96 DPI with 72pt margins, so an
/// SVG at its natural size has its CSS-pixel origin at page pixel (96, 96).
/// </summary>
public class SvgVectorFidelityTests
{
    private const int Origin = 96;

    private static async Task<CodeBrix.Imaging.Image<Rgba32>> RasterizeFirstPage(byte[] pdfBytes)
    {
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            pdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        return raster.CloneAs<Rgba32>();
    }

    private static int CountImageXObjects(byte[] pdfBytes)
        => System.Text.RegularExpressions.Regex.Matches(Encoding.Latin1.GetString(pdfBytes), @"/Subtype\s*/Image\b").Count;

    private static HtmlRenderResult Render(string svg)
        => new HtmlPdfRenderer().RenderHtmlToBytes($"<body>{svg}</body>");

    private static Rgba32 Pixel(CodeBrix.Imaging.Image<Rgba32> image, int cssX, int cssY)
        => image[Origin + cssX, Origin + cssY];

    [Fact]
    public async Task a_two_stop_linear_gradient_is_a_shading()
    {
        //Arrange - red at the left edge, blue at the right, in user space.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' y1='0' x2='100' y2='0'>" +
            "<stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#0000ff'/></linearGradient></defs>" +
            "<rect width='100' height='40' fill='url(#g)'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var left = Pixel(rgba, 3, 20);
        var middle = Pixel(rgba, 50, 20);
        var right = Pixel(rgba, 97, 20);
        left.R.Should().BeGreaterThan(200); left.B.Should().BeLessThan(60);
        right.B.Should().BeGreaterThan(200); right.R.Should().BeLessThan(60);
        middle.R.Should().BeInRange(80, 180); middle.B.Should().BeInRange(80, 180);
    }

    [Fact]
    public async Task a_multi_stop_gradient_stitches_its_intervals()
    {
        //Arrange - red, green at the half-way stop, blue.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' y1='0' x2='100' y2='0'>" +
            "<stop offset='0' stop-color='#ff0000'/><stop offset='0.5' stop-color='#00ff00'/>" +
            "<stop offset='1' stop-color='#0000ff'/></linearGradient></defs>" +
            "<rect width='100' height='40' fill='url(#g)'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var middle = Pixel(rgba, 50, 20);
        middle.G.Should().BeGreaterThan(150);
        middle.R.Should().BeLessThan(110);
        middle.B.Should().BeLessThan(110);
        Pixel(rgba, 3, 20).R.Should().BeGreaterThan(200);
        Pixel(rgba, 97, 20).B.Should().BeGreaterThan(200);
    }

    [Fact]
    public async Task a_bounding_box_gradient_keeps_its_diagonal_on_a_wide_shape()
    {
        //Arrange - SVG's default gradientUnits is objectBoundingBox: this diagonal runs
        // corner to corner of a 200 x 20 rect, so the top-RIGHT corner sits half-way
        // (purple). Mapping only the end points through the box (no brush matrix) would
        // make that corner nearly blue.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='20'>" +
            "<defs><linearGradient id='g' x1='0' y1='0' x2='1' y2='1'>" +
            "<stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#0000ff'/></linearGradient></defs>" +
            "<rect width='200' height='20' fill='url(#g)'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var topLeft = Pixel(rgba, 2, 2);
        var bottomRight = Pixel(rgba, 197, 17);
        var topRight = Pixel(rgba, 197, 2);
        topLeft.R.Should().BeGreaterThan(200); topLeft.B.Should().BeLessThan(70);
        bottomRight.B.Should().BeGreaterThan(200); bottomRight.R.Should().BeLessThan(70);
        topRight.R.Should().BeInRange(80, 180);
        topRight.B.Should().BeInRange(80, 180);
    }

    [Fact]
    public async Task a_radial_gradient_is_a_shading()
    {
        //Arrange - red in the centre, blue at the rim.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" +
            "<defs><radialGradient id='g'><stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#0000ff'/></radialGradient></defs>" +
            "<circle cx='50' cy='50' r='48' fill='url(#g)'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var centre = Pixel(rgba, 50, 50);
        var rim = Pixel(rgba, 50, 6);
        centre.R.Should().BeGreaterThan(200); centre.B.Should().BeLessThan(70);
        rim.B.Should().BeGreaterThan(170); rim.R.Should().BeLessThan(100);
    }

    [Fact]
    public async Task a_focal_radial_gradient_is_a_two_circle_shading()
    {
        //Arrange - a focal point off centre is a two-point conical gradient.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" +
            "<defs><radialGradient id='g' fx='0.25' fy='0.5'><stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#0000ff'/></radialGradient></defs>" +
            "<circle cx='50' cy='50' r='48' fill='url(#g)'/></svg>";

        //Act
        var result = Render(svg);

        //Assert - the red pole moved left of centre.
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var focus = Pixel(rgba, 26, 50);
        var centre = Pixel(rgba, 50, 50);
        focus.R.Should().BeGreaterThan(200);
        centre.B.Should().BeGreaterThan(focus.B);
    }

    [Fact]
    public async Task a_gradient_stroke_is_a_shading_pen()
    {
        //Arrange
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='100'>" +
            "<stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#0000ff'/></linearGradient></defs>" +
            "<line x1='0' y1='20' x2='100' y2='20' stroke='url(#g)' stroke-width='10'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        Pixel(rgba, 3, 20).R.Should().BeGreaterThan(200);
        Pixel(rgba, 97, 20).B.Should().BeGreaterThan(200);
    }

    [Fact]
    public async Task group_opacity_composites_overlapping_children_once()
    {
        //Arrange - two overlapping rects in a half-transparent group: the overlap must be
        // the same pink as the rest (one group composite), not the darker double coverage
        // that multiplying the opacity into each child would give.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<g opacity='0.5'><rect x='0' y='0' width='60' height='40' fill='#ff0000'/>" +
            "<rect x='40' y='0' width='60' height='40' fill='#ff0000'/></g></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var single = Pixel(rgba, 20, 20);
        var overlap = Pixel(rgba, 50, 20);
        single.G.Should().BeInRange(100, 160);
        overlap.G.Should().BeInRange(100, 160);
        Math.Abs(single.G - overlap.G).Should().BeLessThanOrEqualTo(8);
    }

    [Fact]
    public async Task fill_opacity_on_a_gradient_becomes_a_group_of_one()
    {
        //Arrange - a shading carries no alpha; the paint opacity goes on a group around it.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<defs><linearGradient id='g' gradientUnits='userSpaceOnUse' x1='0' x2='100'>" +
            "<stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#ff0000'/></linearGradient></defs>" +
            "<rect width='100' height='40' fill='url(#g)' fill-opacity='0.5'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var pixel = Pixel(rgba, 50, 20);
        pixel.R.Should().BeGreaterThan(200);
        pixel.G.Should().BeInRange(100, 160);
    }

    [Fact]
    public void a_translucent_gradient_stop_still_falls_back_to_a_raster()
    {
        //Arrange
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<defs><linearGradient id='g'><stop offset='0' stop-color='#ff0000' stop-opacity='0.5'/><stop offset='1' stop-color='#0000ff'/></linearGradient></defs>" +
            "<rect width='100' height='40' fill='url(#g)'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Items.Any(i => i.Code == "image.svg.rasterized" && i.Message.Contains("translucent stops")).Should().BeTrue();
        CountImageXObjects(result.PdfBytes).Should().BeGreaterThan(0);
    }

    [Fact]
    public void a_repeating_gradient_still_falls_back_to_a_raster()
    {
        //Arrange
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<defs><linearGradient id='g' x1='0' x2='0.25' spreadMethod='repeat'><stop offset='0' stop-color='#ff0000'/><stop offset='1' stop-color='#0000ff'/></linearGradient></defs>" +
            "<rect width='100' height='40' fill='url(#g)'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Items.Any(i => i.Code == "image.svg.rasterized" && i.Message.Contains("repeating gradient")).Should().BeTrue();
        CountImageXObjects(result.PdfBytes).Should().BeGreaterThan(0);
    }
    [Fact]
    public async Task percentage_rgba_colours_come_through_the_svg_engine()
    {
        //Arrange - the form LilyPond's SVG backend writes for every colour. Up to
        // CodeBrix.SvgParse 1.0.238.103 it parsed as opaque black (every channel lost); the
        // fix arrives through the SVG engine package this project pins, and this test is the
        // tripwire against a pin that loses it again.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='40'>" +
            "<g fill='currentColor' color='black'>" +
            "<g color='rgba(100.0000%, 0.0000%, 0.0000%, 50.0000%)'><rect x='0' y='0' width='50' height='40' fill='currentColor'/></g>" +
            "<g color='rgba(0.0000%, 0.0000%, 100.0000%, 0.0000%)'><rect x='50' y='0' width='50' height='40' fill='currentColor'/></g>" +
            "</g></svg>";

        //Act
        var result = Render(svg);

        //Assert - half-opaque red over white is pink; a fully transparent rect is invisible.
        result.Warnings.Count.Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var left = Pixel(rgba, 25, 20);
        var right = Pixel(rgba, 75, 20);
        left.R.Should().BeGreaterThan(200);
        left.G.Should().BeInRange(100, 160);
        right.R.Should().BeGreaterThan(240);
        right.G.Should().BeGreaterThan(240);
        right.B.Should().BeGreaterThan(240);
    }
}
