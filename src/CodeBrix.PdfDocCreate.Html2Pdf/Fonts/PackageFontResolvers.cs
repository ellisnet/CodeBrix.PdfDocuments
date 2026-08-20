using System;
using System.Collections.Generic;
using System.IO;
using CodeBrix.PdfDocuments.Fonts;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// Resolves one specific static font face by its face name. Registered with
/// MetaFontResolver under the face name so that both face-name typeface requests and
/// GetFont byte requests route here.
/// </summary>
internal sealed class SingleFaceFontResolver : IFontResolver
{
    private readonly PackageFontFace _face;
    private readonly Lazy<byte[]> _bytes;

    public SingleFaceFontResolver(PackageFontFace face)
    {
        _face = face;
        _bytes = new Lazy<byte[]>(() => File.ReadAllBytes(face.FilePath));
    }

    /// <inheritdoc />
    public string DefaultFontName => _face.FaceName;

    /// <inheritdoc />
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic) =>
        familyName != null && familyName.Trim().Equals(_face.FaceName, StringComparison.OrdinalIgnoreCase)
            ? new FontResolverInfo(_face.FaceName)
            : null;

    /// <inheritdoc />
    public byte[] GetFont(string faceName) => _bytes.Value;
}

/// <summary>
/// Resolves a whole package font family for plain family-name requests, mapping the
/// bold flag to weight 700 and the regular request to weight 400. Registered with
/// MetaFontResolver under the family name, and - via its alias-name constructor -
/// under the family names inside the font files themselves (name IDs 1 and 16), which
/// the PDF layer's font-metrics re-resolution asks for. Without the aliases those
/// requests would silently fall through to operating-system fonts.
/// </summary>
internal sealed class PackageFontFamilyResolver : IFontResolver
{
    private readonly PackageFontFamily _family;
    private readonly Dictionary<string, SingleFaceFontResolver> _faceResolvers;
    private readonly string _matchName;

    public PackageFontFamilyResolver(
        PackageFontFamily family,
        Dictionary<string, SingleFaceFontResolver> faceResolvers,
        string aliasName = null)
    {
        _family = family;
        _faceResolvers = faceResolvers;
        _matchName = aliasName ?? family.FamilyName;
    }

    /// <inheritdoc />
    public string DefaultFontName => _matchName;

    /// <inheritdoc />
    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        if (familyName == null
            || !familyName.Trim().Equals(_matchName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var face = _family.SelectFace(isBold ? 700 : 400, isItalic);
        return face != null ? new FontResolverInfo(face.FaceName) : null;
    }

    /// <inheritdoc />
    public byte[] GetFont(string faceName)
    {
        if (!string.IsNullOrWhiteSpace(faceName)
            && _faceResolvers.TryGetValue(faceName.Trim(), out var resolver))
        {
            return resolver.GetFont(faceName);
        }

        throw new InvalidOperationException(
            $"Font face '{faceName}' is not available in the '{_family.FamilyName}' package font family.");
    }
}
