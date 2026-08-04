namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// One physical static font file belonging to a package font family.
/// </summary>
/// <param name="FaceName">Unique face name used for resolver registration - the .ttf file stem, e.g. "Roboto-SemiBold".</param>
/// <param name="Weight">CSS-style numeric weight declared by the manifest (100-900).</param>
/// <param name="IsItalic">True when the face is the italic variant.</param>
/// <param name="Stretch">Manifest stretch value ("Normal", "Condensed", "SemiCondensed").</param>
/// <param name="FilePath">Absolute path of the .ttf file.</param>
internal sealed record PackageFontFace(
    string FaceName,
    int Weight,
    bool IsItalic,
    string Stretch,
    string FilePath);
