using System;
using System.IO;

namespace CodeBrix.PdfDocuments.Drawing;

public abstract class ImageSource
{
    public const int DefaultQuality = 75;

    /// <summary>
    /// Gets or sets the image source implementation to use for reading images.
    /// </summary>
    /// <value>The image source impl.</value>
    public static ImageSource ImageSourceImpl { get; set; }

    public interface IImageSource : IDisposable
    {
        int Width { get; }
        int Height { get; }
        string Name { get; }
        void SaveAsJpeg(MemoryStream ms);
        bool Transparent { get; }
        void SaveAsPdfBitmap(MemoryStream ms);
    }

    protected abstract IImageSource FromFileImpl(string path, int? quality = DefaultQuality);
    protected abstract IImageSource FromBinaryImpl(string name, Func<byte[]> imageSource, int? quality = DefaultQuality);
    protected abstract IImageSource FromStreamImpl(string name, Func<Stream> imageStream, int? quality = DefaultQuality);


    public static IImageSource FromFile(string path, int? quality = DefaultQuality) => 
        ImageSourceImpl.FromFileImpl(path, quality);

    public static IImageSource FromBinary(string name, Func<byte[]> imageSource, int? quality = DefaultQuality) => 
        ImageSourceImpl.FromBinaryImpl(name, imageSource, quality);

    public static IImageSource FromStream(string name, Func<Stream> imageStream, int? quality = DefaultQuality) => 
        ImageSourceImpl.FromStreamImpl(name, imageStream, quality);
}