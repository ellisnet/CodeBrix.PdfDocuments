using System;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Css;

/// <summary>
/// Page geometry gathered from @page rules: page size (named or explicit), orientation,
/// and page margins. Null members mean "not specified" and fall back to the renderer's
/// option defaults.
/// </summary>
internal sealed class PageStyle
{
    public double? PageWidthPoints { get; set; }

    public double? PageHeightPoints { get; set; }

    /// <summary>Named size keyword from @page size ("letter", "a4", ...), when one was used.</summary>
    public string NamedSize { get; set; }

    public bool? Landscape { get; set; }

    public double? MarginTopPoints { get; set; }
    public double? MarginRightPoints { get; set; }
    public double? MarginBottomPoints { get; set; }
    public double? MarginLeftPoints { get; set; }

    /// <summary>Known named page sizes in points (width, height, portrait orientation).</summary>
    public static bool TryGetNamedSize(string keyword, out double widthPoints, out double heightPoints)
    {
        switch ((keyword ?? "").Trim().ToLowerInvariant())
        {
            case "letter": widthPoints = 612; heightPoints = 792; return true;
            case "legal": widthPoints = 612; heightPoints = 1008; return true;
            case "ledger": widthPoints = 792; heightPoints = 1224; return true;
            case "a3": widthPoints = 842; heightPoints = 1191; return true;
            case "a4": widthPoints = 595; heightPoints = 842; return true;
            case "a5": widthPoints = 420; heightPoints = 595; return true;
            case "b4": widthPoints = 709; heightPoints = 1001; return true;
            case "b5": widthPoints = 499; heightPoints = 709; return true;
            default: widthPoints = 0; heightPoints = 0; return false;
        }
    }
}
