using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using CodeBrix.Imaging.Drawing.NoSkia;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Pdf;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// Writes an SVG display list (CodeBrix.Imaging.Drawing.NoSkia's DrawingPicture) into an
/// <see cref="XGraphics"/> as vector operators: saves and restores, matrix
/// concatenations, paths with solid or gradient fills and strokes (caps, joins, miter,
/// dashes), clips, nested pictures, images from their original bytes, text as real PDF
/// text in the embedded face the engine measured with (glyph outlines where there is no
/// face to embed - uncovered characters, strokes, gradients), and - as PDF transparency
/// groups - group opacity and the W3C blend modes.
/// What PDF cannot express is rasterized on its own - the offending command, or the
/// layer or clip scope it governs - at the raster scale, embedded as a transparent PNG,
/// and reported under the warning code <c>image.svg.rasterized</c>, so the output is
/// never wrong and everything else on the page stays vector.
/// </summary>
/// <remarks>
/// Coordinates are the picture's own (CSS pixels); the caller sets the placement
/// transform before emitting. The running transform is tracked here as well as in the
/// graphics object, because a rasterized sub-tree is rendered in picture space and must
/// be drawn back with the running transform cancelled. The picture's command set is
/// closed; a command kind this class does not know is ignored, mirroring the engine's
/// own visitor contract.
/// </remarks>
internal sealed class SvgVectorEmitter
{
    // Keeps a pathological viewBox from allocating an absurd raster.
    private const int MaxPixelsPerSide = 10000;

    private readonly XGraphics _gfx;
    private readonly DrawingPicture _root;
    private readonly string _reference;
    private readonly double _rasterScale;
    private readonly RenderWarnings _warnings;
    private readonly SvgFontMap _fontMap;
    private readonly Dictionary<(string Face, double Size), XFont> _fonts = new Dictionary<(string, double), XFont>();
    private readonly Stack<(XGraphicsState State, Matrix3x2 Ctm)> _saved = new Stack<(XGraphicsState, Matrix3x2)>();
    private Matrix3x2 _ctm = Matrix3x2.Identity;

    // Set on the child emitter that draws a single command inside a transparency group:
    // the group carries the command's blend mode and (for a gradient) its opacity, so
    // the command itself draws plainly. Without it the child would open another group.
    private bool _compositingCarriedByGroup;

    public SvgVectorEmitter(XGraphics gfx, DrawingPicture root, string reference, double rasterScale, RenderWarnings warnings, SvgFontMap fontMap = null)
    {
        _gfx = gfx ?? throw new ArgumentNullException(nameof(gfx));
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _reference = reference ?? "(svg)";
        _rasterScale = Math.Clamp(rasterScale, 0.25, 8.0);
        _warnings = warnings;
        _fontMap = fontMap;
    }

    /// <summary>Emits the whole picture at the graphics object's current transform.</summary>
    public void Emit()
    {
        EmitCommands(_root.Commands, 0, _root.Commands.Count - 1);
        CloseOpenStates();
    }

    private void CloseOpenStates()
    {
        // The picture is balanced; anything left is a defect in the input, not the page.
        while (_saved.Count > 0) { Restore(); }
    }

