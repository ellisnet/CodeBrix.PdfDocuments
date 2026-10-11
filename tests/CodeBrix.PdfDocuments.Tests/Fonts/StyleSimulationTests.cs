using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Fonts;
using CodeBrix.PdfDocuments.Pdf;
using SilverAssertions;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Fonts;

/// <summary>
/// Fences the style simulations a font resolver asks for (FontResolverInfo.MustSimulateBold / MustSimulateItalic):
/// XGlyphTypeface used to drop them, so a family with no bold or italic face - Android's single variable-font
/// Roboto - drew bold and italic text as plain regular text.
/// </summary>
public class StyleSimulationTests
{
    private const string RobotoResourceName = "CodeBrix.PdfDocuments.Tests.SampleFiles.Roboto-Regular.ttf";

    //A family with a Regular face only, resolved with whatever style is asked for simulated.
    private sealed class RegularOnlyResolver : IFontResolver
    {
        private readonly byte[] _bytes;

        public RegularOnlyResolver(string family, string faceName, byte[] bytes)
        {
            DefaultFontName = family;
            FaceName = faceName;
            _bytes = bytes;
        }

        public string DefaultFontName { get; }

        public string FaceName { get; }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
            new FontResolverInfo(FaceName, isBold, isItalic);

        public byte[] GetFont(string faceName) => _bytes;
    }

    private static string RegisterRegularOnlyFamily()
    {
        using var stream = typeof(StyleSimulationTests).Assembly.GetManifestResourceStream(RobotoResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{RobotoResourceName}' not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var id = Guid.NewGuid().ToString("N");
        var family = "RegularOnly" + id;
        var faceName = "RegularOnly-Regular-" + id;
        _ = GlobalFontSettings.FontResolver;
        MetaFontResolver.Instance.RegisterFontResolver(faceName, new RegularOnlyResolver(family, faceName, memory.ToArray()));
        return family;
    }

    [Theory]
    [InlineData(XFontStyle.Bold, XStyleSimulations.BoldSimulation)]
    [InlineData(XFontStyle.Italic, XStyleSimulations.ItalicSimulation)]
    [InlineData(XFontStyle.BoldItalic, XStyleSimulations.BoldItalicSimulation)]
    [InlineData(XFontStyle.Regular, XStyleSimulations.None)]
    public void A_simulated_style_requested_by_the_resolver_reaches_the_typeface(XFontStyle style, XStyleSimulations expected)
    {
        //Arrange
        var family = RegisterRegularOnlyFamily();

        //Act
        var font = new XFont(family, 12, style);

        //Assert
        font.GlyphTypeface.StyleSimulations.Should().Be(expected);
    }

    [Fact]
    public void Simulated_bold_text_is_written_with_fill_and_stroke_rendering()
    {
        //Arrange
        var family = RegisterRegularOnlyFamily();
        var document = new PdfDocument();
        document.Options.CompressContentStreams = false;
        var page = document.AddPage();

        //Act
        using (var gfx = XGraphics.FromPdfPage(page))
            gfx.DrawString("Bold", new XFont(family, 20, XFontStyle.Bold), XBrushes.Black, new XPoint(50, 50));
        var output = new MemoryStream();
        document.Save(output, false);
        var text = Encoding.Latin1.GetString(output.ToArray());

        //Assert
        text.Should().Contain("2 Tr");
    }
}
