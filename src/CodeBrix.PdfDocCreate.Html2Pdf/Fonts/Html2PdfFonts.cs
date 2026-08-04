using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CodeBrix.PdfDocuments.Fonts;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// Discovers the font families delivered by the CodeBrix.Platform.Fonts.* packages and
/// registers them with the CodeBrix.PdfDocuments font pipeline. All Html2Pdf text is
/// rendered with these package fonts - never with operating-system fonts - so output is
/// identical on every platform.
/// </summary>
/// <remarks>
/// The font packages place their files at
/// <c>&lt;application base directory&gt;/CodeBrix.Platform.Fonts.&lt;Name&gt;/Fonts/</c>;
/// in a plain .NET application the CodeBrix.PdfDocCreate.Html2Pdf package's
/// buildTransitive targets perform that copy at build time. Call
/// <see cref="AddFontDirectory"/> before anything renders when fonts live somewhere
/// else (for example a custom plugin layout).
/// </remarks>
public static class Html2PdfFonts
{
    private static readonly object Sync = new object();
    private static readonly List<string> ExtraDirectories = new List<string>();
    private static readonly Dictionary<string, PackageFontFamily> Families =
        new Dictionary<string, PackageFontFamily>(StringComparer.Ordinal);
    private static bool _registered;

    /// <summary>Family name of the default sans-serif package font.</summary>
    public const string DefaultSansFamily = "Roboto";

    /// <summary>Family name of the default serif package font.</summary>
    public const string DefaultSerifFamily = "Merriweather";

    /// <summary>Family name of the default monospace package font.</summary>
    public const string DefaultMonoFamily = "RobotoMono";

    /// <summary>
    /// Adds an extra directory to probe for <c>CodeBrix.Platform.Fonts.*</c> package
    /// folders. Must be called before the first render (or before
    /// <see cref="EnsureRegistered"/>), because font registration happens exactly once
    /// per process.
    /// </summary>
    public static void AddFontDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(directory));
        }

        lock (Sync)
        {
            if (_registered)
            {
                throw new InvalidOperationException(
                    "Font directories must be added before the first render; the package fonts have already been registered.");
            }
            ExtraDirectories.Add(directory);
        }
    }

    /// <summary>
    /// Discovers the package font families and registers them with the PDF font
    /// pipeline. Called automatically by the renderer; calling it eagerly at startup is
    /// harmless. The method is idempotent and thread-safe.
    /// </summary>
    public static void EnsureRegistered()
    {
        lock (Sync)
        {
            if (_registered) { return; }

            foreach (var fontDirectory in EnumerateFontDirectories())
            {
                foreach (var manifestPath in SafeEnumerateFiles(fontDirectory, "*.ttf.manifest"))
                {
                    var familyName = Path.GetFileNameWithoutExtension(
                        Path.GetFileNameWithoutExtension(manifestPath));
                    var key = NormalizeFamilyKey(familyName);
                    if (Families.ContainsKey(key)) { continue; }

                    var faces = FontManifest.ReadFaces(manifestPath);
                    if (faces.Count == 0) { continue; }

                    var family = new PackageFontFamily(familyName, faces);
                    Families.Add(key, family);
                    RegisterFamily(family);
                }
            }

            _registered = true;
        }
    }

    /// <summary>
    /// True when the three default families (sans, serif, monospace) were all found
    /// during registration.
    /// </summary>
    public static bool HasDefaultFamilies
    {
        get
        {
            EnsureRegistered();
            lock (Sync)
            {
                return Families.ContainsKey(NormalizeFamilyKey(DefaultSansFamily))
                    && Families.ContainsKey(NormalizeFamilyKey(DefaultSerifFamily))
                    && Families.ContainsKey(NormalizeFamilyKey(DefaultMonoFamily));
            }
        }
    }

    /// <summary>
    /// Resolves a family name (or a CSS generic family) plus weight and style to the
    /// registered face name the document composer should render with. Returns null when
    /// no package family matches; callers fall back to the default sans family.
    /// </summary>
    internal static string TryResolveFaceName(string familyName, int weight, bool italic)
    {
        EnsureRegistered();

        if (string.IsNullOrWhiteSpace(familyName)) { return null; }

        var name = familyName.Trim().Trim('"', '\'');
        switch (name.ToLowerInvariant())
        {
            case "sans-serif":
            case "ui-sans-serif":
            case "system-ui":
                name = DefaultSansFamily;
                break;
            case "serif":
            case "ui-serif":
                name = DefaultSerifFamily;
                break;
            case "monospace":
            case "ui-monospace":
                name = DefaultMonoFamily;
                break;
        }

        lock (Sync)
        {
            if (!Families.TryGetValue(NormalizeFamilyKey(name), out var family)) { return null; }
            var face = family.SelectFace(ClampWeight(weight), italic);
            return face?.FaceName;
        }
    }

    /// <summary>Names of every discovered package font family, for diagnostics and tests.</summary>
    internal static IReadOnlyCollection<string> RegisteredFamilyNames
    {
        get
        {
            EnsureRegistered();
            lock (Sync) { return Families.Values.Select(f => f.FamilyName).ToArray(); }
        }
    }

    private static void RegisterFamily(PackageFontFamily family)
    {
        var faceResolvers = new Dictionary<string, SingleFaceFontResolver>(StringComparer.OrdinalIgnoreCase);
        foreach (var face in family.Faces)
        {
            if (faceResolvers.ContainsKey(face.FaceName)) { continue; }
            var faceResolver = new SingleFaceFontResolver(face);
            faceResolvers.Add(face.FaceName, faceResolver);
            MetaFontResolver.Instance.RegisterFontResolver(face.FaceName, faceResolver);
        }

        MetaFontResolver.Instance.RegisterFontResolver(
            family.FamilyName,
            new PackageFontFamilyResolver(family, faceResolvers));
    }

    private static IEnumerable<string> EnumerateFontDirectories()
    {
        var roots = new List<string> { AppContext.BaseDirectory };
        roots.AddRange(ExtraDirectories);

        foreach (var root in roots.Where(r => !string.IsNullOrWhiteSpace(r) && Directory.Exists(r)))
        {
            IEnumerable<string> packageDirectories;
            try
            {
                packageDirectories = Directory.EnumerateDirectories(root, "CodeBrix.Platform.Fonts.*");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var packageDirectory in packageDirectories)
            {
                var fontDirectory = Path.Combine(packageDirectory, "Fonts");
                if (Directory.Exists(fontDirectory)) { yield return fontDirectory; }
            }
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directory, string pattern)
    {
        try
        {
            return Directory.EnumerateFiles(directory, pattern).OrderBy(p => p, StringComparer.Ordinal).ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    private static string NormalizeFamilyKey(string familyName)
    {
        var builder = new StringBuilder(familyName.Length);
        foreach (var c in familyName)
        {
            if (char.IsLetterOrDigit(c)) { builder.Append(char.ToLowerInvariant(c)); }
        }
        return builder.ToString();
    }

    private static int ClampWeight(int weight) => Math.Clamp(weight, 100, 900);
}
