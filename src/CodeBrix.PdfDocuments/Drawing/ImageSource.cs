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

    /// <summary>
    /// An image source that draws itself as vector content instead of supplying pixels. The
    /// document object model lays it out like any other image - its natural size is
    /// <see cref="WidthPoints"/> by <see cref="HeightPoints"/> - and at render time the image
    /// renderer asks it to draw into the page's <see cref="XGraphics"/> at the placed rectangle,
    /// so nothing is embedded as a bitmap. The pixel-oriented members inherited from
    /// <see cref="IImageSource"/> are never called for a vector source; an implementation may
    /// throw <see cref="System.NotSupportedException"/> from them.
    /// </summary>
    public interface IVectorImageSource : IImageSource
    {
        /// <summary>The natural width of the drawing, in points.</summary>
        double WidthPoints { get; }

        /// <summary>The natural height of the drawing, in points.</summary>
        double HeightPoints { get; }

        /// <summary>
        /// Draws the content so that its natural bounds fill <paramref name="destination"/>
        /// (top-left origin, in the graphics object's units). The caller saves and restores the
        /// graphics state around the call, so the implementation may change the transform and
        /// clip freely.
        /// </summary>
        void Draw(XGraphics graphics, XRect destination);
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