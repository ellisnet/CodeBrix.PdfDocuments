using System;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests;

public class ProductVersionInfoTests
{
    private readonly ITestOutputHelper _output;

    public ProductVersionInfoTests(ITestOutputHelper output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    [Fact]
    public void can_get_Version()
    {
        Assert.NotNull(ProductVersionInfo.Version);
        Assert.NotEmpty(ProductVersionInfo.Version);
        _output.WriteLine($"{nameof(ProductVersionInfo.Version)}: {ProductVersionInfo.Version}");
    }

    [Fact]
    public void can_get_VersionReferenceDate()
    {
        Assert.NotNull(ProductVersionInfo.VersionReferenceDate);
        Assert.NotEmpty(ProductVersionInfo.VersionReferenceDate);
        _output.WriteLine($"{nameof(ProductVersionInfo.VersionReferenceDate)}: {ProductVersionInfo.VersionReferenceDate}");
    }
}
