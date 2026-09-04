using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Pdf.IO;
using SilverAssertions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.IO;

/// <summary>
/// Fences the reader against a size floor: it used to read a fixed 1,024-byte window with
/// ReadExactly, so every document shorter than that - including one-page documents this
/// package writes itself - was rejected as "not a valid PDF document".
/// The test class PdfReader in this namespace shadows Pdf.IO.PdfReader, so the reader is
/// named through Pdf.IO here.
/// </summary>
public class PdfReaderTests
{
    private const double PageWidthPoints = 200;
    private const double PageHeightPoints = 400;

    [Fact]
    public void Open_one_page_document_under_1024_bytes_opens_in_information_only_mode()
    {
        //Arrange
        byte[] bytes = SaveOnePageDocument(null);

        //Act
        PdfDocument document = Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.InformationOnly);

        //Assert
        bytes.Length.Should().BeLessThan(1024);
        document.Should().NotBeNull();
        document.PageCount.Should().Be(1);
        document.Pages[0].Width.Point.Should().Be(PageWidthPoints);
        document.Pages[0].Height.Point.Should().Be(PageHeightPoints);
    }

    [Fact]
    public void Open_one_page_document_under_1024_bytes_opens_in_import_mode()
    {
        //Arrange
        byte[] bytes = SaveOnePageDocument(null);

        //Act
        PdfDocument document = Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);

        //Assert
        bytes.Length.Should().BeLessThan(1024);
        document.Should().NotBeNull();
        document.PageCount.Should().Be(1);
        document.Pages[0].Width.Point.Should().Be(PageWidthPoints);
        document.Pages[0].Height.Point.Should().Be(PageHeightPoints);
    }

    [Fact]
    public void Open_one_page_document_under_1024_bytes_opens_from_a_file_path()
    {
        //Arrange
        byte[] bytes = SaveOnePageDocument(null);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, bytes);

        try
        {
            //Act
            PdfDocument document = Pdf.IO.PdfReader.Open(path, PdfDocumentOpenMode.InformationOnly);

            //Assert
            new FileInfo(path).Length.Should().BeLessThan(1024L);
            document.PageCount.Should().Be(1);
            document.Pages[0].Width.Point.Should().Be(PageWidthPoints);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Open_hand_written_minimal_document_of_a_few_hundred_bytes_opens()
    {
        //Arrange
        byte[] bytes = MinimalPdfBytes();

        //Act
        PdfDocument information = Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.InformationOnly);
        PdfDocument imported = Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.Import);

        //Assert
        bytes.Length.Should().BeLessThan(600);
        information.PageCount.Should().Be(1);
        imported.PageCount.Should().Be(1);
        imported.Pages[0].Width.Point.Should().Be(PageWidthPoints);
        imported.Pages[0].Height.Point.Should().Be(PageHeightPoints);
    }

    [Fact]
    public void Open_documents_on_both_sides_of_the_1024_byte_boundary_all_open()
    {
        //Arrange
        List<int> sizes = new List<int>();

        //Act
        for (int padding = 0; padding <= 40; padding++)
        {
            byte[] bytes = SaveOnePageDocument(new string('x', padding));
            sizes.Add(bytes.Length);
            PdfDocument document = Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.InformationOnly);
            document.PageCount.Should().Be(1);
            document.Pages[0].Width.Point.Should().Be(PageWidthPoints);
        }

        //Assert
        //The walk has to straddle 1,024 bytes, or it would fence nothing.
        sizes.Exists(size => size < 1024).Should().BeTrue();
        sizes.Exists(size => size > 1024).Should().BeTrue();
    }

    [Fact]
    public void Open_truncated_document_with_a_valid_header_throws_a_reader_exception()
    {
        //Arrange
        //A header and an end-of-file marker, and nothing else: shorter than the back-search
        //window the trailer scan used to assume it could always read.
        byte[] bytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF\n");

        //Act
        Action act = () => Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.InformationOnly);

        //Assert
        bytes.Length.Should().BeLessThan(31);
        act.Should().Throw<PdfReaderException>().WithMessage("The StartXRef table could not be found, the file cannot be opened.");
    }

    [Fact]
    public void Open_ten_byte_junk_stream_is_still_rejected()
    {
        //Arrange
        byte[] bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        //Act
        Action act = () => Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.InformationOnly);

        //Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("The file is not a valid PDF document.");
    }

    [Fact]
    public void Open_empty_stream_is_still_rejected()
    {
        //Arrange
        byte[] bytes = new byte[0];

        //Act
        Action act = () => Pdf.IO.PdfReader.Open(new MemoryStream(bytes), PdfDocumentOpenMode.InformationOnly);

        //Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("The file is not a valid PDF document.");
    }

    [Fact]
    public void TestPdfFile_reports_the_version_of_a_document_under_1024_bytes()
    {
        //Arrange
        byte[] bytes = SaveOnePageDocument(null);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf");
        File.WriteAllBytes(path, bytes);

        try
        {
            //Act
            int fromPath = Pdf.IO.PdfReader.TestPdfFile(path);
            int fromStream;
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                fromStream = Pdf.IO.PdfReader.TestPdfFile(stream);
            }
            int fromBytes = Pdf.IO.PdfReader.TestPdfFile(bytes);

            //Assert
            bytes.Length.Should().BeLessThan(1024);
            fromPath.Should().BeGreaterThan(0);
            fromStream.Should().BeGreaterThan(0);
            fromBytes.Should().BeGreaterThan(0);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void TestPdfFile_reads_the_header_of_a_short_document_from_a_chunked_stream()
    {
        //Arrange
        //A stream that reports it cannot seek and hands back only a few bytes per read, so the
        //header probe has to loop rather than ask for a fixed window in one call.
        byte[] bytes = SaveOnePageDocument(null);
        using ChunkedStream stream = new ChunkedStream(bytes, 7);

        //Act
        int version = Pdf.IO.PdfReader.TestPdfFile(stream);

        //Assert
        bytes.Length.Should().BeLessThan(1024);
        version.Should().BeGreaterThan(0);
    }

    private static byte[] SaveOnePageDocument(string subject)
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage();
        page.Width = XUnit.FromPoint(PageWidthPoints);
        page.Height = XUnit.FromPoint(PageHeightPoints);
        if (subject != null)
            document.Info.Subject = subject;
        MemoryStream stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static byte[] MinimalPdfBytes()
    {
        //A hand-written, structurally valid one-page document of a few hundred bytes.
        StringBuilder body = new StringBuilder();
        int[] offsets = new int[5];
        body.Append("%PDF-1.4\n");
        AppendObject(body, offsets, 1, "<< /Type /Catalog /Pages 2 0 R >>");
        AppendObject(body, offsets, 2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        AppendObject(body, offsets, 3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 "
            + PageWidthPoints.ToString(CultureInfo.InvariantCulture) + " "
            + PageHeightPoints.ToString(CultureInfo.InvariantCulture) + "] /Contents 4 0 R >>");
        AppendObject(body, offsets, 4, "<< /Length 0 >>\nstream\n\nendstream");
        int startXRef = body.Length;
        body.Append("xref\n0 5\n");
        body.Append("0000000000 65535 f \n");
        for (int number = 1; number <= 4; number++)
            body.Append(offsets[number].ToString("D10")).Append(" 00000 n \n");
        body.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n").Append(startXRef).Append("\n%%EOF\n");
        return Encoding.ASCII.GetBytes(body.ToString());
    }

    private static void AppendObject(StringBuilder body, int[] offsets, int number, string content)
    {
        offsets[number] = body.Length;
        body.Append(number).Append(" 0 obj\n").Append(content).Append("\nendobj\n");
    }

    /// <summary>
    /// A read-only stream that reports it cannot seek and returns at most a few bytes per read.
    /// </summary>
    private sealed class ChunkedStream : Stream
    {
        private readonly MemoryStream _inner;
        private readonly int _chunk;

        internal ChunkedStream(byte[] bytes, int chunk)
        {
            _inner = new MemoryStream(bytes);
            _chunk = chunk;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get { return _inner.Position; }
            set { _inner.Position = value; }
        }

        public override void Flush()
        { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, Math.Min(count, _chunk));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
