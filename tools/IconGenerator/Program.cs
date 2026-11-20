using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

internal static class Program
{
    private sealed record Shape(string Data, string? Fill, string? Stroke = null, double Width = 2, double Opacity = 1);

    private static readonly string[] Names = { "Solid", "NotSolid", "Unobscured", "Obscured", "DwgOpen", "DwgReload", "About", "ExcelExport", "ExcelImport" };
    private static readonly string[] Labels = { "Solid", "Not Solid", "Unobscured", "Obscured", "Open DWG", "Reload DWG", "About", "Eksport XLSX", "Import XLSX" };
    private const string BarOutline = "M8,3 C5.2,3 3,5.2 3,8 L3,23 C3,26.3 5.7,29 9,29 L24,29 C26.8,29 29,26.8 29,24 C29,21.2 26.8,19 24,19 L13,19 L13,8 C13,5.2 10.8,3 8,3 Z";
    private const string DocumentOutline = "M7,3 H20 L27,10 V27 Q27,29 25,29 H7 Q5,29 5,27 V5 Q5,3 7,3 Z";

    [STAThread]
    private static void Main(string[] args)
    {
        var root = Path.GetFullPath(args[0]);
        foreach (var dark in new[] { false, true })
        {
            var theme = dark ? "Dark" : "Light";
            var output = Path.Combine(root, "Ico", theme);
            var sources = Path.Combine(root, "Ico", "Source", theme);
            Directory.CreateDirectory(output);
            Directory.CreateDirectory(sources);
            foreach (var name in Names)
            {
                var shapes = GetShapes(name);
                SaveSvg(Path.Combine(sources, name + ".svg"), shapes, dark);
                foreach (var size in new[] { 16, 32 })
                    SavePng(Path.Combine(output, name + size + ".png"), RenderIcon(shapes, dark, size));
            }
        }
        var preview = Path.Combine(root, ".tools", "ribbon-icons-preview.png");
        Directory.CreateDirectory(Path.GetDirectoryName(preview)!);
        SavePreview(root, preview);
        Console.WriteLine("Generated " + Names.Length * 4 + " transparent PNG icons, " + Names.Length * 2 + " SVG sources and " + preview);
    }

    private static List<Shape> GetShapes(string name)
    {
        switch (name)
        {
            case "Solid":
                return new() { new(BarOutline, "accent") };
            case "NotSolid":
                return new() { new(BarOutline, null, "outline") };
            case "Unobscured":
                return new()
                {
                    new("M9,3 H23 V29 H9 Z", null, "outline"),
                    new("M3,26 L28,7", null, "accent", 3.2)
                };
            case "Obscured":
                var obscured = new List<Shape>
                {
                    new("M9,3 H23 V29 H9 Z", "outline", null, 2, 0.08),
                    new("M9,3 H23 V29 H9 Z", null, "outline"),
                    new(Segment(0, 0.24), null, "accent", 3.2),
                    new(Segment(0.80, 1), null, "accent", 3.2)
                };
                foreach (var t in new[] { 0.30, 0.49, 0.68 })
                    obscured.Add(new(Segment(t, t + 0.05), null, "accent", 2.4, 0.55));
                return obscured;
            case "DwgOpen":
            case "ExcelExport":
                return new()
                {
                    new(DocumentOutline, null, "outline"),
                    new("M20,3 V10 H27", null, "outline"),
                    new("M11,22 L21,12 M14,12 H21 V19", null, "accent", 2.8)
                };
            case "ExcelImport":
                return new()
                {
                    new(DocumentOutline, null, "outline"),
                    new("M20,3 V10 H27", null, "outline"),
                    new("M21,13 L11,23 M11,16 V23 H18", null, "accent", 2.8)
                };
            case "DwgReload":
                return new()
                {
                    new(DocumentOutline, null, "outline"),
                    new("M20,3 V10 H27", null, "outline"),
                    new("M22,16 C19,12 13,12 10,16 C7,20 10,26 15,26 C18,26 20,24 21,22 M22,11 V16 H17", null, "accent", 2.8)
                };
            case "About":
                return new()
                {
                    new("M16,3.5 A12.5,12.5 0 1 1 16,28.5 A12.5,12.5 0 1 1 16,3.5 Z", null, "outline", 2.5),
                    new("M16,7.3 A1.7,1.7 0 1 1 16,10.7 A1.7,1.7 0 1 1 16,7.3 Z", "accent"),
                    new("M16,14 L16,23", null, "accent", 3.2)
                };
            default:
                throw new ArgumentOutOfRangeException(nameof(name));
        }
    }

