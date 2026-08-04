using System.Linq;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

public class Html2PdfFontsTests
{
    [Fact]
    public void discovers_the_three_default_package_families()
    {
        //Arrange / Act
        var families = Html2PdfFonts.RegisteredFamilyNames.ToArray();

        //Assert
        families.Should().Contain("Roboto");
        families.Should().Contain("Merriweather");
        families.Should().Contain("RobotoMono");
        Html2PdfFonts.HasDefaultFamilies.Should().BeTrue();
    }

    [Fact]
    public void generic_sans_serif_resolves_to_roboto_regular()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("sans-serif", 400, italic: false);

        //Assert
        face.Should().Be("Roboto-Regular");
    }

    [Fact]
    public void generic_serif_resolves_to_merriweather()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("serif", 400, italic: false);

        //Assert
        face.Should().Be("Merriweather-Regular");
    }

    [Fact]
    public void generic_monospace_resolves_to_roboto_mono()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("monospace", 400, italic: false);

        //Assert
        face.Should().Be("RobotoMono-Regular");
    }

    [Fact]
    public void numeric_weight_600_selects_the_semibold_face()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("Roboto", 600, italic: false);

        //Assert
        face.Should().Be("Roboto-SemiBold");
    }

    [Fact]
    public void bold_italic_selects_the_bold_italic_face()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("Roboto", 700, italic: true);

        //Assert
        face.Should().Be("Roboto-BoldItalic");
    }

    [Fact]
    public void family_name_matching_ignores_spaces_and_case()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("roboto mono", 700, italic: false);

        //Assert
        face.Should().Be("RobotoMono-Bold");
    }

    [Fact]
    public void unknown_family_returns_null()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("Comic Sans MS", 400, italic: false);

        //Assert
        face.Should().BeNull();
    }

    [Fact]
    public void unmatched_weight_falls_back_to_the_nearest_face()
    {
        //Arrange / Act
        var face = Html2PdfFonts.TryResolveFaceName("Roboto", 900, italic: false);

        //Assert - Roboto's heaviest static weight is ExtraBold (800)
        face.Should().Be("Roboto-ExtraBold");
    }
}
