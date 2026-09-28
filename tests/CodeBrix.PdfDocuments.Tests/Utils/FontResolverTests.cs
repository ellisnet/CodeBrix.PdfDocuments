using CodeBrix.PdfDocuments.Fonts;
using CodeBrix.PdfDocuments.Utils;
using SilverAssertions;
using System;
using System.IO;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Utils;

/// <summary>
/// Fences for the system-font discovery in <see cref="FontResolver"/>: the folder scan used on Android, and the
/// empty font set every unrecognised platform gets. None of these tests depend on the fonts installed on the host.
/// </summary>
public class FontResolverTests
{
    private const string RobotoResourceName = "CodeBrix.PdfDocuments.Tests.SampleFiles.Roboto-Regular.ttf";

    private static void WriteRoboto(string path)
    {
        using var stream = typeof(FontResolverTests).Assembly.GetManifestResourceStream(RobotoResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{RobotoResourceName}' not found.");
        using var file = File.Create(path);
        stream.CopyTo(file);
    }

    private static string CreateTempFolder()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"FontResolverTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact]
    public void android_font_folders_start_with_system_fonts()
    {
        FontResolver.AndroidFontFolders.Should().NotBeEmpty();
        FontResolver.AndroidFontFolders[0].Should().Be("/system/fonts");
        FontResolver.AndroidFontFolders.Should().Contain("/system/font");
        FontResolver.AndroidFontFolders.Should().Contain("/product/fonts");
    }

    [Fact]
    public void scan_font_folders_returns_the_ttf_file_in_a_folder()
    {
        var dir = CreateTempFolder();
        try
        {
            var fontPath = Path.Combine(dir, "Roboto-Regular.ttf");
            WriteRoboto(fontPath);

            var found = FontResolver.ScanFontFolders([dir]);

            found.Should().HaveCount(1);
            found[0].Should().Be(fontPath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void scan_font_folders_finds_ttf_files_in_sub_folders_and_ignores_other_files()
    {
        var dir = CreateTempFolder();
        try
        {
            var subDir = Path.Combine(dir, "extra");
            Directory.CreateDirectory(subDir);
            var nestedFont = Path.Combine(subDir, "Roboto-Regular.TTF");
            WriteRoboto(nestedFont);
            File.WriteAllText(Path.Combine(dir, "fonts.xml"), "<familyset />");
            File.WriteAllText(Path.Combine(dir, "NotAFont.otf"), "not a font");

            var found = FontResolver.ScanFontFolders([dir]);

            found.Should().HaveCount(1);
            found[0].Should().Be(nestedFont);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void scan_font_folders_skips_folders_that_do_not_exist_and_returns_empty()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"FontResolverTests_missing_{Guid.NewGuid():N}");

        var found = FontResolver.ScanFontFolders([missing, "", null]);

        found.Should().NotBeNull();
        found.Should().BeEmpty();
    }

    [Fact]
    public void setup_fonts_files_accepts_an_empty_font_list_without_throwing()
    {
        // The unrecognised-platform branch of the static constructor runs exactly this.
        FontResolver.SetupFontsFiles(Array.Empty<string>());

        FontResolver.ScanFontFolders(Array.Empty<string>()).Should().BeEmpty();
    }

    [Fact]
    public void embedded_face_resolves_through_a_meta_resolver_without_system_fonts()
    {
        const string familyName = "FontResolverTestsRoboto";
        const string faceName = "FontResolverTestsRoboto-Regular";
        var meta = new MetaFontResolver();
        meta.RegisterFontResolver(faceName, new EmbeddedFontResolver(
            fontFamilyName: familyName,
            fontFaceResources: [new EmbeddedResourceFontFace(FaceName: faceName, EmbeddedResourceName: RobotoResourceName)],
            fontEmbeddedResourceAssembly: typeof(FontResolverTests).Assembly));

        var info = meta.ResolveTypeface(familyName, isBold: false, isItalic: false);

        info.Should().NotBeNull();
        info.FaceName.Should().Be(faceName);
        meta.GetFont(faceName).Length.Should().BeGreaterThan(0);
    }
}
