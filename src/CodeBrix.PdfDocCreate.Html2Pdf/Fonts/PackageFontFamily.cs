using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// A font family discovered from a CodeBrix.Platform.Fonts.* package, with CSS-style
/// face selection by numeric weight and italic flag.
/// </summary>
internal sealed class PackageFontFamily
{
    private readonly List<PackageFontFace> _faces;

    public PackageFontFamily(string familyName, IEnumerable<PackageFontFace> faces)
    {
        FamilyName = familyName;

        // The CSS dialect has no font-stretch, so normal-stretch faces are preferred;
        // condensed faces only participate when a family has nothing else.
        var all = faces.ToList();
        var normal = all.Where(f => f.Stretch.Equals("Normal", StringComparison.OrdinalIgnoreCase)).ToList();
        _faces = normal.Count > 0 ? normal : all;
    }

    /// <summary>The family name authors use, e.g. "Roboto".</summary>
    public string FamilyName { get; }

    /// <summary>Every face of the family (normal stretch preferred), for resolver registration.</summary>
    public IReadOnlyList<PackageFontFace> Faces => _faces;

    /// <summary>
    /// Selects the face closest to the requested weight and style, following the CSS
    /// font-matching rules for discrete weights: an exact match wins; 400 and 500
    /// substitute for each other first; below 400 lighter weights are preferred, above
    /// 500 heavier weights are preferred. A missing italic (or upright) variant falls
    /// back to the other style rather than failing.
    /// </summary>
    public PackageFontFace SelectFace(int weight, bool italic)
    {
        var styled = _faces.Where(f => f.IsItalic == italic).ToList();
        if (styled.Count == 0) { styled = _faces; }
        if (styled.Count == 0) { return null; }

        var exact = styled.FirstOrDefault(f => f.Weight == weight);
        if (exact != null) { return exact; }

        if (weight == 400)
        {
            var w500 = styled.FirstOrDefault(f => f.Weight == 500);
            if (w500 != null) { return w500; }
        }
        else if (weight == 500)
        {
            var w400 = styled.FirstOrDefault(f => f.Weight == 400);
            if (w400 != null) { return w400; }
        }

        if (weight <= 500)
        {
            var below = styled.Where(f => f.Weight < weight).OrderByDescending(f => f.Weight).FirstOrDefault();
            if (below != null) { return below; }
            return styled.Where(f => f.Weight > weight).OrderBy(f => f.Weight).First();
        }

        var above = styled.Where(f => f.Weight > weight).OrderBy(f => f.Weight).FirstOrDefault();
        if (above != null) { return above; }
        return styled.Where(f => f.Weight < weight).OrderByDescending(f => f.Weight).First();
    }
}
