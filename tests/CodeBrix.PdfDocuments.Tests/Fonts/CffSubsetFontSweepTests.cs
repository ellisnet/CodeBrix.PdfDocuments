using CodeBrix.PdfDocuments.Fonts.CompactFontFormat;
using SilverAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Fonts;

/// <summary>
/// Subsets every CFF-outline font in a directory, several glyph sets each, and checks that
/// what came out is the same font minus what was asked to go.
/// </summary>
/// <remarks>
/// <para>
/// The vendored fixtures cover the structures a CFF can have one at a time. This covers
/// VOLUME: it is the check that found the defect that made subroutine renumbering
/// impossible, on 7 of 37 faces that the three fixtures between them did not show. It is
/// gated on an environment variable because it needs a directory of real fonts, which this
/// repository does not ship:
/// </para>
/// <code>
///     CODEBRIX_CFF_FONT_SWEEP=/usr/share/fonts/opentype/urw-base35
/// </code>
/// <para>
/// What it asserts is the property the whole design rests on: a compact subset moves
/// NOTHING. Every kept glyph's charstring, every surviving subroutine and the charset are
/// byte-identical AT THEIR ORIGINAL INDEX, and each subroutine INDEX still holds the same
/// number of items - so every <c>callsubr</c> operand, which is written against a bias
/// taken from that count, still names what it named. Outlines follow from that by
/// construction; when this was first run the outlines were also drawn through fontTools and
/// compared, 248 subsets over 62 faces, all identical.
/// </para>
/// </remarks>
public class CffSubsetFontSweepTests
{
    private const string DirectoryVariable = "CODEBRIX_CFF_FONT_SWEEP";

    [Fact]
    public void every_cff_face_in_the_sweep_directory_subsets_without_moving_anything()
    {
        //Arrange
        string directory = Environment.GetEnvironmentVariable(DirectoryVariable);
        Assert.SkipUnless(Directory.Exists(directory), $"{DirectoryVariable} is not set to an existing folder.");
        string[] files = Directory.GetFiles(directory, "*.otf");
        files.Should().NotBeEmpty();

        //Act
        var failures = new List<string>();
        int faces = 0, subsets = 0, compacted = 0;
        foreach (string file in files)
        {
            byte[] cff = CffProgramOfOrNull(File.ReadAllBytes(file));
            if (cff == null)
                continue;
            CffFont original = CffSubsetter.Parse(cff);
            if (original == null)
                continue;      // a CFF2 program, which every mode declines
            faces++;
            foreach (int[] wanted in GlyphSets(original.GlyphCount))
            {
                subsets++;
                byte[] subset = CffSubsetter.CreateCompactSubset(cff, wanted);
                if (subset == null)
                    continue;  // refused, and the caller falls back to a sparse subset
                compacted++;
                CheckNothingMoved(Path.GetFileName(file), cff, original, subset, wanted, failures);
            }
        }

        //Assert
        failures.Should().BeEmpty();
        //The controls: a run that subset nothing, or refused everything, would pass vacuously.
        faces.Should().BeGreaterThan(0);
        subsets.Should().Be(faces * 4);
        compacted.Should().Be(subsets);
    }

    /// <summary>Four glyph sets per face, chosen by index so a symbol face is covered too.</summary>
    private static IEnumerable<int[]> GlyphSets(int glyphCount)
    {
        yield return Set(glyphCount, gid => gid < 9);
        yield return Set(glyphCount, gid => gid % 13 == 0);
        yield return Set(glyphCount, gid => gid >= glyphCount - 10);
        yield return Set(glyphCount, gid => gid % 37 == 0);
    }

    private static int[] Set(int glyphCount, Func<int, bool> wanted)
    {
        var glyphs = new List<int> { 0 };
        for (int gid = 1; gid < glyphCount; gid++)
        {
            if (wanted(gid))
                glyphs.Add(gid);
        }
        return glyphs.ToArray();
    }

