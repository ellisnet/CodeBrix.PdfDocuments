using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats.Bmp;
using CodeBrix.Imaging.Formats.Gif;
using CodeBrix.Imaging.Formats.Jpeg;
using CodeBrix.Imaging.Formats.Png;
using CodeBrix.Imaging.Formats.Webp;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.Imaging.Processing;
using CodeBrix.PdfDocuments.Drawing;
using CodeBrix.PdfDocuments.Utils;
using SilverAssertions;
using System;
using System.IO;
using Xunit;

namespace CodeBrix.PdfDocuments.Tests.Utils;

public class ImagingImageSourceTests
{
    private static byte[] GetEmbeddedResourceBytes(string fileName)
    {
        var assembly = typeof(ImagingImageSourceTests).Assembly;
        var resourceName = $"CodeBrix.PdfDocuments.Tests.SampleFiles.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    // ── FromImagingImage ─────────────────────────────────────────────────

    [Fact]
    public void FromImagingImage_WithRgba32Png_SetsPropertiesCorrectly()
    {
        using var image = new Image<Rgba32>(80, 60, new Rgba32(255, 0, 0, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, PngFormat.Instance, quality: 75);

        source.Width.Should().Be(80);
        source.Height.Should().Be(60);
        source.Transparent.Should().BeTrue(); // PNG supports transparency
        source.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void FromImagingImage_WithRgb24Jpeg_SetsPropertiesCorrectly()
    {
        using var image = new Image<Rgb24>(120, 90);
        var source = ImagingImageSource<Rgb24>.FromImagingImage(image, JpegFormat.Instance, quality: 50);

        source.Width.Should().Be(120);
        source.Height.Should().Be(90);
        source.Transparent.Should().BeFalse(); // JPEG does not support transparency
    }

    [Fact]
    public void FromImagingImage_GeneratesUniqueName()
    {
        using var image1 = new Image<Rgba32>(10, 10);
        using var image2 = new Image<Rgba32>(10, 10);

        var source1 = ImagingImageSource<Rgba32>.FromImagingImage(image1, PngFormat.Instance);
        var source2 = ImagingImageSource<Rgba32>.FromImagingImage(image2, PngFormat.Instance);

        source1.Name.Should().NotBe(source2.Name);
    }

    // ── FromFileImpl ─────────────────────────────────────────────────────

    [Fact]
    public void FromFile_WithPng_ReturnsCorrectImageSourceAndTransparency()
    {
        var pngBytes = GetEmbeddedResourceBytes("test-image-01.png");
        var tempPath = Path.Combine(Path.GetTempPath(), $"ImagingImageSourceTest_{Guid.NewGuid()}.png");
        try
        {
            File.WriteAllBytes(tempPath, pngBytes);
            ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();
            var source = ImageSource.FromFile(tempPath);

            source.Width.Should().BeGreaterThan(0);
            source.Height.Should().BeGreaterThan(0);
            source.Transparent.Should().BeTrue();
            source.Name.Should().Be(tempPath);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void FromFile_WithJpeg_ReturnsNonTransparentImageSource()
    {
        var jpgBytes = GetEmbeddedResourceBytes("test-image-01.jpg");
        var tempPath = Path.Combine(Path.GetTempPath(), $"ImagingImageSourceTest_{Guid.NewGuid()}.jpg");
        try
        {
            File.WriteAllBytes(tempPath, jpgBytes);
            ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();
            var source = ImageSource.FromFile(tempPath);

            source.Width.Should().BeGreaterThan(0);
            source.Height.Should().BeGreaterThan(0);
            source.Transparent.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    [Fact]
    public void FromFile_WithBmp_ReturnsNonTransparentImageSource()
    {
        var bmpBytes = GetEmbeddedResourceBytes("test-image-01.bmp");
        var tempPath = Path.Combine(Path.GetTempPath(), $"ImagingImageSourceTest_{Guid.NewGuid()}.bmp");
        try
        {
            File.WriteAllBytes(tempPath, bmpBytes);
            ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();
            var source = ImageSource.FromFile(tempPath);

            source.Width.Should().BeGreaterThan(0);
            source.Height.Should().BeGreaterThan(0);
            source.Transparent.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    // ── FromBinaryImpl ───────────────────────────────────────────────────

    [Fact]
    public void FromBinary_WithPng_LoadsImageCorrectly()
    {
        var pngBytes = GetEmbeddedResourceBytes("test-image-01.png");
        ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();

        var source = ImageSource.FromBinary("test-binary", () => pngBytes);

        source.Width.Should().BeGreaterThan(0);
        source.Height.Should().BeGreaterThan(0);
        source.Name.Should().Be("test-binary");
        source.Transparent.Should().BeTrue();
    }

    [Fact]
    public void FromBinary_WithJpeg_LoadsImageCorrectly()
    {
        var jpgBytes = GetEmbeddedResourceBytes("test-image-01.jpg");
        ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();

        var source = ImageSource.FromBinary("test-binary-jpg", () => jpgBytes);

        source.Width.Should().BeGreaterThan(0);
        source.Height.Should().BeGreaterThan(0);
        source.Transparent.Should().BeFalse();
    }

    // ── FromStreamImpl ───────────────────────────────────────────────────

    [Fact]
    public void FromStream_WithJpeg_LoadsImageCorrectly()
    {
        var jpgBytes = GetEmbeddedResourceBytes("test-image-01.jpg");
        ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();

        var source = ImageSource.FromStream("test-stream", () => new MemoryStream(jpgBytes));

        source.Width.Should().BeGreaterThan(0);
        source.Height.Should().BeGreaterThan(0);
        source.Name.Should().Be("test-stream");
        source.Transparent.Should().BeFalse();
    }

    [Fact]
    public void FromStream_WithPng_LoadsImageCorrectly()
    {
        var pngBytes = GetEmbeddedResourceBytes("test-image-01.png");
        ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();

        var source = ImageSource.FromStream("test-stream-png", () => new MemoryStream(pngBytes));

        source.Width.Should().BeGreaterThan(0);
        source.Height.Should().BeGreaterThan(0);
        source.Transparent.Should().BeTrue();
    }

    // ── SupportsTransparency per format ──────────────────────────────────

    [Fact]
    public void FromImagingImage_WebpFormat_IsTransparent()
    {
        using var image = new Image<Rgba32>(10, 10);
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, WebpFormat.Instance);
        source.Transparent.Should().BeTrue();
    }

    [Fact]
    public void FromImagingImage_GifFormat_IsTransparent()
    {
        using var image = new Image<Rgba32>(10, 10);
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, GifFormat.Instance);
        source.Transparent.Should().BeTrue();
    }

    [Fact]
    public void FromImagingImage_BmpFormat_IsNotTransparent()
    {
        using var image = new Image<Rgba32>(10, 10);
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, BmpFormat.Instance);
        source.Transparent.Should().BeFalse();
    }

    [Fact]
    public void FromImagingImage_JpegFormat_IsNotTransparent()
    {
        using var image = new Image<Rgba32>(10, 10);
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, JpegFormat.Instance);
        source.Transparent.Should().BeFalse();
    }

    // ── SaveAsJpeg ───────────────────────────────────────────────────────

    [Fact]
    public void SaveAsJpeg_ProducesValidJpegOutput()
    {
        using var image = new Image<Rgba32>(100, 80, new Rgba32(0, 128, 255, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, JpegFormat.Instance, quality: 75);

        using var ms = new MemoryStream();
        source.SaveAsJpeg(ms);

        ms.Length.Should().BeGreaterThan(0);
        ms.Position = 0;

        // Verify JPEG SOI marker (0xFF 0xD8)
        var header = new byte[2];
        ms.ReadExactly(header, 0, 2);
        header[0].Should().Be(0xFF);
        header[1].Should().Be(0xD8);
    }

    [Fact]
    public void SaveAsJpeg_RepeatedCalls_ProduceIdenticalOutput()
    {
        using var image = new Image<Rgba32>(50, 50, new Rgba32(100, 200, 50, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, JpegFormat.Instance, quality: 80);

        using var ms1 = new MemoryStream();
        source.SaveAsJpeg(ms1);
        var bytes1 = ms1.ToArray();

        using var ms2 = new MemoryStream();
        source.SaveAsJpeg(ms2);
        var bytes2 = ms2.ToArray();

        bytes1.Should().Equal(bytes2);
    }

    [Fact]
    public void SaveAsJpeg_DifferentQuality_ProducesDifferentSizedOutput()
    {
        // Create image with enough variation for JPEG quality to matter
        using var image1 = new Image<Rgba32>(200, 200, new Rgba32(100, 150, 200, 255));
        image1.Mutate(x => x.GaussianBlur(1));
        using var clone = image1.Clone();

        var sourceLow = ImagingImageSource<Rgba32>.FromImagingImage(image1, JpegFormat.Instance, quality: 10);
        var sourceHigh = ImagingImageSource<Rgba32>.FromImagingImage(clone, JpegFormat.Instance, quality: 100);

        using var msLow = new MemoryStream();
        sourceLow.SaveAsJpeg(msLow);

        using var msHigh = new MemoryStream();
        sourceHigh.SaveAsJpeg(msHigh);

        // Higher quality should produce a larger file
        msHigh.Length.Should().BeGreaterThan(msLow.Length);
    }

    [Fact]
    public void SaveAsJpeg_FromFileSource_ProducesValidJpeg()
    {
        var jpgBytes = GetEmbeddedResourceBytes("test-image-01.jpg");
        var tempPath = Path.Combine(Path.GetTempPath(), $"ImagingImageSourceTest_{Guid.NewGuid()}.jpg");
        try
        {
            File.WriteAllBytes(tempPath, jpgBytes);
            ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();
            var source = ImageSource.FromFile(tempPath);

            using var ms = new MemoryStream();
            source.SaveAsJpeg(ms);

            ms.Length.Should().BeGreaterThan(0);
            ms.Position = 0;
            var header = new byte[2];
            ms.ReadExactly(header, 0, 2);
            header[0].Should().Be(0xFF);
            header[1].Should().Be(0xD8);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    // ── SaveAsPdfBitmap ──────────────────────────────────────────────────

    [Fact]
    public void SaveAsPdfBitmap_ProducesValidBmpOutput()
    {
        using var image = new Image<Rgba32>(100, 80, new Rgba32(255, 0, 0, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, PngFormat.Instance, quality: 75);

        using var ms = new MemoryStream();
        source.SaveAsPdfBitmap(ms);

        ms.Length.Should().BeGreaterThan(0);
        ms.Position = 0;

        // Verify BMP file header signature ('BM')
        var header = new byte[2];
        ms.ReadExactly(header, 0, 2);
        header[0].Should().Be((byte)'B');
        header[1].Should().Be((byte)'M');
    }

    [Fact]
    public void SaveAsPdfBitmap_RepeatedCalls_ProduceIdenticalOutput()
    {
        using var image = new Image<Rgba32>(40, 40, new Rgba32(0, 255, 0, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, PngFormat.Instance, quality: 75);

        using var ms1 = new MemoryStream();
        source.SaveAsPdfBitmap(ms1);
        var bytes1 = ms1.ToArray();

        using var ms2 = new MemoryStream();
        source.SaveAsPdfBitmap(ms2);
        var bytes2 = ms2.ToArray();

        bytes1.Should().Equal(bytes2);
    }

    [Fact]
    public void SaveAsPdfBitmap_Is32bpp()
    {
        using var image = new Image<Rgba32>(10, 10, new Rgba32(128, 64, 32, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, PngFormat.Instance, quality: 75);

        using var ms = new MemoryStream();
        source.SaveAsPdfBitmap(ms);

        // BMP header: offset 28 = bits per pixel (2 bytes, little-endian)
        var data = ms.ToArray();
        var bitsPerPixel = BitConverter.ToUInt16(data, 28);
        bitsPerPixel.Should().Be(32);
    }

    [Fact]
    public void SaveAsPdfBitmap_FromFileSource_ProducesValidBmp()
    {
        var pngBytes = GetEmbeddedResourceBytes("test-image-01.png");
        var tempPath = Path.Combine(Path.GetTempPath(), $"ImagingImageSourceTest_{Guid.NewGuid()}.png");
        try
        {
            File.WriteAllBytes(tempPath, pngBytes);
            ImageSource.ImageSourceImpl = new ImagingImageSource<Rgba32>();
            var source = ImageSource.FromFile(tempPath);

            using var ms = new MemoryStream();
            source.SaveAsPdfBitmap(ms);

            ms.Length.Should().BeGreaterThan(0);
            ms.Position = 0;
            var header = new byte[2];
            ms.ReadExactly(header, 0, 2);
            header[0].Should().Be((byte)'B');
            header[1].Should().Be((byte)'M');
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    // ── Mixed SaveAsJpeg + SaveAsPdfBitmap on same instance ──────────────

    [Fact]
    public void SameSource_CanSaveAsJpegAndBitmap()
    {
        using var image = new Image<Rgba32>(60, 40, new Rgba32(200, 100, 50, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, JpegFormat.Instance, quality: 75);

        using var msJpeg = new MemoryStream();
        source.SaveAsJpeg(msJpeg);
        msJpeg.Length.Should().BeGreaterThan(0);

        using var msBmp = new MemoryStream();
        source.SaveAsPdfBitmap(msBmp);
        msBmp.Length.Should().BeGreaterThan(0);

        // Verify each has correct header
        var jpegBytes = msJpeg.ToArray();
        jpegBytes[0].Should().Be(0xFF);
        jpegBytes[1].Should().Be(0xD8);

        var bmpBytes = msBmp.ToArray();
        bmpBytes[0].Should().Be((byte)'B');
        bmpBytes[1].Should().Be((byte)'M');
    }

    // ── Interleaved repeated calls (regression guard for encoder caching) ─

    [Fact]
    public void InterleavedSaveCalls_ProduceConsistentResults()
    {
        using var image = new Image<Rgba32>(30, 30, new Rgba32(50, 100, 150, 255));
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, PngFormat.Instance, quality: 75);

        // First round
        using var msJpeg1 = new MemoryStream();
        source.SaveAsJpeg(msJpeg1);
        var jpegBytes1 = msJpeg1.ToArray();

        using var msBmp1 = new MemoryStream();
        source.SaveAsPdfBitmap(msBmp1);
        var bmpBytes1 = msBmp1.ToArray();

        // Second round (interleaved order)
        using var msBmp2 = new MemoryStream();
        source.SaveAsPdfBitmap(msBmp2);
        var bmpBytes2 = msBmp2.ToArray();

        using var msJpeg2 = new MemoryStream();
        source.SaveAsJpeg(msJpeg2);
        var jpegBytes2 = msJpeg2.ToArray();

        jpegBytes1.Should().Equal(jpegBytes2);
        bmpBytes1.Should().Equal(bmpBytes2);
    }

    // ── Dispose ──────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_DisposesUnderlyingImage()
    {
        var image = new Image<Rgba32>(10, 10);
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, PngFormat.Instance);

        source.Dispose();

        // After disposal, SaveAsJpeg should throw because the underlying image is disposed
        Assert.ThrowsAny<Exception>(() =>
        {
            using var ms = new MemoryStream();
            source.SaveAsJpeg(ms);
        });
    }

    [Fact]
    public void Dispose_SaveAsPdfBitmap_ThrowsAfterDispose()
    {
        var image = new Image<Rgba32>(10, 10);
        var source = ImagingImageSource<Rgba32>.FromImagingImage(image, PngFormat.Instance);

        source.Dispose();

        Assert.ThrowsAny<Exception>(() =>
        {
            using var ms = new MemoryStream();
            source.SaveAsPdfBitmap(ms);
        });
    }
}
