using System.IO;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.DocumentObjectModel.IO;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.IO;

public class DdlReaderTests
{
    [Fact]
    public void ReadDocument_with_valid_ddl_records_no_errors()
    {
        var errors = new DdlReaderErrors();
        const string ddl = "\\document { \\section { \\paragraph { Hello } } }";

        using var reader = new DdlReader(new StringReader(ddl), errors);
        Document document = reader.ReadDocument();

        document.Should().NotBeNull();
        document.Sections.Count.Should().Be(1);
        errors.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void ReadDocument_with_invalid_ddl_records_errors_in_the_callers_collection()
    {
        var errors = new DdlReaderErrors();
        const string ddl = "\\document { \\section { \\paragraph [ NoSuchAttribute = 1 ] { Hello } } }";

        using var reader = new DdlReader(new StringReader(ddl), errors);
        reader.ReadDocument();

        errors.ErrorCount.Should().BeGreaterThan(0);
    }
}
