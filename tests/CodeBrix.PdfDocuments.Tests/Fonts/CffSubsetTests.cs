using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Fonts;
using CodeBrix.PdfDocuments.Fonts.CompactFontFormat;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Pdf.Advanced;
using CodeBrix.PdfDocuments.Pdf.IO;
using CodeBrix.PdfDocuments.Tests.Helpers;
using SilverAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Fonts;

/// <summary>
/// Embedding a font with PostScript (CFF) outlines: what the default still does, byte for
/// byte, and what <see cref="PdfCffSubsetMode.Sparse"/> writes instead.
/// </summary>
/// <remarks>
/// The fixture is MathJax_AMS-Regular.otf (Apache-2.0, test-only): 261 glyphs, a Private
/// DICT with 55 local subroutines after the CharStrings, no global subroutines, not
/// CID-keyed. MathJax_AMS-CID.otf is the same face re-expressed as a CID-keyed program
/// by SampleFiles/make-mathjax-cid.py (one Font DICT, FDSelect format 3, identity CIDs),
/// for the subsetter's FDArray/FDSelect branch. Both structures were measured with
/// fontTools 4.57 when the fixtures were vendored (2026-08-26), and those figures are the
/// oracle the parser is checked against.
/// </remarks>
public class CffSubsetTests
{
    private const string FamilyName = "MathJax_AMS";
    private const string FaceName = "MathJax_AMS-Regular";
    private const string ResourceName = "CodeBrix.PdfDocuments.Tests.SampleFiles.MathJax_AMS-Regular.otf";
    private const string CidFamilyName = "MathJaxAMSCid";
    private const string CidFaceName = "MathJaxAMSCid-Regular";
    private const string CidResourceName = "CodeBrix.PdfDocuments.Tests.SampleFiles.MathJax_AMS-CID.otf";
    private static readonly string OutDir = "TestResults/CffSubsetTests";

    static CffSubsetTests()
    {
        var resolver = new EmbeddedFontResolver(
            fontFamilyName: FamilyName,
            fontFaceResources:
            [
                new EmbeddedResourceFontFace(FaceName: FaceName, EmbeddedResourceName: ResourceName),
            ],
            fontEmbeddedResourceAssembly: typeof(CffSubsetTests).Assembly);
        MetaFontResolver.Instance.RegisterFontResolver(FaceName, resolver);

        var cidResolver = new EmbeddedFontResolver(
            fontFamilyName: CidFamilyName,
            fontFaceResources:
            [
                new EmbeddedResourceFontFace(FaceName: CidFaceName, EmbeddedResourceName: CidResourceName),
            ],
            fontEmbeddedResourceAssembly: typeof(CffSubsetTests).Assembly);
        MetaFontResolver.Instance.RegisterFontResolver(CidFaceName, cidResolver);
    }

    // ----- the parser against the measured fixture -----

    [Fact]
    public void the_fixture_parses_to_the_structure_fonttools_measured()
    {
        //Arrange
        byte[] cff = CffProgramOf(FixtureBytes());

        //Act
        CffFont font = CffSubsetter.Parse(cff);

        //Assert - every figure is fontTools 4.57's reading of the same file.
        font.Should().NotBeNull();
        font.HeaderSize.Should().Be(4);
        font.GlyphCount.Should().Be(261);
        font.IsCidKeyed.Should().BeFalse();
        font.CharsetOffset.Should().Be(2243);
        font.EncodingOffset.Should().Be(0);
        font.CharStringsOffset.Should().Be(2764);
        font.Private.Size.Should().Be(36);
        font.Private.Offset.Should().Be(49594);
        font.LocalSubrCount.Should().Be(55);
        font.GlobalSubrCount.Should().Be(0);
        cff.Length.Should().Be(54261);
    }

