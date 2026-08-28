using System;
using System.IO;
using System.Linq;
using System.Text;
using CodeBrix.PdfDocCreate.Html2Pdf.Composition;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// Coverage-driven glyph filtering: decisions come from the actual font files' cmap
/// tables, not assumed Unicode ranges. Candidate code points are discovered dynamically
/// from the fonts shipped with the test run, so these tests run unchanged on Windows,
/// macOS and Linux with no external fixture files.
/// </summary>
public class FontCoverageTests
{
    private static string PackageFontPath(string package, string fileName)
        => Path.Combine(AppContext.BaseDirectory, "CodeBrix.Platform.Fonts." + package, "Fonts", fileName);

    /// <summary>Finds a code point in the given ranges matching the coverage predicate.</summary>
    private static int? FindCodePoint(Func<int, bool> predicate, params (int Start, int End)[] ranges)
    {
        foreach (var (start, end) in ranges)
        {
            for (var cp = start; cp <= end; cp++)
            {
                if (predicate(cp)) { return cp; }
            }
        }

        return null;
    }

    // Ranges deliberately OUTSIDE the legacy allow-list, so admission proves the cmap.
    private static readonly (int, int)[] NonLegacyRanges =
    {
        (0x2600, 0x26FF),  // miscellaneous symbols (hearts, ballot boxes, ...)
        (0x2700, 0x27BF),  // dingbats
        (0x1D00, 0x1DBF),  // phonetic extensions
        (0x2300, 0x23FF),  // miscellaneous technical
    };

    [Fact]
    public void cmap_coverage_reads_real_font_files()
    {
        //Arrange
        var coverage = FontGlyphCoverage.Load(PackageFontPath("Roboto", "Roboto-Regular.ttf"));

        //Act + Assert
        coverage.Covers('A').Should().BeTrue();
        coverage.Covers('z').Should().BeTrue();
        coverage.Covers(0x00E9).Should().BeTrue();  // é
        coverage.Covers(0x0104).Should().BeTrue();  // Ą
        coverage.Covers(0x1D12A).Should().BeFalse(); // musical double sharp (astral)
        coverage.Covers(0xE0100).Should().BeFalse(); // variation selector supplement
    }

    [Fact]
    public void font_file_info_reads_family_weight_and_style_from_tables()
    {
        //Arrange + Act
        var regular = FontFileInfo.Read(PackageFontPath("Roboto", "Roboto-Regular.ttf"));
        var boldItalic = FontFileInfo.Read(PackageFontPath("Merriweather", "Merriweather-BoldItalic.ttf"));

        //Assert
        regular.Should().NotBeNull();
        regular.FamilyName.Should().Be("Roboto");
        regular.Weight.Should().Be(400);
        regular.IsItalic.Should().BeFalse();

        boldItalic.Should().NotBeNull();
        // The typographic family of these statics is "Merriweather 24pt" (optical size).
        boldItalic.FamilyName.Should().StartWith("Merriweather");
        boldItalic.IsItalic.Should().BeTrue();
        boldItalic.Weight.Should().BeGreaterThanOrEqualTo(600);
    }

    [Fact]
    public void covered_character_outside_the_legacy_ranges_renders_without_warnings()
    {
        //Arrange - a code point the old hard-coded allow-list rejected but the actual
        // serif font covers; before coverage-driven filtering this produced a warning.
        var face = Html2PdfFonts.TryResolveFaceName("serif", 400, false);
        var coverage = Html2PdfFonts.TryGetFaceCoverage(face);
        coverage.Should().NotBeNull();
        var codePoint = FindCodePoint(cp => coverage.Covers(cp), NonLegacyRanges);
        Assert.SkipWhen(codePoint == null, "The serif font covers none of the probe ranges.");

        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes(
            $"<body><p style='font-family:serif'>x{char.ConvertFromUtf32(codePoint.Value)}y</p></body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
    }

    [Fact]
    public void uncovered_character_is_removed_with_a_warning_by_default()
    {
        //Arrange - Hebrew is neither in the legacy allow-list nor in the package fonts.
        var face = Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false);
        var coverage = Html2PdfFonts.TryGetFaceCoverage(face);
        Assert.SkipWhen(coverage.Covers(0x05D0), "The sans font unexpectedly covers Hebrew.");

        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes("<body><p>xאy</p></body>");

        //Assert
        result.Warnings.Messages.Any(m => m.Contains("U+05D0") && m.Contains("removed")).Should().BeTrue();
    }

    [Fact]
    public void keep_uncovered_characters_option_keeps_them_as_missing_glyphs()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        renderer.Options.KeepUncoveredCharacters = true;

