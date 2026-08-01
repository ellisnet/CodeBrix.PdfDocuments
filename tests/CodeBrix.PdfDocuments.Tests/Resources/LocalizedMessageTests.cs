using System;
using System.Reflection;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Resources;

/// <summary>
/// Guards the three embedded resource sets against the fork defect where the ResourceManager was
/// still constructed with the upstream base name. Every lookup then threw
/// MissingManifestResourceException, silently replacing the library's own messages.
/// </summary>
public class LocalizedMessageTests
{
    private const string ResourceFailureMarker = "Could not find the resource";

    // ── CodeBrix.PdfDocCreate - DocumentObjectModel resource set ─────────

    [Fact]
    public void DocumentObjectModelResources_StyleExpected_ResolvesToTheAuthoredMessage()
    {
        var message = ReadInternalStaticString(
            typeof(Document).Assembly,
            "CodeBrix.PdfDocCreate.DocumentObjectModel.Resources.AppResources",
            "StyleExpected");

        message.Should().NotBeNullOrEmpty();
        message.Should().Contain("must be of type");
        message.Should().NotContain("MigraDoc");
    }

    [Fact]
    public void DocumentObjectModelResources_UnitParseFailure_ProducesTheAuthoredMessage()
    {
        var ex = Assert.Throws<ArgumentException>(() => Unit.Parse("12 zz"));

        ex.Message.Should().Contain("zz");
        ex.Message.Should().NotContain(ResourceFailureMarker);
    }

    // ── CodeBrix.PdfDocCreate - Rendering resource set ───────────────────

    [Fact]
    public void RenderingResources_ObjectNotRenderable_ResolvesToTheAuthoredMessage()
    {
        var message = ReadInternalStaticString(
            typeof(Document).Assembly,
            "CodeBrix.PdfDocCreate.Rendering.Resources.AppResources",
            "ObjectNotRenderable");

        message.Should().NotBeNullOrEmpty();
        message.Should().NotContain(ResourceFailureMarker);
    }

    // ── CodeBrix.PdfDocuments - Messages resource set (PSSR) ─────────────

    [Fact]
    public void PdfDocumentsMessages_UserOrOwnerPasswordRequired_ResolvesToTheAuthoredMessage()
    {
        var message = ReadInternalStaticString(
            typeof(PdfDocuments.Pdf.PdfDocument).Assembly,
            "CodeBrix.PdfDocuments.PSSR",
            "UserOrOwnerPasswordRequired");

        message.Should().NotBeNullOrEmpty();
        message.Should().Contain("password");
        message.Should().NotContain(ResourceFailureMarker);
    }

    [Fact]
    public void PdfDocumentsMessages_UnknownEncryption_DoesNotNameTheUpstreamProject()
    {
        var message = ReadInternalStaticString(
            typeof(PdfDocuments.Pdf.PdfDocument).Assembly,
            "CodeBrix.PdfDocuments.PSSR",
            "UnknownEncryption");

        message.Should().NotBeNullOrEmpty();
        message.Should().NotContain("PDFsharp");
        message.Should().NotContain("PdfSharp");
    }

    /// <summary>
    /// Reads an internal static string member (property or field) by name. Reflection is used so
    /// that the assemblies under test do not have to widen their visibility for this guard.
    /// </summary>
    private static string ReadInternalStaticString(Assembly assembly, string typeName, string memberName)
    {
        var type = assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Type '{typeName}' not found in '{assembly.GetName().Name}'.");

        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        try
        {
            var property = type.GetProperty(memberName, flags);
            if (property is not null)
                return (string)property.GetValue(null);

            var field = type.GetField(memberName, flags)
                ?? throw new InvalidOperationException($"Member '{memberName}' not found on '{typeName}'.");
            return (string)field.GetValue(null);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            // Surface the real failure (historically MissingManifestResourceException) rather than
            // the reflection wrapper, so a regression reads clearly in the test output.
            throw ex.InnerException;
        }
    }
}
