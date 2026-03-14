using CodeBrix.Imaging;
using CodeBrix.Imaging.Formats.Png;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfDocuments.Pdf;
using CodeBrix.PdfRasterizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CodeBrix.PdfDocuments.Tests.Helpers; //Was previously: namespace PdfSharpCore.Test.Helpers;

public class PdfHelper
{
    private static readonly string _rootPath = PathHelper.GetInstance().RootDir;
    private static readonly PageRasterizer _rasterizer = new();

    /// <summary>
    ///   Rasterize all pages within a PDF to PNG images using PDFium.
    /// </summary>
    public static async Task<IList<Image>> Rasterize(PdfDocument document)
    {
        return await _rasterizer.RasterizeToImages(document, dpi: 300);
    }

    public static async Task<List<string>> WriteImageCollection(IList<Image> images, string outDir, string filePrefix)
    {
        var outPaths = new List<string>();
        for (var pageNum = 0; pageNum < images.Count; pageNum++)
        {
            var outPath = GetOutFilePath(outDir, $"{filePrefix}_{pageNum+1}.png");
            await using var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write);
            await images[pageNum].SaveAsync(fs, PngFormat.Instance, CancellationToken.None);
            outPaths.Add(outPath);
        }

        return outPaths;
    }

    public static async Task<string> WriteImage(Image image, string outDir, string fileNameWithoutExtension)
    {
        var outPath = GetOutFilePath(outDir, $"{fileNameWithoutExtension}.png");
        await using var fs = new FileStream(outPath, FileMode.Create, FileAccess.Write);
        await image.SaveAsync(fs, PngFormat.Instance, CancellationToken.None);
        return outPath;
    }

    // Note: For diff to function properly, it requires the underlying image to be in the proper format
    //   For instance, actual and expected must both be sourced from .png files
    public static DiffOutput Diff(string actualImagePath, string expectedImagePath, string outputPath = null, string filePrefix = null, int fuzzPct = 4)
    {
        using var actual = Image.Load<Rgba32>(actualImagePath);
        using var expected = Image.Load<Rgba32>(expectedImagePath);

        if (actual.Width != expected.Width || actual.Height != expected.Height)
        {
            return new DiffOutput
            {
                DiffValue = double.MaxValue
            };
        }

        var diffCount = 0.0;
        var fuzzThreshold = (int)(255 * fuzzPct / 100.0);

        // Allow for subtle differences due to cross-platform rendering of the PDF fonts
        for (var y = 0; y < actual.Height; y++)
        {
            for (var x = 0; x < actual.Width; x++)
            {
                var ap = actual[x, y];
                var ep = expected[x, y];
                if (Math.Abs(ap.R - ep.R) > fuzzThreshold ||
                    Math.Abs(ap.G - ep.G) > fuzzThreshold ||
                    Math.Abs(ap.B - ep.B) > fuzzThreshold ||
                    Math.Abs(ap.A - ep.A) > fuzzThreshold)
                {
                    diffCount++;
                }
            }
        }

        return new DiffOutput
        {
            DiffValue = diffCount
        };
    }

    private static string GetOutFilePath(string outDir, string name)
    {
        var dir = Path.Combine(_rootPath, outDir);
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, name);
    }
}

public class DiffOutput
{
    public double DiffValue;
}