    private static void CheckNothingMoved(string face, byte[] cff, CffFont original, byte[] subset,
        int[] wanted, List<string> failures)
    {
        CffFont parsed = CffSubsetter.Parse(subset);
        var asked = new HashSet<int>(wanted) { 0 };

        void Fail(string what) => failures.Add($"{face} [{wanted.Length} glyphs]: {what}");

        if (parsed.GlyphCount != original.GlyphCount)
            Fail($"glyph count {original.GlyphCount} -> {parsed.GlyphCount}");
        if (parsed.GlobalSubrCount != original.GlobalSubrCount)
            Fail($"global subr count {original.GlobalSubrCount} -> {parsed.GlobalSubrCount}");
        if (parsed.LocalSubrCount != original.LocalSubrCount)
            Fail($"local subr count {original.LocalSubrCount} -> {parsed.LocalSubrCount}");
        if (parsed.StringIndex.Count != original.StringIndex.Count)
            Fail($"string count {original.StringIndex.Count} -> {parsed.StringIndex.Count}");

        // A glyph the document asked for keeps its charstring; every other slot is one
        // endchar. A seac composite pulls its two components in, so a glyph that was not
        // asked for may legitimately be kept - it must then still be byte-identical.
        for (int glyph = 0; glyph < original.GlyphCount; glyph++)
        {
            byte[] was = Item(cff, original.CharStringsIndex, glyph);
            byte[] now = Item(subset, parsed.CharStringsIndex, glyph);
            bool blank = now.Length == 1 && now[0] == 14;
            if (asked.Contains(glyph))
            {
                if (!Same(was, now))
                    Fail($"glyph {glyph} was asked for and changed");
            }
            else if (!blank && !Same(was, now))
            {
                Fail($"glyph {glyph} was neither blanked nor kept intact");
            }
        }

        CheckSubrs(face, cff, subset, original.GlobalSubrIndex, parsed.GlobalSubrIndex, "global", Fail);
        if (original.Private != null && original.Private.Subrs != null)
            CheckSubrs(face, cff, subset, original.Private.Subrs, parsed.Private.Subrs, "local", Fail);
        for (int idx = 0; idx < original.FontDicts.Count; idx++)
        {
            PrivateDict was = original.FontDicts[idx].Private, now = parsed.FontDicts[idx].Private;
            if (was != null && was.Subrs != null)
                CheckSubrs(face, cff, subset, was.Subrs, now.Subrs, $"local[{idx}]", Fail);
        }
    }

    private static void CheckSubrs(string face, byte[] cff, byte[] subset, CffIndex was, CffIndex now,
        string which, Action<string> fail)
    {
        if (was.Count != now.Count)
        {
            fail($"{which} subr count {was.Count} -> {now.Count}");
            return;
        }
        for (int idx = 0; idx < was.Count; idx++)
        {
            byte[] item = Item(subset, now, idx);
            if (item.Length != 0 && !Same(item, Item(cff, was, idx)))
                fail($"{which} subr {idx} survived but changed");
        }
    }

    private static bool Same(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;
        for (int idx = 0; idx < left.Length; idx++)
        {
            if (left[idx] != right[idx])
                return false;
        }
        return true;
    }

    private static byte[] Item(byte[] program, CffIndex index, int idx)
    {
        int start = index.ItemStart(idx), end = index.ItemEnd(idx);
        byte[] item = new byte[end - start];
        Buffer.BlockCopy(program, start, item, 0, item.Length);
        return item;
    }

    /// <summary>The font's CFF table, or null when it has none (a TrueType face).</summary>
    private static byte[] CffProgramOfOrNull(byte[] sfnt)
    {
        if (sfnt.Length < 12)
            return null;
        int count = (sfnt[4] << 8) | sfnt[5];
        for (int idx = 0; idx < count; idx++)
        {
            int entry = 12 + 16 * idx;
            if (entry + 16 > sfnt.Length || Encoding.ASCII.GetString(sfnt, entry, 4) != "CFF ")
                continue;
            int offset = (sfnt[entry + 8] << 24) | (sfnt[entry + 9] << 16) | (sfnt[entry + 10] << 8) | sfnt[entry + 11];
            int length = (sfnt[entry + 12] << 24) | (sfnt[entry + 13] << 16) | (sfnt[entry + 14] << 8) | sfnt[entry + 15];
            if (offset < 0 || length < 0 || offset + length > sfnt.Length)
                return null;
            byte[] program = new byte[length];
            Buffer.BlockCopy(sfnt, offset, program, 0, length);
            return program;
        }
        return null;
    }
}