        //Act
        var result = renderer.RenderHtmlToBytes("<body><p>xאy</p></body>");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Messages.Any(m => m.Contains("missing-glyph")).Should().BeTrue();
        result.Warnings.Messages.Any(m => m.Contains("removed")).Should().BeFalse();
    }

    [Fact]
    public void fallback_family_supplies_glyphs_the_styled_font_lacks()
    {
        //Arrange - find a code point the sans font lacks but the serif font covers,
        // outside the legacy ranges so a cmap-driven fallback is provably what admits
        // it. Which family ends up supplying it is deliberately NOT pinned: every
        // companion family joins the chain automatically at discovery, so more than one
        // registered family may cover the character and the chain order decides. What
        // must hold is that SOME fallback face covers it and nothing is dropped.
        var sansFace = Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false);
        var serifFace = Html2PdfFonts.TryResolveFaceName("serif", 400, false);
        var sansCoverage = Html2PdfFonts.TryGetFaceCoverage(sansFace);
        var serifCoverage = Html2PdfFonts.TryGetFaceCoverage(serifFace);
        var codePoint = FindCodePoint(
            cp => !sansCoverage.Covers(cp) && serifCoverage.Covers(cp), NonLegacyRanges);
        Assert.SkipWhen(codePoint == null, "No code point separates sans and serif coverage in the probe ranges.");

        Html2PdfFonts.AddFallbackFamily(Html2PdfFonts.DefaultSerifFamily);

        //Act - unit level: the segmenter must route the character off the styled face.
        var warnings = new RenderWarnings();
        var segments = GlyphSafety.Segment(
            "x" + char.ConvertFromUtf32(codePoint.Value) + "y",
            sansFace, 400, false, keepUncovered: false, warnings);

        //Assert
        warnings.Count.Should().Be(0);
        segments.Count.Should().Be(3);
        segments[0].FaceName.Should().BeNull();
        segments[2].FaceName.Should().BeNull();
        segments[1].FaceName.Should().NotBeNull();
        segments[1].FaceName.Should().NotBe(sansFace);
        Html2PdfFonts.FaceCovers(segments[1].FaceName, codePoint.Value).Should().BeTrue();

        //Act - end to end: the same text renders with no warnings.
        var renderer = new HtmlPdfRenderer();
        var html = $"<body><p>x{char.ConvertFromUtf32(codePoint.Value)}y</p></body>";
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Count.Should().Be(0);
    }

    [Fact]
    public void companion_families_join_the_fallback_chain_without_registration()
    {
        //Arrange - U+1F00 GREEK SMALL LETTER ALPHA WITH PSILI. Roboto carries exactly
        // ONE code point of the Greek Extended block, so polytonic Greek has to come
        // from a companion. No AddFallbackFamily call here: discovery must have wired
        // the companions in on its own.
        Html2PdfFonts.EnsureRegistered();
        var sansFace = Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false);
        Html2PdfFonts.TryGetFaceCoverage(sansFace).Covers(0x1F00).Should().BeFalse();

        //Act
        var (familyName, faceName) = Html2PdfFonts.TryResolveFallback(0x1F00, 400, false);

        //Assert
        faceName.Should().NotBeNull();
        familyName.Should().NotBeNull();
        Html2PdfFonts.FaceCovers(faceName, 0x1F00).Should().BeTrue();
    }

    [Fact]
    public void polytonic_greek_prefers_the_sans_companion_over_the_serif_one()
    {
        //Arrange - the sans, serif and monospace packages ALL ship a companion covering
        // the Greek Extended block, so this is a genuine preference and not the only
        // option available. Body text is sans by default, so the sans package's
        // companion has to win; otherwise ancient Greek renders serif inside sans
        // paragraphs. Guard the premise first.
        Html2PdfFonts.EnsureRegistered();
        var serifCompanionFace = Html2PdfFonts.TryResolveFaceName("NotoSerif", 400, false);
        Assert.SkipWhen(serifCompanionFace == null, "The serif package's Greek companion is not present.");
        Html2PdfFonts.FaceCovers(serifCompanionFace, 0x1F00).Should().BeTrue();

        //Act
        var (familyName, faceName) = Html2PdfFonts.TryResolveFallback(0x1F00, 400, false);

        //Assert
        familyName.Should().Be("NotoSans");
        faceName.Should().Be(Html2PdfFonts.TryResolveFaceName("NotoSans", 400, false));
    }

    [Theory]
    [InlineData(0x0531, "NotoSansArmenian", "NotoSerifArmenian")]  // Armenian capital Ayb
    [InlineData(0x10D0, "NotoSansGeorgian", "NotoSerifGeorgian")]  // Georgian An
    public void script_companions_resolve_to_the_sans_package_not_the_serif_one(
        int codePoint, string expectedFamily, string serifCounterpart)
    {
        //Arrange - both the sans and the serif package ship a companion for these
        // scripts, so the sans one has to win for sans body text. Noto Sans Georgian is
        // the sharp edge here: it ships in the monospace package TOO, so if a family
        // were ranked by whichever package the filesystem enumerated first, this would
        // pass or fail depending on directory order. Guard the premise first.
        Html2PdfFonts.EnsureRegistered();
        var serifFace = Html2PdfFonts.TryResolveFaceName(serifCounterpart, 400, false);
        Assert.SkipWhen(serifFace == null, $"{serifCounterpart} is not present.");
        Html2PdfFonts.FaceCovers(serifFace, codePoint).Should().BeTrue();

        //Act
        var (familyName, faceName) = Html2PdfFonts.TryResolveFallback(codePoint, 400, false);

        //Assert
        familyName.Should().Be(expectedFamily);
        Html2PdfFonts.FaceCovers(faceName, codePoint).Should().BeTrue();
    }

    [Fact]
    public void polytonic_greek_renders_with_no_warnings_and_keeps_its_characters()
    {
        //Arrange - Iliad 1.1, which exercises psili, dasia, perispomeni, oxia, varia
        // and an iota subscript. Greek Extended sits INSIDE GlyphSafety's legacy
        // allow-list, so before the companions were wired in this text was admitted
        // against Roboto and rendered as tofu with no warning at all - a silent wrong
        // answer rather than a loud one. This test is what keeps that fixed.
        const string polytonic = "\u03BC\u1FC6\u03BD\u03B9\u03BD \u1F04\u03B5\u03B9\u03B4\u03B5 \u03B8\u03B5\u1F70";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body><p>{polytonic}</p></body>");

        //Assert
        result.PdfBytes.Should().NotBeNull();
        result.Warnings.Count.Should().Be(0);

        //Assert - and the characters really are routed to a covering face, not dropped
        // or silently left on a face with no glyph.
        var sansFace = Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false);
        var warnings = new RenderWarnings();
        var segments = GlyphSafety.Segment(polytonic, sansFace, 400, false, keepUncovered: false, warnings);
        warnings.Count.Should().Be(0);
        string.Concat(segments.Select(s => s.Text)).Should().Be(polytonic);
        foreach (var segment in segments.Where(s => s.FaceName != null))
        {
            foreach (var rune in segment.Text.EnumerateRunes())
            {
                Html2PdfFonts.FaceCovers(segment.FaceName, rune.Value).Should().BeTrue();
            }
        }
    }

    [Fact]
    public void structured_warnings_carry_code_occurrences_and_code_points()
    {
        //Arrange - two distinct astral characters, one of them twice, plus a missing
        // image. Messages collapses the astral drops to ONE string; Items keeps one
        // entry per distinct code point with an occurrence count.
        var renderer = new HtmlPdfRenderer();
        const string html = "<body><p>a \U0001F680 b \U0001F680 c \U0001F984 d</p>" +
                            "<img src='no-such-image.png'></body>";

        //Act
        var result = renderer.RenderHtmlToBytes(html);

        //Assert
        result.Warnings.Messages.Count(m => m.Contains("Basic Multilingual Plane")).Should().Be(1);

        var items = result.Warnings.Items;
        var rocket = items.Single(i => i.CodePoint == 0x1F680);
        rocket.Code.Should().Be("font.uncovered.removed");
        rocket.Category.Should().Be(RenderWarningCategory.Font);
        rocket.Occurrences.Should().Be(2);

        var unicorn = items.Single(i => i.CodePoint == 0x1F984);
        unicorn.Occurrences.Should().Be(1);

        var image = items.Single(i => i.Code == "image.load.failed");
        image.Category.Should().Be(RenderWarningCategory.Image);
        image.Message.Should().Contain("no-such-image.png");
    }

    [Fact]
    public void add_font_directory_after_a_render_no_longer_throws()
    {
        //Arrange
        var renderer = new HtmlPdfRenderer();
        renderer.RenderHtmlToBytes("<body><p>first render</p></body>");
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-fontdir-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            //Act
            var exception = Record.Exception(() => Html2PdfFonts.AddFontDirectory(directory));

            //Assert
            exception.Should().BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void add_font_file_reads_a_loose_font_without_a_manifest()
    {
        //Arrange - a copy under an unrelated file name; the family name must come from
        // the font's own tables, not the file name.
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-loosefont-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var loosePath = Path.Combine(directory, "some-renamed-font-file.ttf");
            File.Copy(PackageFontPath("Roboto", "Roboto-Regular.ttf"), loosePath);

            //Act - also exercises foreign-separator tolerance in the path argument.
            var foreignStylePath = loosePath.Replace(Path.DirectorySeparatorChar, Path.DirectorySeparatorChar == '/' ? '\\' : '/');
            var exception = Record.Exception(() => Html2PdfFonts.AddFontFile(foreignStylePath));

            //Assert - the family already exists from the package, so registration is a
            // silent no-op, but the file must parse and resolve without throwing.
            exception.Should().BeNull();
            Html2PdfFonts.TryResolveFaceName("Roboto", 400, false).Should().NotBeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void add_font_file_rejects_a_non_font_file()
    {
        //Arrange
        var directory = Path.Combine(Path.GetTempPath(), "html2pdf-badfont-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var badPath = Path.Combine(directory, "not-a-font.ttf");
            File.WriteAllText(badPath, "this is not a font");

            //Act
            var exception = Record.Exception(() => Html2PdfFonts.AddFontFile(badPath));

            //Assert
            exception.Should().BeOfType<InvalidOperationException>();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
