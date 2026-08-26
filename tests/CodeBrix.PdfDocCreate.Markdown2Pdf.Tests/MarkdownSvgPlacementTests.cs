using System;
using System.Text;
using CodeBrix.PdfDocCreate.Html2Pdf;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Tests;

public class MarkdownSvgPlacementTests
{
    private const string RedSquareDataUri =
        "data:image/svg+xml;base64,PHN2ZyB4bWxucz0naHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmcnIHdpZHRoPSczMicgaGVpZ2h0PSczMic+PHJlY3Qgd2lkdGg9JzMyJyBoZWlnaHQ9JzMyJyBmaWxsPScjZmYwMDAwJy8+PC9zdmc+";

    private static int CountImageXObjects(byte[] pdfBytes)
    {
        // Object dictionaries are plain text even when streams are compressed. Count image
        // XObjects by their subtype - every page also names /ImageB /ImageC /ImageI in its
        // ProcSet, which is not an image.
        var text = Encoding.Latin1.GetString(pdfBytes);
        return System.Text.RegularExpressions.Regex.Matches(text, @"/Subtype\s*/Image\b").Count;
    }

    [Fact]
    public void svg_placement_defaults_to_vector_and_is_forwarded_to_html2pdf()
    {
        //Arrange
        var vector = new MarkdownPdfRenderer();
        var raster = new MarkdownPdfRenderer();
        raster.Options.SvgPlacement = SvgPlacementMode.Raster;
        var markdown = $"# Title\n\n![square]({RedSquareDataUri})\n";

        //Act
        var vectorResult = vector.RenderMarkdownToBytes(markdown);
        var rasterResult = raster.RenderMarkdownToBytes(markdown);

        //Assert
        new MarkdownRenderOptions().SvgPlacement.Should().Be(SvgPlacementMode.Vector);
        CountImageXObjects(vectorResult.PdfBytes).Should().Be(0);
        CountImageXObjects(rasterResult.PdfBytes).Should().BeGreaterThan(0);
    }
}
