using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.PdfDocCreate.Html2Pdf.Svg;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// The vector SVG route: what it writes into the PDF, how it maps the SVG engine's
/// display list onto page operators, and where it falls back to a raster. Pages render
/// at 96 DPI with the default 72pt margins, so an SVG placed at its natural size has
/// its CSS-pixel origin at page pixel (96, 96) and one CSS pixel is one raster pixel.
/// </summary>
public class SvgVectorPlacementTests
{
    private const int Origin = 96;

    private const string RedSquareSvg =
        "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" +
        "<rect width='32' height='32' fill='#ff0000'/></svg>";

    private sealed class Bounds
    {
        public int MinX = int.MaxValue;
        public int MaxX = int.MinValue;
        public int MinY = int.MaxValue;
        public int MaxY = int.MinValue;
        public bool Found => MaxX >= MinX;
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
    }

    private static bool IsRed(Rgba32 pixel) => pixel.R > 180 && pixel.G < 100 && pixel.B < 100;

    private static async Task<CodeBrix.Imaging.Image<Rgba32>> RasterizeFirstPage(byte[] pdfBytes)
    {
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            pdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        return raster.CloneAs<Rgba32>();
    }

    private static async Task<Bounds> FindBounds(byte[] pdfBytes, Func<Rgba32, bool> predicate)
    {
        using var rgba = await RasterizeFirstPage(pdfBytes);
        var bounds = new Bounds();
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                if (predicate(rgba[x, y]))
                {
                    bounds.MinX = Math.Min(bounds.MinX, x);
                    bounds.MaxX = Math.Max(bounds.MaxX, x);
                    bounds.MinY = Math.Min(bounds.MinY, y);
                    bounds.MaxY = Math.Max(bounds.MaxY, y);
                }
            }
        }

        return bounds;
    }

    private static int CountImageXObjects(byte[] pdfBytes)
    {
        // Object dictionaries are plain text even when streams are compressed. Count image
        // XObjects by their subtype - every page also names /ImageB /ImageC /ImageI in its
        // ProcSet, which is not an image.
        var text = Encoding.Latin1.GetString(pdfBytes);
        return System.Text.RegularExpressions.Regex.Matches(text, @"/Subtype\s*/Image\b").Count;
    }

    private static HtmlRenderResult Render(string svg, SvgPlacementMode mode = SvgPlacementMode.Vector)
    {
        var renderer = new HtmlPdfRenderer();
        renderer.Options.SvgPlacement = mode;
        return renderer.RenderHtmlToBytes($"<body>{svg}</body>");
    }

    [Fact]
    public void vector_placement_is_the_default()
    {
        //Arrange + Act + Assert
        new HtmlRenderOptions().SvgPlacement.Should().Be(SvgPlacementMode.Vector);
    }

    [Theory]
    [InlineData(SvgPlacementMode.Vector)]
    [InlineData(SvgPlacementMode.Raster)]
    public async Task the_red_square_renders_at_its_natural_size_in_both_modes(SvgPlacementMode mode)
    {
        //Arrange + Act
        var result = Render(RedSquareSvg, mode);

        //Assert - 32 CSS px is 24pt is 32 raster px at 96 DPI.
        result.Warnings.Count.Should().Be(0);
        var bounds = await FindBounds(result.PdfBytes, IsRed);
        bounds.Found.Should().BeTrue();
        bounds.Width.Should().BeInRange(30, 34);
        bounds.Height.Should().BeInRange(30, 34);
    }

    [Fact]
    public void vector_placement_embeds_no_image_and_raster_placement_embeds_one()
    {
        //Arrange + Act
        var vector = Render(RedSquareSvg, SvgPlacementMode.Vector);
        var raster = Render(RedSquareSvg, SvgPlacementMode.Raster);

        //Assert
        vector.Warnings.Count.Should().Be(0);
        raster.Warnings.Count.Should().Be(0);
        CountImageXObjects(vector.PdfBytes).Should().Be(0);
        CountImageXObjects(raster.PdfBytes).Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task nested_transforms_are_restored_before_a_sibling_draws()
    {
        //Arrange - the bracketing case: a scaled inner group, then a sibling rect that
        // must draw under the OUTER transform only (translated, never scaled). A defect
        // here ships a plausible-looking page with everything after the first group
        // drifting.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" +
            "<g transform='translate(20, 20)'>" +
            "<g transform='scale(2)'><rect x='0' y='0' width='10' height='10' fill='#0000ff'/></g>" +
            "<rect x='0' y='40' width='10' height='10' fill='#ff0000'/>" +
            "</g></svg>";

        //Act
        var result = Render(svg);

        //Assert - red lands at (20, 60), ten pixels square.
        result.Warnings.Count.Should().Be(0);
        var red = await FindBounds(result.PdfBytes, IsRed);
        red.Found.Should().BeTrue();
        red.MinX.Should().BeInRange(Origin + 18, Origin + 22);
        red.MinY.Should().BeInRange(Origin + 58, Origin + 62);
        red.Width.Should().BeInRange(8, 12);
        red.Height.Should().BeInRange(8, 12);

        // And the blue one really was scaled: twenty pixels square at (20, 20).
        var blue = await FindBounds(result.PdfBytes, p => p.B > 180 && p.R < 100 && p.G < 100);
        blue.Found.Should().BeTrue();
        blue.MinX.Should().BeInRange(Origin + 18, Origin + 22);
        blue.Width.Should().BeInRange(18, 22);
    }

    [Fact]
    public async Task rotation_follows_the_svg_transform()
    {
        //Arrange - a 60 x 10 bar rotated a quarter turn about the picture centre becomes
        // a 10 x 60 bar centred at (50, 40). The matrix order between the engine and the
        // page is the thing under test; a transposed matrix lands it elsewhere.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'>" +
            "<g transform='rotate(90 50 50)'><rect x='10' y='45' width='60' height='10' fill='#ff0000'/></g></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        var red = await FindBounds(result.PdfBytes, IsRed);
        red.Found.Should().BeTrue();
        red.Width.Should().BeInRange(8, 12);
        red.Height.Should().BeInRange(58, 62);
        red.MinX.Should().BeInRange(Origin + 43, Origin + 47);
        red.MinY.Should().BeInRange(Origin + 8, Origin + 12);
    }

    [Fact]
    public async Task dash_intervals_are_absolute_units_not_pen_widths()
    {
        //Arrange - five 20px dashes with 20px gaps on a 4px line. The page's dash
        // pattern is expressed in multiples of the pen width, so a pattern handed over
        // unconverted would draw 80px dashes and the row would show two runs, not five.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='20'>" +
            "<line x1='0' y1='10' x2='200' y2='10' stroke='#ff0000' stroke-width='4' stroke-dasharray='20,20'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var runs = 0;
        var inRun = false;
        var y = Origin + 10;
        for (var x = Origin - 2; x < Origin + 202; x++)
        {
            var red = IsRed(rgba[x, y]);
            if (red && !inRun) { runs++; }
            inRun = red;
        }

        runs.Should().BeInRange(4, 6);
    }

    [Fact]
    public async Task zero_width_strokes_draw_nothing()
    {
        //Arrange - SVG says a zero stroke-width paints no stroke; a zero-width PDF line
        // would be the thinnest line the device can draw.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='100' height='20'>" +
            "<line x1='0' y1='10' x2='100' y2='10' stroke='#ff0000' stroke-width='0'/>" +
            "<circle cx='50' cy='10' r='5' fill='none' stroke='#ff0000' stroke-width='0.0'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindBounds(result.PdfBytes, IsRed)).Found.Should().BeFalse();
    }

    [Fact]
    public void declared_physical_units_size_the_picture_exactly()
    {
        //Arrange - 80mm x 30mm is 226.77pt x 85.04pt; the CSS-pixel bounds the engine
        // reports are rounded to whole pixels (302 x 113), which would place 0.1% off.
        Html2PdfFonts.EnsureRegistered();
        var svg = Encoding.UTF8.GetBytes(
            "<svg xmlns='http://www.w3.org/2000/svg' width='80mm' height='30mm' viewBox='0 0 800 300'>" +
            "<rect width='800' height='300' fill='#ff0000'/></svg>");
        var warnings = new RenderWarnings();

        //Act
        var loaded = SvgDocumentLoader.Load(svg, "declared.svg", warnings);

        //Assert
        warnings.Count.Should().Be(0);
        loaded.NaturalWidthPoints.Should().BeApproximately(80.0 * 72.0 / 25.4, 0.001);
        loaded.NaturalHeightPoints.Should().BeApproximately(30.0 * 72.0 / 25.4, 0.001);
    }

    [Fact]
    public void pixel_sized_pictures_are_placed_at_three_quarters_of_a_point_per_pixel()
    {
        //Arrange
        Html2PdfFonts.EnsureRegistered();
        var svg = Encoding.UTF8.GetBytes(RedSquareSvg);
        var warnings = new RenderWarnings();

        //Act
        var loaded = SvgDocumentLoader.Load(svg, "square.svg", warnings);

        //Assert
        loaded.NaturalWidthPoints.Should().BeApproximately(24.0, 0.001);
        loaded.NaturalHeightPoints.Should().BeApproximately(24.0, 0.001);
    }

    [Fact]
    public async Task an_image_filter_falls_back_to_a_raster_and_says_so()
    {
        //Arrange - a blur has no vector expression; the blurred part is rasterized on
        // its own, embedded, and reported. The rest of the page stays vector.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='60'>" +
            "<filter id='b'><feGaussianBlur stdDeviation='2'/></filter>" +
            "<rect x='10' y='10' width='40' height='40' fill='#ff0000' filter='url(#b)'/>" +
            "<rect x='70' y='10' width='40' height='40' fill='#0000ff'/></svg>";

        //Act
        var result = Render(svg);

        //Assert
        var rasterized = result.Warnings.Items.Where(i => i.Code == "image.svg.rasterized").ToList();
        rasterized.Should().NotBeEmpty();
        rasterized[0].Message.Should().Contain("image filter");
        CountImageXObjects(result.PdfBytes).Should().BeGreaterThan(0);
        (await FindBounds(result.PdfBytes, IsRed)).Found.Should().BeTrue();
        (await FindBounds(result.PdfBytes, p => p.B > 180 && p.R < 100 && p.G < 100)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task group_opacity_is_a_transparency_group_not_a_raster()
    {
        //Arrange - a half-transparent group over white must come out pink, not red, and
        // since S2 it does so as a PDF transparency group: no image, no warning.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='60' height='60'>" +
            "<g opacity='0.5'><rect x='10' y='10' width='40' height='40' fill='#ff0000'/></g></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        var pink = await FindBounds(result.PdfBytes, p => p.R > 200 && p.G > 90 && p.G < 170 && p.B > 90 && p.B < 170);
        pink.Found.Should().BeTrue();
        pink.Width.Should().BeInRange(36, 44);
        (await FindBounds(result.PdfBytes, IsRed)).Found.Should().BeFalse();
    }

    [Fact]
    public async Task use_elements_render_through_nested_pictures()
    {
        //Arrange - a <use> is a nested picture in the display list; a visitor that does
        // not descend draws nothing for it.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' width='100' height='100'>" +
            "<defs><rect id='r' width='10' height='10' fill='#ff0000'/></defs>" +
            "<use xlink:href='#r' x='10' y='10'/><use xlink:href='#r' x='40' y='40'/></svg>";

        //Act
        var result = Render(svg);

        //Assert - two squares ten pixels apart in x and y: a 40 x 40 red envelope.
        result.Warnings.Count.Should().Be(0);
        var red = await FindBounds(result.PdfBytes, IsRed);
        red.Found.Should().BeTrue();
        red.Width.Should().BeInRange(38, 42);
        red.Height.Should().BeInRange(38, 42);
        red.MinX.Should().BeInRange(Origin + 8, Origin + 12);
    }

    [Fact]
    public async Task svg_text_is_real_text_in_the_embedded_face_the_engine_measured_with()
    {
        //Arrange - text becomes PDF text: the face is embedded as a subset (a FontFile2
        // stream) with a ToUnicode map, no image, no outline paths.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='160' height='40'>" +
            "<text x='4' y='30' font-family='Roboto' font-weight='bold' font-size='28' fill='#ff0000'>MMM</text></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        var text = Encoding.Latin1.GetString(result.PdfBytes);
        text.Should().Contain("/FontFile2");
        text.Should().Contain("/ToUnicode");
        text.Should().Contain("Roboto,Bold");
        var red = await FindBounds(result.PdfBytes, IsRed);
        red.Found.Should().BeTrue();
        red.Height.Should().BeInRange(16, 24);
    }

    [Fact]
    public async Task stroked_svg_text_stays_glyph_outlines()
    {
        //Arrange - PDF text cannot be stroked with the SVG's pen, so a stroke-only run is
        // emitted as outline paths and embeds no font.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='160' height='40'>" +
            "<text x='4' y='30' font-family='Roboto' font-size='28' fill='none' stroke='#ff0000' stroke-width='1'>MMM</text></svg>";

        //Act
        var result = Render(svg);

        //Assert
        result.Warnings.Count.Should().Be(0);
        Encoding.Latin1.GetString(result.PdfBytes).Should().NotContain("/FontFile2");
        (await FindBounds(result.PdfBytes, IsRed)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task per_character_positions_place_each_glyph_where_the_svg_put_it()
    {
        //Arrange - an x list spreads three glyphs far apart; real text must honour it.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='40'>" +
            "<text x='10 90 170' y='30' font-family='Roboto' font-size='20' fill='#ff0000'>III</text></svg>";

        //Act
        var result = Render(svg);

        //Assert - the red envelope spans from the first glyph to the third.
        result.Warnings.Count.Should().Be(0);
        var red = await FindBounds(result.PdfBytes, IsRed);
        red.Found.Should().BeTrue();
        red.Width.Should().BeInRange(155, 175);
        Encoding.Latin1.GetString(result.PdfBytes).Should().Contain("/FontFile2");
    }

    [Fact]
    public void the_font_bridge_registers_the_faces_the_text_asks_for()
    {
        //Arrange - a serif request at two weights, a comma list whose first candidate
        // is unknown, an unknown family, and the SVG "sans" spelling.
        Html2PdfFonts.EnsureRegistered();
        var svg = new CodeBrix.Imaging.Drawing.NoSkia.Svg.DrawingSvg();
        var requests = new List<SvgFontRequest>
        {
            new SvgFontRequest("serif", 400, false),
            new SvgFontRequest("serif", 700, false),
            new SvgFontRequest("NoSuchFace,serif", 400, true),
            new SvgFontRequest("NoSuchFontFamilyAnywhere", 400, false),
            new SvgFontRequest("sans", 400, false),
        };

        //Act
        SvgFontBridge.Register(svg.Fonts, requests);

        //Assert - the default sans face first (the engine's fallback), then one entry per
        // distinct face-and-name; the unknown-only family gets the default sans face too.
        var names = svg.Fonts.GetRegisteredFamilyNames();
        names.Count.Should().Be(6);
        names[1].Should().Be("serif");
        names[2].Should().Be("serif");
        names[3].Should().Be("serif");
        names[4].Should().Be("NoSuchFontFamilyAnywhere");
        names[5].Should().Be("sans");
        svg.Fonts.TryGetFontData("serif", out _, out var resolved).Should().BeTrue();
        resolved.Should().NotBeNull();
    }

    [Fact]
    public void malformed_svg_warns_and_is_skipped()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes("<body><p>Text</p><svg xmlns='http://www.w3.org/2000/svg' width='10'><rect</svg></body>");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Items.Any(i => i.Code == "image.svg.failed").Should().BeTrue();
    }
    [Fact]
    public async Task dotted_strokes_with_zero_length_dashes_render_as_dots()
    {
        //Arrange - how engraving software writes a dotted line: zero-length dashes with
        // round caps. PDF allows a zero dash element (only negatives and all-zero arrays
        // are invalid); the page's pen used to reject it, and the whole picture fell over.
        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='200' height='20'>" +
            "<line x1='4' y1='10' x2='196' y2='10' stroke='#ff0000' stroke-width='6' stroke-linecap='round' stroke-dasharray='0,12'/></svg>";

        //Act
        var result = Render(svg);

        //Assert - separate dots along the row, not a solid line and not a grey box.
        result.Warnings.Count.Should().Be(0);
        CountImageXObjects(result.PdfBytes).Should().Be(0);
        using var rgba = await RasterizeFirstPage(result.PdfBytes);
        var runs = 0;
        var inRun = false;
        for (var x = Origin; x < Origin + 200; x++)
        {
            var red = IsRed(rgba[x, Origin + 10]);
            if (red && !inRun) { runs++; }
            inRun = red;
        }

        runs.Should().BeInRange(12, 18);
    }
}
