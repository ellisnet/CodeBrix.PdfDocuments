using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Tests;

public class CommonMarkSpecTests
{
    private sealed record SpecExample(int Number, string Section, string Markdown, string Html);

    private static List<SpecExample> LoadExamples()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "commonmark-spec-0.31.2.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var examples = new List<SpecExample>();
        foreach (var entry in document.RootElement.EnumerateArray())
        {
            examples.Add(new SpecExample(
                entry.GetProperty("example").GetInt32(),
                entry.GetProperty("section").GetString(),
                entry.GetProperty("markdown").GetString(),
                entry.GetProperty("html").GetString()));
        }
        return examples;
    }

    [Fact]
    public void all_652_commonmark_spec_examples_render_correctly()
    {
        //Arrange
        var examples = LoadExamples();
        var md = new MarkdownParser(MarkdownPreset.CommonMark);
        var failures = new List<string>();

        //Act
        foreach (var example in examples)
        {
            string actual;
            try
            {
                actual = md.Render(example.Markdown);
            }
            catch (Exception ex)
            {
                failures.Add($"#{example.Number} [{example.Section}] THREW {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            // markdown-it's own CommonMark harness applies this same normalization:
            // upstream (and therefore this faithful port) renders an empty blockquote
            // without an inner newline, a known cosmetic deviation from the spec text.
            var expected = example.Html.Replace("<blockquote>\n</blockquote>", "<blockquote></blockquote>");

            if (actual != expected)
            {
                failures.Add(
                    $"#{example.Number} [{example.Section}]\n--- markdown ---\n{example.Markdown}\n--- expected ---\n{expected}\n--- actual ---\n{actual}");
            }
        }

        //Assert
        if (failures.Count > 0)
        {
            var report = new StringBuilder();
            report.AppendLine($"{failures.Count} of {examples.Count} spec examples failed. First failures:");
            for (var i = 0; i < Math.Min(failures.Count, 10); i++)
            {
                report.AppendLine(failures[i]);
                report.AppendLine("================");
            }
            Assert.Fail(report.ToString());
        }
    }
}
