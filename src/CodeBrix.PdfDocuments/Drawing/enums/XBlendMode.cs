namespace CodeBrix.PdfDocuments.Drawing;

/// <summary>
/// The PDF blend modes (PDF 1.4, section 11.3.5): how a drawn object's colour combines with
/// what is already on the page. Normal is source-over. The names are the PDF names.
/// </summary>
public enum XBlendMode
{
    /// <summary>Source over: the object replaces the backdrop where it is opaque.</summary>
    Normal = 0,
    /// <summary>Multiplies backdrop and source colours.</summary>
    Multiply,
    /// <summary>Multiplies the complements of backdrop and source colours.</summary>
    Screen,
    /// <summary>Multiply or screen, depending on the backdrop.</summary>
    Overlay,
    /// <summary>The darker of backdrop and source.</summary>
    Darken,
    /// <summary>The lighter of backdrop and source.</summary>
    Lighten,
    /// <summary>Brightens the backdrop to reflect the source.</summary>
    ColorDodge,
    /// <summary>Darkens the backdrop to reflect the source.</summary>
    ColorBurn,
    /// <summary>Multiply or screen, depending on the source.</summary>
    HardLight,
    /// <summary>Darkens or lightens, depending on the source.</summary>
    SoftLight,
    /// <summary>The absolute difference of backdrop and source.</summary>
    Difference,
    /// <summary>Like Difference with lower contrast.</summary>
    Exclusion,
    /// <summary>The hue of the source with the saturation and luminosity of the backdrop.</summary>
    Hue,
    /// <summary>The saturation of the source with the hue and luminosity of the backdrop.</summary>
    Saturation,
    /// <summary>The hue and saturation of the source with the luminosity of the backdrop.</summary>
    Color,
    /// <summary>The luminosity of the source with the hue and saturation of the backdrop.</summary>
    Luminosity,
}
