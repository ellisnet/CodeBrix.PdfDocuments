using System.IO;
using System.Reflection;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfDocuments.Pdf.IO;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests; //Was previously: namespace PdfSharpCore.Test;

public class PdfReader
{
    [Fact]
    public void Should_beAbleToReadExistingPdf_When_inputIsStream()
    {
        var root = Path.GetDirectoryName(GetType().GetTypeInfo().Assembly.Location);
        var existingPdfPath = Path.Combine(root, "Assets", "FamilyTree.pdf");

        var fs = File.OpenRead(existingPdfPath);
        PdfDocument inputDocument = Pdf.IO.PdfReader.Open(fs, PdfDocumentOpenMode.Import);
        fs.Dispose();

        Assert.True(true);
    }
}