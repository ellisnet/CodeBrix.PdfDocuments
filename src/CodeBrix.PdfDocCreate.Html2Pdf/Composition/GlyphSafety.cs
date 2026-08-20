using System;
using System.Collections.Generic;
using System.Text;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// Decides, per code point, which registered font face renders a piece of text: the
/// face the style resolved to when that font's own cmap covers the character, else the
/// first covering per-glyph fallback family. Characters nothing covers degrade to a
/// collected warning (removed by default, kept as missing-glyph boxes on opt-in).
/// Coverage is read from the actual font files, so a new font package extends what
/// renders without any change here.
/// </summary>
internal static class GlyphSafety
{
    /// <summary>A stretch of text plus the face it renders with (null = the style's face).</summary>
    internal readonly record struct TextSegment(string Text, string FaceName);

    /// <summary>
    /// Splits text into segments per rendering face and filters uncovered characters.
    /// The legacy character allow-list is retained as a floor: anything it admitted
    /// before coverage-driven filtering existed is still admitted, so no previously
    /// rendering document loses characters.
    /// </summary>
    public static List<TextSegment> Segment(
        string text,
        string primaryFace,
        int weight,
        bool italic,
        bool keepUncovered,
        RenderWarnings warnings)
    {
        var result = new List<TextSegment>();
        if (string.IsNullOrEmpty(text)) { return result; }

        var coverage = Html2PdfFonts.TryGetFaceCoverage(primaryFace);
        var current = new StringBuilder(text.Length);
        string currentFace = null;

        void Flush()
        {
            if (current.Length > 0)
            {
                result.Add(new TextSegment(current.ToString(), currentFace));
                current.Clear();
            }
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var codePoint = (int)c;
            var isPair = false;
            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                codePoint = char.ConvertToUtf32(c, text[i + 1]);
                isPair = true;
            }

            if (c == '﻿' || (c >= '︀' && c <= '️'))
            {
                continue; // byte-order marks and variation selectors vanish silently
            }

            string targetFace;
            if (c is '\t' or '\n' or '\r')
            {
                targetFace = null;
            }
            else if (coverage != null && coverage.Covers(codePoint))
            {
                targetFace = null;
            }
            else
            {
                var fallbackFace = Html2PdfFonts.TryResolveFallbackFace(codePoint, weight, italic);
                if (fallbackFace != null
                    && !fallbackFace.Equals(primaryFace, StringComparison.OrdinalIgnoreCase))
                {
                    targetFace = fallbackFace;
                }
                else if (!isPair && IsAllowed(c))
                {
                    targetFace = null; // legacy floor: previously admitted characters stay
                }
                else if (keepUncovered)
                {
                    targetFace = null;
                    warnings.Add(RenderWarnings.CategoryFont,
                        $"Characters not covered by any registered font were kept and will render as missing-glyph boxes (first seen: U+{codePoint:X4}).",
                        "font.uncovered.kept", codePoint);
                }
                else
                {
                    warnings.Add(RenderWarnings.CategoryFont, isPair
                        ? "Emoji and other characters outside the Basic Multilingual Plane are not covered by the package fonts and were removed."
                        : $"Characters in an unsupported script or symbol range (first seen: U+{codePoint:X4}) are not covered by the package fonts and were removed.",
                        "font.uncovered.removed", codePoint);
                    if (isPair) { i++; }
                    continue;
                }
            }

            if (!string.Equals(targetFace, currentFace, StringComparison.OrdinalIgnoreCase))
            {
                Flush();
                currentFace = targetFace;
            }

            current.Append(c);
            if (isPair)
            {
                current.Append(text[i + 1]);
                i++;
            }
        }

        Flush();
        return result;
    }

    private static bool IsAllowed(char c)
    {
        if (c == '\t' || c == '\n' || c == '\r') { return true; }
        if (c < 0x20) { return false; }
        if (c <= 0x7E) { return true; }
        if (c < 0xA0) { return false; }

        var code = (int)c;
        return code switch
        {
            <= 0x024F => true,          // Latin-1, Latin Extended-A/B
            <= 0x02FF => true,          // IPA, spacing modifier letters
            <= 0x036F => true,          // combining diacritical marks
            <= 0x03FF => true,          // Greek and Coptic
            <= 0x052F => true,          // Cyrillic + supplement
            <= 0x058F => true,          // Armenian
            >= 0x10A0 and <= 0x10FF => true,   // Georgian
            >= 0x1C90 and <= 0x1CBF => true,   // Georgian Extended
            >= 0x1E00 and <= 0x1EFF => true,   // Latin Extended Additional
            >= 0x1F00 and <= 0x1FFF => true,   // Greek Extended
            >= 0x2000 and <= 0x206F => true,   // general punctuation
            >= 0x2070 and <= 0x209F => true,   // superscripts and subscripts
            >= 0x20A0 and <= 0x20CF => true,   // currency symbols
            >= 0x2100 and <= 0x218F => true,   // letterlike symbols, number forms
            >= 0x2190 and <= 0x21FF => true,   // arrows
            >= 0x2200 and <= 0x22FF => true,   // mathematical operators
            >= 0x2500 and <= 0x25FF => true,   // box drawing, blocks, geometric shapes
            >= 0x2C60 and <= 0x2C7F => true,   // Latin Extended-C
            >= 0xFB00 and <= 0xFB4F => true,   // alphabetic presentation forms (fi ligature etc.)
            _ => false,
        };
    }
}
