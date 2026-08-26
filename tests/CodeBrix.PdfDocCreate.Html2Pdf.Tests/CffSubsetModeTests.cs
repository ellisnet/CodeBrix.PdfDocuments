using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Pdf.Advanced;
using CodeBrix.PdfDocuments.Pdf.IO;
using SilverAssertions;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// <see cref="HtmlRenderOptions.CffSubsetMode"/> reaches the document: off, a CFF-outline
/// face added through <see cref="Html2PdfFonts.AddFontFile(string, bool)"/> is embedded
/// whole as it always was; on, it is a sparse OpenType subset.
/// </summary>
public class CffSubsetModeTests
{
    private static readonly string FixturePath = Path.Combine(AppContext.BaseDirectory, "SampleFiles", "MathJax_AMS-Regular.otf");
    private const string Html = "<html><body><p style=\"font-family: MathJax_AMS; font-size: 24pt\">ABC</p></body></html>";

    static CffSubsetModeTests()
    {
        Html2PdfFonts.AddFontFile(FixturePath, false);
    }

    [Fact]
    public void the_default_embeds_a_cff_face_whole_as_before()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        renderer.Options.CffSubsetMode.Should().Be(PdfCffSubsetMode.None);

        //Act
        byte[] pdf = renderer.RenderHtmlToBytes(Html, AppContext.BaseDirectory).PdfBytes;
        var (cidSubtype, fontFileKey, programLength) = ReadEmbeddedFont(pdf);

        //Assert
        cidSubtype.Should().Be("/CIDFontType2");
        fontFileKey.Should().Be("/FontFile2");
        programLength.Should().Be(new FileInfo(FixturePath).Length);
        Encoding.ASCII.GetString(pdf, 0, 8).Should().Be("%PDF-1.4");
    }

    [Fact]
    public void sparse_mode_reaches_the_document_and_subsets_the_face()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        renderer.Options.CffSubsetMode = PdfCffSubsetMode.Sparse;

        //Act
        byte[] pdf = renderer.RenderHtmlToBytes(Html, AppContext.BaseDirectory).PdfBytes;
        var (cidSubtype, fontFileKey, programLength) = ReadEmbeddedFont(pdf);

        //Assert
        cidSubtype.Should().Be("/CIDFontType0");
        fontFileKey.Should().Be("/FontFile3");
        programLength.Should().BeLessThan(new FileInfo(FixturePath).Length / 4);
        Encoding.ASCII.GetString(pdf, 0, 8).Should().Be("%PDF-1.6");
    }

    /// <summary>The MathJax font's descendant CIDFont: its subtype, its program key and the program's length.</summary>
    private static (string CidSubtype, string FontFileKey, long ProgramLength) ReadEmbeddedFont(byte[] pdf)
    {
        PdfDocument document = PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import);
        PdfDictionary fonts = document.Pages[0].Resources.Elements.GetDictionary("/Font");
        PdfDictionary type0 = fonts.Elements.Values
            .Select(v => v is PdfReference r ? r.Value as PdfDictionary : v as PdfDictionary)
            .First(d => d != null && d.Elements.GetName("/Subtype") == "/Type0" && d.Elements.GetName("/BaseFont").Contains("MathJax", StringComparison.Ordinal));
        PdfArray descendants = type0.Elements.GetArray("/DescendantFonts");
        PdfDictionary cid = descendants.Elements[0] is PdfReference cidRef ? (PdfDictionary)cidRef.Value : (PdfDictionary)descendants.Elements[0];
        PdfDictionary descriptor = cid.Elements.GetDictionary("/FontDescriptor");
        foreach (string key in new[] { "/FontFile2", "/FontFile3" })
        {
            PdfDictionary stream = descriptor.Elements.GetDictionary(key);
            if (stream != null)
                return (cid.Elements.GetName("/Subtype"), key, stream.Stream.UnfilteredValue.Length);
        }
        throw new InvalidOperationException("No font program is embedded.");
    }
}
