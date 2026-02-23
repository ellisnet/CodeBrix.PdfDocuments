using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Pdf.Content;
using CodeBrix.PdfDocuments.Pdf.Content.Objects;
using CodeBrix.PdfDocuments.Pdf.IO;
using CodeBrix.PdfDocuments.Tests.Helpers;
using SilverAssertions;
using System.IO;
using System.Text;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests;

/// <summary>
/// Tests ensuring coverage for code paths flagged by CA2022 (inexact Stream.Read calls).
/// These tests exercise the methods that will be changed from .Read() to .ReadExactly().
/// </summary>
public class CA2022CoverageTests
{
    // -----------------------------------------------------------------------
    // Coverage for: PdfReader.cs line 122 — TestPdfFile(Stream)
    //   stream.Read(bytes, 0, 1024)
    // -----------------------------------------------------------------------

    [Fact]
    public void TestPdfFile_Stream_WithValidPdf_ReturnsVersion()
    {
        var pdfPath = PathHelper.GetInstance().GetAssetPath("FamilyTree.pdf");
        using var stream = File.OpenRead(pdfPath);

        var version = Pdf.IO.PdfReader.TestPdfFile(stream);

        version.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TestPdfFile_Stream_WithInvalidData_ReturnsZero()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("This is not a PDF file at all"));

        var version = Pdf.IO.PdfReader.TestPdfFile(stream);

