using CodeBrix.PdfDocuments.Pdf;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Pdfs;

public class PdfInternalsTests
{
    [Fact]
    public void CustomValueKey_DefaultsToTheCodeBrixKey()
    {
        var document = new PdfDocument();

        document.Internals.CustomValueKey.Should().Be("/CodeBrix.PdfDocuments.CustomValue");
    }

    [Fact]
    public void CustomValueKey_IsAssignable_SoOlderDocumentsRemainReadable()
    {
        // The key is written into the PDF itself, so documents produced before the rename stored
        // custom values under the upstream key. Assigning it back is the supported way to read one.
        var document = new PdfDocument
        {
            Internals =
            {
                CustomValueKey = "/PdfSharpCore.CustomValue"
            }
        };

        document.Internals.CustomValueKey.Should().Be("/PdfSharpCore.CustomValue");
    }
}