    private void EmitCommands(IReadOnlyList<DrawingCommand> commands, int from, int to)
    {
        for (var i = from; i <= to && i < commands.Count; i++)
        {
            switch (commands[i])
            {
                case SaveCommand:
                    Save();
                    break;

                case RestoreCommand:
                    Restore();
                    break;

                case SetMatrixCommand setMatrix:
                    Concat(setMatrix.Delta);
                    break;

                case SaveLayerCommand layer:
                {
                    var end = FindMatchingRestore(commands, i);
                    var reason = LayerRasterReason(layer);
                    if (reason != null)
                    {
                        // The layer and everything it governs, up to its own restore.
                        i = RasterizeRange(commands, i, end, reason);
                        break;
                    }

                    if (LayerNeedsGroup(layer))
                    {
                        // Group opacity and the W3C blend modes are PDF transparency groups:
                        // the layer's content composites as ONE object.
                        var opacity = layer.Paint.Color.Alpha / 255.0;
                        if (EmitGroup(commands, i + 1, end - 1, opacity, ToBlendMode(layer.Paint.BlendMode)))
                        {
                            i = end;
                        }
                        else
                        {
                            i = RasterizeRange(commands, i, end, "group opacity");
                        }
                        break;
                    }

                    Save();
                    break;
                }

                case ClipRectCommand clipRect:
                    if (clipRect.Operation == DrawingClipOperation.Intersect)
                    {
                        _gfx.IntersectClip(ToXRect(clipRect.Rect));
                    }
                    else
                    {
                        // A difference clip governs the rest of its scope; PDF has no such clip.
                        i = RasterizeRange(commands, i, FindScopeEnd(commands, i) - 1, "difference clip");
                    }
                    break;

                case ClipPathCommand clipPath:
                    if (clipPath.Operation == DrawingClipOperation.Intersect)
                    {
                        _gfx.IntersectClip(ToXPath(clipPath.Path));
                    }
                    else
                    {
                        i = RasterizeRange(commands, i, FindScopeEnd(commands, i) - 1, "difference clip");
                    }
                    break;

                case DrawPathCommand drawPath:
                    i = EmitDraw(commands, i, drawPath.Paint, () => DrawPath(drawPath.Path, drawPath.Paint));
                    break;

                case DrawTextCommand text:
                    i = EmitDraw(commands, i, text.Paint, () => DrawText(text));
                    break;

                case DrawPositionedTextCommand positioned:
                    i = EmitDraw(commands, i, positioned.Paint, () => DrawPositionedText(positioned));
                    break;

                case DrawTextOnPathCommand:
                    // The engine produces no outline for text on a path (its own raster
                    // route draws nothing either); say so rather than lose it silently.
                    _warnings?.Add(RenderWarnings.CategoryImage,
                        $"SVG image '{_reference}' places text on a path, which is not supported; that text was not drawn.",
                        "image.svg.text-unsupported");
                    break;

                case DrawImageCommand image:
                {
                    var reason = ImageReason(image);
                    if (reason != null)
                    {
                        i = RasterizeRange(commands, i, i, reason);
                        break;
                    }

                    DrawImage(image);
                    break;
                }

                case DrawPictureCommand nested:
                    if (nested.Picture != null)
                    {
                        // A nested picture is isolated, as a recorded picture is when replayed.
                        Save();
                        EmitCommands(nested.Picture.Commands, 0, nested.Picture.Commands.Count - 1);
                        Restore();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Emits one drawing command: plainly, inside a transparency group of one (a W3C blend
    /// mode, or a gradient under a paint opacity), or as a raster when PDF cannot express
    /// its paint. Returns the index of the last command consumed.
    /// </summary>
    private int EmitDraw(IReadOnlyList<DrawingCommand> commands, int index, DrawingPaint paint, Action draw)
    {
        var reason = PaintRasterReason(paint);
        if (reason != null)
        {
            return RasterizeRange(commands, index, index, reason);
        }

        if (!_compositingCarriedByGroup && paint != null)
        {
            var blend = ToBlendMode(paint.BlendMode);
            var translucentGradient = HasGradient(paint) && GradientOpacity(paint) < 1.0;
            if (blend != XBlendMode.Normal || translucentGradient)
            {
                var opacity = translucentGradient ? GradientOpacity(paint) : 1.0;
                if (EmitGroup(commands, index, index, opacity, blend))
                {
                    return index;
                }

                return RasterizeRange(commands, index, index, blend != XBlendMode.Normal ? "blend mode " + paint.BlendMode : "translucent gradient");
            }
        }

        draw();
        return index;
    }

    // ----- graphics state ---------------------------------------------------------------

    private void Save()
    {
        _saved.Push((_gfx.Save(), _ctm));
    }

    private void Restore()
    {
        if (_saved.Count == 0) { return; }

        var (state, ctm) = _saved.Pop();
        _gfx.Restore(state);
        _ctm = ctm;
    }

    private void Concat(Matrix3x2 delta)
    {
        // Row-vector convention on both sides: the new transform applies to points first.
        _ctm = delta * _ctm;
        _gfx.MultiplyTransform(ToXMatrix(delta), XMatrixOrder.Prepend);
    }

    // ----- what PDF cannot express ------------------------------------------------------

    /// <summary>Why a layer must be rasterized, or null when it can be a save or a group.</summary>
    private static string LayerRasterReason(SaveLayerCommand layer)
    {
        var paint = layer.Paint;
        if (paint == null) { return null; }

        if (paint.ImageFilter != null) { return "image filter (" + paint.ImageFilter.Description + ")"; }
        if (paint.ColorFilter != null) { return "color filter"; }
        if (paint.Shader != null) { return "layer shader"; }
        if (paint.BlendMode != DrawingBlendMode.SrcOver && ToBlendMode(paint.BlendMode) == XBlendMode.Normal)
        {
            return "blend mode " + paint.BlendMode;
        }

        return null;
    }

    private static bool LayerNeedsGroup(SaveLayerCommand layer)
    {
        var paint = layer.Paint;
        return paint != null && (paint.Color.Alpha < 255 || ToBlendMode(paint.BlendMode) != XBlendMode.Normal);
    }

    /// <summary>Why a draw must be rasterized, or null when its paint has a vector form.</summary>
    private static string PaintRasterReason(DrawingPaint paint)
    {
        if (paint == null) { return null; }

        var shader = paint.Shader;
        if (shader != null)
        {
            switch (shader.Kind)
            {
                case DrawingShaderKind.Color:
                    break;
                case DrawingShaderKind.LinearGradient:
                case DrawingShaderKind.RadialGradient:
                case DrawingShaderKind.TwoPointConicalGradient:
                    if (shader.TileMode == DrawingShaderTileMode.Repeat || shader.TileMode == DrawingShaderTileMode.Mirror)
                    {
                        return "repeating gradient";
                    }

                    var colors = shader.Colors;
                    if (colors == null || colors.Length == 0) { return "gradient without stops"; }
                    // A PDF shading carries no alpha. One alpha shared by every stop is the
                    // element's fill-opacity folded in (the engine does that at load) and
                    // becomes a group opacity; stops that differ in alpha need a soft mask.
                    if (!UniformStopAlpha(shader, out _)) { return "gradient with translucent stops"; }
                    break;
                default:
                    return "pattern fill";
            }
        }

        if (paint.BlendMode != DrawingBlendMode.SrcOver && ToBlendMode(paint.BlendMode) == XBlendMode.Normal)
        {
            return "blend mode " + paint.BlendMode;
        }

        if (paint.ColorFilter != null) { return "color filter"; }
        if (paint.ImageFilter != null) { return "image filter (" + paint.ImageFilter.Description + ")"; }
        return null;
    }

    private static bool HasGradient(DrawingPaint paint)
        => paint.Shader != null && paint.Shader.Kind != DrawingShaderKind.Color;

    /// <summary>True when every stop of a gradient has the same alpha, which is then returned.</summary>
    private static bool UniformStopAlpha(DrawingShader shader, out byte alpha)
    {
        var colors = shader.Colors;
        alpha = colors != null && colors.Length > 0 ? colors[0].Alpha : (byte)255;
        if (colors == null) { return true; }
        foreach (var color in colors)
        {
            if (color.Alpha != alpha) { return false; }
        }

        return true;
    }

    /// <summary>
    /// The opacity a draw's gradient carries: the paint alpha times the (uniform) stop alpha.
    /// </summary>
    private static double GradientOpacity(DrawingPaint paint)
    {
        UniformStopAlpha(paint.Shader, out var stopAlpha);
        return paint.Color.Alpha / 255.0 * (stopAlpha / 255.0);
    }

    private static string ImageReason(DrawImageCommand image)
    {
        if (image.Image == null && image.EncodedData == null) { return "image without pixels"; }

        var paintReason = PaintRasterReason(image.Paint);
        if (paintReason != null) { return paintReason; }
        if (image.Paint != null && (image.Paint.Color.Alpha < 255 || image.Paint.BlendMode != DrawingBlendMode.SrcOver))
        {
            return image.Paint.Color.Alpha < 255 ? "image opacity" : "blend mode " + image.Paint.BlendMode;
        }

        // A source rectangle that crops the bitmap has no counterpart in a plain image draw.
        if (image.Image != null)
        {
            var source = image.Source;
            if (Math.Abs(source.Left) > 0.01f || Math.Abs(source.Top) > 0.01f
                || Math.Abs(source.Width - image.Image.Width) > 0.01f
                || Math.Abs(source.Height - image.Image.Height) > 0.01f)
            {
                return "cropped image";
            }
        }

        return null;
    }

    /// <summary>
    /// The PDF blend mode for an engine blend mode: the W3C separable and non-separable
    /// modes map one to one; source-over is Normal; every Porter-Duff compositing operator
    /// and the Plus/Modulate arithmetic modes have no PDF form and also map to Normal - the
    /// callers tell those apart with the engine mode itself.
    /// </summary>
    private static XBlendMode ToBlendMode(DrawingBlendMode mode) => mode switch
    {
        DrawingBlendMode.Multiply => XBlendMode.Multiply,
        DrawingBlendMode.Screen => XBlendMode.Screen,
        DrawingBlendMode.Overlay => XBlendMode.Overlay,
        DrawingBlendMode.Darken => XBlendMode.Darken,
        DrawingBlendMode.Lighten => XBlendMode.Lighten,
        DrawingBlendMode.ColorDodge => XBlendMode.ColorDodge,
        DrawingBlendMode.ColorBurn => XBlendMode.ColorBurn,
        DrawingBlendMode.HardLight => XBlendMode.HardLight,
        DrawingBlendMode.SoftLight => XBlendMode.SoftLight,
        DrawingBlendMode.Difference => XBlendMode.Difference,
        DrawingBlendMode.Exclusion => XBlendMode.Exclusion,
        DrawingBlendMode.Hue => XBlendMode.Hue,
        DrawingBlendMode.Saturation => XBlendMode.Saturation,
        DrawingBlendMode.Color => XBlendMode.Color,
        DrawingBlendMode.Luminosity => XBlendMode.Luminosity,
        _ => XBlendMode.Normal,
    };

    // ----- drawing ----------------------------------------------------------------------

    private void DrawPath(DrawingPath path, DrawingPaint paint)
    {
        if (path == null || path.IsEmpty || paint == null) { return; }

        var color = EffectiveColor(paint);
        var xPath = ToXPath(path);

        var fill = paint.Style == DrawingPaintStyle.Fill || paint.Style == DrawingPaintStyle.StrokeAndFill;
        var stroke = paint.Style == DrawingPaintStyle.Stroke || paint.Style == DrawingPaintStyle.StrokeAndFill;

        // SVG stroke-width 0 means no stroke; PDF's zero-width line is the thinnest line
        // the device can draw. Never emit one.
        if (stroke && !(paint.StrokeWidth > 0)) { stroke = false; }

        var gradient = HasGradient(paint);
        if (!gradient && color.Alpha == 0) { return; }

        XBrush brush = fill ? ToBrush(paint, color) : null;
        XPen pen = stroke ? ToXPen(paint, color) : null;

        if (brush != null && pen != null)
        {
            _gfx.DrawPath(pen, brush, xPath);
        }
        else if (brush != null)
        {
            _gfx.DrawPath(brush, xPath);
        }
        else if (pen != null)
        {
            _gfx.DrawPath(pen, xPath);
        }
    }

    // ----- text -------------------------------------------------------------------------

    /// <summary>
    /// The embedded font for a run, or null when the run must be drawn as outlines: a
    /// stroked or gradient paint (PDF text has neither), or a family no registered face
    /// provides (the engine drew missing-glyph boxes for it).
    /// </summary>
    private XFont TryGetFont(DrawingPaint paint, DrawingTextStyle style)
    {
        if (_fontMap == null || paint == null || style == null) { return null; }
        if (paint.Style != DrawingPaintStyle.Fill || HasGradient(paint)) { return null; }
        if (!(style.Size > 0)) { return null; }

        var faceName = _fontMap.TryFindFace(style.FamilyName, (int)style.Weight, style.Slant != DrawingFontSlant.Upright);
        if (faceName == null) { return null; }

        var key = (faceName, (double)style.Size);
        if (!_fonts.TryGetValue(key, out var font))
        {
            try
            {
                // The face name is registered with the font resolver as its own family, so
                // the style is already in the name; Unicode encoding keeps every script and
                // writes a ToUnicode map, which is what makes the text selectable.
                font = new XFont(faceName, style.Size, XFontStyle.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
            }
            catch (Exception)
            {
                font = null;
            }

            _fonts[key] = font;
        }

        return font;
    }

    /// <summary>
    /// A whole run as one string at its baseline origin (the engine resolved text-anchor
    /// already). The page lays the glyphs out with the same font file's advances; the only
    /// thing that can differ from the engine's measurement is kerning, which stays inside
    /// the run. Whole runs are also what keeps the text extractable as words.
    /// </summary>
    private void DrawText(DrawTextCommand text)
    {
        var font = TryGetFont(text.Paint, text.Style);
        if (font == null || string.IsNullOrEmpty(text.Text))
        {
            DrawPath(text.GetOutline(), text.Paint);
            return;
        }

        var color = EffectiveColor(text.Paint);
        if (color.Alpha == 0) { return; }

        _gfx.DrawString(text.Text, font, new XSolidBrush(ToXColor(color)), new XPoint(text.X, text.Y), XStringFormats.BaseLineLeft);
    }

    /// <summary>
    /// A run with one position per code point - what an SVG x/y list per character
    /// produces - placed glyph by glyph where the document put them.
    /// </summary>
    private void DrawPositionedText(DrawPositionedTextCommand text)
    {
        var font = TryGetFont(text.Paint, text.Style);
        var value = text.Text;
        var positions = text.Positions;
        if (font == null || string.IsNullOrEmpty(value) || positions == null)
        {
            DrawPath(text.GetOutline(), text.Paint);
            return;
        }

        var color = EffectiveColor(text.Paint);
        if (color.Alpha == 0) { return; }

        var brush = new XSolidBrush(ToXColor(color));
        var index = 0;
        for (var k = 0; k < value.Length && index < positions.Length; k++)
        {
            var length = char.IsHighSurrogate(value[k]) && k + 1 < value.Length && char.IsLowSurrogate(value[k + 1]) ? 2 : 1;
            var glyph = value.Substring(k, length);
            var position = positions[index++];
            k += length - 1;

            if (string.IsNullOrWhiteSpace(glyph)) { continue; }

            _gfx.DrawString(glyph, font, brush, new XPoint(text.X + position.X, text.Y + position.Y), XStringFormats.BaseLineLeft);
        }
    }

    private void DrawImage(DrawImageCommand image)
    {
        byte[] encoded = null;
        if (image.EncodedData != null && image.EncodedFormat is DrawingEncodedImageFormat.Png
            or DrawingEncodedImageFormat.Jpeg or DrawingEncodedImageFormat.Bmp or DrawingEncodedImageFormat.Gif)
        {
            // The original file bytes: a JPEG passes through as-is, nothing is re-encoded.
            encoded = image.EncodedData;
        }
        else if (image.Image != null)
        {
            encoded = EncodePng(image.Image);
        }

        if (encoded == null) { return; }

        var captured = encoded;
        using (var xImage = XImage.FromStream(() => new MemoryStream(captured, writable: false)))
        {
            _gfx.DrawImage(xImage, ToXRect(image.Dest));
        }
    }

    // ----- transparency groups ----------------------------------------------------------

    /// <summary>
    /// Draws commands[from..to] into a transparency-group form the size of the page, in the
    /// page's own coordinates (the current transform is replicated on the form), then
    /// composites the form onto the page with the given opacity and blend mode. Returns
    /// false when there is no document to hold a form (a measure context).
    /// </summary>
    private bool EmitGroup(IReadOnlyList<DrawingCommand> commands, int from, int to, double opacity, XBlendMode blendMode)
    {
        var document = _gfx.PdfDocument;
        if (document == null) { return false; }

        var page = _gfx.PageSize;
        if (page.Width < 1 || page.Height < 1) { return false; }

        var form = new XForm(document, new XRect(0, 0, page.Width, page.Height));
        form.MakeTransparencyGroup();

        var formGfx = XGraphics.FromForm(form);
        formGfx.MultiplyTransform(_gfx.Transform, XMatrixOrder.Prepend);

        var child = new SvgVectorEmitter(formGfx, _root, _reference, _rasterScale, _warnings, _fontMap)
        {
            _ctm = _ctm,
            _compositingCarriedByGroup = from == to,
        };
        child.EmitCommands(commands, from, to);
        child.CloseOpenStates();

        // Place the form 1:1 on the page: cancel the current transform, draw, restore.
        var state = _gfx.Save();
        try
        {
            var inverse = _gfx.Transform;
            inverse.Invert();
            _gfx.MultiplyTransform(inverse, XMatrixOrder.Prepend);
            _gfx.DrawTransparencyGroup(form, new XRect(0, 0, page.Width, page.Height), opacity, blendMode);
        }
        finally
        {
            _gfx.Restore(state);
        }

        return true;
    }

    // ----- raster fallback --------------------------------------------------------------

    /// <summary>
    /// Rasterizes commands[from..to] (inclusive) in picture space at the running
    /// transform, draws the result back with that transform cancelled, warns, and
    /// returns the index of the last command consumed.
    /// </summary>
    private int RasterizeRange(IReadOnlyList<DrawingCommand> commands, int from, int to, string reason)
    {
        to = Math.Min(to, commands.Count - 1);
        var captured = new List<DrawingCommand>(to - from + 1);
        for (var k = from; k <= to; k++) { captured.Add(commands[k]); }

        _warnings?.Add(RenderWarnings.CategoryImage,
            $"SVG image '{_reference}' uses a {reason}, which PDF cannot express as vectors; that part was rasterized.",
            "image.svg.rasterized");

        byte[] png;
        try
        {
            png = Rasterize(new DrawingPicture(_root.CullRect, captured));
        }
        catch (Exception ex)
        {
            _warnings?.Add(RenderWarnings.CategoryImage,
                $"SVG image '{_reference}': a part that had to be rasterized could not be ({ex.GetType().Name}) and was skipped.",
                "image.svg.failed");
            return to;
        }

        if (png == null) { return to; }

        var cull = _root.CullRect;
        var state = _gfx.Save();
        try
        {
            if (Matrix3x2.Invert(_ctm, out var inverse))
            {
                _gfx.MultiplyTransform(ToXMatrix(inverse), XMatrixOrder.Prepend);
            }

            using (var xImage = XImage.FromStream(() => new MemoryStream(png, writable: false)))
            {
                _gfx.DrawImage(xImage, ToXRect(cull));
            }
        }
        finally
        {
            _gfx.Restore(state);
        }

        return to;
    }

    private byte[] Rasterize(DrawingPicture picture)
    {
        var cull = _root.CullRect;
        if (cull.Width <= 0 || cull.Height <= 0) { return null; }

        var scale = Math.Min(_rasterScale, MaxPixelsPerSide / Math.Max(cull.Width, cull.Height));
        var width = Math.Max(1, (int)Math.Ceiling(cull.Width * scale));
        var height = Math.Max(1, (int)Math.Ceiling(cull.Height * scale));

        using var bitmap = new DrawingBitmap(width, height, DrawingColorType.Rgba8888, DrawingAlphaType.Premul);
        using (var canvas = new DrawingCanvas(bitmap))
        {
            canvas.Clear(new DrawingColor(0, 0, 0, 0));
            canvas.Scale((float)scale);
            canvas.Translate(-cull.Left, -cull.Top);
            canvas.Concat(_ctm);
            canvas.DrawPicture(picture);
        }

        return EncodePng(bitmap);
    }

    private static byte[] EncodePng(DrawingBitmap bitmap)
    {
        using var image = DrawingImage.FromBitmap(bitmap);
        var data = image.Encode(DrawingEncodedImageFormat.Png, 100);
        return data?.ToArray();
    }

    // ----- scope arithmetic -------------------------------------------------------------

    /// <summary>Index of the restore that closes the save/layer at <paramref name="index"/>.</summary>
    private static int FindMatchingRestore(IReadOnlyList<DrawingCommand> commands, int index)
    {
        var depth = 0;
        for (var j = index; j < commands.Count; j++)
        {
            switch (commands[j])
            {
                case SaveCommand:
                case SaveLayerCommand:
                    depth++;
                    break;
                case RestoreCommand:
                    depth--;
                    if (depth == 0) { return j; }
                    break;
            }
        }

        return commands.Count - 1;
    }

    /// <summary>
    /// Index of the restore that closes the scope command <paramref name="index"/> sits in,
    /// or the command count when the scope runs to the end of the list.
    /// </summary>
    private static int FindScopeEnd(IReadOnlyList<DrawingCommand> commands, int index)
    {
        var depth = 0;
        for (var j = index + 1; j < commands.Count; j++)
        {
            switch (commands[j])
            {
                case SaveCommand:
                case SaveLayerCommand:
                    depth++;
                    break;
                case RestoreCommand:
                    if (depth == 0) { return j; }
                    depth--;
                    break;
            }
        }

        return commands.Count;
    }

    // ----- conversions ------------------------------------------------------------------

    private DrawingColor EffectiveColor(DrawingPaint paint)
    {
        if (paint.Shader != null && paint.Shader.Kind == DrawingShaderKind.Color)
        {
            // A color shader replaces the paint color; the paint's alpha still modulates it.
            var shaderColor = paint.Shader.Color;
            var alpha = (byte)Math.Round(shaderColor.Alpha * paint.Color.Alpha / 255.0);
            return shaderColor.WithAlpha(alpha);
        }

        return paint.Color;
    }

    /// <summary>A solid brush, or a shading brush for a gradient paint.</summary>
    private XBrush ToBrush(DrawingPaint paint, DrawingColor color)
    {
        if (!HasGradient(paint)) { return new XSolidBrush(ToXColor(color)); }

        return ToShadingBrush(paint.Shader);
    }

    private static XShadingBrush ToShadingBrush(DrawingShader shader)
    {
        var colors = shader.Colors;
        var positions = shader.Positions;
        var stops = new List<XGradientStop>(colors.Length);
        for (var k = 0; k < colors.Length; k++)
        {
            var offset = positions != null && positions.Length == colors.Length
                ? positions[k]
                : colors.Length == 1 ? 0.0 : (double)k / (colors.Length - 1);
            stops.Add(new XGradientStop(offset, ToXColor(colors[k].WithAlpha(255))));
        }

        XShadingBrush brush;
        switch (shader.Kind)
        {
            case DrawingShaderKind.LinearGradient:
                brush = new XShadingBrush(ToXPoint(shader.Start), ToXPoint(shader.End), stops);
                break;
            case DrawingShaderKind.RadialGradient:
                brush = new XShadingBrush(ToXPoint(shader.Center), 0, ToXPoint(shader.Center), shader.Radius, stops);
                break;
            default:
                brush = new XShadingBrush(ToXPoint(shader.Start), shader.StartRadius, ToXPoint(shader.End), shader.EndRadius, stops);
                break;
        }

        // Clamp continues the end colours; Decal stops the shading at its ends.
        var extend = shader.TileMode != DrawingShaderTileMode.Decal;
        brush.ExtendStart = extend;
        brush.ExtendEnd = extend;

        if (shader.LocalMatrix is Matrix3x2 local && !local.IsIdentity)
        {
            brush.Transform = ToXMatrix(local);
        }

        return brush;
    }

    private XPen ToXPen(DrawingPaint paint, DrawingColor color)
    {
        var width = paint.StrokeWidth;
        var pen = HasGradient(paint)
            ? new XPen(ToShadingBrush(paint.Shader), width)
            : new XPen(ToXColor(color), width);
        pen.LineCap = paint.StrokeCap switch
        {
            DrawingStrokeCap.Round => XLineCap.Round,
            DrawingStrokeCap.Square => XLineCap.Square,
            _ => XLineCap.Flat,
        };
        pen.LineJoin = paint.StrokeJoin switch
        {
            DrawingStrokeJoin.Round => XLineJoin.Round,
            DrawingStrokeJoin.Bevel => XLineJoin.Bevel,
            _ => XLineJoin.Miter,
        };
        pen.MiterLimit = paint.StrokeMiter > 0 ? paint.StrokeMiter : 4;

        var dash = paint.PathEffect;
        if (dash != null && dash.Intervals != null && dash.Intervals.Length >= 2)
        {
            // The pen's dash pattern and offset are in multiples of the pen width; the
            // effect's intervals and phase are absolute user units.
            var pattern = new double[dash.Intervals.Length];
            for (var k = 0; k < pattern.Length; k++) { pattern[k] = dash.Intervals[k] / width; }
            pen.DashPattern = pattern;
            pen.DashOffset = dash.Phase / width;
        }

        return pen;
    }

    internal static XGraphicsPath ToXPath(DrawingPath path)
    {
        var result = new XGraphicsPath
        {
            FillMode = path.FillType == DrawingPathFillType.EvenOdd ? XFillMode.Alternate : XFillMode.Winding,
        };

        var current = new DrawingPoint(0, 0);
        var start = current;
        foreach (var segment in path.GetVerbs())
        {
            var points = segment.Points;
            switch (segment.Verb)
            {
                case DrawingPathVerb.Move:
                    current = points[0];
                    start = current;
                    result.AddMove(current.X, current.Y);
                    break;

                case DrawingPathVerb.Line:
                    result.AddLine(current.X, current.Y, points[0].X, points[0].Y);
                    current = points[0];
                    break;

                case DrawingPathVerb.Quad:
                {
                    // A quadratic elevates to a cubic exactly.
                    var control = points[0];
                    var end = points[1];
                    var c1X = current.X + 2f / 3f * (control.X - current.X);
                    var c1Y = current.Y + 2f / 3f * (control.Y - current.Y);
                    var c2X = end.X + 2f / 3f * (control.X - end.X);
                    var c2Y = end.Y + 2f / 3f * (control.Y - end.Y);
                    result.AddBezier(current.X, current.Y, c1X, c1Y, c2X, c2Y, end.X, end.Y);
                    current = end;
                    break;
                }

                case DrawingPathVerb.Cubic:
                    result.AddBezier(current.X, current.Y, points[0].X, points[0].Y, points[1].X, points[1].Y, points[2].X, points[2].Y);
                    current = points[2];
                    break;

                case DrawingPathVerb.Close:
                    result.CloseFigure();
                    current = start;
                    break;
            }
        }

        return result;
    }

    private static XMatrix ToXMatrix(Matrix3x2 m) => new XMatrix(m.M11, m.M12, m.M21, m.M22, m.M31, m.M32);

    private static XRect ToXRect(DrawingRect rect) => new XRect(rect.Left, rect.Top, rect.Width, rect.Height);

    private static XPoint ToXPoint(DrawingPoint point) => new XPoint(point.X, point.Y);

    private static XColor ToXColor(DrawingColor color) => XColor.FromArgb((uint)color);
}
