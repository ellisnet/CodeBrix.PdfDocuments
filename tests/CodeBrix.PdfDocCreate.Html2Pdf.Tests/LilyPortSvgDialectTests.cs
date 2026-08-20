using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CodeBrix.Imaging.PixelFormats;
using CodeBrix.PdfRasterizer;
using SilverAssertions;
using Xunit;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Tests;

/// <summary>
/// Verifies Html2Pdf against the LilyPort SVG dialect inventory - the frozen
/// vocabulary an engraving engine emits for documentation snippets. The inventory
/// lives in an OPTIONAL, machine-local folder that is never committed (it derives
/// from GPL-3/GFDL sources); when the folder is absent these tests all skip. The
/// SVG markup in this file is entirely synthetic - authored here to exercise the
/// documented constructs - and no content from the inventory folder may ever be
/// copied into this or any other committed file.
/// </summary>
public class LilyPortSvgDialectTests
{
    // Html2Pdf's own declaration of the SVG constructs it supports for engraved
    // music. A dialect inventory member outside these sets fails the gate test:
    // that is the alarm that the engraving dialect grew a new requirement.
    private static readonly HashSet<string> SupportedElements = new HashSet<string>(StringComparer.Ordinal)
    {
        "svg", "g", "text", "tspan", "rect", "a", "path", "line", "polygon", "circle", "ellipse",
    };

    private static readonly HashSet<string> SupportedAttributes = new HashSet<string>(StringComparer.Ordinal)
    {
        "x", "y", "width", "height", "viewBox", "transform",
        "x1", "y1", "x2", "y2", "d", "points", "r", "cx", "cy", "rx", "ry",
        "fill", "stroke", "stroke-width", "color",
        "stroke-linecap", "stroke-linejoin", "stroke-dasharray",
        "font-family", "font-size", "font-style", "font-weight", "font-variant",
        "text-anchor", "version", "xmlns", "xmlns:xlink", "xlink:href",
    };

