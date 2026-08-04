using System;
using System.Collections.Generic;
using System.Linq;
using CodeBrix.MarkupParse.Css.Parser;
using CodeBrix.MarkupParse.Dom;
using CodeBrix.StyleSheetParse;
using MatchSelector = CodeBrix.MarkupParse.Css.Dom.ISelector;
using Specificity = CodeBrix.MarkupParse.Css.Priority;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Css;

/// <summary>
/// The style engine: parses stylesheets with CodeBrix.StyleSheetParse, matches selectors
/// against DOM elements with CodeBrix.MarkupParse's selector engine, and computes the
/// effective style of each element by applying the cascade (default sheet &lt; author
/// rules by specificity and source order &lt; inline style, with !important author
/// declarations above all non-important ones) plus inheritance and unit resolution.
/// </summary>
internal sealed class StyleResolver
{
    // Cascade tiers, applied in ascending order. The built-in default stylesheet always
    // yields to author CSS, and its declarations never gain !important standing.
    private const int TierDefaultSheet = 0;
    private const int TierAuthor = 1;
    private const int TierInline = 2;
    private const int TierAuthorImportant = 3;
    private const int TierInlineImportant = 4;

    private readonly StylesheetParser _stylesheetParser = new StylesheetParser(
        includeUnknownRules: false,
        includeUnknownDeclarations: true,
        tolerateInvalidSelectors: true,
        tolerateInvalidValues: true,
        tolerateInvalidConstraints: true,
        preserveComments: false,
        preserveDuplicateProperties: true);

    private readonly CssSelectorParser _selectorParser = new CssSelectorParser();
    private readonly List<RuleEntry> _rules = new List<RuleEntry>();
    private readonly RenderWarnings _warnings;
    private int _sourceOrder;
    private double _rootFontSizePoints = 12.0;

    private sealed class RuleEntry
    {
        public MatchSelector Matcher;
        public Specificity SelectorSpecificity;
        public StyleDeclaration Declarations;
        public int SourceOrder;
        public bool IsDefaultSheet;
    }

    public StyleResolver(RenderWarnings warnings)
    {
        _warnings = warnings;
    }

    /// <summary>Page geometry from @page rules; author sheets override the default sheet.</summary>
    public PageStyle Page { get; } = new PageStyle();

    /// <summary>
    /// Parses a stylesheet and adds its rules to the cascade. Sheets must be added in
    /// document order; the default sheet first.
    /// </summary>
    public void AddStylesheet(string cssText, bool isDefaultSheet)
    {
        if (string.IsNullOrWhiteSpace(cssText)) { return; }

        Stylesheet sheet;
        try
        {
            sheet = _stylesheetParser.Parse(cssText);
        }
        catch (Exception ex)
        {
            _warnings.Add(RenderWarnings.CategoryCss, $"A stylesheet could not be parsed and was ignored ({ex.GetType().Name}).");
            return;
        }

        foreach (var rule in sheet.StyleRules)
        {
            var order = _sourceOrder++;
            foreach (var componentText in EnumerateSelectorComponents(rule))
            {
                MatchSelector matcher;
                try
                {
                    matcher = _selectorParser.ParseSelector(componentText);
                }
                catch (Exception)
                {
                    matcher = null;
                }

                if (matcher == null)
                {
                    _warnings.Add(RenderWarnings.CategoryCss, $"Selector '{componentText}' is not supported and was ignored.");
                    continue;
                }

                _rules.Add(new RuleEntry
                {
                    Matcher = matcher,
                    SelectorSpecificity = matcher.Specificity,
                    Declarations = rule.Style,
                    SourceOrder = order,
                    IsDefaultSheet = isDefaultSheet,
                });
            }
        }

        foreach (var pageRule in sheet.PageRules)
        {
            ApplyPageRule(pageRule);
        }
    }