    private static string Segment(double start, double end) => FormattableString.Invariant(
        $"M{3 + 25 * start},{26 - 19 * start} L{3 + 25 * end},{26 - 19 * end}");

    private static string ColorValue(string key, bool dark) => key == "accent"
        ? (dark ? "#FFC12B" : "#C87800")
        : (dark ? "#ECECEC" : "#282828");

    private static Brush? BrushFor(string? key, bool dark) => key == null ? null
        : new SolidColorBrush((Color)ColorConverter.ConvertFromString(ColorValue(key, dark)));

    private static BitmapSource RenderIcon(IEnumerable<Shape> shapes, bool dark, int size)
    {
        var visual = new DrawingVisual();
        using (var drawing = visual.RenderOpen())
        {
            drawing.PushTransform(new ScaleTransform(size / 32.0, size / 32.0));
            foreach (var shape in shapes)
            {
                Pen? pen = shape.Stroke == null ? null : new Pen(BrushFor(shape.Stroke, dark), shape.Width)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round,
                    LineJoin = PenLineJoin.Round
                };
                drawing.PushOpacity(shape.Opacity);
                drawing.DrawGeometry(BrushFor(shape.Fill, dark), pen, Geometry.Parse(shape.Data));
                drawing.Pop();
            }
            drawing.Pop();
        }
        var image = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
        image.Render(visual);
        image.Freeze();
        return image;
    }

    private static void SaveSvg(string path, IEnumerable<Shape> shapes, bool dark)
    {
        XNamespace svg = "http://www.w3.org/2000/svg";
        var root = new XElement(svg + "svg", new XAttribute("viewBox", "0 0 32 32"),
            new XAttribute("width", 32), new XAttribute("height", 32));
        foreach (var shape in shapes)
        {
            root.Add(new XElement(svg + "path", new XAttribute("d", shape.Data),
                new XAttribute("fill", shape.Fill == null ? "none" : ColorValue(shape.Fill, dark)),
                new XAttribute("stroke", shape.Stroke == null ? "none" : ColorValue(shape.Stroke, dark)),
                new XAttribute("stroke-width", shape.Width), new XAttribute("stroke-linecap", "round"),
                new XAttribute("stroke-linejoin", "round"), new XAttribute("opacity", shape.Opacity)));
        }
        new XDocument(root).Save(path);
    }

    private static void SavePng(string path, BitmapSource bitmap)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var output = File.Create(path);
        encoder.Save(output);
    }

    private static void SavePreview(string root, string path)
    {
        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.NearestNeighbor);
        using (var drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, Names.Length * 170 + 40, 555));
            Text(drawing, "AmberBIM — ikony 16 / 32 px", 24, 15, 24, Brushes.Black);
            for (var row = 0; row < 2; row++)
            {
                var dark = row == 1;
                var y = 65 + row * 240;
                var foreground = dark ? Brushes.WhiteSmoke : Brushes.Black;
                drawing.DrawRoundedRectangle(new SolidColorBrush((Color)ColorConverter.ConvertFromString(dark ? "#2B2B2B" : "#F5F5F5")),
                    null, new Rect(10, y, Names.Length * 170 + 20, 225), 12, 12);
                Text(drawing, dark ? "DARK" : "LIGHT", 24, y + 10, 14, foreground);
                for (var i = 0; i < Names.Length; i++)
                {
                    var x = 32 + i * 170;
                    var theme = dark ? "Dark" : "Light";
                    var large = new BitmapImage(new Uri(Path.Combine(root, "Ico", theme, Names[i] + "32.png")));
                    var small = new BitmapImage(new Uri(Path.Combine(root, "Ico", theme, Names[i] + "16.png")));
                    drawing.DrawImage(large, new Rect(x + 38, y + 43, 64, 64));
                    drawing.DrawImage(small, new Rect(x + 42, y + 143, 16, 16));
                    drawing.DrawImage(large, new Rect(x + 75, y + 135, 32, 32));
                    Text(drawing, Labels[i], x + 16, y + 185, 15, foreground);
                }
            }
        }
        var preview = new RenderTargetBitmap(Names.Length * 170 + 40, 555, 96, 96, PixelFormats.Pbgra32);
        preview.Render(visual);
        SavePng(path, preview);
    }

    private static void Text(DrawingContext drawing, string text, double x, double y, double size, Brush brush)
    {
        var formatted = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, brush, 1.0);
        drawing.DrawText(formatted, new Point(x, y));
    }
}
