using System;
using System.Globalization;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Css;

/// <summary>Units the CSS dialect accepts for length values.</summary>
internal enum CssUnit
{
    Point,
    Pixel,
    Em,
    Rem,
    Percent,
    Inch,
    Centimeter,
    Millimeter,
    Pica,

    /// <summary>A bare number - only meaningful for line-height multipliers.</summary>
    Number,
}

/// <summary>
/// A parsed CSS length. CSS pixels convert to points at the standard 96-per-inch ratio
/// (1px = 0.75pt); relative units resolve against a font size or percentage basis
/// supplied by the caller.
/// </summary>
internal readonly struct CssLength
{
    public CssLength(double value, CssUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    public double Value { get; }

    public CssUnit Unit { get; }

    /// <summary>
    /// Parses a single CSS length token such as "12pt", "1.5em", "80%", "24px" or "0".
    /// </summary>
    public static bool TryParse(string text, out CssLength length)
    {
        length = default;
        if (string.IsNullOrWhiteSpace(text)) { return false; }

        var token = text.Trim().ToLowerInvariant();
        var unit = CssUnit.Number;
        var numberPart = token;

        if (token.EndsWith("%", StringComparison.Ordinal))
        {
            unit = CssUnit.Percent;
            numberPart = token.Substring(0, token.Length - 1);
        }
        else if (token.Length > 2)
        {
            var suffix = token.Substring(token.Length - 2);
            switch (suffix)
            {
                case "pt": unit = CssUnit.Point; break;
                case "px": unit = CssUnit.Pixel; break;
                case "em": unit = CssUnit.Em; break;
                case "in": unit = CssUnit.Inch; break;
                case "cm": unit = CssUnit.Centimeter; break;
                case "mm": unit = CssUnit.Millimeter; break;
                case "pc": unit = CssUnit.Pica; break;
            }

            if (unit != CssUnit.Number)
            {
                numberPart = token.Substring(0, token.Length - 2);
            }
            else if (token.EndsWith("rem", StringComparison.Ordinal))
            {
                unit = CssUnit.Rem;
                numberPart = token.Substring(0, token.Length - 3);
            }
        }

        if (!double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        length = new CssLength(value, unit);
        return true;
    }

    /// <summary>
    /// Resolves the length to points. <paramref name="emBasisPoints"/> is the font size
    /// em and percent values are relative to; <paramref name="rootEmPoints"/> backs rem
    /// values; a bare number resolves as a multiple of the em basis.
    /// </summary>
    public double ResolvePoints(double emBasisPoints, double rootEmPoints)
    {
        switch (Unit)
        {
            case CssUnit.Point: return Value;
            case CssUnit.Pixel: return Value * 0.75;
            case CssUnit.Em: return Value * emBasisPoints;
            case CssUnit.Rem: return Value * rootEmPoints;
            case CssUnit.Percent: return Value / 100.0 * emBasisPoints;
            case CssUnit.Inch: return Value * 72.0;
            case CssUnit.Centimeter: return Value * 72.0 / 2.54;
            case CssUnit.Millimeter: return Value * 72.0 / 25.4;
            case CssUnit.Pica: return Value * 12.0;
            case CssUnit.Number: return Value * emBasisPoints;
            default: return Value;
        }
    }
}
