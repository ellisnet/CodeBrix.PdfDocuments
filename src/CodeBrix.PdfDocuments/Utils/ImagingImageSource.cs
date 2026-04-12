using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats;
using CodeBrix.Imaging.Formats.Bmp;
using CodeBrix.Imaging.Formats.Gif;
using CodeBrix.Imaging.Formats.Jpeg;
using CodeBrix.Imaging.Formats.Png;
using CodeBrix.Imaging.Formats.Webp;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocuments.Drawing;
using System;
using System.IO;

namespace CodeBrix.PdfDocuments.Utils; //Was previously: namespace PdfSharpCore.Utils;

public class ImagingImageSource<TPixel> : ImageSource where TPixel : unmanaged, IPixel<TPixel>
{
    public static IImageSource FromImagingImage(Image<TPixel> image, IImageFormat imgFormat, int? quality = DefaultQuality)
    {
        var path = "*" + Guid.NewGuid().ToString("B");
        return new ImagingImageSourceImpl<TPixel>(path, image, quality, SupportsTransparency(imgFormat));
    }

    protected override IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, int? quality = DefaultQuality)
    {
        var image = Image.Load<TPixel>(imageSource.Invoke(), out IImageFormat imgFormat);
        return new ImagingImageSourceImpl<TPixel>(name, image, quality, SupportsTransparency(imgFormat));
    }

    protected override IImageSource FromFileImpl(string path, int? quality = DefaultQuality)
    {
        var image = Image.Load<TPixel>(path, out IImageFormat imgFormat);
        return new ImagingImageSourceImpl<TPixel>(path, image, quality, SupportsTransparency(imgFormat));
    }

    protected override IImageSource FromStreamImpl(string name, Func<Stream> imageStream, int? quality = DefaultQuality)
    {
        using var stream = imageStream.Invoke();
        var image = Image.Load<TPixel>(stream, out IImageFormat imgFormat);
        return new ImagingImageSourceImpl<TPixel>(name, image, quality, SupportsTransparency(imgFormat));
    }

    private static bool SupportsTransparency(IImageFormat format)
        => format is PngFormat or WebpFormat or GifFormat;

    private class ImagingImageSourceImpl<TPixel2> : IImageSource where TPixel2 : unmanaged, IPixel<TPixel2>
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once StaticMemberInGenericType
        private static readonly BmpEncoder _pdfBitmapEncoder = new() { BitsPerPixel = BmpBitsPerPixel.Pixel32 };

        private Image<TPixel2> Image { get; }
        private readonly JpegEncoder _jpegEncoder;

        public int Width => Image.Width;
        public int Height => Image.Height;
        public string Name { get; }
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public bool Transparent { get; internal set; }

        public ImagingImageSourceImpl(string name, Image<TPixel2> image, int? quality, bool isTransparent)
        {
            Name = name;
            Image = image;
            _jpegEncoder = new JpegEncoder { Quality = quality };
            Transparent = isTransparent;
        }

        public void SaveAsJpeg(MemoryStream ms) => Image.SaveAsJpeg(ms, _jpegEncoder);

        public void Dispose() => Image.Dispose();

        public void SaveAsPdfBitmap(MemoryStream ms) => Image.Save(ms, _pdfBitmapEncoder);
    }
}
