using System;
using CodeBrix.PdfDocCreate.DocumentObjectModel;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Css;

/// <summary>One resolved border edge of an element.</summary>
internal sealed class BorderEdge
{
    public double WidthPoints { get; set; }

    /// <summary>CSS border-style keyword ("solid", "dashed", "dotted", "double", "none", ...).</summary>
    public string LineStyle { get; set; } = "none";

    public Color LineColor { get; set; } = Color.Empty;

    public bool IsVisible =>
        WidthPoints > 0
        && !LineStyle.Equals("none", StringComparison.OrdinalIgnoreCase)
        && !LineStyle.Equals("hidden", StringComparison.OrdinalIgnoreCase);

    public BorderEdge Clone() => new BorderEdge
    {
        WidthPoints = WidthPoints,
        LineStyle = LineStyle,
        LineColor = LineColor,
    };
}

/// <summary>
/// The computed style of one element: every supported CSS property resolved to absolute
/// values. Inherited properties flow from the parent via <see cref="CreateChildBase"/>;
/// box properties always reset.
/// </summary>
internal sealed class ComputedStyle
{
    // ---- inherited properties -------------------------------------------------

    /// <summary>font-family stack, first entry preferred. Entries are raw family names or CSS generics.</summary>
    public string[] FontFamilies { get; set; } = new[] { "sans-serif" };

    public double FontSizePoints { get; set; } = 12.0;

    public int FontWeight { get; set; } = 400;

    public bool Italic { get; set; }

    public Color TextColor { get; set; } = new Color(0x1a, 0x1a, 0x1a);

    /// <summary>Line height as a multiplier of the font size (the "number" CSS form, also the default).</summary>
    public double LineHeightMultiplier { get; set; } = 1.4;

    /// <summary>Line height as absolute points; overrides the multiplier when set.</summary>
    public double? LineHeightPoints { get; set; }

    /// <summary>"left", "right", "center" or "justify".</summary>
    public string TextAlign { get; set; } = "left";

    public bool Underline { get; set; }

    public bool Strikethrough { get; set; }

    /// <summary>list-style-type keyword; null means "use the list element's default".</summary>
    public string ListStyleType { get; set; }

    /// <summary>"none", "uppercase", "lowercase" or "capitalize".</summary>
    public string TextTransform { get; set; } = "none";

    /// <summary>"normal", "pre", "pre-wrap" or "pre-line".</summary>
    public string WhiteSpace { get; set; } = "normal";

    // ---- non-inherited properties ---------------------------------------------

    /// <summary>Background color; <see cref="Color.Empty"/> means none.</summary>
    public Color BackgroundColor { get; set; } = Color.Empty;

    public double MarginTop { get; set; }
    public double MarginRight { get; set; }
    public double MarginBottom { get; set; }
    public double MarginLeft { get; set; }

    public double PaddingTop { get; set; }
    public double PaddingRight { get; set; }
    public double PaddingBottom { get; set; }
    public double PaddingLeft { get; set; }

    public BorderEdge BorderTop { get; set; } = new BorderEdge();
    public BorderEdge BorderRight { get; set; } = new BorderEdge();
    public BorderEdge BorderBottom { get; set; } = new BorderEdge();
    public BorderEdge BorderLeft { get; set; } = new BorderEdge();

    /// <summary>Explicit width in points, when the width property used an absolute unit.</summary>
    public double? WidthPoints { get; set; }

    /// <summary>Explicit width as a percentage (0-100), when the width property used %.</summary>
    public double? WidthPercent { get; set; }

    /// <summary>Explicit height in points (absolute units only).</summary>
    public double? HeightPoints { get; set; }

    public bool PageBreakBefore { get; set; }

    public bool PageBreakAfter { get; set; }

    public bool DisplayNone { get; set; }

    public double TextIndentPoints { get; set; }

    /// <summary>"top", "middle"/"center", "bottom", "super" or "sub" - element-dependent meaning.</summary>
    public string VerticalAlign { get; set; }

    /// <summary>The resolved line height in points for this style's font size.</summary>
    public double ResolvedLineHeightPoints =>
        LineHeightPoints ?? (FontSizePoints * LineHeightMultiplier);

    /// <summary>The style every document starts from (the CSS initial values of the dialect).</summary>
    public static ComputedStyle CreateRoot() => new ComputedStyle();

    /// <summary>
    /// Creates the starting style for a child element: inherited properties copied,
    /// box/layout properties reset to their initial values.
    /// </summary>
    public ComputedStyle CreateChildBase() => new ComputedStyle
    {
        FontFamilies = FontFamilies,
        FontSizePoints = FontSizePoints,
        FontWeight = FontWeight,
        Italic = Italic,
        TextColor = TextColor,
        LineHeightMultiplier = LineHeightMultiplier,
        LineHeightPoints = LineHeightPoints,
        TextAlign = TextAlign,
        Underline = Underline,
        Strikethrough = Strikethrough,
        ListStyleType = ListStyleType,
        TextTransform = TextTransform,
        WhiteSpace = WhiteSpace,
    };
}
