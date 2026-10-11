using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeBrix.PdfDocCreate.Html2Pdf.Fonts;
using CodeBrix.PdfDocuments.Fonts;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// Fences the Android font delivery: on Android the build targets pack the
/// CodeBrix.Platform.Fonts.* folders as APK assets under CodeBrix.Html2Pdf/, and Html2Pdf reads
/// them in place through the AssetManager (FontStorage / AndroidFontAssets). A fake asset source
/// stands in for the AssetManager here: it serves this test project's own copied font folders
/// through NON-seekable streams, as Android's asset streams are, and counts every open.
/// </summary>
[Collection(nameof(FontStorageTestsCollection))]
public class FontStorageTests : IDisposable
{
    private readonly FakeAssetSource _assets = new FakeAssetSource(AppContext.BaseDirectory);

    public FontStorageTests()
    {
        FontStorage.SetAssetSourceForTesting(_assets);
    }

    public void Dispose()
    {
        FontStorage.SetAssetSourceForTesting(null);
    }

    private static string RobotoFontsAssetFolder =>
        FontStorage.AssetLocation(FontStorage.AndroidAssetFolder + "/CodeBrix.Platform.Fonts.Roboto/Fonts");

    [Fact]
    public void asset_font_directories_are_the_packed_package_folders()
    {
        //Act
        var directories = FontStorage.EnumerateAssetFontDirectories();

        //Assert
        directories.Should().Contain(RobotoFontsAssetFolder);
        directories.Should().Contain(FontStorage.AssetLocation("CodeBrix.Html2Pdf/CodeBrix.Platform.Fonts.RobotoMono/Fonts"));
        directories.Should().OnlyContain(d => FontStorage.IsAsset(d));
    }

    [Fact]
    public void there_are_no_asset_font_directories_without_an_asset_source()
    {
        //Arrange
        FontStorage.SetAssetSourceForTesting(null);

        //Act + Assert
        FontStorage.EnumerateAssetFontDirectories().Should().BeEmpty();
        FontStorage.FileExists(FontStorage.Combine(RobotoFontsAssetFolder, "Roboto.ttf.manifest")).Should().BeFalse();
    }

    [Fact]
    public void an_asset_reads_the_same_bytes_as_the_file_through_a_non_seekable_stream()
    {
        //Arrange
        var asset = FontStorage.Combine(RobotoFontsAssetFolder, "Roboto-Bold.ttf");
        var file = Path.Combine(AppContext.BaseDirectory, "CodeBrix.Platform.Fonts.Roboto", "Fonts", "Roboto-Bold.ttf");

        //Act
        var bytes = FontStorage.ReadAllBytes(asset);

        //Assert
        FontStorage.FileExists(asset).Should().BeTrue();
        bytes.Should().Equal(File.ReadAllBytes(file));
    }

    [Fact]
    public void reading_a_manifest_from_assets_opens_no_font_file()
    {
        //Arrange
        var manifest = FontStorage.Combine(RobotoFontsAssetFolder, "Roboto.ttf.manifest");

        //Act
        var faces = FontManifest.ReadFaces(manifest);

        //Assert
        faces.Should().NotBeEmpty();
        faces.Should().OnlyContain(f => FontStorage.IsAsset(f.FilePath));
        faces.Should().Contain(f => f.FaceName == "Roboto-Bold" && f.Weight == 700 && !f.IsItalic);
        _assets.FontFileOpens.Should().Be(0);
    }

    [Fact]
    public void registering_a_family_reads_no_font_file_until_a_face_is_used()
    {
        //Arrange - a uniquely named copy of the asset Roboto family, so the global registry is not shared
        var unique = "AssetTest" + Guid.NewGuid().ToString("N");
        var faces = FontManifest.ReadFaces(FontStorage.Combine(RobotoFontsAssetFolder, "Roboto.ttf.manifest"))
            .Select(f => f with { FaceName = unique + "-" + f.FaceName })
            .ToList();
        var family = new PackageFontFamily(unique, faces);

        //Act
        Html2PdfFonts.RegisterFamily(family);
        var opensAfterRegistration = _assets.FontFileOpens;
        var info = MetaFontResolver.Instance.ResolveTypeface(unique, isBold: true, isItalic: false);
        var bytes = MetaFontResolver.Instance.GetFont(info.FaceName);

        //Assert
        opensAfterRegistration.Should().Be(0);
        info.FaceName.Should().Be(unique + "-Roboto-Bold");
        bytes.Length.Should().BeGreaterThan(1000);
        _assets.FontFileOpens.Should().Be(1);
        //The face's own name-table family is registered once its bytes are read.
        MetaFontResolver.Instance.ResolveTypeface("Roboto", isBold: true, isItalic: false).Should().NotBeNull();
    }

    [Fact]
    public void the_face_bytes_are_read_once_and_the_load_callback_runs_once()
    {
        //Arrange
        var face = FontManifest.ReadFaces(FontStorage.Combine(RobotoFontsAssetFolder, "Roboto.ttf.manifest")).First();
        var callbacks = 0;
        var resolver = new SingleFaceFontResolver(face, _ => callbacks++);

        //Act
        resolver.GetFont(face.FaceName);
        resolver.GetFont(face.FaceName);

        //Assert
        callbacks.Should().Be(1);
        _assets.FontFileOpens.Should().Be(1);
    }

    /// <summary>Serves CodeBrix.Html2Pdf/&lt;package&gt;/Fonts/... from the copied package folders.</summary>
    private sealed class FakeAssetSource : IFontAssetSource
    {
        private readonly string _root;
        private int _fontFileOpens;

        public FakeAssetSource(string root) { _root = root; }

        public int FontFileOpens => _fontFileOpens;

        private string ToFile(string assetPath)
        {
            const string prefix = FontStorage.AndroidAssetFolder;
            if (assetPath == prefix) { return _root; }
            if (!assetPath.StartsWith(prefix + "/", StringComparison.Ordinal)) { return null; }
            return Path.Combine(_root, assetPath.Substring(prefix.Length + 1).Replace('/', Path.DirectorySeparatorChar));
        }

        public string[] List(string folder)
        {
            var path = ToFile(folder);
            if (path == null || !Directory.Exists(path)) { return Array.Empty<string>(); }
            return Directory.EnumerateFileSystemEntries(path).Select(Path.GetFileName).ToArray();
        }

        public Stream Open(string path)
        {
            if (path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                System.Threading.Interlocked.Increment(ref _fontFileOpens);
            var file = ToFile(path) ?? throw new FileNotFoundException(path);
            return new ForwardOnlyStream(File.OpenRead(file));
        }
    }

    /// <summary>A stream that, like Android's asset streams, can only be read forwards.</summary>
    private sealed class ForwardOnlyStream : Stream
    {
        private readonly Stream _inner;
        public ForwardOnlyStream(Stream inner) { _inner = inner; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) { if (disposing) { _inner.Dispose(); } base.Dispose(disposing); }
    }
}

/// <summary>The asset source is process-wide: these tests must not run beside one another.</summary>
[CollectionDefinition(nameof(FontStorageTestsCollection), DisableParallelization = true)]
public class FontStorageTestsCollection
{
}
