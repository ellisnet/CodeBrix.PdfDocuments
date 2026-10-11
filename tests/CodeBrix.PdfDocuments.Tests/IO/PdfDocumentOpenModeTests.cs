using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Pdf.IO;
using SilverAssertions;
using System;
using System.IO;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.IO;

/// <summary>
/// Fences the open-mode gate (PdfDocument.CanModify, as in empira PDFsharp 6.2.4): a document created in code or
/// opened in Modify mode can be modified and saved; one opened in Import (or the obsolete ReadOnly/InformationOnly)
/// mode cannot - Save, Close, the Version/PageLayout/PageMode setters, AddPage and InsertPage throw
/// InvalidOperationException. The test class PdfReader in this namespace shadows Pdf.IO.PdfReader, so the reader
/// is named through Pdf.IO here.
/// </summary>
public class PdfDocumentOpenModeTests
{
    [Fact]
    public void A_new_document_can_be_saved_and_is_not_read_only()
    {
        //Arrange
        var document = new PdfDocument();
        document.AddPage();

        //Act
        Action save = () => document.Save(new MemoryStream(), false);

        //Assert
        save.Should().NotThrow();
        document.IsReadOnly.Should().BeFalse();
    }

    [Fact]
    public void A_document_opened_in_Modify_mode_can_be_changed_and_saved()
    {
        //Arrange
        var document = Pdf.IO.PdfReader.Open(new MemoryStream(TwoPageDocument()), PdfDocumentOpenMode.Modify);

        //Act
        document.AddPage();
        document.PageLayout = PdfPageLayout.TwoColumnLeft;
        var output = new MemoryStream();
        document.Save(output, false);

        //Assert
        document.IsReadOnly.Should().BeFalse();
        Pdf.IO.PdfReader.Open(new MemoryStream(output.ToArray()), PdfDocumentOpenMode.Import).PageCount.Should().Be(3);
    }

    [Theory]
    [InlineData(PdfDocumentOpenMode.Import)]
#pragma warning disable CS0618
    [InlineData(PdfDocumentOpenMode.ReadOnly)]
#pragma warning restore CS0618
    public void A_document_not_opened_in_Modify_mode_cannot_be_saved(PdfDocumentOpenMode mode)
    {
        //Arrange
        var document = Pdf.IO.PdfReader.Open(new MemoryStream(TwoPageDocument()), mode);
        document.Pages.Count.Should().Be(2);

        //Act
        Action saveToStream = () => document.Save(new MemoryStream(), false);
        Action saveToPath = () => document.Save(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".pdf"));

        //Assert
        document.IsReadOnly.Should().BeTrue();
        saveToStream.Should().Throw<InvalidOperationException>();
        saveToPath.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void A_document_opened_in_Import_mode_cannot_be_modified()
    {
        //Arrange
        var document = Pdf.IO.PdfReader.Open(new MemoryStream(TwoPageDocument()), PdfDocumentOpenMode.Import);

        //Act + Assert
        ((Action)(() => document.AddPage())).Should().Throw<InvalidOperationException>();
        ((Action)(() => document.InsertPage(0))).Should().Throw<InvalidOperationException>();
        ((Action)(() => document.Version = 16)).Should().Throw<InvalidOperationException>();
        ((Action)(() => document.PageLayout = PdfPageLayout.SinglePage)).Should().Throw<InvalidOperationException>();
        ((Action)(() => document.PageMode = PdfPageMode.UseNone)).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void A_document_opened_in_Import_mode_reports_its_page_count()
    {
        //Arrange + Act
        var document = Pdf.IO.PdfReader.Open(new MemoryStream(TwoPageDocument()), PdfDocumentOpenMode.Import);

        //Assert
        document.PageCount.Should().Be(2);
    }

    [Fact]
    public void Pages_of_an_Import_mode_document_can_be_copied_into_a_new_document_and_saved()
    {
        //Arrange
        var source = Pdf.IO.PdfReader.Open(new MemoryStream(TwoPageDocument()), PdfDocumentOpenMode.Import);
        var target = new PdfDocument();

        //Act
        foreach (var page in source.Pages)
            target.AddPage(page);
        var output = new MemoryStream();
        target.Save(output, false);

        //Assert
        Pdf.IO.PdfReader.Open(new MemoryStream(output.ToArray()), PdfDocumentOpenMode.Import).PageCount.Should().Be(2);
    }

    [Fact]
    public void An_XPdfForm_from_a_stream_can_be_drawn_into_a_new_document()
    {
        //Arrange
        var form = XPdfForm.FromStream(new MemoryStream(TwoPageDocument()));
        var target = new PdfDocument();
        var page = target.AddPage();

        //Act
        using (var graphics = XGraphics.FromPdfPage(page))
            graphics.DrawImage(form, 0, 0);
        var output = new MemoryStream();
        Action save = () => target.Save(output, false);

        //Assert
        form.PageCount.Should().Be(2);
        save.Should().NotThrow();
        output.Length.Should().BeGreaterThan(0);
    }

    private static byte[] TwoPageDocument()
    {
        var document = new PdfDocument();
        document.AddPage();
        document.AddPage();
        var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }
}
