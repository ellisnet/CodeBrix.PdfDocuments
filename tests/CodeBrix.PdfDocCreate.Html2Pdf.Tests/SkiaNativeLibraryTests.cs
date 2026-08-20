using System;
using CodeBrix.PdfDocCreate.Html2Pdf.Svg;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// SVG rendering is the only feature that needs the SkiaSharp native library, and on
/// Linux the consuming application - not this package - must supply it. When it is
/// absent the failure has to name the packages that fix it, because the raw runtime
/// error is a type-initializer chain that says nothing about NuGet. These tests pin the
/// detection and the message; they cannot exercise the real load failure, since the
/// native is present wherever the suite runs.
/// </summary>
public class SkiaNativeLibraryTests
{
    [Fact]
    public void dll_not_found_is_recognized_as_a_missing_native()
    {
        //Arrange
        var exception = new DllNotFoundException("Unable to load shared library 'libSkiaSharp'.");

        //Act
        var recognized = SkiaNativeLibrary.IsMissingNativeLibrary(exception);

        //Assert
        recognized.Should().BeTrue();
    }

    [Fact]
    public void nested_type_initializer_chain_is_recognized_as_a_missing_native()
    {
        //Arrange - the shape the runtime actually produces: the first touch of SkiaSharp
        //happens in a static constructor, so the load failure is wrapped twice over.
        var exception = new TypeInitializationException(
            "SkiaSharp.SKImageInfo",
            new TypeInitializationException(
                "SkiaSharp.SkiaApi",
                new DllNotFoundException("Unable to load shared library 'libSkiaSharp'.")));

        //Act
        var recognized = SkiaNativeLibrary.IsMissingNativeLibrary(exception);

        //Assert
        recognized.Should().BeTrue();
    }

    [Fact]
    public void an_ordinary_rendering_failure_is_not_a_missing_native()
    {
        //Arrange - a malformed SVG must keep its own warning rather than being reported
        //as an environment problem.
        var exception = new InvalidOperationException("The SVG content produced no drawable picture.");

        //Act
        var recognized = SkiaNativeLibrary.IsMissingNativeLibrary(exception);

        //Assert
        recognized.Should().BeFalse();
    }

    private const string PlainPackage = "SkiaSharp.NativeAssets.Linux";
    private const string NoDependenciesSuffix = ".NoDependencies";

    /// <summary>
    /// Counts mentions of the plain package only. A plain Contains check cannot do this:
    /// "SkiaSharp.NativeAssets.Linux" is a prefix of
    /// "SkiaSharp.NativeAssets.Linux.NoDependencies", so it matches even in a message that
    /// names the NoDependencies variant alone - exactly the regression these tests exist
    /// to catch.
    /// </summary>
    private static int CountPlainPackageMentions(string message)
    {
        var count = 0;
        var index = 0;
        while ((index = message.IndexOf(PlainPackage, index, StringComparison.Ordinal)) >= 0)
        {
            var after = index + PlainPackage.Length;
            var isLongerName = string.CompareOrdinal(
                message, after, NoDependenciesSuffix, 0, NoDependenciesSuffix.Length) == 0;
            if (!isLongerName) { count++; }
            index = after;
        }

        return count;
    }

    [Fact]
    public void the_message_names_both_linux_native_asset_packages()
    {
        //Act
        var message = SkiaNativeLibrary.BuildMessage();

        //Assert - a reader must be able to act on this without consulting the docs, and
        //both packages must be named in their own right.
        CountPlainPackageMentions(message).Should().BeGreaterThan(0);
        message.Should().Contain(PlainPackage + NoDependenciesSuffix);
    }

    [Fact]
    public void the_message_does_not_steer_the_consumer_toward_either_package()
    {
        //Arrange - an application may already depend on one variant for reasons of its
        //own; the guidance must present the two as equally acceptable rather than
        //nudging anyone into swapping.
        var message = SkiaNativeLibrary.BuildMessage();

        //Assert - neither package may be singled out as the one to install.
        foreach (var preference in new[]
                 {
                     "better choice", "recommended", "preferred", "usual choice",
                     "we suggest", "should use", "best option",
                 })
        {
            message.IndexOf(preference, StringComparison.OrdinalIgnoreCase)
                .Should().BeLessThan(0, $"the guidance must not say '{preference}'");
        }

        //Assert - and each must appear on its own, so neither can quietly drop out.
        CountPlainPackageMentions(message).Should().BeGreaterThan(0);
        message.Should().Contain(PlainPackage + NoDependenciesSuffix);
    }

    [Fact]
    public void the_message_explains_that_only_svg_content_is_affected()
    {
        //Act
        var message = SkiaNativeLibrary.BuildMessage();

        //Assert
        message.Should().Contain("SVG");
        message.Should().Contain("renders normally without it");
    }

    [Fact]
    public void the_translated_exception_carries_the_guidance_and_keeps_the_original()
    {
        //Arrange
        var original = new DllNotFoundException("Unable to load shared library 'libSkiaSharp'.");

        //Act
        var translated = new SkiaNativeLibraryMissingException(original);

        //Assert
        translated.Message.Should().Contain("SkiaSharp.NativeAssets.Linux.NoDependencies");
        translated.InnerException.Should().BeSameAs(original);
    }
}
