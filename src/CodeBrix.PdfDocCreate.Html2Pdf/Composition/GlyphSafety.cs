using System;
using System.Text;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// Filters text down to the character ranges the package fonts actually cover, so
/// unsupported scripts and astral-plane characters (emoji, rare symbols) degrade to a
/// collected warning instead of rendering as tofu boxes.
/// </summary>
internal static class GlyphSafety
{
    /// <summary>
    /// Removes characters outside the supported coverage. Returns the filtered text;
    /// removed content is reported once per category through the warnings collector.
    /// </summary>
    public static string Filter(string text, RenderWarnings warnings)
    {
        if (string.IsNullOrEmpty(text)) { return text ?? ""; }

        StringBuilder builder = null;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (char.IsHighSurrogate(c) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]))
            {
                builder ??= new StringBuilder(text, 0, i, text.Length);
                warnings.Add(RenderWarnings.CategoryFont,
                    "Emoji and other characters outside the Basic Multilingual Plane are not covered by the package fonts and were removed.");
                i++; // skip the low surrogate too
                continue;
            }

            if (IsAllowed(c))
            {
                builder?.Append(c);
                continue;
            }

            builder ??= new StringBuilder(text, 0, i, text.Length);

            if (c is '﻿' or '​' or '‌' or '‍' || (c >= '︀' && c <= '️'))
            {
                continue; // zero-width characters and variation selectors vanish silently
            }

            warnings.Add(RenderWarnings.CategoryFont,
                $"Characters in an unsupported script or symbol range (first seen: U+{(int)c:X4}) are not covered by the package fonts and were removed.");
        }

        return builder?.ToString() ?? text;
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
