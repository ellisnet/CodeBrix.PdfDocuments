using System;
using System.Collections.Generic;

namespace CodeBrix.PdfDocuments.Drawing;

/// <summary>Whether a shading runs along an axis or between two circles.</summary>
public enum XShadingKind
{
    /// <summary>An axial (linear) shading from Start to End (PDF shading type 2).</summary>
    Axial = 0,
    /// <summary>
    /// A radial shading between the circle (Start, StartRadius) and the circle (End, EndRadius)
    /// (PDF shading type 3). A plain radial gradient has both centres equal and StartRadius 0.
    /// </summary>
    Radial = 1,
}

/// <summary>One colour stop of a shading: an offset in 0..1 along the shading and its colour.</summary>
public readonly struct XGradientStop
{
    /// <summary>Creates a stop.</summary>
    public XGradientStop(double offset, XColor color)
    {
        Offset = offset;
        Color = color;
    }

    /// <summary>Where along the shading the stop sits, 0 at Start and 1 at End.</summary>
    public double Offset { get; }

    /// <summary>The colour at the stop. Alpha is ignored: a PDF shading carries no alpha.</summary>
    public XColor Color { get; }
}

/// <summary>
/// A gradient brush with any number of colour stops, an axial or radial geometry, per-end
/// extension flags, and a brush-space transform. It is the general form of
/// <see cref="XLinearGradientBrush"/> and <see cref="XRadialGradientBrush"/>: the geometry is
/// given in the brush's own space, and <see cref="XBaseGradientBrush.Transform"/> maps that
/// space into the user space of the drawing - so a gradient defined on a shape's bounding box
/// (as SVG does by default) keeps its shape when the box is not square. Realized as a PDF
/// shading pattern with an exponential function for two stops and a stitching function for
/// more.
/// </summary>
public sealed class XShadingBrush : XBaseGradientBrush
{
    private readonly List<XGradientStop> _stops = new List<XGradientStop>();

    /// <summary>Creates an axial shading brush.</summary>
    public XShadingBrush(XPoint start, XPoint end, IEnumerable<XGradientStop> stops)
        : base(XColors.Black, XColors.Black)
    {
        Kind = XShadingKind.Axial;
        Start = start;
        End = end;
        SetStops(stops);
    }

    /// <summary>Creates a radial shading brush between two circles.</summary>
    public XShadingBrush(XPoint startCenter, double startRadius, XPoint endCenter, double endRadius, IEnumerable<XGradientStop> stops)
        : base(XColors.Black, XColors.Black)
    {
        if (startRadius < 0) throw new ArgumentOutOfRangeException("startRadius");
        if (endRadius < 0) throw new ArgumentOutOfRangeException("endRadius");
        Kind = XShadingKind.Radial;
        Start = startCenter;
        End = endCenter;
        StartRadius = startRadius;
        EndRadius = endRadius;
        SetStops(stops);
    }

    /// <summary>Axial or radial.</summary>
    public XShadingKind Kind { get; }

    /// <summary>The start point (axial) or the start circle's centre (radial), in brush space.</summary>
    public XPoint Start { get; }

    /// <summary>The end point (axial) or the end circle's centre (radial), in brush space.</summary>
    public XPoint End { get; }

    /// <summary>The start circle's radius (radial only), in brush space.</summary>
    public double StartRadius { get; }

    /// <summary>The end circle's radius (radial only), in brush space.</summary>
    public double EndRadius { get; }

    /// <summary>
    /// The stops in ascending offset order, always beginning at offset 0 and ending at offset 1
    /// (a first stop above 0 or a last stop below 1 is padded with its own colour).
    /// </summary>
    public IReadOnlyList<XGradientStop> Stops => _stops;

    /// <summary>Whether the first colour continues before Start (true) or the shading stops there.</summary>
    public bool ExtendStart { get; set; } = true;

    /// <summary>Whether the last colour continues beyond End (true) or the shading stops there.</summary>
    public bool ExtendEnd { get; set; } = true;

    private void SetStops(IEnumerable<XGradientStop> stops)
    {
        if (stops == null) throw new ArgumentNullException("stops");

        var list = new List<XGradientStop>();
        foreach (XGradientStop stop in stops)
            list.Add(new XGradientStop(Math.Min(1, Math.Max(0, stop.Offset)), stop.Color));
        if (list.Count == 0)
            throw new ArgumentException("A shading needs at least one stop.", "stops");

        // Stable sort by offset; equal offsets keep their order (a hard edge).
        for (int i = 1; i < list.Count; i++)
        {
            XGradientStop key = list[i];
            int j = i - 1;
            while (j >= 0 && list[j].Offset > key.Offset)
            {
                list[j + 1] = list[j];
                j--;
            }
            list[j + 1] = key;
        }

        if (list[0].Offset > 0)
            list.Insert(0, new XGradientStop(0, list[0].Color));
        if (list[list.Count - 1].Offset < 1)
            list.Add(new XGradientStop(1, list[list.Count - 1].Color));
        if (list.Count == 1)
            list.Add(new XGradientStop(1, list[0].Color));

        _stops.AddRange(list);
        _color1 = list[0].Color;
        _color2 = list[list.Count - 1].Color;
    }
}
