namespace CodeBrix.PdfDocuments.Shared;

internal static class PackageVersionInfo
{
    // Build = days since 2026-01-01  -  change values ONLY here
    public const string VersionMajor = "1"; // Also used for NuGet Version.
    public const string VersionMinor = "0"; // Also used for NuGet Version.
    public const string VersionBuild = "101"; // Also used for NuGet Version.
    public const string VersionPatch = "0"; // NOT used for NuGet Version.

    /// <summary>
    /// E.g. "1/1/2005", for use in NuGet Script.
    /// </summary>
    public const string VersionReferenceDate = "2026-01-01";
}
