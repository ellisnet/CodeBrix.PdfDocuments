using System;
using System.Collections.Generic;
using CodeBrix.PdfDocCreate.DocumentObjectModel;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// Measures text widths (in points) with the real package font faces, for table column
/// sizing. Uses a throwaway PDF page as the measurement surface.
/// </summary>
internal sealed class MeasureHelper : IDisposable
{
    private readonly PdfDocument _document;
    private readonly XGraphics _graphics;
    private readonly Dictionary<string, TextMeasurement> _measurers =
        new Dictionary<string, TextMeasurement>(StringComparer.Ordinal);

    public MeasureHelper()
    {
        _document = new PdfDocument();
        var page = _document.AddPage();
        _graphics = XGraphics.FromPdfPage(page);
    }

    public double MeasureWidth(string text, string faceName, double sizePoints)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(faceName) || sizePoints <= 0)
        {
            return 0;
        }

        var key = faceName + "|" + sizePoints.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        if (!_measurers.TryGetValue(key, out var measurement))
        {
            measurement = new TextMeasurement(_graphics, new Font(faceName, Unit.FromPoint(sizePoints)));
            _measurers.Add(key, measurement);
        }

        return measurement.MeasureString(text).Width;
    }

    public void Dispose()
    {
        _graphics.Dispose();
        _document.Dispose();
    }
}
