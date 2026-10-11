using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Fonts;
using SilverAssertions;
using System;
using System.IO;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Fonts;

/// <summary>
/// Fences the font-source cache against two DIFFERENT font files that carry the same internal font name (a device's
/// system Roboto Bold and the Roboto Bold of a font package): caching the second one used to throw
/// ArgumentException ("An item with the same key has already been added") from FontFactory.CacheFontSource, and the
/// half-registered typeface then failed every later XFont with a NullReferenceException.
/// </summary>
public class FontFactoryTests
{
    private const string RobotoResourceName = "CodeBrix.PdfDocuments.Tests.SampleFiles.Roboto-Regular.ttf";

    public FontFactoryTests()
    {
        //The global font resolver must be in place before the first font source is cached (GlobalFontSettings refuses to
        //install its default once FontFactory holds a font source), exactly as XFont does it in ordinary use.
        _ = GlobalFontSettings.FontResolver;
    }

    private static byte[] RobotoBytes()
    {
        using var stream = typeof(FontFactoryTests).Assembly.GetManifestResourceStream(RobotoResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{RobotoResourceName}' not found.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    //The same font with trailing padding: a different file (different checksum key), the same internal name.
    private static byte[] PaddedCopy(byte[] bytes)
    {
        var padded = new byte[bytes.Length + 16];
        Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
        Guid.NewGuid().ToByteArray().CopyTo(padded, bytes.Length);
        return padded;
    }

    [Fact]
    public void Two_different_files_with_the_same_font_name_can_both_be_cached()
    {
        //Arrange
        var original = PaddedCopy(RobotoBytes());
        var other = PaddedCopy(RobotoBytes());

        //Act
        var first = XFontSource.GetOrCreateFrom(original);
        XFontSource second = null;
        Action cacheSecond = () => second = XFontSource.GetOrCreateFrom(other);

        //Assert
        cacheSecond.Should().NotThrow();
        second.Should().NotBeSameAs(first);
        second.FontName.Should().Be(first.FontName);
        FontFactory.TryGetFontSourceByKey(second.Key, out var byKey).Should().BeTrue();
        byKey.Should().BeSameAs(second);
    }

    [Fact]
    public void Two_different_files_with_the_same_font_name_can_both_be_cached_under_typeface_keys()
    {
        //Arrange
        var original = PaddedCopy(RobotoBytes());
        var other = PaddedCopy(RobotoBytes());
        var firstKey = "FontFactoryTests-" + Guid.NewGuid().ToString("N");
        var secondKey = "FontFactoryTests-" + Guid.NewGuid().ToString("N");

        //Act
        var first = XFontSource.GetOrCreateFrom(firstKey, original);
        XFontSource second = null;
        Action cacheSecond = () => second = XFontSource.GetOrCreateFrom(secondKey, other);

        //Assert
        cacheSecond.Should().NotThrow();
        FontFactory.GetFontSourceByTypefaceKey(firstKey).Should().BeSameAs(first);
        FontFactory.GetFontSourceByTypefaceKey(secondKey).Should().BeSameAs(second);
    }
}