    [Fact]
    public void a_sparse_subset_keeps_every_glyph_slot_and_only_the_wanted_charstrings()
    {
        //Arrange
        byte[] cff = CffProgramOf(FixtureBytes());
        CffFont original = CffSubsetter.Parse(cff);
        int[] wanted = { 2, 3, 4 }; // A, B, C in this font's glyph order

        //Act
        byte[] subset = CffSubsetter.CreateSparseSubset(cff, wanted);
        CffFont parsed = CffSubsetter.Parse(subset);

        //Assert - the structure survives...
        parsed.GlyphCount.Should().Be(original.GlyphCount);
        parsed.IsCidKeyed.Should().BeFalse();
        parsed.LocalSubrCount.Should().Be(original.LocalSubrCount);
        parsed.GlobalSubrCount.Should().Be(original.GlobalSubrCount);
        parsed.Private.Size.Should().Be(original.Private.Size);
        parsed.EncodingOffset.Should().Be(0);

        //...the verbatim pieces are verbatim...
        Item(subset, parsed.NameIndex, 0).Should().Equal(Item(cff, original.NameIndex, 0));
        Slice(subset, parsed.CharsetOffset, CharsetLengthOf(original)).Should().Equal(Slice(cff, original.CharsetOffset, CharsetLengthOf(original)));
        Slice(subset, parsed.Private.Offset, parsed.Private.Size).Should().Equal(Slice(cff, original.Private.Offset, original.Private.Size));

        //...the wanted glyphs and .notdef keep their charstrings, every other slot is one endchar...
        for (int glyph = 0; glyph < original.GlyphCount; glyph++)
        {
            byte[] charstring = Item(subset, parsed.CharStringsIndex, glyph);
            if (glyph == 0 || wanted.Contains(glyph))
                charstring.Should().Equal(Item(cff, original.CharStringsIndex, glyph));
            else
                charstring.Should().Equal(new byte[] { 14 });
        }

        //...and the program is a fraction of its former size.
        subset.Length.Should().BeLessThan(cff.Length / 5);
    }

    [Fact]
    public void a_cff2_program_is_declined_rather_than_mangled()
    {
        //Arrange - the major version byte of CFF2 is 2.
        byte[] cff2 = { 2, 0, 5, 0, 0 };

        //Act
        byte[] subset = CffSubsetter.CreateSparseSubset(cff2, new[] { 1 });

        //Assert
        subset.Should().BeNull();
        CffSubsetter.Parse(cff2).Should().BeNull();
    }

    // ----- the CID-keyed branch -----

    [Fact]
    public void the_cid_keyed_fixture_parses_to_the_structure_fonttools_measured()
    {
        //Arrange
        byte[] cff = CffProgramOf(FixtureBytes(CidResourceName));

        //Act
        CffFont font = CffSubsetter.Parse(cff);

        //Assert - fontTools 4.57's reading of the derived file.
        font.IsCidKeyed.Should().BeTrue();
        font.GlyphCount.Should().Be(261);
        font.CharsetOffset.Should().Be(204);
        font.FdSelectOffset.Should().Be(209);
        font.CharStringsOffset.Should().Be(217);
        font.FdArrayOffset.Should().Be(47047);
        font.Private.Should().BeNull();
        font.FontDicts.Should().HaveCount(1);
        font.FontDicts[0].Private.Size.Should().Be(28);
        font.FontDicts[0].Private.Offset.Should().Be(47063);
        font.FontDicts[0].Private.LocalSubrCount.Should().Be(55);
        font.GlobalSubrCount.Should().Be(0);
        cff.Length.Should().Be(51722);
    }

    [Fact]
    public void a_sparse_subset_of_a_cid_keyed_program_keeps_its_fdarray_fdselect_and_charset()
    {
        //Arrange
        byte[] cff = CffProgramOf(FixtureBytes(CidResourceName));
        CffFont original = CffSubsetter.Parse(cff);
        int[] wanted = { 2, 3, 4 };
        int charsetLength = original.FdSelectOffset - original.CharsetOffset;     // 5: one format-2 range
        int fdSelectLength = original.CharStringsOffset - original.FdSelectOffset; // 8: format 3, one range

        //Act
        byte[] subset = CffSubsetter.CreateSparseSubset(cff, wanted);
        CffFont parsed = CffSubsetter.Parse(subset);

        //Assert
        parsed.IsCidKeyed.Should().BeTrue();
        parsed.GlyphCount.Should().Be(261);
        parsed.FontDicts.Should().HaveCount(1);
        parsed.FontDicts[0].Private.Size.Should().Be(28);
        parsed.FontDicts[0].Private.LocalSubrCount.Should().Be(55);
        Slice(subset, parsed.CharsetOffset, charsetLength).Should().Equal(Slice(cff, original.CharsetOffset, charsetLength));
        Slice(subset, parsed.FdSelectOffset, fdSelectLength).Should().Equal(Slice(cff, original.FdSelectOffset, fdSelectLength));
        Slice(subset, parsed.FontDicts[0].Private.Offset, 28).Should().Equal(Slice(cff, original.FontDicts[0].Private.Offset, 28));
        for (int glyph = 0; glyph < original.GlyphCount; glyph++)
        {
            byte[] charstring = Item(subset, parsed.CharStringsIndex, glyph);
            if (glyph == 0 || wanted.Contains(glyph))
                charstring.Should().Equal(Item(cff, original.CharStringsIndex, glyph));
            else
                charstring.Should().Equal(new byte[] { 14 });
        }
        subset.Length.Should().BeLessThan(cff.Length / 5);
    }

