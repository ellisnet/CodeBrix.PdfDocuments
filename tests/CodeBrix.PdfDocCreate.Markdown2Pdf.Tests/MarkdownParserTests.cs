using System.Linq;
using CodeBrix.PdfDocCreate.Markdown2Pdf.MarkdownIt;
using CodeBrix.PdfDocCreate.Markdown2Pdf.Plugins;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Markdown2Pdf.Tests;

public class MarkdownParserTests
{
    [Fact]
    public void gfm_table_renders_like_upstream_markdown_it()
    {
        //Arrange
        var md = new MarkdownParser();

        //Act
        var html = md.Render("| a | b |\n|---|:--:|\n| 1 | 2 |");

        //Assert - expected output generated from upstream markdown-it 14.1.0
        html.Should().Be(
            "<table>\n<thead>\n<tr>\n<th>a</th>\n<th style=\"text-align:center\">b</th>\n</tr>\n</thead>\n" +
            "<tbody>\n<tr>\n<td>1</td>\n<td style=\"text-align:center\">2</td>\n</tr>\n</tbody>\n</table>\n");
    }

    [Fact]
    public void strikethrough_renders_like_upstream_markdown_it()
    {
        //Arrange
        var md = new MarkdownParser();

        //Act
        var html = md.Render("~~gone~~ and **kept**");

        //Assert - expected output generated from upstream markdown-it 14.1.0
        html.Should().Be("<p><s>gone</s> and <strong>kept</strong></p>\n");
    }

    [Fact]
    public void footnote_plugin_renders_reference_and_list()
    {
        //Arrange
        var md = new MarkdownParser();
        md.Use(FootnotePlugin.Apply);

        //Act
        var html = md.Render("Here is a footnote reference[^1].\n\n[^1]: And the note itself.\n");

        //Assert
        html.Should().Contain("<sup class=\"footnote-ref\"><a href=\"#fn1\" id=\"fnref1\">[1]</a></sup>");
        html.Should().Contain("<section class=\"footnotes\">");
        html.Should().Contain("<li id=\"fn1\" class=\"footnote-item\">");
        html.Should().Contain("And the note itself.");
        html.Should().Contain("footnote-backref");
    }

    [Fact]
    public void task_list_plugin_renders_styleable_checkbox_spans()
    {
        //Arrange
        var md = new MarkdownParser();
        md.Use(TaskListPlugin.Apply);

        //Act
        var html = md.Render("- [ ] open item\n- [x] done item\n");

        //Assert
        html.Should().Contain("class=\"contains-task-list\"");
        html.Should().Contain("class=\"task-list-item\"");
        html.Should().Contain("<span class=\"task-list-item-checkbox task-unchecked\">□</span>");
        html.Should().Contain("<span class=\"task-list-item-checkbox task-checked\">■</span>");
        html.Should().Contain("open item");
        html.Should().Contain("done item");
    }

    [Fact]
    public void front_matter_plugin_consumes_leading_yaml_and_reports_it()
    {
        //Arrange
        var md = new MarkdownParser();
        string captured = null;
        FrontMatterPlugin.Apply(md, text => captured = text);

        //Act
        var html = md.Render("---\ntitle: My Document\nauthor: Someone\n---\n\n# Heading\n");

        //Assert
        captured.Should().Contain("title: My Document");
        captured.Should().Contain("author: Someone");
        html.Should().NotContain("title: My Document");
        html.Should().Contain("<h1>Heading</h1>");
    }

    [Fact]
    public void fenced_code_with_language_gets_highlight_spans()
    {
        //Arrange
        var md = new MarkdownParser(MarkdownPreset.Default, options =>
        {
            options.Highlight = (content, lang, _) =>
                Highlighting.CodeHighlighter.Highlight(content, lang);
        });

        //Act
        var html = md.Render("```csharp\nvar x = \"hello\"; // greet\n```\n");

        //Assert
        html.Should().Contain("<span class=\"hl-keyword\">var</span>");
        html.Should().Contain("<span class=\"hl-string\">&quot;hello&quot;</span>");
        html.Should().Contain("<span class=\"hl-comment\">// greet</span>");
        html.Should().Contain("language-csharp");
    }

    [Fact]
    public void unknown_fence_language_escapes_plainly()
    {
        //Arrange
        var md = new MarkdownParser(MarkdownPreset.Default, options =>
        {
            options.Highlight = (content, lang, _) =>
                Highlighting.CodeHighlighter.Highlight(content, lang);
        });

        //Act
        var html = md.Render("```brainfuck\n<+>\n```\n");

        //Assert
        html.Should().Contain("&lt;+&gt;");
        html.Should().NotContain("hl-keyword");
    }

    [Fact]
    public void reference_links_and_images_resolve()
    {
        //Arrange
        var md = new MarkdownParser();

        //Act
        var html = md.Render("![logo][img] and [site][www]\n\n[img]: /logo.png \"Logo\"\n[www]: https://example.com\n");

        //Assert
        html.Should().Contain("<img src=\"/logo.png\" alt=\"logo\" title=\"Logo\">");
        html.Should().Contain("<a href=\"https://example.com\">site</a>");
    }

    [Fact]
    public void nested_lists_with_multi_block_items_parse()
    {
        //Arrange
        var md = new MarkdownParser();

        //Act
        var html = md.Render("1. first\n\n   continued paragraph\n\n   - inner bullet\n2. second\n");

        //Assert
        html.Should().Contain("<ol>");
        html.Should().Contain("<p>continued paragraph</p>");
        html.Should().Contain("<ul>");
        html.Should().Contain("<li>inner bullet</li>");
    }

    [Fact]
    public void javascript_links_are_blocked_by_the_validator()
    {
        //Arrange
        var md = new MarkdownParser();

        //Act
        var html = md.Render("[bad](javascript:alert(1))");

        //Assert
        html.Should().NotContain("href=\"javascript");
    }

    [Fact]
    public void tokens_expose_source_maps_for_blocks()
    {
        //Arrange
        var md = new MarkdownParser();

        //Act
        var tokens = md.Parse("# One\n\ntext\n", new MdEnv());

        //Assert
        var heading = tokens.First(t => t.Type == "heading_open");
        heading.Map[0].Should().Be(0);
        var paragraph = tokens.First(t => t.Type == "paragraph_open");
        paragraph.Map[0].Should().Be(2);
    }
}
