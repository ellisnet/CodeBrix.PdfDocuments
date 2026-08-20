using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

public class SvgSupportTests
{
    private const string RedSquareSvg =
        "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" +
        "<rect width='32' height='32' fill='#ff0000'/></svg>";

    private sealed class RedBounds
    {
        public int MinX = int.MaxValue;
        public int MaxX = int.MinValue;
        public int MinY = int.MaxValue;
        public int MaxY = int.MinValue;
        public bool Found => MaxX >= MinX;
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
    }

    private static async Task<RedBounds> FindRedBounds(byte[] pdfBytes)
    {
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            pdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        var bounds = new RedBounds();
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var pixel = rgba[x, y];
                if (pixel.R > 180 && pixel.G < 100 && pixel.B < 100)
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

    [Fact]
    public async Task referenced_svg_file_renders_into_the_pdf()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-svg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "note.svg"), RedSquareSvg);
            var renderer = new HtmlPdfRenderer();

            //Act
            var result = renderer.RenderHtmlToBytes("<body><img src='note.svg'></body>", directory);

            //Assert
            result.Warnings.Count.Should().Be(0);
            (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task base64_svg_data_uri_renders_into_the_pdf()
    {
        //Arrange
        var dataUri = "data:image/svg+xml;base64," +
                      Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(RedSquareSvg));
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body><img src='{dataUri}'></body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task plain_payload_svg_data_uri_renders_into_the_pdf()
    {
        //Arrange - the comma form carries percent-encoded markup rather than base64.
        var dataUri = "data:image/svg+xml," + Uri.EscapeDataString(RedSquareSvg);
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body><img src='{dataUri}'></body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task inline_svg_element_renders_as_a_block_image()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{RedSquareSvg}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task inline_svg_inside_a_paragraph_renders_with_the_text()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body><p>Before {RedSquareSvg} after.</p></body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task css_physical_size_controls_svg_placement()
    {
        //Arrange - 72pt is one inch, which is 96 pixels at the 96 DPI raster used below.
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-svg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "note.svg"), RedSquareSvg);
            var renderer = new HtmlPdfRenderer();

            //Act
            var result = renderer.RenderHtmlToBytes(
                "<body><img src='note.svg' style='width:72pt;height:72pt'></body>", directory);

            //Assert
            result.Warnings.Count.Should().Be(0);
            var bounds = await FindRedBounds(result.PdfBytes);
            bounds.Found.Should().BeTrue();
            bounds.Width.Should().BeInRange(92, 100);
            bounds.Height.Should().BeInRange(92, 100);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task raster_scale_does_not_change_layout_size()
    {
        //Arrange - the raster scale sharpens pixels; the placed size must not move.
        var lowScale = new HtmlPdfRenderer();
        lowScale.Options.SvgRasterScale = 1.0;
        var highScale = new HtmlPdfRenderer();
        highScale.Options.SvgRasterScale = 4.0;
        var html = $"<body>{RedSquareSvg}</body>";

        //Act
        var low = lowScale.RenderHtmlToBytes(html);
        var high = highScale.RenderHtmlToBytes(html);

        //Assert
        var lowBounds = await FindRedBounds(low.PdfBytes);
        var highBounds = await FindRedBounds(high.PdfBytes);
        lowBounds.Found.Should().BeTrue();
        highBounds.Found.Should().BeTrue();
        Math.Abs(lowBounds.Width - highBounds.Width).Should().BeLessThanOrEqualTo(2);
        Math.Abs(lowBounds.Height - highBounds.Height).Should().BeLessThanOrEqualTo(2);
    }

    [Fact]
    public void broken_svg_is_a_warning_not_an_error()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-svg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "bad.svg"), "<svg><rect width='10'");
            var renderer = new HtmlPdfRenderer();

            //Act
            var result = renderer.RenderHtmlToBytes("<body><p>Text</p><img src='bad.svg' alt='gone'></body>", directory);

            //Assert
            result.PdfBytes.Should().NotBeNull();
            result.Warnings.Messages.Any(m => m.Contains("bad.svg")).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task svg_text_renders_with_a_registered_family()
    {
        //Arrange - text set in a package font must rasterize with real glyphs.
        const string textSvg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'>" +
            "<text x='4' y='30' font-family='Roboto' font-size='28' fill='#ff0000'>MMM</text></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{textSvg}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task svg_text_with_unknown_family_falls_back_to_the_default_package_face()
    {
        //Arrange - the typeface chain can only ever return registered font files, so an
        // unknown family resolves like the HTML side does: to the default sans package
        // face. System fonts are structurally unreachable on every operating system.
        const string textSvg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'>" +
            "<text x='4' y='30' font-family='NoSuchFontFamilyAnywhere' font-size='28' fill='#ff0000'>MMM</text></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{textSvg}</body>");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public void svg_text_missing_glyphs_warn_with_code_and_code_point()
    {
        //Arrange - SVG text has no per-glyph fallback, so uncovered characters render
        // as missing-glyph shapes; each must surface as a structured warning with an
        // occurrence count, so coverage gaps are baselined instead of invisible.
        var serifFace = Html2PdfFonts.TryResolveFaceName("serif", 400, false);
        var serifCoverage = Html2PdfFonts.TryGetFaceCoverage(serifFace);
        Assert.SkipWhen(serifCoverage.Covers(0x05D0) || serifCoverage.Covers(0x05D1),
            "The serif font unexpectedly covers Hebrew.");

        const string hebrewSvg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'>" +
            "<text x='4' y='30' font-family='serif' font-size='20'>\u05D0\u05D0\u05D1</text></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{hebrewSvg}</body>");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        var items = result.Warnings.Items.Where(i => i.Code == "font.svg-text.notdef").ToList();
        var alef = items.Single(i => i.CodePoint == 0x05D0);
        alef.Occurrences.Should().Be(2);
        alef.Category.Should().Be(RenderWarningCategory.Font);
        alef.Message.Should().Contain("missing-glyph");
        items.Single(i => i.CodePoint == 0x05D1).Occurrences.Should().Be(1);
    }

    [Fact]
    public async Task svg_font_family_lists_try_each_candidate_in_order()
    {
        //Arrange + Assert - unit level: a comma list resolves per candidate, in order.
        var serifFace = Html2PdfFonts.TryResolveFaceName("serif", 400, false);
        CodeBrix.PdfDocCreate.Html2Pdf.Svg.SvgFontResolution
            .TryResolveFaceName("SomeUnknownFace,serif", 400, false).Should().Be(serifFace);
        CodeBrix.PdfDocCreate.Html2Pdf.Svg.SvgFontResolution
            .TryResolveFaceName("SomeUnknownFace", 400, false).Should().BeNull();
        CodeBrix.PdfDocCreate.Html2Pdf.Svg.SvgFontResolution
            .TryResolveFaceName("'Roboto', serif", 400, false)
            .Should().Be(Html2PdfFonts.TryResolveFaceName("Roboto", 400, false));

        //Arrange - end to end: a glyph only serif covers renders warning-free when the
        // list names serif as the second candidate.
        var sansFace = Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false);
        var sansCoverage = Html2PdfFonts.TryGetFaceCoverage(sansFace);
        var serifCoverage = Html2PdfFonts.TryGetFaceCoverage(serifFace);
        int? probe = null;
        for (var cp = 0x2600; cp <= 0x27BF && probe == null; cp++)
        {
            if (serifCoverage.Covers(cp) && !sansCoverage.Covers(cp)) { probe = cp; }
        }
        Assert.SkipWhen(probe == null, "No code point separates serif and sans coverage in the probe range.");

        var glyph = char.ConvertFromUtf32(probe.Value);
        var svg = "<svg xmlns='http://www.w3.org/2000/svg' width='120' height='40'>" +
                  $"<text x='4' y='30' font-family=\"SomeUnknownFace,serif\" font-size='24' fill='#ff0000'>{glyph}</text></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{svg}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task svg_text_music_glyphs_render_via_the_fallback_chain()
    {
        //Arrange - the snippet-01349 case: a flat (and an astral double sharp) in SVG
        // text whose styled face lacks them. The fallback rewrite hands them to Noto
        // Music, so they draw for real and no notdef warning fires.
        var serifFace = Html2PdfFonts.TryResolveFaceName("serif", 400, false);
        var serifCoverage = Html2PdfFonts.TryGetFaceCoverage(serifFace);
        Assert.SkipWhen(serifCoverage.Covers(0x266D), "The serif font unexpectedly covers the flat sign.");

        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='160' height='40'>" +
            "<text x='4' y='30' font-family='serif' font-size='24' fill='#ff0000'>B♭ and \U0001D12A</text></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{svg}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindRedBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task svg_transparency_survives_into_the_pdf()
    {
        //Arrange - a red circle on a transparent canvas; no black halo may appear.
        const string circleSvg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='32' height='32'>" +
            "<circle cx='16' cy='16' r='12' fill='#ff0000'/></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{circleSvg}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            result.PdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        var sawRed = false;
        var sawBlack = false;
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var pixel = rgba[x, y];
                if (pixel.R > 180 && pixel.G < 100 && pixel.B < 100) { sawRed = true; }
                if (pixel.R < 60 && pixel.G < 60 && pixel.B < 60) { sawBlack = true; }
            }
        }

        sawRed.Should().BeTrue();
        sawBlack.Should().BeFalse();
    }
}
