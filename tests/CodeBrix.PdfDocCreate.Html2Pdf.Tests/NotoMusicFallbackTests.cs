using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocCreate.Html2Pdf.Composition;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// The Noto Music package font is a per-glyph fallback: it renders the musical
/// characters the text families lack - including supplementary-plane characters, which
/// exercise the cmap format 12 and surrogate-pair handling end to end - and is never a
/// body-text default.
/// </summary>
public class NotoMusicFallbackTests
{
    private static async Task<bool> RasterHasDarkPixel(byte[] pdfBytes)
    {
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            pdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var pixel = rgba[x, y];
                if (pixel.R < 100 && pixel.G < 100 && pixel.B < 100) { return true; }
            }
        }

        return false;
    }

    [Fact]
    public void noto_music_is_discovered_and_joins_the_fallback_chain()
    {
        //Arrange
        Html2PdfFonts.EnsureRegistered();

        //Act
        var face = Html2PdfFonts.TryResolveFaceName("NotoMusic", 400, false);
        var flatFallback = Html2PdfFonts.TryResolveFallbackFace(0x266D, 400, false);   // ♭
        var doubleSharpFallback = Html2PdfFonts.TryResolveFallbackFace(0x1D12A, 400, false); // 𝄪

        //Assert
        face.Should().NotBeNull();
        flatFallback.Should().Be(face);
        doubleSharpFallback.Should().Be(face);
        var coverage = Html2PdfFonts.TryGetFaceCoverage(face);
        coverage.Covers(0x266D).Should().BeTrue();
        coverage.Covers(0x1D12A).Should().BeTrue();  // requires the format 12 cmap
        coverage.Covers(0x1D134).Should().BeTrue();  // common time 𝄴
    }

    [Fact]
    public void bmp_music_glyphs_render_without_warnings()
    {
        //Arrange - the LilyPort reproduction: before Noto Music + coverage-driven
        // filtering, every symbol was removed with a warning.
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p>FLAT[♭] NATURAL[♮] SHARP[♯]</p></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Count.Should().Be(0);
    }

    [Fact]
    public void astral_music_glyphs_render_without_warnings()
    {
        //Arrange - all four supplementary-plane symbols from the LilyPort reproduction.
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p>DBLSHARP[\U0001D12A] DBLFLAT[\U0001D12B] COMMON[\U0001D134] CUT[\U0001D135]</p></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Count.Should().Be(0);
    }

    [Fact]
    public async Task astral_music_glyph_visibly_draws_in_the_pdf()
    {
        //Arrange - a page whose ONLY content is one supplementary-plane glyph; a dark
        // pixel proves the glyph mapped (cmap 12), embedded, and rasterized for real.
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p style='font-size:36pt'>\U0001D12A</p></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await RasterHasDarkPixel(result.PdfBytes)).Should().BeTrue();
    }

    [Fact]
    public async Task bmp_music_glyph_visibly_draws_in_the_pdf()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p style='font-size:36pt'>♭</p></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await RasterHasDarkPixel(result.PdfBytes)).Should().BeTrue();
    }

    [Fact]
    public void music_glyphs_inline_in_serif_prose_render_without_warnings()
    {
        //Arrange - the music-glossary case: flats and sharps mid-sentence in prose
        // whose font is chosen by a stylesheet the author does not control.
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p style='font-family:serif'>" +
                            "The chord of B♭ major contains the notes B♭, D and F, " +
                            "while F♯ minor contains F♯, A and C♯.</p></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Count.Should().Be(0);
    }

    [Fact]
    public void noto_music_is_never_a_body_text_default()
    {
        //Arrange - generic families still resolve to the text fonts, and a music
        // character splits into its own run instead of dragging prose into Noto Music.
        var sansFace = Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false);
        var musicFace = Html2PdfFonts.TryResolveFaceName("NotoMusic", 400, false);
        sansFace.Should().NotBe(musicFace);

        //Act
        var warnings = new RenderWarnings();
        var segments = GlyphSafety.Segment("A♭B", sansFace, 400, false, keepUncovered: false, warnings);

        //Assert - prose stays on the styled face; only the flat switches.
        warnings.Count.Should().Be(0);
        segments.Count.Should().Be(3);
        segments[0].Text.Should().Be("A");
        segments[0].FaceName.Should().BeNull();
        segments[1].Text.Should().Be("♭");
        segments[1].FaceName.Should().Be(musicFace);
        segments[2].Text.Should().Be("B");
        segments[2].FaceName.Should().BeNull();
    }
}
