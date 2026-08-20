using System;
using System.Collections.Generic;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// The one place SVG font-family values resolve to registered faces, shared by the
/// typeface provider (which draws the text) and the coverage scanner (which warns
/// about missing glyphs) so the two can never disagree. A font-family value is a
/// comma-separated candidate list ("Linux Libertine O,serif"); candidates are tried
/// in order and the first one a registered family (or generic) satisfies wins.
/// </summary>
internal static class SvgFontResolution
{
    /// <summary>
    /// Resolves a font-family attribute value (possibly a comma-separated list) to a
    /// registered face name, or null when no candidate resolves.
    /// </summary>
    public static string TryResolveFaceName(string fontFamilyList, int weight, bool italic)
    {
        if (string.IsNullOrWhiteSpace(fontFamilyList)) { return null; }

        foreach (var candidate in SplitList(fontFamilyList))
        {
            var faceName = Html2PdfFonts.TryResolveFaceName(candidate, weight, italic);
            if (faceName != null) { return faceName; }
        }

        return null;
    }

    /// <summary>
    /// Resolves like <see cref="TryResolveFaceName"/> but never returns null: when no
    /// candidate resolves, the default sans face is returned - the same final fallback
    /// the rendering side exhibits.
    /// </summary>
    public static string ResolveFaceNameOrDefault(string fontFamilyList, int weight, bool italic)
        => TryResolveFaceName(fontFamilyList, weight, italic)
           ?? Html2PdfFonts.TryResolveFaceName("sans-serif", weight, italic);

    private static IEnumerable<string> SplitList(string fontFamilyList)
    {
        foreach (var part in fontFamilyList.Split(','))
        {
            var candidate = part.Trim().Trim('"', '\'').Trim();
            if (candidate.Length > 0) { yield return candidate; }
        }
    }
}