    [Fact]
    public async Task a_cid_keyed_face_round_trips_through_the_document_and_renders_the_same_pixels()
    {
        //Arrange
        string text = "ABC MATHJAX";
        PdfDocument whole = Create(PdfCffSubsetMode.None, text, CidFamilyName);
        PdfDocument sparse = Create(PdfCffSubsetMode.Sparse, text, CidFamilyName);
        byte[] sparsePdf = Save(PdfCffSubsetMode.Sparse, text, CidFamilyName);
        Directory.CreateDirectory(OutDir);
        File.WriteAllBytes(Path.Combine(OutDir, "mathjax-cid-whole.pdf"), Save(PdfCffSubsetMode.None, text, CidFamilyName));
        File.WriteAllBytes(Path.Combine(OutDir, "mathjax-cid-sparse.pdf"), sparsePdf);

        //Act
        EmbeddedFont font = ReadEmbeddedFont(sparsePdf);
        var wholeFiles = await PdfHelper.WriteImageCollection(await PdfHelper.Rasterize(whole), OutDir, "cid-whole");
        var sparseFiles = await PdfHelper.WriteImageCollection(await PdfHelper.Rasterize(sparse), OutDir, "cid-sparse");

        //Assert
        font.CidSubtype.Should().Be("/CIDFontType0");
        font.FontFileKey.Should().Be("/FontFile3");
        font.StreamSubtype.Should().Be("/OpenType");
        font.Program.Length.Should().BeLessThan(FixtureBytes(CidResourceName).Length / 4);
        File.ReadAllBytes(sparseFiles[0]).Should().Equal(File.ReadAllBytes(wholeFiles[0]));
    }

    // ----- the document, by default -----

    [Fact]
    public void by_default_a_cff_face_is_embedded_whole_and_declared_as_truetype()
    {
        //Arrange - this is what every version before the option did, spelled out.
        byte[] pdf = Save(PdfCffSubsetMode.None, "ABC");

        //Act
        EmbeddedFont font = ReadEmbeddedFont(pdf);

        //Assert
        font.CidSubtype.Should().Be("/CIDFontType2");
        font.FontFileKey.Should().Be("/FontFile2");
        font.StreamSubtype.Should().BeNull();
        font.Length1.Should().Be(FixtureBytes().Length);
        font.Program.Should().Equal(FixtureBytes());
        Encoding.ASCII.GetString(pdf, 0, 8).Should().Be("%PDF-1.4");
    }

    // ----- the document, opted in -----

    [Fact]
    public void sparse_mode_writes_an_opentype_fontfile3_on_a_cidfonttype0()
    {
        //Arrange
        byte[] pdf = Save(PdfCffSubsetMode.Sparse, "ABC");

        //Act
        EmbeddedFont font = ReadEmbeddedFont(pdf);

        //Assert - PDF 32000-1:2008 section 9.9, table 126: an OpenType font program with
        //CFF outlines goes in FontFile3 with Subtype OpenType, on a Type 0 CIDFont, and
        //that is PDF 1.6.
        font.CidSubtype.Should().Be("/CIDFontType0");
        font.FontFileKey.Should().Be("/FontFile3");
        font.StreamSubtype.Should().Be("/OpenType");
        font.Length1.Should().BeNull();
        Encoding.ASCII.GetString(pdf, 0, 8).Should().Be("%PDF-1.6");
        Encoding.ASCII.GetString(font.Program, 0, 4).Should().Be("OTTO");
        font.Program.Length.Should().BeLessThan(FixtureBytes().Length / 4);
    }

