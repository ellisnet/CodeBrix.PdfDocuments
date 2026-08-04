using System.Linq;
using CodeBrix.MarkupParse.Html.Parser;
using CodeBrix.PdfDocCreate.Html2Pdf.Css;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

public class StyleResolverTests
{
    private static (StyleResolver Resolver, CodeBrix.MarkupParse.Html.Dom.IHtmlDocument Dom, RenderWarnings Warnings)
        Setup(string html, string css)
    {
        var warnings = new RenderWarnings();
        var resolver = new StyleResolver(warnings);
        if (css != null) { resolver.AddStylesheet(css, isDefaultSheet: false); }
        var dom = new HtmlParser().ParseDocument(html);
        return (resolver, dom, warnings);
    }

    [Fact]
    public void class_selector_beats_type_selector()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<p class='special'>x</p>",
            "p { color: #ff0000; } .special { color: #0000ff; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.TextColor.B.Should().Be(255);
        style.TextColor.R.Should().Be(0);
    }

    [Fact]
    public void important_declaration_beats_higher_specificity()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<p id='p1' class='special'>x</p>",
            "p { color: #ff0000 !important; } #p1 { color: #0000ff; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.TextColor.R.Should().Be(255);
        style.TextColor.B.Should().Be(0);
    }

    [Fact]
    public void inline_style_beats_stylesheet_rules()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<p id='p1' style='color: #00ff00'>x</p>",
            "#p1 { color: #0000ff; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.TextColor.G.Should().Be(255);
        style.TextColor.B.Should().Be(0);
    }

    [Fact]
    public void later_rule_wins_at_equal_specificity()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<p>x</p>",
            "p { color: #ff0000; } p { color: #0000ff; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.TextColor.B.Should().Be(255);
    }

    [Fact]
    public void color_inherits_but_margin_does_not()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<div><p>x</p></div>",
            "div { color: #ff0000; margin-left: 20pt; }");
        var root = ComputedStyle.CreateRoot();

        //Act
        var divStyle = resolver.Compute(dom.QuerySelector("div"), root);
        var pStyle = resolver.Compute(dom.QuerySelector("p"), divStyle);

        //Assert
        pStyle.TextColor.R.Should().Be(255);
        divStyle.MarginLeft.Should().Be(20.0);
        pStyle.MarginLeft.Should().Be(0.0);
    }

    [Fact]
    public void em_font_size_resolves_against_parent_and_other_lengths_against_own()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<div><p>x</p></div>",
            "div { font-size: 20pt; } p { font-size: 0.5em; margin-top: 2em; }");
        var root = ComputedStyle.CreateRoot();

        //Act
        var divStyle = resolver.Compute(dom.QuerySelector("div"), root);
        var pStyle = resolver.Compute(dom.QuerySelector("p"), divStyle);

        //Assert
        pStyle.FontSizePoints.Should().Be(10.0);   // 0.5 x parent 20pt
        pStyle.MarginTop.Should().Be(20.0);        // 2 x own 10pt
    }

    [Fact]
    public void px_lengths_convert_to_points()
    {
        //Arrange
        var (resolver, dom, _) = Setup("<p>x</p>", "p { font-size: 16px; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.FontSizePoints.Should().Be(12.0);
    }

    [Fact]
    public void margin_shorthand_expands_to_four_sides()
    {
        //Arrange
        var (resolver, dom, _) = Setup("<p>x</p>", "p { margin: 10pt 20pt; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.MarginTop.Should().Be(10.0);
        style.MarginBottom.Should().Be(10.0);
        style.MarginLeft.Should().Be(20.0);
        style.MarginRight.Should().Be(20.0);
    }

    [Fact]
    public void border_shorthand_sets_width_style_and_color()
    {
        //Arrange
        var (resolver, dom, _) = Setup("<p>x</p>", "p { border: 2pt dashed #ff0000; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.BorderTop.IsVisible.Should().BeTrue();
        style.BorderTop.WidthPoints.Should().Be(2.0);
        style.BorderTop.LineStyle.Should().Be("dashed");
        style.BorderTop.LineColor.R.Should().Be(255);
        style.BorderLeft.WidthPoints.Should().Be(2.0);
    }

    [Fact]
    public void font_weight_keywords_and_numbers_resolve()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<div><p>x</p><span>y</span></div>",
            "p { font-weight: bold; } span { font-weight: 300; }");
        var root = ComputedStyle.CreateRoot();

        //Act
        var pStyle = resolver.Compute(dom.QuerySelector("p"), root);
        var spanStyle = resolver.Compute(dom.QuerySelector("span"), root);

        //Assert
        pStyle.FontWeight.Should().Be(700);
        spanStyle.FontWeight.Should().Be(300);
    }

    [Fact]
    public void text_decoration_none_clears_inherited_underline()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<a><span>x</span></a>",
            "a { text-decoration: underline; } span { text-decoration: none; }");
        var root = ComputedStyle.CreateRoot();

        //Act
        var aStyle = resolver.Compute(dom.QuerySelector("a"), root);
        var spanStyle = resolver.Compute(dom.QuerySelector("span"), aStyle);

        //Assert
        aStyle.Underline.Should().BeTrue();
        spanStyle.Underline.Should().BeFalse();
    }

    [Fact]
    public void page_rule_provides_size_and_margins()
    {
        //Arrange
        var (resolver, _, _) = Setup("<p>x</p>", "@page { size: a4 landscape; margin: 1in 2cm; }");

        //Act
        var page = resolver.Page;

        //Assert
        page.PageWidthPoints.Should().Be(595.0);
        page.PageHeightPoints.Should().Be(842.0);
        page.Landscape.Should().Be(true);
        page.MarginTopPoints.Should().Be(72.0);
        page.MarginLeftPoints.Should().BeApproximately(56.7, 0.1);
    }

    [Fact]
    public void unsupported_property_produces_a_warning_and_is_ignored()
    {
        //Arrange
        var (resolver, dom, warnings) = Setup("<p>x</p>", "p { float: left; color: #ff0000; }");

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.TextColor.R.Should().Be(255);
        warnings.Messages.Any(m => m.Contains("float")).Should().BeTrue();
    }

    [Fact]
    public void display_none_is_computed()
    {
        //Arrange
        var (resolver, dom, _) = Setup("<p style='display:none'>x</p>", null);

        //Act
        var style = resolver.Compute(dom.QuerySelector("p"), ComputedStyle.CreateRoot());

        //Assert
        style.DisplayNone.Should().BeTrue();
    }

    [Fact]
    public void descendant_selector_matches_through_ancestors()
    {
        //Arrange
        var (resolver, dom, _) = Setup(
            "<div class='note'><p><span>x</span></p></div>",
            ".note span { color: #00ff00; }");
        var root = ComputedStyle.CreateRoot();

        //Act
        var style = resolver.Compute(dom.QuerySelector("span"), root);

        //Assert
        style.TextColor.G.Should().Be(255);
    }
}