        version.Should().Be(0);
    }

    [Fact]
    public void TestPdfFile_Stream_WithEmptyStream_ReturnsZero()
    {
        using var stream = new MemoryStream();

        var version = Pdf.IO.PdfReader.TestPdfFile(stream);

        version.Should().Be(0);
    }

    [Fact]
    public void TestPdfFile_Stream_PreservesStreamPosition()
    {
        var pdfPath = PathHelper.GetInstance().GetAssetPath("FamilyTree.pdf");
        using var stream = File.OpenRead(pdfPath);
        var originalPosition = stream.Position;

        Pdf.IO.PdfReader.TestPdfFile(stream);

        stream.Position.Should().Be(originalPosition);
    }

    // -----------------------------------------------------------------------
    // Coverage for: PdfReader.cs line 86 — TestPdfFile(string path)
    //   stream.Read(bytes, 0, 1024)
    // -----------------------------------------------------------------------

    [Fact]
    public void TestPdfFile_Path_WithValidPdf_ReturnsVersion()
    {
        var pdfPath = PathHelper.GetInstance().GetAssetPath("FamilyTree.pdf");

        var version = Pdf.IO.PdfReader.TestPdfFile(pdfPath);

        version.Should().BeGreaterThan(0);
    }

    [Fact]
    public void TestPdfFile_Path_WithNonExistentFile_ReturnsZero()
    {
        var version = Pdf.IO.PdfReader.TestPdfFile("nonexistent_file_xyz.pdf");

        version.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // Coverage for: PdfReader.cs line 386 — Open(Stream, ...)
    //   stream.Read(header, 0, 1024)
    // AND
    // Coverage for: Lexer.cs line 253 — ReadRawString(position, length)
    //   _pdfSteam.Read(bytes, 0, length)
    // Both are exercised when opening and parsing a PDF document.
    // -----------------------------------------------------------------------

    [Fact]
    public void Open_Stream_ExercisesHeaderReadAndLexerReadRawString()
    {
        var pdfPath = PathHelper.GetInstance().GetAssetPath("FamilyTree.pdf");
        using var stream = File.OpenRead(pdfPath);

        var document = Pdf.IO.PdfReader.Open(stream, PdfDocumentOpenMode.Import);

        document.Should().NotBeNull();
        document.PageCount.Should().BeGreaterThan(0);
        document.Info.Should().NotBeNull();
    }

    [Fact]
    public void Open_Stream_WithCreatedPdf_RoundTripsSuccessfully()
    {
        // Create a PDF with text content so the round-trip exercises
        // Lexer.ReadRawString for string objects in the content stream.
        var originalDoc = new PdfDocument();
        var page = originalDoc.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawString("Hello World", new XFont("Arial", 12), XBrushes.Black, new XPoint(100, 100));

        using var ms = new MemoryStream();
        originalDoc.Save(ms);
        ms.Position = 0;

        var reopened = Pdf.IO.PdfReader.Open(ms, PdfDocumentOpenMode.Import);

        reopened.Should().NotBeNull();
        reopened.PageCount.Should().Be(1);
    }

    // -----------------------------------------------------------------------
    // Coverage for: CObjects.cs line 322 — CSequence.ToContent()
    //   stream.Read(bytes, 0, count)
    // -----------------------------------------------------------------------

    [Fact]
    public void CSequence_ToContent_RoundTripsContentBytes()
    {
        // Create a simple PDF content stream as bytes
        var contentString = "BT /F1 12 Tf 100 700 Td (Hello World) Tj ET";
        var contentBytes = Encoding.ASCII.GetBytes(contentString);

        // Parse the content bytes into a CSequence
        var sequence = ContentReader.ReadContent(contentBytes);
        sequence.Count.Should().BeGreaterThan(0);

        // Round-trip via ToContent() — this exercises the stream.Read at line 322
        var outputBytes = sequence.ToContent();

        outputBytes.Should().NotBeNull();
        outputBytes.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CSequence_ToContent_FromPdfPage_RoundTrips()
    {
        // Create a PDF with actual rendered content
        var document = new PdfDocument();
        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        gfx.DrawString("Test content for round-trip", new XFont("Arial", 12), XBrushes.Black, new XPoint(50, 50));

        // Save and reopen to get a proper page with content stream
        using var ms = new MemoryStream();
        document.Save(ms);
        ms.Position = 0;

        var reopened = Pdf.IO.PdfReader.Open(ms, PdfDocumentOpenMode.Modify);
        var reopenedPage = reopened.Pages[0];

        // Read the page's content stream
        var sequence = ContentReader.ReadContent(reopenedPage);
        sequence.Count.Should().BeGreaterThan(0);

        // Exercise ToContent() — the CA2022 code path
        var contentBytes = sequence.ToContent();

        contentBytes.Should().NotBeNull();
        contentBytes.Length.Should().BeGreaterThan(0);

        // Verify the round-tripped content can be parsed again
        var reparsed = ContentReader.ReadContent(contentBytes);
        reparsed.Count.Should().Be(sequence.Count);
    }

    // -----------------------------------------------------------------------
    // Coverage for: AESEncryptor.cs line 103
    //   cs.Read(encryptionKey, 0, 32)
    // Exercised when opening an AES-encrypted PDF document.
    // -----------------------------------------------------------------------

    [Fact]
    public void Open_AesEncryptedPdf_ExercisesAESEncryptorRead()
    {
        var file = PathHelper.GetInstance().GetAssetPath("AesEncrypted.pdf");
        var document = Pdf.IO.PdfReader.Open(file, PdfDocumentOpenMode.Import);

        document.Should().NotBeNull();
        document.PageCount.Should().BeGreaterThan(0);

        // Verify it was AES-encrypted
        var cf = document.SecurityHandler.Elements.GetDictionary("/CF");
        cf.Should().NotBeNull();
    }

    // -----------------------------------------------------------------------
    // Coverage for: IImageImporter.cs line 68 — StreamReaderHelper constructor
    //   _stream.Read(_data, 0, _length)
    //
    // NOTE: StreamReaderHelper and ImageImporter are internal classes with no
    // InternalsVisibleTo for the test project. The StreamReaderHelper is only
    // reachable through ImageImporter.ImportImage(), which is not called by
    // any public API in the normal code path (CodeBrix.Imaging/ImagingImageSource
    // handles image loading instead). This code path cannot be exercised
    // through public APIs. The .Read() → .ReadExactly() change in
    // StreamReaderHelper is safe because it reads from a stream whose full
    // length is already known (_length = (int)_stream.Length), so the read
    // should always return the expected number of bytes.
    // -----------------------------------------------------------------------
}
