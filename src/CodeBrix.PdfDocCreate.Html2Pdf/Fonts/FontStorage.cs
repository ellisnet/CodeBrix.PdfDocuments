using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// Where the package font files live, and how to read them. A font location is either an
/// ordinary file-system path or, on Android, an application asset written as
/// <c>android-asset:&lt;asset path&gt;</c>. Every font-file access in Html2Pdf goes through
/// here, so asset-delivered fonts are read in place, on demand, with no copy to disk.
/// </summary>
/// <remarks>
/// On Android the CodeBrix.PdfDocCreate.Html2Pdf build targets pack the
/// CodeBrix.Platform.Fonts.* files into the APK as assets under
/// <see cref="AndroidAssetFolder"/>; the build output folder that desktop discovery reads
/// does not exist inside an APK.
/// </remarks>
internal static class FontStorage
{
    /// <summary>The prefix that marks a location as an application asset.</summary>
    public const string AssetScheme = "android-asset:";

    /// <summary>The asset folder the build targets put the font package folders in.</summary>
    public const string AndroidAssetFolder = "CodeBrix.Html2Pdf";

    private static IFontAssetSource _assetSource;
    private static bool _assetSourceResolved;
    private static readonly object Sync = new object();

    /// <summary>
    /// The application's assets, or null where there are none (every platform but Android).
    /// Tests replace it through <see cref="SetAssetSourceForTesting"/>.
    /// </summary>
    public static IFontAssetSource AssetSource
    {
        get
        {
            lock (Sync)
            {
                if (!_assetSourceResolved)
                {
                    _assetSource = AndroidFontAssets.TryCreate();
                    _assetSourceResolved = true;
                }
                return _assetSource;
            }
        }
    }

    /// <summary>Replaces the asset source (null = none). Test seam.</summary>
    internal static void SetAssetSourceForTesting(IFontAssetSource source)
    {
        lock (Sync)
        {
            _assetSource = source;
            _assetSourceResolved = true;
        }
    }

    /// <summary>True when the location names an application asset.</summary>
    public static bool IsAsset(string location)
        => location != null && location.StartsWith(AssetScheme, StringComparison.Ordinal);

    /// <summary>Builds the location of an asset path.</summary>
    public static string AssetLocation(string assetPath) => AssetScheme + assetPath;

    private static string AssetPath(string location) => location.Substring(AssetScheme.Length);

    /// <summary>The location of <paramref name="name"/> inside the folder <paramref name="folder"/>.</summary>
    public static string Combine(string folder, string name)
        => IsAsset(folder) ? folder.TrimEnd('/') + "/" + name : Path.Combine(folder, name);

    /// <summary>The folder part of a location.</summary>
    public static string GetDirectoryName(string location)
    {
        if (!IsAsset(location)) { return Path.GetDirectoryName(location); }
        var slash = location.LastIndexOf('/');
        return slash > AssetScheme.Length ? location.Substring(0, slash) : AssetScheme;
    }

    /// <summary>The file name (last segment) of a location.</summary>
    public static string GetFileName(string location)
    {
        if (!IsAsset(location)) { return Path.GetFileName(location); }
        var slash = location.LastIndexOf('/');
        return slash >= 0 ? location.Substring(slash + 1) : AssetPath(location);
    }

    /// <summary>True when a file exists at the location.</summary>
    public static bool FileExists(string location)
    {
        if (!IsAsset(location)) { return File.Exists(location); }

        var source = AssetSource;
        if (source == null) { return false; }
        var folder = AssetPath(GetDirectoryName(location));
        var name = GetFileName(location);
        return source.List(folder).Contains(name, StringComparer.Ordinal);
    }

    /// <summary>Opens the file at the location for reading. The stream may not be seekable.</summary>
    public static Stream OpenRead(string location)
    {
        if (!IsAsset(location)) { return File.OpenRead(location); }

        var source = AssetSource ?? throw new FileNotFoundException("No application assets on this platform.", location);
        return source.Open(AssetPath(location));
    }

    /// <summary>Reads the whole file at the location.</summary>
    public static byte[] ReadAllBytes(string location)
    {
        if (!IsAsset(location)) { return File.ReadAllBytes(location); }

        using var stream = OpenRead(location);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    /// <summary>Reads the whole file at the location as UTF-8 text.</summary>
    public static string ReadAllText(string location)
    {
        if (!IsAsset(location)) { return File.ReadAllText(location); }

        using var reader = new StreamReader(OpenRead(location));
        return reader.ReadToEnd();
    }

    /// <summary>
    /// The files directly inside <paramref name="folder"/> whose names end with
    /// <paramref name="suffix"/> (case-insensitive), in ordinal order. Never throws.
    /// </summary>
    public static IReadOnlyList<string> EnumerateFiles(string folder, string suffix)
    {
        try
        {
            IEnumerable<string> names;
            if (IsAsset(folder))
            {
                var source = AssetSource;
                if (source == null) { return Array.Empty<string>(); }
                names = source.List(AssetPath(folder));
            }
            else
            {
                names = Directory.EnumerateFiles(folder).Select(Path.GetFileName);
            }

            return names
                .Where(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                .Select(n => Combine(folder, n))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// The <c>CodeBrix.Platform.Fonts.*/Fonts</c> folders packed as application assets, in
    /// ordinal order; empty where there are no assets. Never throws.
    /// </summary>
    public static IReadOnlyList<string> EnumerateAssetFontDirectories()
    {
        var source = AssetSource;
        if (source == null) { return Array.Empty<string>(); }

        try
        {
            return source.List(AndroidAssetFolder)
                .Where(n => n.StartsWith("CodeBrix.Platform.Fonts.", StringComparison.Ordinal))
                .OrderBy(n => n, StringComparer.Ordinal)
                .Select(n => AndroidAssetFolder + "/" + n + "/Fonts")
                .Where(p => source.List(p).Length > 0)
                .Select(AssetLocation)
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Array.Empty<string>();
        }
    }
}

/// <summary>Read access to an application's packed assets (Android's AssetManager).</summary>
internal interface IFontAssetSource
{
    /// <summary>The names of the entries directly inside an asset folder (empty when none).</summary>
    string[] List(string folder);

    /// <summary>Opens an asset for reading.</summary>
    Stream Open(string path);
}
