using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using CodeBrix.Imaging.Drawing.NoSkia.Svg.Rendering;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Svg;

/// <summary>
/// Fills an SVG document's font registry from the registered document fonts (package
/// fonts plus anything added through <see cref="Html2PdfFonts"/>), so SVG text draws with
/// exactly the faces the rest of the document uses and never with a system font. Family
/// matching and CSS weight/style face selection are <see cref="SvgFontResolution"/>'s,
/// so the face the HTML text layer would pick is the face the SVG engine gets.
/// </summary>
/// <remarks>
/// The SVG engine resolves a font-family value by trying each comma-separated candidate
/// against the names fonts were registered under (then their embedded family names), and
/// falls back to the FIRST registered font for a family nothing provides. Each requested
/// family is therefore registered under the candidate name that resolved it, and the
/// default sans face is registered first so an unknown family lands where the HTML side
/// lands it. Registering the same name at several weights and styles is deliberate: the
/// engine selects the closest face within a name, which is how bold and italic text gets
/// its real faces.
/// </remarks>
internal static class SvgFontBridge
{
    private static readonly ConcurrentDictionary<string, byte[]> FileBytes =
        new ConcurrentDictionary<string, byte[]>(StringComparer.Ordinal);

    private static readonly ConcurrentDictionary<string, FontFileInfo> FileInfos =
        new ConcurrentDictionary<string, FontFileInfo>(StringComparer.Ordinal);

    /// <summary>
    /// Registers the faces that satisfy <paramref name="requests"/> (plus the default sans
    /// face as the fallback) on <paramref name="fonts"/>, and returns the map from the
    /// engine's resolved family names back to those faces. Must run before the document
    /// loads.
    /// </summary>
    public static SvgFontMap Register(NoSkiaFontRegistry fonts, IEnumerable<SvgFontRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(fonts);
        Html2PdfFonts.EnsureRegistered();

        var registered = new HashSet<(string Path, string Name)>();
        var map = new SvgFontMap();

        // First registration = the engine's fallback for families nothing matches.
        RegisterFace(fonts, registered, map, Html2PdfFonts.TryResolveFaceName("sans-serif", 400, false), null);

        if (requests == null) { return map; }

        foreach (var request in requests)
        {
            var matched = false;
            foreach (var candidate in SvgFontResolution.SplitList(request.FamilyList))
            {
                var faceName = Html2PdfFonts.TryResolveFaceName(candidate, request.Weight, request.Italic);
                if (faceName == null) { continue; }

                RegisterFace(fonts, registered, map, faceName, candidate);
                matched = true;
                break;
            }

            if (matched) { continue; }

            // No candidate resolves: the HTML side falls back to the default sans face at
            // the requested weight and style, so register that under the first candidate.
            string firstCandidate = null;
            foreach (var candidate in SvgFontResolution.SplitList(request.FamilyList))
            {
                firstCandidate = candidate;
                break;
            }

            if (firstCandidate != null)
            {
                RegisterFace(fonts, registered, map,
                    Html2PdfFonts.TryResolveFaceName("sans-serif", request.Weight, request.Italic), firstCandidate);
            }
        }

        return map;
    }

    private static void RegisterFace(
        NoSkiaFontRegistry fonts, HashSet<(string Path, string Name)> registered, SvgFontMap map, string faceName, string overrideName)
    {
        var filePath = Html2PdfFonts.TryGetFaceFilePath(faceName);
        if (filePath == null) { return; }

        // The map is keyed by what the engine will report: the file's own family names.
        var info = FileInfos.GetOrAdd(filePath, static path => FontFileInfo.Read(path));
        if (info != null)
        {
            map.Add(info.FamilyName, info.LegacyFamilyName, faceName, info.Weight, info.IsItalic);
        }

        if (!registered.Add((filePath, overrideName ?? string.Empty))) { return; }

        var bytes = FileBytes.GetOrAdd(filePath, static path => File.ReadAllBytes(path));
        if (overrideName == null)
        {
            fonts.RegisterFont(bytes);
        }
        else
        {
            fonts.RegisterFont(bytes, overrideName);
        }
    }
}
