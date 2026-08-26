using System;
using System.Collections.Generic;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// The faces registered on one SVG document's font registry, keyed the way the SVG
/// engine reports them back: a text run carries the RESOLVED family name (the font
/// file's own name-table family, e.g. "Merriweather 24pt") plus the requested weight and
/// slant, and this map turns that into the Html2Pdf face name whose file the engine
/// measured with - so the face embedded for real text is byte for byte the face the
/// outlines came from.
/// </summary>
internal sealed class SvgFontMap
{
    private readonly List<Entry> _entries = new List<Entry>();

    /// <summary>One registered face.</summary>
    internal readonly record struct Entry(string FamilyName, string LegacyFamilyName, string FaceName, int Weight, bool IsItalic);

    /// <summary>Records a registered face.</summary>
    public void Add(string familyName, string legacyFamilyName, string faceName, int weight, bool isItalic)
    {
        if (string.IsNullOrEmpty(faceName)) { return; }
        foreach (var entry in _entries)
        {
            if (entry.FaceName == faceName) { return; }
        }

        _entries.Add(new Entry(familyName, legacyFamilyName, faceName, weight, isItalic));
    }

    /// <summary>How many faces the map holds.</summary>
    public int Count => _entries.Count;

    /// <summary>
    /// The face name for a resolved family at a weight and slant: among the faces whose
    /// name-table family (typographic or legacy) is the resolved family, the one whose
    /// slant matches and whose weight is nearest - the engine's own selection rule.
    /// Null when the family names no registered face (an uncovered run).
    /// </summary>
    public string TryFindFace(string resolvedFamilyName, int weight, bool italic)
    {
        if (string.IsNullOrWhiteSpace(resolvedFamilyName)) { return null; }

        string best = null;
        var bestScore = int.MaxValue;
        foreach (var entry in _entries)
        {
            if (!string.Equals(entry.FamilyName, resolvedFamilyName, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(entry.LegacyFamilyName, resolvedFamilyName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var score = Math.Abs(entry.Weight - weight) + (entry.IsItalic == italic ? 0 : 1000);
            if (score < bestScore)
            {
                bestScore = score;
                best = entry.FaceName;
            }
        }

        return best;
    }
}
