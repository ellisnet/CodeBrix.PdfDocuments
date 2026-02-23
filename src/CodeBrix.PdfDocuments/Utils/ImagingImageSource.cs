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
    public static IImageSource FromImagingImage(Image<TPixel> image, IImageFormat imgFormat, int? quality = 75)
    {
        var _path = "*" + Guid.NewGuid().ToString("B");
        return new ImagingImageSourceImpl<TPixel>(_path, image, (int)quality, SupportsTransparency(imgFormat));
    }

    protected override IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, int? quality = 75)
    {
        var image = Image.Load<TPixel>(imageSource.Invoke(), out IImageFormat imgFormat);
        return new ImagingImageSourceImpl<TPixel>(name, image, (int)quality, SupportsTransparency(imgFormat));
    }

    protected override IImageSource FromFileImpl(string path, int? quality = 75)
    {
        var image = Image.Load<TPixel>(path, out IImageFormat imgFormat);
        return new ImagingImageSourceImpl<TPixel>(path, image, (int) quality, SupportsTransparency(imgFormat));
    }

    protected override IImageSource FromStreamImpl(string name, Func<Stream> imageStream, int? quality = 75)
    {
        using (var stream = imageStream.Invoke())
        {
            var image = Image.Load<TPixel>(stream, out IImageFormat imgFormat);
            return new ImagingImageSourceImpl<TPixel>(name, image, (int)quality, SupportsTransparency(imgFormat));
        }
    }

    private static bool SupportsTransparency(IImageFormat format)
        => format is PngFormat or WebpFormat or GifFormat;

    private class ImagingImageSourceImpl<TPixel2> : IImageSource where TPixel2 : unmanaged, IPixel<TPixel2>
    {
        private Image<TPixel2> Image { get; }
        private readonly int _quality;

        public int Width => Image.Width;
        public int Height => Image.Height;
        public string Name { get; }
        public bool Transparent { get; internal set; }

        public ImagingImageSourceImpl(string name, Image<TPixel2> image, int quality, bool isTransparent)
        {
            Name = name;
            Image = image;
            _quality = quality;
            Transparent = isTransparent;
        }

        public void SaveAsJpeg(MemoryStream ms)
        {
            Image.SaveAsJpeg(ms, new JpegEncoder() { Quality = this._quality });
        }

        public void Dispose()
        {
            Image.Dispose();
        }
        public void SaveAsPdfBitmap(MemoryStream ms)
        {
            BmpEncoder bmp = new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32 };
            Image.Save(ms, bmp);
        }
    }
}