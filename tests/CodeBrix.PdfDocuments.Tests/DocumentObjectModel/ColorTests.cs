using System;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.DocumentObjectModel;

public class ColorTests
{
    // ── Parse: CSS "#" hex colors (always opaque) ────────────────────────

    [Theory]
    [InlineData("#fffde7", 255, 253, 231)]
    [InlineData("#c0c0c0", 192, 192, 192)]
    [InlineData("#000000", 0, 0, 0)]
    [InlineData("#FFFFFF", 255, 255, 255)]
    public void Parse_WithCssHexSixDigits_ReturnsOpaqueColor(string text, uint r, uint g, uint b)
    {
        var color = Color.Parse(text);

        color.A.Should().Be(255);
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
    }

    [Theory]
    [InlineData("#ccc", 204, 204, 204)]
    [InlineData("#abc", 170, 187, 204)]
    [InlineData("#F00", 255, 0, 0)]
    public void Parse_WithCssHexShorthand_ExpandsEachDigitAndReturnsOpaqueColor(
        string text, uint r, uint g, uint b)
    {
        var color = Color.Parse(text);

        color.A.Should().Be(255);
        color.R.Should().Be(r);
        color.G.Should().Be(g);
        color.B.Should().Be(b);
    }

    [Fact]
    public void Parse_WithCssHexEightDigits_ThrowsBecauseAlphaOrderIsAmbiguous()
    {
        // CSS writes #rrggbbaa (alpha last) while the "0x" form is 0xAARRGGBB (alpha first),
        // so an eight-digit "#" value is rejected rather than silently misread.
        Action act = () => Color.Parse("#FF0000FF");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("#")]
    [InlineData("#ab")]
    [InlineData("#abcd")]
    [InlineData("#abcde")]
    [InlineData("#abcdefg")]
    public void Parse_WithCssHexOfUnsupportedLength_ThrowsArgumentException(string text)
    {
        Action act = () => Color.Parse(text);

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("#gggggg")]
    [InlineData("#ab ")]
    [InlineData("#12 45 ")]
    public void Parse_WithCssHexContainingNonHexCharacters_ThrowsArgumentException(string text)
    {
        Action act = () => Color.Parse(text);

        act.Should().Throw<ArgumentException>();
    }

    // ── Parse: "0x" packed ARGB (alpha is NOT defaulted) ─────────────────

    [Fact]
    public void Parse_WithZeroXSixDigits_LeavesAlphaAtZero()
    {
        // Deliberate, inherited behaviour: "0x" introduces a packed 0xAARRGGBB integer, so six
        // digits means the alpha byte is zero. Documented as a pitfall rather than "fixed",
        // because callers may depend on it. Use "#c0c0c0" or Color.FromRgb for an opaque color.
        var color = Color.Parse("0xc0c0c0");

        color.A.Should().Be(0);
        color.R.Should().Be(192);
        color.G.Should().Be(192);
        color.B.Should().Be(192);
    }

    [Fact]
    public void Parse_WithZeroXEightDigits_UsesTheSuppliedAlpha()
    {
        var color = Color.Parse("0xFFFFFDE7");

        color.A.Should().Be(255);
        color.R.Should().Be(255);
        color.G.Should().Be(253);
        color.B.Should().Be(231);
    }

    [Fact]
    public void Parse_WithColorName_ReturnsOpaqueColor()
    {
        var color = Color.Parse("SteelBlue");

        color.A.Should().Be(255);
        color.Should().Be(Colors.SteelBlue);
    }

    // ── Parse: error messages must come from the resource set ────────────

    [Fact]
    public void Parse_WithInvalidString_ThrowsArgumentExceptionCarryingTheAuthoredMessage()
    {
        // Regression test for the stale resource base name in
        // Resources/AppResources.Designer.cs: every DomSR lookup used to throw
        // MissingManifestResourceException instead of producing the intended message.
        var ex = Assert.Throws<ArgumentException>(() => Color.Parse("not-a-color"));

        ex.Message.Should().Contain("not-a-color");
        ex.Message.Should().NotContain("Could not find the resource");
    }

    // ── Static factories ─────────────────────────────────────────────────

    [Fact]
    public void FromRgb_ReturnsOpaqueColor()
    {
        var color = Color.FromRgb(255, 253, 231);

        color.A.Should().Be(255);
        color.R.Should().Be(255);
        color.G.Should().Be(253);
        color.B.Should().Be(231);
        color.Should().Be(new Color(255, 253, 231));
    }

    [Fact]
    public void FromArgb_UsesTheSuppliedAlpha()
    {
        var color = Color.FromArgb(128, 255, 253, 231);

        color.A.Should().Be(128);
        color.R.Should().Be(255);
        color.G.Should().Be(253);
        color.B.Should().Be(231);
        color.Should().Be(new Color(128, 255, 253, 231));
    }

    [Fact]
    public void FromRgb_AndCssHexParse_AgreeOnTheSameColor()
    {
        Color.FromRgb(192, 192, 192).Should().Be(Color.Parse("#c0c0c0"));
    }
}
