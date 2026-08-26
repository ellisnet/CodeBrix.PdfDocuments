namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// One font the SVG text asked for: a font-family attribute value (possibly a
/// comma-separated candidate list) at a CSS weight and style. The text pre-pass
/// collects these so the SVG engine's per-document font registry can be filled with
/// exactly the faces the document needs - registering every package font for every
/// picture would cost more than loading the picture.
/// </summary>
internal readonly record struct SvgFontRequest(string FamilyList, int Weight, bool Italic);
