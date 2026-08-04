using System;
using System.Globalization;
using CodeBrix.PdfDocCreate.DocumentObjectModel;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Css;

/// <summary>
/// Parses the CSS color forms the dialect accepts: named colors, #rgb / #rrggbb hex,
/// and rgb() / rgba() functions. "transparent" parses successfully to
/// <see cref="Color.Empty"/>, which callers treat as "no color".
/// </summary>
internal static class CssColorParser
{
    public static bool TryParse(string text, out Color color)
    {
        color = Color.Empty;
        if (string.IsNullOrWhiteSpace(text)) { return false; }

        var token = text.Trim();
        if (token.Equals("transparent", StringComparison.OrdinalIgnoreCase)) { return true; }

        if (token.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return TryParseRgbFunction(token, ref color);
        }

        try
        {
            // Color.Parse accepts every name from the Colors class plus CSS hex forms
            // (#rgb and #rrggbb, always opaque).
            color = Color.Parse(token);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool TryParseRgbFunction(string token, ref Color color)
    {
        var open = token.IndexOf('(');
        var close = token.LastIndexOf(')');
        if (open < 0 || close <= open) { return false; }

        var body = token.Substring(open + 1, close - open - 1);
        var parts = body.Split(new[] { ',', '/', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) { return false; }

        if (!TryParseChannel(parts[0], out var r)
            || !TryParseChannel(parts[1], out var g)
            || !TryParseChannel(parts[2], out var b))
        {
            return false;
        }

        var alpha = (byte)255;
        if (parts.Length >= 4)
        {
            if (!double.TryParse(parts[3].TrimEnd('%'), NumberStyles.Float, CultureInfo.InvariantCulture, out var a))
            {
                return false;
            }
            if (parts[3].EndsWith("%", StringComparison.Ordinal)) { a /= 100.0; }
            alpha = (byte)Math.Clamp(Math.Round(a * 255.0), 0, 255);
        }

        color = new Color(alpha, r, g, b);
        return true;
    }

    private static bool TryParseChannel(string part, out byte channel)
    {
        channel = 0;
        var token = part.Trim();
        var isPercent = token.EndsWith("%", StringComparison.Ordinal);
        if (isPercent) { token = token.Substring(0, token.Length - 1); }

        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        if (isPercent) { value = value / 100.0 * 255.0; }
        channel = (byte)Math.Clamp(Math.Round(value), 0, 255);
        return true;
    }
}
