using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// The engraved-music corpus gate. Points at a folder of REAL engraving-engine SVG
/// output through the HTML2PDF_LILYPORT_SVG_CORPUS environment variable; when it is
/// unset the test skips. The corpus is GFDL/GPL-3 material that lives outside this
/// repository and is never committed - the test reads it where it is. It places every
/// picture through the vector route into one document and asserts the whole run is
/// clean: no picture failed, nothing fell back to a raster, no image XObject was
/// embedded, and the only warnings are the per-glyph coverage notes the corpus is known
/// to carry (Hebrew, Greek and an arrow in lyrics that no package font covers).
/// </summary>
public class LilyPortCorpusGateTests
{
    private static string TryGetCorpusDirectory()
    {
        var directory = Environment.GetEnvironmentVariable("HTML2PDF_LILYPORT_SVG_CORPUS");
        return !string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory) ? directory : null;
    }

    [Fact]
    public void every_engraving_in_the_corpus_places_as_vectors_without_a_fallback()
    {
        //Arrange
        var directory = TryGetCorpusDirectory();
        Assert.SkipWhen(directory == null, "HTML2PDF_LILYPORT_SVG_CORPUS is not set to an existing folder.");
        var files = Directory.GetFiles(directory, "*.svg").OrderBy(f => f, StringComparer.Ordinal).ToList();
        Assert.SkipWhen(files.Count == 0, "The corpus folder holds no SVG files.");

        var html = new StringBuilder("<html><body>");
        foreach (var file in files)
        {
            html.Append("<p>").Append(Path.GetFileName(file)).Append("</p><img src=\"").Append(Path.GetFileName(file)).Append("\">");
        }
        html.Append("</body></html>");
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes(html.ToString(), directory);

        //Assert
        result.PageCount.Should().BeGreaterThan(0);
        var unexpected = result.Warnings.Items.Where(w => w.Code != "font.svg-text.notdef").ToList();
        unexpected.Should().BeEmpty();
        Regex.Matches(Encoding.Latin1.GetString(result.PdfBytes), @"/Subtype\s*/Image\b").Count.Should().Be(0);
    }
}
