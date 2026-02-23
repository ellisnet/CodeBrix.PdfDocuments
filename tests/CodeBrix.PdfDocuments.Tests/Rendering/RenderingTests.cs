using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocuments.Tests.Helpers;
using SilverAssertions;
using System.IO;
using System.Text;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Rendering;

public class RenderingTests
{
    private readonly string _outDir = Path.Combine(PathHelper.GetInstance().RootDir, "Out", "Rendering");

    private string GetOutputPath(string fileName)
    {
        if (!Directory.Exists(_outDir))
        {
            Directory.CreateDirectory(_outDir);
        }
        var path = Path.Combine(_outDir, fileName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return path;
    }

    private static void ValidateFileIsPdf(string path)
    {
        Assert.True(File.Exists(path));
        var fi = new FileInfo(path);
        Assert.True(fi.Length > 1);

        using var stream = File.OpenRead(path);
        var readBuffer = new byte[5];
        var pdfSignature = Encoding.ASCII.GetBytes("%PDF-");
        stream.ReadExactly(readBuffer, 0, readBuffer.Length);
        readBuffer.Should().Equal(pdfSignature);
    }

    // --- TestLayout tests ---

    [Fact]
    public void TestLayout_TwoParagraphs()
    {
        var path = GetOutputPath("Layout_TwoParagraphs.pdf");
        TestLayout.TwoParagraphs(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestLayout_A1000Paragraphs()
    {
        var path = GetOutputPath("Layout_A1000Paragraphs.pdf");
        TestLayout.A1000Paragraphs(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestLayout_DumpParagraph()
    {
        var result = TestLayout.DumpParagraph();
        Assert.NotNull(result);
    }

    // --- TestParagraphRenderer tests ---

    [Fact]
    public void TestParagraphRenderer_TextAndBlanks()
    {
        var path = GetOutputPath("ParagraphRenderer_TextAndBlanks.pdf");
        TestParagraphRenderer.TextAndBlanks(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestParagraphRenderer_Formatted()
    {
        var path = GetOutputPath("ParagraphRenderer_Formatted.pdf");
        TestParagraphRenderer.Formatted(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestParagraphRenderer_Alignment()
    {
        var path = GetOutputPath("ParagraphRenderer_Alignment.pdf");
        TestParagraphRenderer.Alignment(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestParagraphRenderer_Tabs()
    {
        var path = GetOutputPath("ParagraphRenderer_Tabs.pdf");
        TestParagraphRenderer.Tabs(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestParagraphRenderer_Borders()
    {
        var path = GetOutputPath("ParagraphRenderer_Borders.pdf");
        TestParagraphRenderer.Borders(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestParagraphRenderer_Fields()
    {
        var path = GetOutputPath("ParagraphRenderer_Fields.pdf");
        TestParagraphRenderer.Fields(path);
        ValidateFileIsPdf(path);
    }

    // --- TestTable tests ---

    [Fact]
    public void TestTable_Borders()
    {
        var path = GetOutputPath("Table_Borders.pdf");
        TestTable.Borders(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestTable_CellMerge()
    {
        var path = GetOutputPath("Table_CellMerge.pdf");
        TestTable.CellMerge(path);
        ValidateFileIsPdf(path);
    }

    [Fact]
    public void TestTable_VerticalAlign()
    {
        var path = GetOutputPath("Table_VerticalAlign.pdf");
        TestTable.VerticalAlign(path);
        ValidateFileIsPdf(path);
    }

    // --- TestParagraphIterator tests ---

    private static Paragraph CreateTestParagraph()
    {
        var doc = new Document();
        var section = doc.AddSection();
        var par = section.AddParagraph();
        par.AddText("Hello");
        par.AddCharacter(SymbolName.Blank);
        par.AddFormattedText("World", TextFormat.Bold);
        par.AddLineBreak();
        par.AddText("Second line");
        return par;
    }

    [Fact]
    public void TestParagraphIterator_GetIterators()
    {
        var par = CreateTestParagraph();
        var result = TestParagraphIterator.GetIterators(par);
        result.Should().NotBeNullOrEmpty();
        Assert.Contains("[Text:]Hello", result);
        Assert.Contains("[Text:]Second line", result);
    }

    [Fact]
    public void TestParagraphIterator_GetBackIterators()
    {
        var par = CreateTestParagraph();
        var result = TestParagraphIterator.GetBackIterators(par);
        result.Should().NotBeNullOrEmpty();
        Assert.Contains("[Text:]Second line", result);
        Assert.Contains("[Text:]Hello", result);
    }

    // --- ValueDumper test ---

    [Fact]
    public void ValueDumper_DumpValues()
    {
        var par = new Document();
        var result = ValueDumper.DumpValues(par);
        result.Should().NotBeNullOrEmpty();
        Assert.StartsWith("[", result);
    }
}
