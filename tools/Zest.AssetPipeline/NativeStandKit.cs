public static class NativeStandKit
{
    private const int Width = 170;
    private const int Height = 136;
    private const string SheetFileName = "prop_zest_stand_upgrades_4x1_v01.png";
    private const string SourceDirectory = "art-source/production/final-grid-v03";
    private const string RuntimeDirectory = "game/art/production/final-grid-v03";

    private static readonly Rgba Clear = Rgba.Parse("#00000000");
    private static readonly Rgba Ink = Rgba.Parse("#292621FF");
    private static readonly Rgba Charcoal = Rgba.Parse("#253333FF");
    private static readonly Rgba Cream = Rgba.Parse("#FFF8E8FF");
    private static readonly Rgba Paper = Rgba.Parse("#F2EAD8FF");
    private static readonly Rgba Yellow = Rgba.Parse("#E8B447FF");
    private static readonly Rgba Sun = Rgba.Parse("#EACB6AFF");
    private static readonly Rgba Leaf = Rgba.Parse("#4F7155FF");
    private static readonly Rgba DeepLeaf = Rgba.Parse("#2F4A3CFF");
    private static readonly Rgba Moss = Rgba.Parse("#738C55FF");
    private static readonly Rgba LightLeaf = Rgba.Parse("#A6B85EFF");
    private static readonly Rgba Rust = Rgba.Parse("#A65338FF");
    private static readonly Rgba River = Rgba.Parse("#557C78FF");
    private static readonly Rgba RiverLight = Rgba.Parse("#7FA6A0FF");
    private static readonly Rgba Wood = Rgba.Parse("#71543AFF");
    private static readonly Rgba DeepWood = Rgba.Parse("#4A3528FF");
    private static readonly Rgba Orange = Rgba.Parse("#C98A42FF");
    private static readonly Rgba Amber = Rgba.Parse("#D68E2EFF");
    private static readonly Rgba Skin = Rgba.Parse("#E7B990FF");
    private static readonly Rgba WorldShadow = Rgba.Parse("#3F4D3DFF");
    private static readonly Rgba Stone = Rgba.Parse("#A49A85FF");

    private static readonly (string FileName, StandVariant Variant)[] Assets =
    [
        ("prop_zest_stand_base_idle_v03.png", StandVariant.Base),
        ("prop_zest_stand_better_counter_idle_v02.png", StandVariant.BetterCounter),
        ("prop_zest_stand_electric_juicer_idle_v02.png", StandVariant.ElectricJuicer),
        ("prop_zest_stand_bigger_cooler_idle_v02.png", StandVariant.BiggerCooler),
    ];

    public static void Generate(string root)
    {
        string sourceRoot = Resolve(root, SourceDirectory);
        string runtimeRoot = Resolve(root, RuntimeDirectory);
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(runtimeRoot);
        RgbaImage sheet = new(Width * Assets.Length, Height, Clear);
        for (int frame = 0; frame < Assets.Length; frame++)
        {
            (string fileName, StandVariant variant) = Assets[frame];
            RgbaImage image = DrawStand(variant);
            string source = Path.Combine(sourceRoot, fileName);
            string runtime = Path.Combine(runtimeRoot, fileName);
            Png.Write(source, image);
            File.Copy(source, runtime, overwrite: true);
            Blit(image, sheet, frame * Width, 0);
            Console.WriteLine($"STAND GEN    {SourceDirectory}/{fileName}");
            Console.WriteLine($"STAND PUB    {RuntimeDirectory}/{fileName}");
        }
        string sourceSheet = Path.Combine(sourceRoot, SheetFileName);
        string runtimeSheet = Path.Combine(runtimeRoot, SheetFileName);
        Png.Write(sourceSheet, sheet);
        File.Copy(sourceSheet, runtimeSheet, overwrite: true);
        Console.WriteLine($"STAND ATLAS  {RuntimeDirectory}/{SheetFileName}");
    }

    public static void Validate(string root, IReadOnlyList<string> paletteValues)
    {
        HashSet<Rgba> palette = paletteValues.Select(Rgba.Parse).ToHashSet();
        foreach ((string fileName, _) in Assets)
        {
            string source = Path.Combine(Resolve(root, SourceDirectory), fileName);
            string runtime = Path.Combine(Resolve(root, RuntimeDirectory), fileName);
            RgbaImage sourceImage = ValidateImage(source, palette);
            ValidateImage(runtime, palette);
            if (!File.ReadAllBytes(source).AsSpan().SequenceEqual(File.ReadAllBytes(runtime)))
                throw new InvalidDataException($"Native stand runtime differs from source: {fileName}");
            int transparent = sourceImage.Pixels.Count(pixel => pixel.A == 0);
            if (transparent < Width * 20) throw new InvalidDataException($"Native stand lacks a clean transparent silhouette: {fileName}");
            Console.WriteLine($"STAND VALID  {fileName}");
        }
        string sourceSheet = Path.Combine(Resolve(root, SourceDirectory), SheetFileName);
        string runtimeSheet = Path.Combine(Resolve(root, RuntimeDirectory), SheetFileName);
        RgbaImage sheet = Png.Read(sourceSheet);
        if (sheet.Width != Width * Assets.Length || sheet.Height != Height)
            throw new InvalidDataException($"Native stand atlas must be {Width * Assets.Length}x{Height}.");
        Rgba[] outside = sheet.Pixels.Where(pixel => pixel.A != 0 && !palette.Contains(pixel)).Distinct().ToArray();
        if (outside.Length > 0) throw new InvalidDataException("Native stand atlas contains off-palette pixels.");
        if (!File.ReadAllBytes(sourceSheet).AsSpan().SequenceEqual(File.ReadAllBytes(runtimeSheet)))
            throw new InvalidDataException("Native stand atlas runtime differs from source.");
        Console.WriteLine($"STAND VALID  {SheetFileName}");
    }

    private static RgbaImage ValidateImage(string path, HashSet<Rgba> palette)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Missing native ART-04 stand.", path);
        RgbaImage image = Png.Read(path);
        if (image.Width != Width || image.Height != Height)
            throw new InvalidDataException($"Native ART-04 stand must be {Width}x{Height}: {path}");
        Rgba[] outside = image.Pixels.Where(pixel => pixel.A != 0 && !palette.Contains(pixel)).Distinct().ToArray();
        if (outside.Length > 0)
            throw new InvalidDataException($"Native ART-04 stand contains off-palette pixels: {string.Join(", ", outside.Take(5))}");
        return image;
    }

    private static RgbaImage DrawStand(StandVariant variant)
    {
        RgbaImage image = new(Width, Height, Clear);

        // Hard-edged contact shadow anchors the sprite without introducing blur.
        Rect(image, 17, 132, 139, 3, WorldShadow);
        Rect(image, 28, 135, 116, 1, WorldShadow);

        DrawFoliage(image, 4, 72, mirrored: false);
        DrawFoliage(image, 143, 72, mirrored: true);

        // Sign: offset shadow, bevelled timber frame and inset painted board.
        Rect(image, 29, 2, 116, 28, WorldShadow);
        Rect(image, 27, 0, 116, 28, Ink);
        Rect(image, 29, 2, 112, 24, DeepWood);
        Rect(image, 31, 3, 108, 2, Orange);
        Rect(image, 31, 5, 108, 19, Cream);
        Rect(image, 31, 24, 108, 2, Wood);
        Rect(image, 34, 7, 2, 15, Paper);
        Pixel(image, 32, 6, Stone); Pixel(image, 137, 6, Stone);
        Pixel(image, 32, 22, Ink); Pixel(image, 137, 22, Ink);
        DrawWord(image, "ZEST", 57, 8, 2, Ink);
        DrawLeafMark(image, 39, 10);
        DrawLemonMark(image, 124, 9);

        // Awning and scalloped valance.
        Rect(image, 14, 28, 142, 3, Ink);
        FillQuad(image, 14, 155, 9, 160, 30, 54, Ink);
        FillQuad(image, 17, 152, 12, 157, 32, 51, Yellow);
        for (int band = 0; band < 10; band++)
        {
            int topLeft = 17 + band * 13;
            int topRight = band == 9 ? 152 : topLeft + 13;
            int bottomLeft = 12 + band * 14;
            int bottomRight = band == 9 ? 157 : bottomLeft + 14;
            Rgba stripe = (band & 1) == 0 ? Yellow : Cream;
            FillQuad(image, topLeft, topRight, bottomLeft, bottomRight, 32, 51, stripe);
            Rect(image, bottomLeft, 50, bottomRight - bottomLeft + 1, 5, stripe);
            Rect(image, bottomLeft + 2, 55, Math.Max(3, bottomRight - bottomLeft - 3), 2, stripe);
        }
        Rect(image, 17, 32, 136, 2, Sun);
        // One-pixel fold shadows make the canopy read as cloth, not flat stripes.
        for (int x = 28; x < 150; x += 28)
        {
            Rect(image, x, 35, 1, 14, Amber);
            Pixel(image, x - 1, 48, Amber);
        }
        Rect(image, 17, 57, 136, 2, Ink);

        // Service cavity, shelves, posts and small bunting.
        Rect(image, 20, 55, 9, 70, Ink);
        Rect(image, 23, 58, 4, 64, Wood);
        Rect(image, 24, 60, 1, 55, Orange);
        Pixel(image, 24, 72, Ink); Pixel(image, 24, 103, Ink);
        Rect(image, 141, 55, 9, 70, Ink);
        Rect(image, 143, 58, 4, 64, Wood);
        Rect(image, 143, 60, 1, 55, Orange);
        Pixel(image, 145, 72, Ink); Pixel(image, 145, 103, Ink);
        Rect(image, 28, 58, 113, 39, Ink);
        Rect(image, 30, 60, 109, 35, Charcoal);
        Rect(image, 31, 72, 107, 3, DeepWood);
        Rect(image, 32, 72, 105, 1, Orange);
        Rect(image, 32, 74, 105, 1, Ink);
        for (int x = 35; x <= 125; x += 18)
        {
            Rect(image, x, 59, 5, 2, Cream);
            Rect(image, x + 1, 61, 3, 4, (x / 18) % 2 == 0 ? Yellow : Paper);
        }

        DrawEquipment(image, variant);
        DrawVendor(image);
        DrawCounterAndFront(image, variant);
        DrawPlanter(image, 2, 98);
        DrawPlanter(image, 147, 98);
        return image;
    }

    private static void DrawEquipment(RgbaImage image, StandVariant variant)
    {
        if (variant == StandVariant.ElectricJuicer)
        {
            // Electric press: strong cream body, dark stem, orange half and green lamp.
            Rect(image, 35, 67, 18, 26, Ink);
            Rect(image, 38, 70, 12, 20, Cream);
            Rect(image, 39, 84, 10, 5, Paper);
            Rect(image, 41, 65, 6, 7, DeepWood);
            Rect(image, 38, 64, 12, 3, Yellow);
            Rect(image, 40, 72, 8, 3, Amber);
            Rect(image, 42, 75, 4, 4, Orange);
            Rect(image, 42, 86, 2, 2, Leaf);
            DrawPitcher(image, 55, 73, 13, 19);
        }
        else
        {
            DrawPitcher(image, 35, 68, 18, 24);
        }

        if (variant == StandVariant.BiggerCooler)
        {
            // Cooler: broad readable mint body with open lid and four bottle silhouettes.
            Rect(image, 103, 66, 34, 27, Ink);
            Rect(image, 105, 68, 30, 22, RiverLight);
            Rect(image, 104, 64, 32, 8, Ink);
            Rect(image, 106, 65, 28, 5, Cream);
            Rect(image, 108, 71, 24, 3, River);
            for (int x = 109; x < 132; x += 6)
            {
                Rect(image, x + 1, 70, 3, 3, Cream);
                Rect(image, x, 73, 5, 9, Paper);
                Rect(image, x + 1, 76, 3, 5, Yellow);
            }
            Rect(image, 116, 84, 7, 4, DeepWood);
            Rect(image, 118, 85, 3, 2, Yellow);
        }
        else
        {
            DrawLemonCrate(image, 111, 78);
        }

        // Cups and a small dark till keep the counter operational in every state.
        Rect(image, 74, 81, 5, 11, Ink);
        Rect(image, 75, 82, 3, 9, Cream);
        Rect(image, 94, 82, 10, 10, Ink);
        Rect(image, 96, 84, 6, 6, DeepWood);
        Rect(image, 97, 85, 4, 1, Yellow);
    }

    private static void DrawVendor(RgbaImage image)
    {
        // 18x36 hand-clustered character, sized to the 16x24 world character rule
        // while allowing the counter to occlude the lower body.
        Rect(image, 78, 60, 16, 4, Ink);
        Rect(image, 80, 57, 12, 5, Yellow);
        Rect(image, 83, 56, 6, 2, Sun);
        Pixel(image, 81, 59, Amber); Pixel(image, 90, 59, Amber);
        Rect(image, 76, 65, 5, 12, DeepWood);
        Rect(image, 92, 65, 5, 12, DeepWood);
        Rect(image, 79, 64, 15, 15, DeepWood);
        Rect(image, 81, 66, 11, 11, Skin);
        Pixel(image, 80, 68, Skin); Pixel(image, 92, 68, Skin);
        Rect(image, 81, 66, 3, 2, DeepWood);
        Rect(image, 89, 66, 3, 2, DeepWood);
        Pixel(image, 83, 71, Ink); Pixel(image, 90, 71, Ink);
        Pixel(image, 86, 74, Rust); Pixel(image, 87, 74, Rust);
        Rect(image, 77, 78, 19, 16, Ink);
        Rect(image, 79, 79, 15, 13, Cream);
        Rect(image, 82, 81, 9, 11, Leaf);
        Rect(image, 84, 84, 4, 3, Yellow);
        Pixel(image, 85, 84, Cream);
        Rect(image, 72, 83, 7, 5, Skin);
        Rect(image, 93, 83, 7, 5, Skin);
        Rect(image, 71, 88, 8, 3, Ink);
        Rect(image, 92, 88, 8, 3, Ink);
    }

    private static void DrawCounterAndFront(RgbaImage image, StandVariant variant)
    {
        int counterTop = variant == StandVariant.BetterCounter ? 92 : 94;
        int counterHeight = variant == StandVariant.BetterCounter ? 12 : 8;
        Rect(image, 14, counterTop, 142, counterHeight, Ink);
        Rect(image, 17, counterTop + 2, 136, counterHeight - 4, variant == StandVariant.BetterCounter ? Sun : Amber);
        Rect(image, 18, counterTop + 3, 134, 2, Yellow);
        if (variant == StandVariant.BetterCounter)
        {
            Rect(image, 18, 99, 134, 3, Wood);
            Rect(image, 19, 94, 5, 8, Yellow);
            Rect(image, 146, 94, 5, 8, Yellow);
            Rect(image, 32, 95, 20, 4, Orange);
            Rect(image, 118, 95, 20, 4, Orange);
        }

        int frontTop = counterTop + counterHeight;
        Rect(image, 20, frontTop, 130, 31, Ink);
        Rect(image, 23, frontTop + 2, 124, 26, Orange);
        Rect(image, 26, frontTop + 4, 118, 3, Amber);
        for (int x = 36; x < 142; x += 18)
        {
            Rect(image, x, frontTop + 7, 2, 19, Wood);
            Rect(image, x + 3, frontTop + 9, 1, 13, Amber);
            Pixel(image, x + 8, frontTop + 12, Wood);
            Pixel(image, x + 9, frontTop + 12, Wood);
        }
        Rect(image, 25, frontTop + 25, 120, 2, DeepWood);
        // Diagonal end braces break the boxy silhouette and imply joinery.
        Line(image, 25, frontTop + 25, 42, frontTop + 8, Wood, 2);
        Line(image, 145, frontTop + 25, 128, frontTop + 8, Wood, 2);
        Rect(image, 20, 132, 130, 3, Ink);
        Rect(image, 73, frontTop + 4, 25, 19, Cream);
        Rect(image, 77, frontTop + 7, 17, 10, Yellow);
        Rect(image, 80, frontTop + 9, 11, 6, Cream);
    }

    private static void DrawPitcher(RgbaImage image, int x, int y, int width, int height)
    {
        Rect(image, x, y, width, height, Ink);
        Rect(image, x + 2, y + 2, width - 4, height - 4, Paper);
        Rect(image, x + 3, y + height / 2, width - 6, height / 2 - 3, Yellow);
        Rect(image, x + 4, y + height / 2 + 1, width - 8, 2, Sun);
        Rect(image, x - 2, y + 5, 3, height - 9, Ink);
        Rect(image, x + width, y + 5, 3, height - 9, Ink);
        Rect(image, x + 4, y - 2, width - 8, 3, Ink);
        Pixel(image, x + width - 5, y + height / 2 + 2, Cream);
        Pixel(image, x + 4, y + 4, Cream);
    }

    private static void DrawLemonCrate(RgbaImage image, int x, int y)
    {
        Rect(image, x, y + 5, 24, 10, Ink);
        Rect(image, x + 2, y + 7, 20, 6, Orange);
        Rect(image, x + 5, y + 2, 6, 6, Yellow);
        Rect(image, x + 12, y, 6, 8, Sun);
        Rect(image, x + 18, y + 3, 5, 5, Yellow);
        Pixel(image, x + 15, y, Leaf);
        Pixel(image, x + 21, y + 2, Leaf);
    }

    private static void DrawFoliage(RgbaImage image, int x, int y, bool mirrored)
    {
        int shift = mirrored ? 1 : 0;
        Rect(image, x + 7, y + 9, 10, 31, DeepLeaf);
        Rect(image, x + 3 + shift, y + 15, 18, 19, Leaf);
        Rect(image, x + 6, y + 6, 12, 12, Moss);
        Rect(image, x + 1 + shift, y + 21, 8, 9, Moss);
        Rect(image, x + 14 - shift, y + 18, 9, 11, Leaf);
        Rect(image, x + 8, y + 9, 6, 5, LightLeaf);
        Rect(image, x + 3, y + 18, 5, 4, LightLeaf);
        Pixel(image, x + 11, y + 7, DeepLeaf);
        Pixel(image, x + 20 - shift, y + 21, LightLeaf);
        Pixel(image, x + 2 + shift, y + 27, DeepLeaf);
        foreach ((int fx, int fy, Rgba flower) in new[]
        {
            (x + 5, y + 17, Yellow), (x + 16, y + 14, Cream),
            (x + 11, y + 25, Yellow), (x + 18, y + 27, Cream)
        })
        {
            Rect(image, fx, fy, 3, 3, flower);
            Pixel(image, fx + 1, fy + 1, Orange);
        }
    }

    private static void DrawPlanter(RgbaImage image, int x, int y)
    {
        Rect(image, x, y, 21, 30, Ink);
        Rect(image, x + 2, y + 2, 17, 26, DeepWood);
        Rect(image, x + 4, y + 4, 13, 22, Orange);
        Rect(image, x + 3, y + 11, 15, 3, Wood);
        Rect(image, x + 6, y + 2, 2, 24, Amber);
        Rect(image, x + 14, y + 2, 2, 24, Wood);
    }

    private static void DrawLeafMark(RgbaImage image, int x, int y)
    {
        Rect(image, x + 1, y, 6, 4, Leaf);
        Rect(image, x + 5, y + 4, 7, 4, Moss);
        Pixel(image, x + 5, y + 3, DeepLeaf);
        Pixel(image, x + 8, y + 5, LightLeaf);
    }

    private static void DrawLemonMark(RgbaImage image, int x, int y)
    {
        Rect(image, x + 2, y, 7, 1, Yellow);
        Rect(image, x, y + 2, 11, 7, Yellow);
        Rect(image, x + 2, y + 10, 7, 1, Yellow);
        Rect(image, x + 2, y + 2, 7, 7, Sun);
        Rect(image, x + 5, y + 2, 1, 7, Cream);
        Rect(image, x + 2, y + 5, 7, 1, Cream);
    }

    private static void DrawWord(RgbaImage image, string text, int x, int y, int scale, Rgba color)
    {
        foreach (char glyph in text)
        {
            string[] rows = glyph switch
            {
                'Z' => ["11111", "00010", "00100", "01000", "10000", "10000", "11111"],
                'E' => ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
                'S' => ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
                'T' => ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
                _ => ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
            };
            for (int row = 0; row < rows.Length; row++)
            for (int column = 0; column < rows[row].Length; column++)
                if (rows[row][column] == '1') Rect(image, x + column * scale, y + row * scale, scale, scale, color);
            x += 6 * scale;
        }
    }

    private static void Rect(RgbaImage image, int x, int y, int width, int height, Rgba color)
    {
        for (int py = Math.Max(0, y); py < Math.Min(image.Height, y + height); py++)
        for (int px = Math.Max(0, x); px < Math.Min(image.Width, x + width); px++) image[px, py] = color;
    }

    private static void Pixel(RgbaImage image, int x, int y, Rgba color) => Rect(image, x, y, 1, 1, color);

    private static void Line(RgbaImage image, int x0, int y0, int x1, int y1, Rgba color, int thickness = 1)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int error = dx + dy;
        while (true)
        {
            Rect(image, x0, y0, thickness, thickness, color);
            if (x0 == x1 && y0 == y1) break;
            int twice = 2 * error;
            if (twice >= dy) { error += dy; x0 += sx; }
            if (twice <= dx) { error += dx; y0 += sy; }
        }
    }

    private static void FillQuad(RgbaImage image, int topLeft, int topRight, int bottomLeft, int bottomRight, int top, int bottom, Rgba color)
    {
        int height = Math.Max(1, bottom - top);
        for (int y = top; y <= bottom; y++)
        {
            float t = (y - top) / (float)height;
            int left = (int)MathF.Round(topLeft + (bottomLeft - topLeft) * t);
            int right = (int)MathF.Round(topRight + (bottomRight - topRight) * t);
            Rect(image, left, y, right - left + 1, 1, color);
        }
    }

    private static void Blit(RgbaImage source, RgbaImage target, int targetX, int targetY)
    {
        for (int y = 0; y < source.Height; y++)
        for (int x = 0; x < source.Width; x++)
            target[targetX + x, targetY + y] = source[x, y];
    }
    private static string Resolve(string root, string relative) => Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

    private enum StandVariant { Base, BetterCounter, ElectricJuicer, BiggerCooler }
}
