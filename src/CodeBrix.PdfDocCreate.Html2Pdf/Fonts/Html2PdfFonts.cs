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
    private static readonly HashSet<string> ProcessedFontDirectories =
        new HashSet<string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> FaceFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private static readonly List<string> FallbackFamilies = new List<string>();
    private static readonly List<string> RegisteredFontFiles = new List<string>();
    private static bool _registered;
    private static int _registrationVersion;

    /// <summary>Family name of the default sans-serif package font.</summary>
    public const string DefaultSansFamily = "Roboto";

    /// <summary>Family name of the default serif package font.</summary>
    public const string DefaultSerifFamily = "Merriweather";

    /// <summary>Family name of the default monospace package font.</summary>
    public const string DefaultMonoFamily = "RobotoMono";

    /// <summary>
    /// Adds an extra directory to probe for <c>CodeBrix.Platform.Fonts.*</c> package
    /// folders. Directories added after the first render take effect immediately;
    /// repeat calls with the same directory are harmless. A family name that is already
    /// registered keeps its first registration.
    /// </summary>
    public static void AddFontDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(directory));
        }

        lock (Sync)
        {
            if (!ExtraDirectories.Contains(directory, StringComparer.Ordinal))
            {
                ExtraDirectories.Add(directory);
            }

            if (_registered) { ScanFontDirectories(); }
        }
    }

    /// <summary>
    /// Registers a single loose .ttf or .otf font file. No manifest is needed - the
    /// family name, weight and style are read from the font's own name and OS/2 tables.
    /// Registered fonts are usable from CSS font-family values and from SVG text.
    /// With <paramref name="includeInFallback"/> the font's family also joins the
    /// per-glyph fallback chain consulted for characters the styled font lacks.
    /// </summary>
    public static void AddFontFile(string filePath, bool includeInFallback = false)
        => AddFontFiles(new[] { filePath }, includeInFallback);

    /// <summary>
    /// Registers several loose .ttf/.otf font files together, grouping faces that share
    /// a family name into one family. See <see cref="AddFontFile"/>.
    /// </summary>
    public static void AddFontFiles(IEnumerable<string> filePaths, bool includeInFallback = false)
    {
        if (filePaths == null) { throw new ArgumentNullException(nameof(filePaths)); }

        var resolvedPaths = new List<string>();
        foreach (var filePath in filePaths)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A font file path cannot be null or blank.", nameof(filePaths));
            }

            // Tolerate a path written with the other platform's separators.
            var candidate = filePath;
            if (!File.Exists(candidate))
            {
                var normalized = filePath
                    .Replace('/', Path.DirectorySeparatorChar)
                    .Replace('\\', Path.DirectorySeparatorChar);
                if (File.Exists(normalized)) { candidate = normalized; }
            }

            if (!File.Exists(candidate))
            {
                throw new FileNotFoundException($"Font file not found: '{filePath}'.", filePath);
            }

            resolvedPaths.Add(Path.GetFullPath(candidate));
        }

        lock (Sync)
        {
            var byFamily = new Dictionary<string, List<PackageFontFace>>(StringComparer.Ordinal);
            var familyDisplayNames = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in resolvedPaths)
            {
                var info = FontFileInfo.Read(path);
                if (info == null)
                {
                    throw new InvalidOperationException(
                        $"'{path}' is not a readable .ttf/.otf font file (no family name could be read from it).");
                }

                var key = NormalizeFamilyKey(info.FamilyName);
                if (!byFamily.TryGetValue(key, out var faces))
                {
                    faces = new List<PackageFontFace>();
                    byFamily.Add(key, faces);
                    familyDisplayNames.Add(key, info.FamilyName);
                }

                faces.Add(new PackageFontFace(
                    Path.GetFileNameWithoutExtension(path),
                    info.Weight,
                    info.IsItalic,
                    info.Stretch,
                    path));
            }

            foreach (var pair in byFamily)
            {
                if (!Families.ContainsKey(pair.Key))
                {
                    var family = new PackageFontFamily(familyDisplayNames[pair.Key], pair.Value);
                    Families.Add(pair.Key, family);
                    RegisterFamily(family);
                }

                if (includeInFallback) { AddFallbackFamilyLocked(familyDisplayNames[pair.Key]); }
            }

            _registrationVersion++;
        }
    }

    /// <summary>
    /// Registers every .ttf/.otf file found directly in a directory (no manifest
    /// needed). See <see cref="AddFontFile"/>.
    /// </summary>
    public static void AddFontFilesFromDirectory(string directory, bool includeInFallback = false)
    {
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(directory));
        }

        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Font directory not found: '{directory}'.");
        }

        var files = SafeEnumerateFiles(directory, "*.ttf")
            .Concat(SafeEnumerateFiles(directory, "*.otf"))
            .ToList();
        if (files.Count > 0) { AddFontFiles(files, includeInFallback); }
    }

    /// <summary>
    /// Appends an already-registered family to the per-glyph fallback chain: when a
    /// character has no glyph in the font a run resolved to, the fallback families are
    /// consulted in registration order and the first one covering the character renders
    /// it. Fallback families never substitute whole runs - only individual characters.
    /// </summary>
    public static void AddFallbackFamily(string familyName)
    {
        if (string.IsNullOrWhiteSpace(familyName))
        {
            throw new ArgumentException("Value cannot be null or blank.", nameof(familyName));
        }

        lock (Sync) { AddFallbackFamilyLocked(familyName); }
    }

    /// <summary>
    /// Discovers the package font families and registers them with the PDF font
    /// pipeline. Called automatically by the renderer; calling it eagerly at startup is
    /// harmless. The method is idempotent and thread-safe, and picks up any font
    /// directories added since the previous call.
    /// </summary>
    public static void EnsureRegistered()
    {
        lock (Sync)
        {
            ScanFontDirectories();
            _registered = true;
        }
    }

    private static void ScanFontDirectories()
    {
        // Companion families found this pass, with the rank of the package they came
        // from. They join the fallback chain after the whole scan rather than during
        // it, because the chain is consulted in order and directory enumeration order
        // is not the order we want - see PackageFallbackRank.
        var companions = new List<(int Rank, string FamilyName)>();

        foreach (var fontDirectory in EnumerateFontDirectories())
        {
            if (!ProcessedFontDirectories.Add(fontDirectory)) { continue; }

            var packageFamilies = new List<string>();

            foreach (var manifestPath in SafeEnumerateFiles(fontDirectory, "*.ttf.manifest"))
            {
                var familyName = Path.GetFileNameWithoutExtension(
                    Path.GetFileNameWithoutExtension(manifestPath));
                var key = NormalizeFamilyKey(familyName);

                if (!Families.ContainsKey(key))
                {
                    var faces = FontManifest.ReadFaces(manifestPath);
                    if (faces.Count == 0) { continue; }

                    var family = new PackageFontFamily(familyName, faces);
                    Families.Add(key, family);
                    RegisterFamily(family);
                }

                // Record the family against this package even when another package
                // already registered it. Noto Sans Georgian ships in BOTH the sans and
                // the monospace package, so attributing it only to whichever directory
                // the filesystem enumerated first would rank it arbitrarily - and rank
                // it as monospace half the time, putting the SERIF Georgian companion
                // ahead of it for Georgian text in a sans document. Listing it under
                // every package that ships it lets the best rank win; the chain itself
                // de-duplicates.
                packageFamilies.Add(familyName);
            }

            // A CodeBrix.Platform.Fonts.* package ships one primary family plus the
            // companion families that cover the scripts the primary lacks - polytonic
            // Greek, Armenian, Georgian, music notation. Every family in the package
            // that is NOT one of the three defaults is such a companion, which
            // reproduces each package's own CODEBRIX-DEVELOP.json fallbackFontUris list
            // exactly, without this code having to read that file or know any family
            // name. A new font package therefore extends the fallback chain with no
            // change here.
            var rank = PackageFallbackRank(packageFamilies);
            foreach (var familyName in packageFamilies)
            {
                if (IsDefaultFamily(familyName)) { continue; }
                companions.Add((rank, familyName));
            }
        }

        // OrderBy is stable, so within one package the families keep discovery order.
        foreach (var (_, familyName) in companions.OrderBy(c => c.Rank))
        {
            AddFallbackFamilyLocked(familyName);
        }

        _registrationVersion++;
    }

    /// <summary>
    /// Orders the packages whose companions feed the fallback chain: the sans package
    /// first, then the serif package, then a special-purpose package (one carrying none
    /// of the three body defaults, such as the Noto Music package), and the monospace
    /// package last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Order matters wherever two companions cover the same character, and body text is
    /// sans by default. Polytonic Greek is the case that forced the sans-first rule: the
    /// sans package's Noto Sans, the serif package's Noto Serif, and the monospace
    /// package's Noto Sans Mono and Iosevka ALL cover the Greek Extended block, so
    /// without a rank the directory-enumeration order would put the serif Noto Serif
    /// first and render ancient Greek in a serif face inside sans paragraphs.
    /// </para>
    /// <para>
    /// The monospace package ranks LAST rather than third because a monospaced glyph is
    /// the most jarring substitution inside proportional text, and because Iosevka's
    /// very wide repertoire (some 7,500 code points, including a few musical symbols)
    /// would otherwise shadow the special-purpose families whose whole reason to exist
    /// is those characters - Noto Music being exactly that case.
    /// </para>
    /// </remarks>
    private static int PackageFallbackRank(List<string> packageFamilies)
    {
        if (packageFamilies.Any(f => Matches(f, DefaultSansFamily))) { return 0; }
        if (packageFamilies.Any(f => Matches(f, DefaultSerifFamily))) { return 1; }
        if (packageFamilies.Any(f => Matches(f, DefaultMonoFamily))) { return 3; }
        return 2;

        static bool Matches(string familyName, string defaultFamily)
            => NormalizeFamilyKey(familyName) == NormalizeFamilyKey(defaultFamily);
    }

    /// <summary>
    /// True for the three body-text defaults. They are never auto-added to the fallback
    /// chain: a document styled sans must not silently pick up serif or monospace
    /// glyphs. A consumer that wants one can still add it with
    /// <see cref="AddFallbackFamily"/>.
    /// </summary>
    private static bool IsDefaultFamily(string familyName)
    {
        var key = NormalizeFamilyKey(familyName);
        return key == NormalizeFamilyKey(DefaultSansFamily)
            || key == NormalizeFamilyKey(DefaultSerifFamily)
            || key == NormalizeFamilyKey(DefaultMonoFamily);
    }

    private static void AddFallbackFamilyLocked(string familyName)
    {
        if (!FallbackFamilies.Contains(familyName, StringComparer.OrdinalIgnoreCase))
        {
            FallbackFamilies.Add(familyName);
            _registrationVersion++;
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
            case "sans": // SVG output (e.g. LilyPond's) abbreviates the generic this way
                name = DefaultSansFamily;
                break;
            case "serif":
            case "ui-serif":
                name = DefaultSerifFamily;
                break;
            case "monospace":
            case "ui-monospace":
            case "mono":
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
            if (!FaceFiles.ContainsKey(face.FaceName))
            {
                FaceFiles.Add(face.FaceName, face.FilePath);
                RegisteredFontFiles.Add(face.FilePath);
            }
        }

        MetaFontResolver.Instance.RegisterFontResolver(
            family.FamilyName,
            new PackageFontFamilyResolver(family, faceResolvers));

        // The font files' own family names (typographic and legacy) must resolve to
        // this family too: the PDF layer re-resolves fonts by the name-table family
        // during metrics computation, and an unknown name there would silently fall
        // through to operating-system fonts.
        var registeredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { family.FamilyName };
        foreach (var face in family.Faces)
        {
            var info = FontFileInfo.Read(face.FilePath);
            if (info == null) { continue; }

            foreach (var aliasName in new[] { info.FamilyName, info.LegacyFamilyName })
            {
                if (string.IsNullOrWhiteSpace(aliasName) || !registeredNames.Add(aliasName)) { continue; }
                MetaFontResolver.Instance.RegisterFontResolver(
                    aliasName,
                    new PackageFontFamilyResolver(family, faceResolvers, aliasName));
            }
        }
    }

    /// <summary>
    /// True when the registered face's own cmap contains a glyph for the code point.
    /// Unknown face names report no coverage.
    /// </summary>
    internal static bool FaceCovers(string faceName, int codePoint)
    {
        var coverage = TryGetFaceCoverage(faceName);
        return coverage != null && coverage.Covers(codePoint);
    }

    /// <summary>The font file behind a registered face, or null for unknown face names.</summary>
    internal static string TryGetFaceFilePath(string faceName)
    {
        if (string.IsNullOrEmpty(faceName)) { return null; }

        lock (Sync)
        {
            return FaceFiles.TryGetValue(faceName, out var filePath) ? filePath : null;
        }
    }

    /// <summary>
    /// The cmap coverage of a registered face, or null for unknown face names. Callers
    /// that test many characters against one face use this to avoid a lookup per
    /// character.
    /// </summary>
    internal static FontGlyphCoverage TryGetFaceCoverage(string faceName)
    {
        if (string.IsNullOrEmpty(faceName)) { return null; }

        string filePath;
        lock (Sync)
        {
            if (!FaceFiles.TryGetValue(faceName, out filePath)) { return null; }
        }

        return FontGlyphCoverage.Load(filePath);
    }

    /// <summary>
    /// Finds the face of the first fallback family that covers the code point, matched
    /// to the requested weight and style. Returns null when no fallback family covers it.
    /// </summary>
    internal static string TryResolveFallbackFace(int codePoint, int weight, bool italic)
        => TryResolveFallback(codePoint, weight, italic).FaceName;

    /// <summary>
    /// Finds the first fallback family covering the code point, returning both the
    /// family name (for markup that selects by family, e.g. an SVG tspan) and the
    /// concrete face matched to the requested weight and style.
    /// </summary>
    internal static (string FamilyName, string FaceName) TryResolveFallback(int codePoint, int weight, bool italic)
    {
        List<string> fallbacks;
        lock (Sync)
        {
            if (FallbackFamilies.Count == 0) { return (null, null); }
            fallbacks = new List<string>(FallbackFamilies);
        }

        foreach (var familyName in fallbacks)
        {
            var faceName = TryResolveFaceName(familyName, weight, italic);
            if (faceName != null && FaceCovers(faceName, codePoint)) { return (familyName, faceName); }
        }

        return (null, null);
    }

    /// <summary>
    /// Monotonic counter that changes whenever font registration changes; consumers
    /// (e.g. the SVG rasterizer's typeface chain) use it to refresh caches.
    /// </summary>
    internal static int RegistrationVersion
    {
        get { lock (Sync) { return _registrationVersion; } }
    }

    /// <summary>Absolute paths of every registered font file, for the SVG typeface chain.</summary>
    internal static IReadOnlyList<string> RegisteredFontFilesSnapshot
    {
        get
        {
            EnsureRegistered();
            lock (Sync) { return RegisteredFontFiles.ToArray(); }
        }
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