    /// <summary>
    /// Computes the effective style for an element given its parent's computed style.
    /// </summary>
    public ComputedStyle Compute(IElement element, ComputedStyle parent)
    {
        var declarations = new List<(int Tier, Specificity Spec, int Order, Property Prop)>();

        foreach (var rule in _rules)
        {
            bool matches;
            try
            {
                matches = rule.Matcher.Match(element, null);
            }
            catch (Exception)
            {
                continue;
            }

            if (!matches) { continue; }

            foreach (var property in rule.Declarations.Declarations)
            {
                var tier = rule.IsDefaultSheet
                    ? TierDefaultSheet
                    : (property.IsImportant ? TierAuthorImportant : TierAuthor);
                declarations.Add((tier, rule.SelectorSpecificity, rule.SourceOrder, property));
            }
        }

        var inlineStyle = element.GetAttribute("style");
        if (!string.IsNullOrWhiteSpace(inlineStyle))
        {
            foreach (var property in ParseInlineDeclarations(inlineStyle))
            {
                var tier = property.IsImportant ? TierInlineImportant : TierInline;
                declarations.Add((tier, Specificity.Inline, int.MaxValue, property));
            }
        }

        var bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, _, _, property) in declarations
                     .OrderBy(d => d.Tier)
                     .ThenBy(d => d.Spec)
                     .ThenBy(d => d.Order))
        {
            var value = ReadValue(property);
            if (value.Length == 0) { continue; }
            ApplyDeclaration(bag, property.Name, value);
        }

        var style = BuildComputedStyle(bag, parent);

        if (element.ParentElement == null
            || element.LocalName.Equals("html", StringComparison.OrdinalIgnoreCase))
        {
            _rootFontSizePoints = style.FontSizePoints;
        }

        return style;
    }

    // ---- cascade helpers ------------------------------------------------------

    private IEnumerable<string> EnumerateSelectorComponents(IStyleRule rule)
    {
        if (rule.Selector is ListSelector list)
        {
            foreach (var component in list)
            {
                var text = component?.Text?.Trim() ?? "";
                if (text.Length > 0) { yield return text; }
            }
            yield break;
        }

        var single = rule.SelectorText?.Trim() ?? "";
        if (single.Length > 0) { yield return single; }
    }

    private IEnumerable<Property> ParseInlineDeclarations(string inlineStyle)
    {
        try
        {
            var sheet = _stylesheetParser.Parse("*{" + inlineStyle + "}");
            var rule = sheet.StyleRules.FirstOrDefault();
            return rule?.Style?.Declarations ?? Enumerable.Empty<Property>();
        }
        catch (Exception)
        {
            _warnings.Add(RenderWarnings.CategoryCss, "An inline style attribute could not be parsed and was ignored.");
            return Enumerable.Empty<Property>();
        }
    }

    private static string ReadValue(Property property)
    {
        // Original preserves the author's raw text; Value falls back to the normalized
        // form when no original is recorded. "initial" is the parser's placeholder for
        // an absent value.
        var original = property.Original?.Trim() ?? "";
        if (original.Length > 0 && !original.Equals("initial", StringComparison.OrdinalIgnoreCase))
        {
            return original;
        }

        var value = property.Value?.Trim() ?? "";
        return value.Equals("initial", StringComparison.OrdinalIgnoreCase) ? "" : value;
    }

    /// <summary>
    /// Writes one declaration into the property bag, expanding the supported shorthands
    /// (margin, padding, border and friends) into longhand entries so that later
    /// longhand declarations override correctly.
    /// </summary>
    private void ApplyDeclaration(Dictionary<string, string> bag, string name, string value)
    {
        var property = (name ?? "").Trim().ToLowerInvariant();

        switch (property)
        {
            case "margin":
                ExpandBoxShorthand(bag, "margin", value);
                return;
            case "padding":
                ExpandBoxShorthand(bag, "padding", value);
                return;
            case "border":
                foreach (var edge in Edges) { bag["border-" + edge] = value; }
                return;
            case "border-width":
                ExpandBoxShorthand(bag, "border", value, "-width");
                return;
            case "border-style":
                ExpandBoxShorthand(bag, "border", value, "-style");
                return;
            case "border-color":
                ExpandBoxShorthand(bag, "border", value, "-color");
                return;
            case "background":
                // Color-only background support: the last token that parses as a color wins.
                var colorToken = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .LastOrDefault(t => CssColorParser.TryParse(t, out _));
                if (colorToken != null) { bag["background-color"] = colorToken; }
                else { _warnings.Add(RenderWarnings.CategoryCss, "Only the color component of the 'background' shorthand is supported."); }
                return;
            case "text-decoration":
            case "text-decoration-line":
                bag["text-decoration"] = value;
                return;
            case "break-before":
                bag["page-break-before"] = value;
                return;
            case "break-after":
                bag["page-break-after"] = value;
                return;
        }

        if (SupportedProperties.Contains(property))
        {
            bag[property] = value;
        }
        else
        {
            _warnings.Add(RenderWarnings.CategoryCss, $"CSS property '{property}' is not supported and was ignored.");
        }
    }

    private static readonly string[] Edges = { "top", "right", "bottom", "left" };

    private static readonly HashSet<string> SupportedProperties = new HashSet<string>(StringComparer.Ordinal)
    {
        "font-family", "font-size", "font-weight", "font-style",
        "color", "background-color", "line-height",
        "text-align", "text-indent", "text-transform", "white-space",
        "margin-top", "margin-right", "margin-bottom", "margin-left",
        "padding-top", "padding-right", "padding-bottom", "padding-left",
        "border-top", "border-right", "border-bottom", "border-left",
        "border-top-width", "border-right-width", "border-bottom-width", "border-left-width",
        "border-top-style", "border-right-style", "border-bottom-style", "border-left-style",
        "border-top-color", "border-right-color", "border-bottom-color", "border-left-color",
        "width", "height",
        "page-break-before", "page-break-after",
        "list-style-type", "display", "vertical-align", "text-decoration",
    };

    private void ExpandBoxShorthand(Dictionary<string, string> bag, string prefix, string value, string suffix = "")
    {
        var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) { return; }

        string top, right, bottom, left;
        switch (parts.Length)
        {
            case 1: top = right = bottom = left = parts[0]; break;
            case 2: top = bottom = parts[0]; right = left = parts[1]; break;
            case 3: top = parts[0]; right = left = parts[1]; bottom = parts[2]; break;
            default: top = parts[0]; right = parts[1]; bottom = parts[2]; left = parts[3]; break;
        }

        bag[prefix + "-top" + suffix] = top;
        bag[prefix + "-right" + suffix] = right;
        bag[prefix + "-bottom" + suffix] = bottom;
        bag[prefix + "-left" + suffix] = left;
    }

    // ---- computed-style construction ------------------------------------------

    private ComputedStyle BuildComputedStyle(Dictionary<string, string> bag, ComputedStyle parent)
    {
        var style = parent.CreateChildBase();

        // font-size resolves first: its relative units use the PARENT font size, and
        // every other length on the element uses the element's OWN font size.
        if (bag.TryGetValue("font-size", out var fontSize))
        {
            style.FontSizePoints = ResolveFontSize(fontSize, parent.FontSizePoints);
        }

        var em = style.FontSizePoints;

        foreach (var pair in bag)
        {
            ApplyToStyle(style, parent, pair.Key.ToLowerInvariant(), pair.Value.Trim(), em);
        }

        return style;
    }

    private double ResolveFontSize(string value, double parentSize)
    {
        switch (value.ToLowerInvariant())
        {
            case "xx-small": return parentSize * 0.5787;
            case "x-small": return parentSize * 0.6944;
            case "small": return parentSize * 0.8333;
            case "medium": return parentSize;
            case "large": return parentSize * 1.2;
            case "x-large": return parentSize * 1.44;
            case "xx-large": return parentSize * 1.728;
            case "smaller": return parentSize * 0.8333;
            case "larger": return parentSize * 1.2;
        }

        if (CssLength.TryParse(value, out var length) && length.Unit != CssUnit.Number)
        {
            var resolved = length.ResolvePoints(parentSize, _rootFontSizePoints);
            if (resolved > 0) { return resolved; }
        }

        _warnings.Add(RenderWarnings.CategoryCss, $"font-size value '{value}' is not valid and was ignored.");
        return parentSize;
    }

    private void ApplyToStyle(ComputedStyle style, ComputedStyle parent, string property, string value, double em)
    {
        switch (property)
        {
            case "font-size":
                return; // already applied

            case "font-family":
                style.FontFamilies = value
                    .Split(',')
                    .Select(f => f.Trim().Trim('"', '\''))
                    .Where(f => f.Length > 0)
                    .ToArray();
                if (style.FontFamilies.Length == 0) { style.FontFamilies = parent.FontFamilies; }
                return;

            case "font-weight":
                style.FontWeight = ResolveFontWeight(value, parent.FontWeight);
                return;

            case "font-style":
                style.Italic = value.StartsWith("italic", StringComparison.OrdinalIgnoreCase)
                               || value.StartsWith("oblique", StringComparison.OrdinalIgnoreCase);
                return;

            case "color":
                if (CssColorParser.TryParse(value, out var textColor) && !textColor.IsEmpty)
                {
                    style.TextColor = textColor;
                }
                else { WarnValue(property, value); }
                return;

            case "background-color":
                if (CssColorParser.TryParse(value, out var background))
                {
                    style.BackgroundColor = background;
                }
                else { WarnValue(property, value); }
                return;

            case "line-height":
                ApplyLineHeight(style, value, em);
                return;

            case "text-align":
                switch (value.ToLowerInvariant())
                {
                    case "left": case "start": style.TextAlign = "left"; break;
                    case "right": case "end": style.TextAlign = "right"; break;
                    case "center": style.TextAlign = "center"; break;
                    case "justify": style.TextAlign = "justify"; break;
                    default: WarnValue(property, value); break;
                }
                return;

            case "text-decoration":
                ApplyTextDecoration(style, value);
                return;

            case "text-indent":
                if (CssLength.TryParse(value, out var indent))
                {
                    style.TextIndentPoints = indent.ResolvePoints(em, _rootFontSizePoints);
                }
                else { WarnValue(property, value); }
                return;

            case "text-transform":
                style.TextTransform = value.ToLowerInvariant();
                return;

            case "white-space":
                style.WhiteSpace = value.ToLowerInvariant();
                return;

            case "list-style-type":
                style.ListStyleType = value.ToLowerInvariant();
                return;

            case "display":
                style.DisplayNone = value.Equals("none", StringComparison.OrdinalIgnoreCase);
                return;

            case "vertical-align":
                style.VerticalAlign = value.ToLowerInvariant();
                return;

            case "width":
                if (CssLength.TryParse(value, out var width))
                {
                    if (width.Unit == CssUnit.Percent) { style.WidthPercent = width.Value; }
                    else { style.WidthPoints = width.ResolvePoints(em, _rootFontSizePoints); }
                }
                else if (!value.Equals("auto", StringComparison.OrdinalIgnoreCase)) { WarnValue(property, value); }
                return;

            case "height":
                if (CssLength.TryParse(value, out var height) && height.Unit != CssUnit.Percent)
                {
                    style.HeightPoints = height.ResolvePoints(em, _rootFontSizePoints);
                }
                else if (!value.Equals("auto", StringComparison.OrdinalIgnoreCase)) { WarnValue(property, value); }
                return;

            case "page-break-before":
                style.PageBreakBefore = IsPageBreak(value);
                return;

            case "page-break-after":
                style.PageBreakAfter = IsPageBreak(value);
                return;
        }

        if (property.StartsWith("margin-", StringComparison.Ordinal))
        {
            ApplyBoxLength(style, property, value, em, isMargin: true);
            return;
        }

        if (property.StartsWith("padding-", StringComparison.Ordinal))
        {
            ApplyBoxLength(style, property, value, em, isMargin: false);
            return;
        }

        if (property.StartsWith("border-", StringComparison.Ordinal))
        {
            ApplyBorder(style, property, value, em);
        }
    }

    private static bool IsPageBreak(string value) =>
        value.Equals("always", StringComparison.OrdinalIgnoreCase)
        || value.Equals("page", StringComparison.OrdinalIgnoreCase)
        || value.Equals("left", StringComparison.OrdinalIgnoreCase)
        || value.Equals("right", StringComparison.OrdinalIgnoreCase);

    private int ResolveFontWeight(string value, int parentWeight)
    {
        switch (value.ToLowerInvariant())
        {
            case "normal": return 400;
            case "bold": return 700;
            case "bolder": return Math.Min(900, parentWeight + 300);
            case "lighter": return Math.Max(100, parentWeight - 300);
        }

        if (int.TryParse(value, out var numeric) && numeric >= 1 && numeric <= 1000)
        {
            return Math.Clamp(numeric, 100, 900);
        }

        WarnValue("font-weight", value);
        return parentWeight;
    }

    private void ApplyLineHeight(ComputedStyle style, string value, double em)
    {
        if (value.Equals("normal", StringComparison.OrdinalIgnoreCase))
        {
            style.LineHeightMultiplier = 1.4;
            style.LineHeightPoints = null;
            return;
        }

        if (CssLength.TryParse(value, out var length))
        {
            if (length.Unit == CssUnit.Number)
            {
                style.LineHeightMultiplier = length.Value;
                style.LineHeightPoints = null;
            }
            else
            {
                style.LineHeightPoints = length.ResolvePoints(em, _rootFontSizePoints);
            }
            return;
        }

        WarnValue("line-height", value);
    }

    private static void ApplyTextDecoration(ComputedStyle style, string value)
    {
        var tokens = value.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Contains("none"))
        {
            style.Underline = false;
            style.Strikethrough = false;
            return;
        }

        if (tokens.Contains("underline")) { style.Underline = true; }
        if (tokens.Contains("line-through")) { style.Strikethrough = true; }
    }

    private void ApplyBoxLength(ComputedStyle style, string property, string value, double em, bool isMargin)
    {
        if (value.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return; // auto centering is outside the dialect; keep the initial 0
        }

        if (!CssLength.TryParse(value, out var length))
        {
            WarnValue(property, value);
            return;
        }

        var points = length.ResolvePoints(em, _rootFontSizePoints);
        var edge = property.Substring(property.IndexOf('-') + 1);

        if (isMargin)
        {
            switch (edge)
            {
                case "top": style.MarginTop = points; break;
                case "right": style.MarginRight = points; break;
                case "bottom": style.MarginBottom = points; break;
                case "left": style.MarginLeft = points; break;
            }
        }
        else
        {
            switch (edge)
            {
                case "top": style.PaddingTop = points; break;
                case "right": style.PaddingRight = points; break;
                case "bottom": style.PaddingBottom = points; break;
                case "left": style.PaddingLeft = points; break;
            }
        }
    }

    private void ApplyBorder(ComputedStyle style, string property, string value, double em)
    {
        // Forms: border-top (shorthand), border-top-width, border-top-style, border-top-color.
        var parts = property.Split('-');
        if (parts.Length < 2) { return; }

        var edge = GetBorderEdge(style, parts[1]);
        if (edge == null) { return; }

        if (parts.Length == 2)
        {
            ApplyBorderShorthand(edge, value, em);
            return;
        }

        switch (parts[2])
        {
            case "width":
                if (TryParseBorderWidth(value, em, out var width)) { edge.WidthPoints = width; }
                else { WarnValue(property, value); }
                break;
            case "style":
                edge.LineStyle = value.ToLowerInvariant();
                if (edge.WidthPoints <= 0 && edge.IsVisible == false
                    && !edge.LineStyle.Equals("none", StringComparison.Ordinal)
                    && !edge.LineStyle.Equals("hidden", StringComparison.Ordinal))
                {
                    // CSS gives styled borders a "medium" default width.
                    if (edge.WidthPoints <= 0) { edge.WidthPoints = 2.25; }
                }
                break;
            case "color":
                if (CssColorParser.TryParse(value, out var color)) { edge.LineColor = color; }
                else { WarnValue(property, value); }
                break;
        }
    }

    private void ApplyBorderShorthand(BorderEdge edge, string value, double em)
    {
        var sawStyle = false;
        foreach (var token in value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (TryParseBorderWidth(token, em, out var width))
            {
                edge.WidthPoints = width;
                continue;
            }

            if (IsBorderStyleKeyword(token))
            {
                edge.LineStyle = token.ToLowerInvariant();
                sawStyle = true;
                continue;
            }

            if (CssColorParser.TryParse(token, out var color))
            {
                edge.LineColor = color;
            }
        }

        if (sawStyle && edge.WidthPoints <= 0
            && !edge.LineStyle.Equals("none", StringComparison.Ordinal)
            && !edge.LineStyle.Equals("hidden", StringComparison.Ordinal))
        {
            edge.WidthPoints = 2.25; // CSS "medium"
        }
    }

    private static bool TryParseBorderWidth(string token, double em, out double points)
    {
        switch (token.ToLowerInvariant())
        {
            case "thin": points = 0.75; return true;
            case "medium": points = 2.25; return true;
            case "thick": points = 3.75; return true;
        }

        if (CssLength.TryParse(token, out var length)
            && length.Unit != CssUnit.Number
            && length.Unit != CssUnit.Percent)
        {
            points = length.ResolvePoints(em, 12.0);
            return true;
        }

        points = 0;
        return false;
    }

    private static bool IsBorderStyleKeyword(string token)
    {
        switch (token.ToLowerInvariant())
        {
            case "none": case "hidden": case "solid": case "dashed": case "dotted":
            case "double": case "groove": case "ridge": case "inset": case "outset":
                return true;
            default:
                return false;
        }
    }

    private static BorderEdge GetBorderEdge(ComputedStyle style, string edgeName)
    {
        switch (edgeName)
        {
            case "top": return style.BorderTop;
            case "right": return style.BorderRight;
            case "bottom": return style.BorderBottom;
            case "left": return style.BorderLeft;
            default: return null;
        }
    }

    private void WarnValue(string property, string value) =>
        _warnings.Add(RenderWarnings.CategoryCss, $"Value '{value}' for CSS property '{property}' is not valid and was ignored.");

    // ---- @page ----------------------------------------------------------------

    private void ApplyPageRule(IPageRule pageRule)
    {
        foreach (var property in pageRule.Style.Declarations)
        {
            var name = property.Name?.Trim().ToLowerInvariant() ?? "";
            var value = ReadValue(property);
            if (value.Length == 0) { continue; }

            switch (name)
            {
                case "size":
                    ApplyPageSize(value);
                    break;
                case "margin":
                    var expanded = new Dictionary<string, string>(StringComparer.Ordinal);
                    ExpandBoxShorthand(expanded, "margin", value);
                    ApplyPageMargin("top", expanded["margin-top"]);
                    ApplyPageMargin("right", expanded["margin-right"]);
                    ApplyPageMargin("bottom", expanded["margin-bottom"]);
                    ApplyPageMargin("left", expanded["margin-left"]);
                    break;
                case "margin-top": ApplyPageMargin("top", value); break;
                case "margin-right": ApplyPageMargin("right", value); break;
                case "margin-bottom": ApplyPageMargin("bottom", value); break;
                case "margin-left": ApplyPageMargin("left", value); break;
                default:
                    _warnings.Add(RenderWarnings.CategoryCss, $"@page property '{name}' is not supported and was ignored.");
                    break;
            }
        }
    }

    private void ApplyPageSize(string value)
    {
        var lengths = new List<CssLength>();

        foreach (var token in value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Equals("landscape", StringComparison.OrdinalIgnoreCase))
            {
                Page.Landscape = true;
                continue;
            }

            if (token.Equals("portrait", StringComparison.OrdinalIgnoreCase))
            {
                Page.Landscape = false;
                continue;
            }

            if (PageStyle.TryGetNamedSize(token, out var width, out var height))
            {
                Page.NamedSize = token.ToLowerInvariant();
                Page.PageWidthPoints = width;
                Page.PageHeightPoints = height;
                continue;
            }

            if (CssLength.TryParse(token, out var length)
                && length.Unit != CssUnit.Number
                && length.Unit != CssUnit.Percent
                && length.Unit != CssUnit.Em
                && length.Unit != CssUnit.Rem)
            {
                lengths.Add(length);
            }
        }

        if (lengths.Count >= 1)
        {
            Page.NamedSize = null;
            Page.PageWidthPoints = lengths[0].ResolvePoints(12.0, 12.0);
            Page.PageHeightPoints = lengths.Count >= 2
                ? lengths[1].ResolvePoints(12.0, 12.0)
                : Page.PageWidthPoints;
        }
    }

    private void ApplyPageMargin(string edge, string value)
    {
        if (!CssLength.TryParse(value, out var length)
            || length.Unit == CssUnit.Percent
            || length.Unit == CssUnit.Number)
        {
            _warnings.Add(RenderWarnings.CategoryCss, $"@page margin value '{value}' is not valid and was ignored.");
            return;
        }

        var points = length.ResolvePoints(12.0, 12.0);
        switch (edge)
        {
            case "top": Page.MarginTopPoints = points; break;
            case "right": Page.MarginRightPoints = points; break;
            case "bottom": Page.MarginBottomPoints = points; break;
            case "left": Page.MarginLeftPoints = points; break;
        }
    }
}