    [Fact]
    public void the_embedded_subset_is_a_well_formed_opentype_font_with_the_wanted_glyphs()
    {
        //Arrange
        byte[] pdf = Save(PdfCffSubsetMode.Sparse, "ABC");
        EmbeddedFont font = ReadEmbeddedFont(pdf);

        //Act - walk the sfnt table directory the way any reader does.
        Dictionary<string, (int Offset, int Length)> tables = TableDirectoryOf(font.Program);
        byte[] cff = Slice(font.Program, tables["CFF "].Offset, tables["CFF "].Length);
        CffFont parsed = CffSubsetter.Parse(cff);
        CffFont original = CffSubsetter.Parse(CffProgramOf(FixtureBytes()));

        //Assert - the wrapper carries what a CFF OpenType font needs and nothing it does not.
        tables.Keys.Should().BeEquivalentTo(new[] { "CFF ", "OS/2", "cmap", "head", "hhea", "hmtx", "maxp", "name", "post" });
        Slice(font.Program, tables["hmtx"].Offset, tables["hmtx"].Length)
            .Should().Equal(Slice(FixtureBytes(), TableDirectoryOf(FixtureBytes())["hmtx"].Offset, tables["hmtx"].Length));
        parsed.GlyphCount.Should().Be(261);
        int kept = 0;
        for (int glyph = 0; glyph < parsed.GlyphCount; glyph++)
        {
            byte[] charstring = Item(cff, parsed.CharStringsIndex, glyph);
            if (charstring.Length != 1 || charstring[0] != 14)
            {
                kept++;
                charstring.Should().Equal(Item(CffProgramOf(FixtureBytes()), original.CharStringsIndex, glyph));
            }
        }
        kept.Should().Be(4); // .notdef, A, B, C
    }

    [Fact]
    public async Task sparse_and_whole_render_the_same_pixels()
    {
        //Arrange - the outlines that remain are the very bytes the whole font carried, so
        //PDFium must draw the two documents identically.
        string text = "ABC MATHJAX";
        PdfDocument whole = Create(PdfCffSubsetMode.None, text);
        PdfDocument sparse = Create(PdfCffSubsetMode.Sparse, text);

        //Act
        var wholeImages = await PdfHelper.Rasterize(whole);
        var sparseImages = await PdfHelper.Rasterize(sparse);
        var wholeFiles = await PdfHelper.WriteImageCollection(wholeImages, OutDir, "whole");
        var sparseFiles = await PdfHelper.WriteImageCollection(sparseImages, OutDir, "sparse");

        //Assert
        wholeFiles.Should().HaveCount(1);
        sparseFiles.Should().HaveCount(1);
        File.ReadAllBytes(sparseFiles[0]).Should().Equal(File.ReadAllBytes(wholeFiles[0]));
    }

    [Fact]
    public void a_truetype_face_is_not_touched_by_the_option()
    {
        //Arrange - Roboto has glyf/loca outlines and has always been subset; the option
        //must leave that path exactly as it is.
        RobotoRegistration.EnsureRegistered();
        byte[] withOption = Save(PdfCffSubsetMode.Sparse, "ABC", "Roboto");
        byte[] without = Save(PdfCffSubsetMode.None, "ABC", "Roboto");

        //Act
        EmbeddedFont a = ReadEmbeddedFont(withOption);
        EmbeddedFont b = ReadEmbeddedFont(without);

        //Assert
        a.CidSubtype.Should().Be("/CIDFontType2");
        a.FontFileKey.Should().Be("/FontFile2");
        a.Program.Should().Equal(b.Program);
        Encoding.ASCII.GetString(withOption, 0, 8).Should().Be("%PDF-1.4");
    }

    [Fact]
    public void every_document_written_here_is_kept_for_the_external_checks()
    {
        //Arrange - not an assertion about the library: the session that added this feature
        //ran pdffonts, pdftotext, qpdf --check, mutool and fontTools over these files, and
        //the files are left in TestResults so that can be repeated.
        Directory.CreateDirectory(OutDir);

        //Act
        File.WriteAllBytes(Path.Combine(OutDir, "mathjax-whole.pdf"), Save(PdfCffSubsetMode.None, "ABC MATHJAX"));
        File.WriteAllBytes(Path.Combine(OutDir, "mathjax-sparse.pdf"), Save(PdfCffSubsetMode.Sparse, "ABC MATHJAX"));

        //Assert
        File.Exists(Path.Combine(OutDir, "mathjax-sparse.pdf")).Should().BeTrue();
    }

    // ----- helpers -----

    private static PdfDocument Create(PdfCffSubsetMode mode, string text, string family = FamilyName)
    {
        var document = new PdfDocument();
        document.Options.CffSubsetMode = mode;
        PdfPage page = document.AddPage();
        using XGraphics gfx = XGraphics.FromPdfPage(page);
        gfx.DrawString(text, new XFont(family, 36), XBrushes.Black, new XPoint(72, 144));
        return document;
    }

    private static byte[] Save(PdfCffSubsetMode mode, string text, string family = FamilyName)
    {
        using var stream = new MemoryStream();
        Create(mode, text, family).Save(stream);
        return stream.ToArray();
    }

