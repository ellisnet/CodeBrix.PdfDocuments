using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// Reads the <c>.ttf.manifest</c> JSON files that the CodeBrix.Platform.Fonts.* packages
/// ship next to their font files. Each manifest describes the static faces of one font
/// family as entries of font_style / font_weight / font_stretch plus a URI whose final
/// segment is the face's .ttf file name.
/// </summary>
internal static class FontManifest
{
    /// <summary>
    /// Parses a single manifest file and returns one face entry per usable static font
    /// file that exists on disk next to the manifest. Returns an empty list when the
    /// manifest cannot be parsed.
    /// </summary>
    public static List<PackageFontFace> ReadFaces(string manifestPath)
    {
        var faces = new List<PackageFontFace>();
        var fontDirectory = Path.GetDirectoryName(manifestPath);

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (!document.RootElement.TryGetProperty("fonts", out var fonts)
                || fonts.ValueKind != JsonValueKind.Array)
            {
                return faces;
            }

            foreach (var entry in fonts.EnumerateArray())
            {
                var style = GetString(entry, "font_style");
                var stretch = GetString(entry, "font_stretch");
                var fileUri = GetString(entry, "family_name");
                var weight = entry.TryGetProperty("font_weight", out var weightElement)
                             && weightElement.TryGetInt32(out var parsedWeight)
                    ? parsedWeight
                    : 400;

                if (fileUri.Length == 0) { continue; }

                // The URI form is ms-appx:///<package>/Fonts/<file>.ttf - only the file
                // name matters here because the manifest sits in the same folder.
                var fileName = fileUri.Replace('\\', '/');
                var lastSlash = fileName.LastIndexOf('/');
                if (lastSlash >= 0) { fileName = fileName.Substring(lastSlash + 1); }

                var filePath = Path.Combine(fontDirectory, fileName);
                if (!File.Exists(filePath)) { continue; }

                faces.Add(new PackageFontFace(
                    Path.GetFileNameWithoutExtension(fileName),
                    weight,
                    style.Equals("Italic", StringComparison.OrdinalIgnoreCase),
                    stretch.Length == 0 ? "Normal" : stretch,
                    filePath));
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            // An unreadable manifest simply contributes no faces; discovery of the other
            // families must not fail because of it.
        }

        return faces;
    }

    private static string GetString(JsonElement entry, string propertyName) =>
        entry.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString().Trim()
            : "";
}
