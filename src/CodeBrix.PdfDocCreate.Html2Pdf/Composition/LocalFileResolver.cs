using System;
using System.IO;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Composition;

/// <summary>
/// Resolves document-relative file references (img src values, stylesheet hrefs) to
/// on-disk paths in a way that behaves the same on Windows, macOS and Linux: a
/// reference written with either separator style ("images/dot.png" or
/// "images\dot.png") finds the file on every platform, and percent-encoded names
/// ("my%20image.png") are retried decoded.
/// </summary>
internal static class LocalFileResolver
{
    /// <summary>
    /// Resolves <paramref name="reference"/> against <paramref name="baseDirectory"/>.
    /// Candidates are tried in order - the platform-native interpretation first, so any
    /// path that resolved before separator tolerance existed still resolves to the same
    /// file - and the first existing candidate wins. When nothing exists, the primary
    /// candidate is returned so the caller's failure path reports a sensible name.
    /// </summary>
    public static string Resolve(string reference, string baseDirectory)
    {
        if (Path.IsPathRooted(reference))
        {
            if (!File.Exists(reference))
            {
                var unescapedRooted = Uri.UnescapeDataString(reference);
                if (File.Exists(unescapedRooted)) { return unescapedRooted; }
            }

            return reference;
        }

        var primary = Path.Combine(baseDirectory ?? "", reference.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(primary)) { return primary; }

        var unescaped = Uri.UnescapeDataString(primary);
        if (File.Exists(unescaped)) { return unescaped; }

        // Foreign-separator tolerance: a reference authored on Windows ("images\dot.png")
        // must resolve on Linux/macOS too, and vice versa. The percent-decoded form is
        // normalized separately because decoding can itself produce separators (%5C).
        var normalized = Path.Combine(baseDirectory ?? "", NormalizeSeparators(reference));
        if (File.Exists(normalized)) { return normalized; }

        var unescapedNormalized = Path.Combine(baseDirectory ?? "", NormalizeSeparators(Uri.UnescapeDataString(reference)));
        if (File.Exists(unescapedNormalized)) { return unescapedNormalized; }

        return primary;
    }

    private static string NormalizeSeparators(string reference)
        => reference.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
}