    private static byte[] FixtureBytes(string resourceName = ResourceName)
    {
        using Stream stream = typeof(CffSubsetTests).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("The MathJax fixture is not embedded.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>Reads an sfnt table directory: tag to (offset, length).</summary>
    private static Dictionary<string, (int Offset, int Length)> TableDirectoryOf(byte[] sfnt)
    {
        int count = (sfnt[4] << 8) | sfnt[5];
        var tables = new Dictionary<string, (int, int)>(StringComparer.Ordinal);
        for (int idx = 0; idx < count; idx++)
        {
            int entry = 12 + 16 * idx;
            string tag = Encoding.ASCII.GetString(sfnt, entry, 4);
            int offset = (sfnt[entry + 8] << 24) | (sfnt[entry + 9] << 16) | (sfnt[entry + 10] << 8) | sfnt[entry + 11];
            int length = (sfnt[entry + 12] << 24) | (sfnt[entry + 13] << 16) | (sfnt[entry + 14] << 8) | sfnt[entry + 15];
            tables[tag] = (offset, length);
        }
        return tables;
    }

    private static byte[] CffProgramOf(byte[] sfnt)
    {
        var table = TableDirectoryOf(sfnt)["CFF "];
        return Slice(sfnt, table.Offset, table.Length);
    }

    private static byte[] Slice(byte[] bytes, int offset, int length)
    {
        byte[] slice = new byte[length];
        Buffer.BlockCopy(bytes, offset, slice, 0, length);
        return slice;
    }

    private static byte[] Item(byte[] program, CffIndex index, int idx)
        => Slice(program, index.ItemStart(idx), index.ItemEnd(idx) - index.ItemStart(idx));

    private static int CharsetLengthOf(CffFont font)
        => font.CharStringsOffset - font.CharsetOffset; // the charset sits right before the charstrings in this fixture

    private sealed class EmbeddedFont
    {
        public string CidSubtype;
        public string FontFileKey;
        public string StreamSubtype;
        public int? Length1;
        public byte[] Program;
    }

    /// <summary>The first Type 0 font on the first page, read back through PdfReader.</summary>
    private static EmbeddedFont ReadEmbeddedFont(byte[] pdf)
    {
        PdfDocument document = CodeBrix.PdfDocuments.Pdf.IO.PdfReader.Open(new MemoryStream(pdf), PdfDocumentOpenMode.Import);
        PdfDictionary fonts = document.Pages[0].Resources.Elements.GetDictionary("/Font");
        PdfDictionary type0 = fonts.Elements.Values
            .Select(v => v is PdfReference r ? r.Value as PdfDictionary : v as PdfDictionary)
            .First(d => d != null && d.Elements.GetName("/Subtype") == "/Type0");
        PdfArray descendants = type0.Elements.GetArray("/DescendantFonts");
        PdfDictionary cid = descendants.Elements[0] is PdfReference cidRef ? (PdfDictionary)cidRef.Value : (PdfDictionary)descendants.Elements[0];
        PdfDictionary descriptor = cid.Elements.GetDictionary("/FontDescriptor");

        var font = new EmbeddedFont { CidSubtype = cid.Elements.GetName("/Subtype") };
        foreach (string key in new[] { "/FontFile", "/FontFile2", "/FontFile3" })
        {
            PdfDictionary stream = descriptor.Elements.GetDictionary(key);
            if (stream == null)
                continue;
            font.FontFileKey.Should().BeNull(); // exactly one program
            font.FontFileKey = key;
            font.StreamSubtype = stream.Elements.ContainsKey("/Subtype") ? stream.Elements.GetName("/Subtype") : null;
            font.Length1 = stream.Elements.ContainsKey("/Length1") ? stream.Elements.GetInteger("/Length1") : null;
            font.Program = stream.Stream.UnfilteredValue;
        }
        font.FontFileKey.Should().NotBeNull();
        return font;
    }

    /// <summary>Registers the Roboto fixture the other test classes use, once.</summary>
    private static class RobotoRegistration
    {
        private static readonly object Gate = new();
        private static bool _done;

        public static void EnsureRegistered()
        {
            lock (Gate)
            {
                if (_done)
                    return;
                var resolver = new EmbeddedFontResolver(
                    fontFamilyName: "Roboto",
                    fontFaceResources:
                    [
                        new EmbeddedResourceFontFace(FaceName: "Roboto-Regular", EmbeddedResourceName: "CodeBrix.PdfDocuments.Tests.SampleFiles.Roboto-Regular.ttf"),
                    ],
                    fontEmbeddedResourceAssembly: typeof(CffSubsetTests).Assembly);
                MetaFontResolver.Instance.RegisterFontResolver("Roboto-Regular", resolver);
                _done = true;
            }
        }
    }
}
