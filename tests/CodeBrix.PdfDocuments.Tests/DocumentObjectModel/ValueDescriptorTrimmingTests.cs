using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocCreate.DocumentObjectModel.Internals;
using SilverAssertions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.DocumentObjectModel;

/// <summary>
/// Fences the trimming support of the document object model: ValueDescriptor.CreateValue builds
/// the value of a [DV] field or property through the member type's public parameterless
/// constructor, by reflection. In a trimmed application those constructors are kept only by the
/// DynamicDependency list on CreateValue (without it, DdlReader threw a NullReferenceException
/// reading back a paragraph with shading or borders).
/// </summary>
public class ValueDescriptorTrimmingTests
{
    [Fact]
    public void Every_dv_member_document_object_type_is_kept_for_trimming()
    {
        //Arrange
        const BindingFlags all = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        var constructed = typeof(DocumentObject).Assembly.GetTypes()
            .Where(t => typeof(DocumentObject).IsAssignableFrom(t))
            .SelectMany(t => t.GetFields(all).Select(f => (Member: (MemberInfo)f, Type: f.FieldType))
                .Concat(t.GetProperties(all).Select(p => (Member: (MemberInfo)p, Type: p.PropertyType))))
            .Where(m => m.Member.GetCustomAttribute<DVAttribute>(false) != null)
            .Select(m => m.Type)
            .Where(t => typeof(DocumentObject).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) != null)
            .Distinct()
            .ToList();

        //Act
        var kept = typeof(ValueDescriptor).GetMethod(nameof(ValueDescriptor.CreateValue))
            .GetCustomAttributes<DynamicDependencyAttribute>()
            .Where(a => a.MemberTypes == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)
            .Select(a => a.Type)
            .ToList();

        //Assert
        constructed.Should().NotBeEmpty();
        constructed.Except(kept).Select(t => t.FullName).Should().BeEmpty();
    }

    [Fact]
    public void Ddl_with_formatted_paragraphs_and_tables_round_trips()
    {
        //Arrange
        var document = new Document();
        var section = document.AddSection();
        var paragraph = section.AddParagraph("Hello");
        paragraph.Format.Font.Bold = true;
        paragraph.Format.Shading.Color = Colors.Yellow;
        paragraph.Format.Borders.Width = 1;
        var table = section.AddTable();
        table.AddColumn(Unit.FromCentimeter(4));
        var row = table.AddRow();
        row.Cells[0].AddParagraph("cell");
        row.Shading.Color = Colors.LightGray;
        var ddl = PdfDocCreate.DocumentObjectModel.IO.DdlWriter.WriteToString(document);

        //Act
        var readBack = PdfDocCreate.DocumentObjectModel.IO.DdlReader.DocumentFromString(ddl);

        //Assert
        PdfDocCreate.DocumentObjectModel.IO.DdlWriter.WriteToString(readBack).Should().Be(ddl);
    }
}
