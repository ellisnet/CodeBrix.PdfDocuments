using CodeBrix.PdfDocCreate.DocumentObjectModel;
using SilverAssertions;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.DocumentObjectModel;

/// <summary>
/// Fences the process-wide default page setup against concurrent first use: it used to be
/// published before its properties were set, so a document rendering on another thread at the
/// same moment read a half-initialized default - wrong page size and margins in Release, a
/// NullReferenceException in Debug (seen as "flaky" rendering tests under parallel runs).
/// </summary>
[Collection(nameof(PageSetupTestsCollection))]
public class PageSetupTests
{
    private static readonly FieldInfo DefaultField =
        typeof(PageSetup).GetField("defaultPageSetup", BindingFlags.NonPublic | BindingFlags.Static);

    [Fact]
    public async Task Every_thread_sees_a_fully_initialized_default_on_first_use()
    {
        DefaultField.Should().NotBeNull();
        var original = DefaultField.GetValue(null);
        try
        {
            for (int round = 0; round < 300; round++)
            {
                //Arrange - forget the default, so the next read builds it again
                DefaultField.SetValue(null, null);
                const int threads = 8;
                using var start = new Barrier(threads);

                //Act
                var results = Enumerable.Range(0, threads)
                    .Select(_ => Task.Run(() =>
                    {
                        start.SignalAndWait();
                        var setup = PageSetup.DefaultPageSetup;
                        return (Width: setup.PageWidth.Centimeter, Left: setup.LeftMargin.Centimeter, Format: setup.PageFormat);
                    }))
                    .ToArray();
                var seen = await Task.WhenAll(results);

                //Assert
                foreach (var result in seen)
                {
                    result.Width.Should().BeApproximately(21, 0.001);
                    result.Left.Should().BeApproximately(2.5, 0.001);
                    result.Format.Should().Be(PageFormat.A4);
                }
            }
        }
        finally
        {
            DefaultField.SetValue(null, original ?? PageSetup.DefaultPageSetup);
        }
    }
}

/// <summary>The default page setup is process-wide: this test must not run beside rendering tests.</summary>
[CollectionDefinition(nameof(PageSetupTestsCollection), DisableParallelization = true)]
public class PageSetupTestsCollection
{
}