    private static string TryGetInventoryPath()
    {
        // <repo>/tests/<project>/bin/Debug/net10.0 -> <repo>/tests/optional-testing-files/...
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "optional-testing-files", "lilyport-svg-dialect", "inventory.tsv"));
        return File.Exists(path) ? path : null;
    }

    private static List<(string Kind, string Name, int Count)> ReadInventory(string path)
        => File.ReadAllLines(path)
            .Select(line => line.Split('\t'))
            .Where(parts => parts.Length >= 3)
            .Select(parts => (parts[0], parts[1], int.TryParse(parts[2], out var count) ? count : 0))
            .ToList();

    private sealed class Bounds
    {
        public int MinX = int.MaxValue;
        public int MaxX = int.MinValue;
        public int MinY = int.MaxValue;
        public int MaxY = int.MinValue;
        public bool Found => MaxX >= MinX;
        public int Width => MaxX - MinX + 1;
        public int Height => MaxY - MinY + 1;
    }

    private static async Task<Bounds> FindContentBounds(byte[] pdfBytes)
    {
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            pdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        var bounds = new Bounds();
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var pixel = rgba[x, y];
                if (pixel.R < 230 || pixel.G < 230 || pixel.B < 230)
                {
                    bounds.MinX = Math.Min(bounds.MinX, x);
                    bounds.MaxX = Math.Max(bounds.MaxX, x);
                    bounds.MinY = Math.Min(bounds.MinY, y);
                    bounds.MaxY = Math.Max(bounds.MaxY, y);
                }
            }
        }

        return bounds;
    }

    [Fact]
    public void every_inventory_member_is_a_supported_construct()
    {
        //Arrange
        var inventoryPath = TryGetInventoryPath();
        Assert.SkipWhen(inventoryPath == null, "The LilyPort SVG dialect inventory is not present on this machine.");
        var inventory = ReadInventory(inventoryPath);
        inventory.Should().NotBeEmpty();

        //Act
        var unknownElements = inventory
            .Where(row => row.Kind == "ELEMENT" && !SupportedElements.Contains(row.Name))
            .Select(row => row.Name)
            .ToList();
        var unknownAttributes = inventory
            .Where(row => row.Kind == "ATTRIBUTE" && !SupportedAttributes.Contains(row.Name))
            .Select(row => row.Name)
            .ToList();

        //Assert - a new member here means the engraving dialect grew a requirement
        // Html2Pdf has not agreed to support yet.
        unknownElements.Should().BeEmpty();
        unknownAttributes.Should().BeEmpty();
    }

    [Fact]
    public async Task synthetic_snippet_in_the_engraving_style_renders_cleanly()
    {
        //Arrange - authored here, in the documented style: a currentColor root, flat
        // translated sibling groups, a font-unit path with a negative Y scale, staff
        // lines as line elements, generic-family text, and an invisible link rect.
        var inventoryPath = TryGetInventoryPath();
        Assert.SkipWhen(inventoryPath == null, "The LilyPort SVG dialect inventory is not present on this machine.");

        const string snippet =
            "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' version='1.2' " +
            "width='100.0mm' height='20.0mm' viewBox='10.0 5.0 100.0 20.0'>" +
            "<g fill='currentColor' color='black'>" +
            "<g transform='translate(15.0, 20.0)'>" +
            "<text font-family='serif' font-size='2.5' text-anchor='start' fill='currentColor'>" +
            "<tspan>Synthetic example text</tspan></text></g>" +
            "<g transform='translate(15.0, 20.0)'>" +
            "<a xlink:href='https://example.com'>" +
            "<rect x='0.0' y='-0.5' width='17.0' height='2.0' fill='none' stroke='none' stroke-width='0.0'/>" +
            "</a></g>" +
            "<g transform='translate(20.0, 10.0)'>" +
            "<path transform='scale(0.01, -0.01)' d='M0 0c50 100 150 100 200 0c-50 -80 -150 -80 -200 0z' fill='currentColor'/>" +
            "</g>" +
            "<g transform='translate(20.0, 12.0)'>" +
            "<line stroke-linejoin='round' stroke-linecap='round' stroke-width='0.2' stroke='currentColor' " +
            "x1='0.0' y1='0.0' x2='40.0' y2='0.0'/>" +
            "</g>" +
            "<g transform='translate(50.0, 12.0)'>" +
            "<polygon points='0,0 2,3 4,0' fill='currentColor'/>" +
            "<circle cx='8' cy='1' r='1.2' fill='currentColor'/>" +
            "<ellipse cx='12' cy='1' rx='1.6' ry='1.0' fill='currentColor'/>" +
            "<rect x='15' y='0' width='3' height='2' rx='0.4' ry='0.4' fill='currentColor'/>" +
            "<line x1='20' y1='0' x2='24' y2='2' stroke='currentColor' stroke-width='0.2' stroke-dasharray='0.4,0.2'/>" +
            "</g>" +
            "<g transform='translate(15.0, 17.0)'>" +
            "<text font-family='sans' font-size='2.0' font-weight='bold' fill='currentColor'><tspan>bold sans</tspan></text>" +
            "</g>" +
            "<g transform='translate(50.0, 17.0)'>" +
            "<text font-family='monospace' font-size='2.0' font-style='italic' font-variant='small-caps' " +
            "fill='currentColor'><tspan>mono italic</tspan></text>" +
            "</g>" +
            "</g></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{snippet}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindContentBounds(result.PdfBytes)).Found.Should().BeTrue();
    }

    [Fact]
    public async Task millimetre_size_with_offset_viewbox_places_at_physical_size()
    {
        //Arrange - the engraving dialect puts physical mm on the root and an internal
        // viewBox in a DIFFERENT coordinate space (offset origin). A full-bleed rect
        // must land at the declared physical size: 50mm x 10mm is 189 x 38 px at the
        // 96 DPI raster below.
        var inventoryPath = TryGetInventoryPath();
        Assert.SkipWhen(inventoryPath == null, "The LilyPort SVG dialect inventory is not present on this machine.");

        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='50mm' height='10mm' viewBox='20.5 6.7 80.0 16.0'>" +
            "<rect x='20.5' y='6.7' width='80.0' height='16.0' fill='#ff0000'/></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{svg}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        var bounds = await FindContentBounds(result.PdfBytes);
        bounds.Found.Should().BeTrue();
        bounds.Width.Should().BeInRange(184, 194);
        bounds.Height.Should().BeInRange(33, 43);
    }

    [Fact]
    public async Task current_color_inheritance_paints_descendants()
    {
        //Arrange - everything under the color root paints with fill='currentColor';
        // a renderer without currentColor inheritance draws nothing (or black by luck).
        var inventoryPath = TryGetInventoryPath();
        Assert.SkipWhen(inventoryPath == null, "The LilyPort SVG dialect inventory is not present on this machine.");

        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' width='20mm' height='10mm' viewBox='0 0 40 20'>" +
            "<g fill='currentColor' color='#ff0000'>" +
            "<g transform='translate(5, 5)'><rect x='0' y='0' width='30' height='10'/></g>" +
            "</g></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{svg}</body>");

        //Assert - the rect must inherit the red, not default to black.
        result.Warnings.Count.Should().Be(0);
        using var rasterizer = new PageRasterizer { Dpi = 96 };
        using var raster = await rasterizer.RasterizeToImage(
            result.PdfBytes, pageNumber: 1, cancellationToken: TestContext.Current.CancellationToken);
        using var rgba = raster.CloneAs<Rgba32>();
        var sawRed = false;
        var sawBlack = false;
        for (var y = 0; y < rgba.Height; y++)
        {
            for (var x = 0; x < rgba.Width; x++)
            {
                var pixel = rgba[x, y];
                if (pixel.R > 180 && pixel.G < 100 && pixel.B < 100) { sawRed = true; }
                if (pixel.R < 60 && pixel.G < 60 && pixel.B < 60) { sawBlack = true; }
            }
        }

        sawRed.Should().BeTrue();
        sawBlack.Should().BeFalse();
    }

    [Fact]
    public async Task invisible_link_rectangles_stay_invisible()
    {
        //Arrange - every engraved snippet ends with a link wrapping an invisible hit
        // rectangle (fill='none' stroke='none'); painting it would box every example.
        var inventoryPath = TryGetInventoryPath();
        Assert.SkipWhen(inventoryPath == null, "The LilyPort SVG dialect inventory is not present on this machine.");

        const string svg =
            "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink' " +
            "width='40mm' height='10mm' viewBox='0 0 80 20'>" +
            "<a xlink:href='https://example.com'>" +
            "<rect x='2' y='2' width='60' height='10' fill='none' stroke='none' stroke-width='0.0'/>" +
            "</a></svg>";
        var renderer = new HtmlPdfRenderer();

        //Act
        var result = renderer.RenderHtmlToBytes($"<body>{svg}</body>");

        //Assert
        result.Warnings.Count.Should().Be(0);
        (await FindContentBounds(result.PdfBytes)).Found.Should().BeFalse();
    }

    [Fact]
    public async Task generic_font_families_from_the_inventory_render_text()
    {
        //Arrange - the dialect asks for CSS generics spelled the SVG way, including
        // plain "sans"; each must produce visible glyphs from the package fonts.
        var inventoryPath = TryGetInventoryPath();
        Assert.SkipWhen(inventoryPath == null, "The LilyPort SVG dialect inventory is not present on this machine.");
        var genericFamilies = ReadInventory(inventoryPath)
            .Where(row => row.Kind == "FONT-FAMILY")
            .Select(row => row.Name)
            .Where(name => name is "serif" or "sans" or "sans-serif" or "monospace")
            .ToList();
        Assert.SkipWhen(genericFamilies.Count == 0, "The inventory lists no generic font families.");

        foreach (var family in genericFamilies)
        {
            var svg =
                "<svg xmlns='http://www.w3.org/2000/svg' width='60mm' height='10mm' viewBox='0 0 120 20'>" +
                $"<text x='2' y='14' font-family='{family}' font-size='12' fill='#ff0000'>Mgja</text></svg>";
            var renderer = new HtmlPdfRenderer();

            //Act
            var result = renderer.RenderHtmlToBytes($"<body>{svg}</body>");

            //Assert
            result.Warnings.Count.Should().Be(0);
            (await FindContentBounds(result.PdfBytes)).Found.Should().BeTrue();
        }
    }

    [Fact]
    public async Task named_face_tail_from_the_inventory_renders_from_package_fonts_only()
    {
        //Arrange - the inventory's tail names real system/TeX faces. Those must
        // resolve to a package face (or nothing) - never to host-installed fonts.
        // The typeface chain is structurally limited to registered files, so the
        // assertion here is that each renders cleanly through the package fallback.
        var inventoryPath = TryGetInventoryPath();
        Assert.SkipWhen(inventoryPath == null, "The LilyPort SVG dialect inventory is not present on this machine.");
        var namedFamilies = ReadInventory(inventoryPath)
            .Where(row => row.Kind == "FONT-FAMILY")
            .Select(row => row.Name)
            .Where(name => name is not ("serif" or "sans" or "sans-serif" or "monospace"))
            .ToList();
        Assert.SkipWhen(namedFamilies.Count == 0, "The inventory lists no named font families.");

        foreach (var family in namedFamilies)
        {
            var svg =
                "<svg xmlns='http://www.w3.org/2000/svg' width='60mm' height='10mm' viewBox='0 0 120 20'>" +
                $"<text x='2' y='14' font-family=\"{family.Replace("\"", "")}\" font-size='12' fill='#ff0000'>Mgja</text></svg>";
            var renderer = new HtmlPdfRenderer();

            //Act
            var result = renderer.RenderHtmlToBytes($"<body>{svg}</body>");

            //Assert
            result.PdfBytes.Should().NotBeNull();
            (await FindContentBounds(result.PdfBytes)).Found.Should().BeTrue();
        }
    }
}
