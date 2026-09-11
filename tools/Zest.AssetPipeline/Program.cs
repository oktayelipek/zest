using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

string root = FindRepositoryRoot(Directory.GetCurrentDirectory());
string command = args.FirstOrDefault(item => !item.StartsWith("--", StringComparison.Ordinal)) ?? "all";
bool force = args.Contains("--force", StringComparer.Ordinal);
Pipeline pipeline = LoadPipeline(Path.Combine(root, "art-source", "pipeline.json"));

switch (command)
{
    case "clean-customer-poses":
        CustomerPoseCleanup.Run(root);
        break;
    case "clean-vendor-poses":
        VendorPoseCleanup.Run(root);
        break;
    case "clean-stand-layer":
        StandLayerCleanup.Run(root);
        break;
    case "samples":
        GenerateSamples(root, pipeline, force);
        break;
    case "publish":
        Publish(root, pipeline);
        break;
    case "validate":
        ValidateAll(root, pipeline);
        break;
    case "stand-kit":
        NativeStandKit.Generate(root);
        NativeStandKit.Validate(root, pipeline.Palette);
        break;
    case "stand-kit-validate":
        NativeStandKit.Validate(root, pipeline.Palette);
        break;
    case "environment-tiles":
        GenerateEnvironmentTiles(root);
        break;
    case "all":
        GenerateSamples(root, pipeline, force);
        Publish(root, pipeline);
        ValidateAll(root, pipeline);
        break;
    default:
        throw new ArgumentException("Usage: Zest.AssetPipeline [samples|publish|validate|all|stand-kit|stand-kit-validate|clean-stand-layer|clean-vendor-poses|clean-customer-poses] [--force]");
}

static void GenerateEnvironmentTiles(string root)
{
    string source = Path.Combine(root, "art-source/production/native-v01/environment/tile_grass_path_5x1_v01.png");
    string runtime = Path.Combine(root, "game/art/production/native-v01/environment/tile_grass_path_5x1_v01.png");
    Directory.CreateDirectory(Path.GetDirectoryName(source)!);
    Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
    Rgba grass = Rgba.Parse("#78945FFF"), grassDark = Rgba.Parse("#4F7155FF"), grassLight = Rgba.Parse("#A6B85EFF");
    Rgba sand = Rgba.Parse("#D8B879FF"), sandDark = Rgba.Parse("#B18C58FF"), stone = Rgba.Parse("#8B8C72FF");
    RgbaImage sheet = new(160, 32, grass);
    for (int tile = 0; tile < 5; tile++)
    {
        int ox = tile * 32;
        if (tile is 1 or 2 or 3 or 4) FillRect(sheet, ox, 0, 32, 32, sand);
        if (tile is 2 or 3 or 4)
        {
            FillRect(sheet, ox, 0, 4, 32, grass);
            for (int y = 2; y < 30; y += 7) FillRect(sheet, ox + 4, y, 2, 3, stone);
        }
        if (tile == 3) { FillRect(sheet, ox, 0, 32, 4, grass); for (int x = 2; x < 30; x += 7) FillRect(sheet, ox + x, 4, 3, 2, stone); }
        if (tile == 4) { FillRect(sheet, ox, 0, 4, 32, grass); FillRect(sheet, ox, 0, 32, 4, grass); }
        for (int i = 0; i < 5; i++)
        {
            int x = ox + ((i * 11 + tile * 3) % 27), y = 4 + ((i * 7 + tile * 5) % 24);
            FillRect(sheet, x, y, 2, 2, tile == 0 ? (i % 2 == 0 ? grassDark : grassLight) : sandDark);
        }
    }
    Png.Write(source, sheet);
    File.Copy(source, runtime, true);
    Console.WriteLine($"ENVIRONMENT {source}");
}

static string FindRepositoryRoot(string start)
{
    DirectoryInfo? cursor = new(start);
    while (cursor is not null)
    {
        if (File.Exists(Path.Combine(cursor.FullName, "art-source", "pipeline.json"))) return cursor.FullName;
        cursor = cursor.Parent;
    }
    throw new DirectoryNotFoundException("Could not find art-source/pipeline.json from the current directory.");
}

static Pipeline LoadPipeline(string path)
{
    JsonSerializerOptions options = new() { PropertyNameCaseInsensitive = true };
    Pipeline? pipeline = JsonSerializer.Deserialize<Pipeline>(File.ReadAllText(path), options);
    return pipeline ?? throw new InvalidDataException("ART-10 pipeline manifest is empty.");
}

static void GenerateSamples(string root, Pipeline pipeline, bool force)
{
    foreach (AssetDefinition asset in pipeline.Assets)
    {
        string path = Resolve(root, asset.Source);
        if (File.Exists(path) && !force)
        {
            Console.WriteLine($"SOURCE KEEP  {asset.Source}");
            continue;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        RgbaImage image = asset.Generator switch
        {
            "grass_tile" => GenerateGrass(asset.Width, asset.Height),
            "lemon_crate" => GenerateCrate(asset.Width, asset.Height),
            "guest_walk" => GenerateGuestSheet(asset),
            "zest_stand" => GenerateZestStand(asset.Width, asset.Height),
            "park_tree" => GenerateParkTree(asset.Width, asset.Height),
            "park_bench" => GenerateParkBench(asset.Width, asset.Height),
            "park_lamp_sign" => GenerateParkLampSign(asset.Width, asset.Height),
            "park_planter" => GenerateParkPlanter(asset.Width, asset.Height),
            "path_edge" => GeneratePathEdge(asset.Width, asset.Height),
            _ => throw new InvalidDataException($"Unknown sample generator '{asset.Generator}'."),
        };
        Png.Write(path, image);
        Console.WriteLine($"SOURCE GEN   {asset.Source}");
    }
}

static void Publish(string root, Pipeline pipeline)
{
    ValidateDefinitions(pipeline);
    HashSet<Rgba> palette = pipeline.Palette.Select(Rgba.Parse).ToHashSet();
    foreach (AssetDefinition asset in pipeline.Assets)
    {
        string source = Resolve(root, asset.Source);
        ValidateImage(source, asset, palette);
        string runtime = Resolve(root, asset.Runtime);
        Directory.CreateDirectory(Path.GetDirectoryName(runtime)!);
        byte[] bytes = File.ReadAllBytes(source);
        if (!File.Exists(runtime) || !File.ReadAllBytes(runtime).AsSpan().SequenceEqual(bytes)) File.WriteAllBytes(runtime, bytes);
        Console.WriteLine($"PUBLISH      {asset.Runtime}");
    }
    WriteCatalog(root, pipeline);
    WriteContactSheet(root, pipeline);
}

static void ValidateAll(string root, Pipeline pipeline)
{
    ValidateDefinitions(pipeline);
    HashSet<Rgba> palette = pipeline.Palette.Select(Rgba.Parse).ToHashSet();
    foreach (AssetDefinition asset in pipeline.Assets)
    {
        ValidateImage(Resolve(root, asset.Source), asset, palette);
        ValidateImage(Resolve(root, asset.Runtime), asset, palette);
        Console.WriteLine($"VALID        {asset.Id}");
    }
    Console.WriteLine($"ART-10 validated {pipeline.Assets.Count} source assets and {pipeline.Assets.Count} runtime assets.");
}

static void ValidateDefinitions(Pipeline pipeline)
{
    if (pipeline.SchemaVersion != 1) throw new InvalidDataException("Unsupported ART-10 schema version.");
    if (pipeline.SourceGridPixels != 16) throw new InvalidDataException("Zest source grid must remain 16 px.");
    if (pipeline.Assets.Count == 0) throw new InvalidDataException("Pipeline needs at least one asset.");
    if (pipeline.Assets.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != pipeline.Assets.Count)
        throw new InvalidDataException("Asset IDs must be unique.");

    Regex naming = new("^(tile|prop|chr)_[a-z0-9]+(?:_[a-z0-9]+)*_(?:idle|walk)_v[0-9]{2}\\.png$", RegexOptions.CultureInvariant);
    foreach (AssetDefinition asset in pipeline.Assets)
    {
        string fileName = Path.GetFileName(asset.Runtime);
        if (!naming.IsMatch(fileName)) throw new InvalidDataException($"Invalid runtime filename '{fileName}'.");
        string expectedPrefix = asset.Kind switch { "tile" => "tile_", "prop" => "prop_", "character" => "chr_", _ => throw new InvalidDataException($"Unknown asset kind '{asset.Kind}'.") };
        if (!fileName.StartsWith(expectedPrefix, StringComparison.Ordinal)) throw new InvalidDataException($"'{fileName}' does not match kind '{asset.Kind}'.");
        if (asset.Width != asset.FrameWidth * asset.Columns || asset.Height != asset.FrameHeight * asset.Rows)
            throw new InvalidDataException($"'{asset.Id}' sheet dimensions do not match frame grid.");
        if (asset.FrameWidth % pipeline.SourceGridPixels != 0 || asset.FrameHeight % 8 != 0)
            throw new InvalidDataException($"'{asset.Id}' frame does not follow the 16 px width / 8 px height unit system.");
        if (asset.PivotX != asset.FrameWidth / 2 || asset.PivotY < asset.FrameHeight - 4 || asset.PivotY > asset.FrameHeight)
            throw new InvalidDataException($"'{asset.Id}' pivot must be bottom-center within four source pixels.");
        if (asset.Kind == "character" && (asset.Columns != 4 || asset.Rows != 4 || asset.FrameWidth != 16 || asset.FrameHeight != 24))
            throw new InvalidDataException($"'{asset.Id}' must use the 4×4 directional 16×24 character sheet contract.");
    }
}

static void ValidateImage(string path, AssetDefinition asset, HashSet<Rgba> palette)
{
    if (!File.Exists(path)) throw new FileNotFoundException($"Missing ART-10 asset '{path}'.");
    RgbaImage image = Png.Read(path);
    if (image.Width != asset.Width || image.Height != asset.Height)
        throw new InvalidDataException($"'{asset.Id}' is {image.Width}×{image.Height}; expected {asset.Width}×{asset.Height}.");
    HashSet<Rgba> outside = image.Pixels.Where(pixel => pixel.A != 0 && !palette.Contains(pixel)).ToHashSet();
    if (outside.Count > 0)
        throw new InvalidDataException($"'{asset.Id}' contains {outside.Count} off-palette colors: {string.Join(", ", outside.Take(5))}.");
}

static void WriteCatalog(string root, Pipeline pipeline)
{
    object catalog = new
    {
        schemaVersion = pipeline.SchemaVersion,
        sourceGridPixels = pipeline.SourceGridPixels,
        directionRows = new[] { "south", "west", "east", "north" },
        assets = pipeline.Assets.Select(asset => new
        {
            asset.Id,
            asset.Kind,
            path = "res://" + asset.Runtime["game/".Length..].Replace('\\', '/'),
            frame = new { width = asset.FrameWidth, height = asset.FrameHeight, columns = asset.Columns, rows = asset.Rows },
            pivot = new { x = asset.PivotX, y = asset.PivotY },
            animation = asset.Kind == "character" ? new { fps = 8, loop = true } : null,
        }),
    };
    JsonSerializerOptions options = new() { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
    string path = Path.Combine(root, "game", "art", "pixel", "art10-catalog.json");
    File.WriteAllText(path, JsonSerializer.Serialize(catalog, options) + Environment.NewLine);
    Console.WriteLine("CATALOG      game/art/pixel/art10-catalog.json");
}

static void WriteContactSheet(string root, Pipeline pipeline)
{
    RgbaImage sheet = new(512, 224, Rgba.Parse("#F2EAD8FF"));
    RgbaImage tile = Png.Read(Resolve(root, pipeline.Assets.Single(item => item.Id == "grass_base").Source));
    RgbaImage prop = Png.Read(Resolve(root, pipeline.Assets.Single(item => item.Id == "lemon_crate").Source));
    RgbaImage character = Png.Read(Resolve(root, pipeline.Assets.Single(item => item.Id == "guest_base_walk").Source));
    BlitScaled(tile, sheet, 28, 48, 8);
    BlitScaled(prop, sheet, 192, 48, 4);
    BlitFrameScaled(character, sheet, 376, 48, 5, 16, 24, 0, 0);
    string output = Path.Combine(root, "docs", "artifacts", "art-10-pipeline-samples.png");
    Png.Write(output, sheet);
    Console.WriteLine("PREVIEW      docs/artifacts/art-10-pipeline-samples.png");
}

static void BlitScaled(RgbaImage source, RgbaImage target, int targetX, int targetY, int scale)
{
    for (int y = 0; y < source.Height; y++)
    for (int x = 0; x < source.Width; x++)
    for (int sy = 0; sy < scale; sy++)
    for (int sx = 0; sx < scale; sx++)
    {
        Rgba pixel = source[x, y];
        if (pixel.A != 0) target[targetX + x * scale + sx, targetY + y * scale + sy] = pixel;
    }
}

static void BlitFrameScaled(RgbaImage source, RgbaImage target, int targetX, int targetY, int scale, int frameWidth, int frameHeight, int frameX, int frameY)
{
    for (int y = 0; y < frameHeight; y++)
    for (int x = 0; x < frameWidth; x++)
    for (int sy = 0; sy < scale; sy++)
    for (int sx = 0; sx < scale; sx++)
    {
        Rgba pixel = source[frameX * frameWidth + x, frameY * frameHeight + y];
        if (pixel.A != 0) target[targetX + x * scale + sx, targetY + y * scale + sy] = pixel;
    }
}

static RgbaImage GenerateGrass(int width, int height)
{
    Rgba grass = Rgba.Parse("#91A76FFF");
    Rgba leaf = Rgba.Parse("#4F7155FF");
    Rgba moss = Rgba.Parse("#738C55FF");
    Rgba light = Rgba.Parse("#A6B85EFF");
    RgbaImage image = new(width, height, grass);
    image[2, 4] = moss; image[3, 3] = leaf; image[4, 4] = moss;
    image[11, 12] = leaf; image[12, 10] = moss; image[13, 12] = leaf;
    image[7, 7] = light;
    return image;
}

static RgbaImage GenerateCrate(int width, int height)
{
    Rgba clear = Rgba.Parse("#00000000");
    Rgba ink = Rgba.Parse("#292621FF");
    Rgba wood = Rgba.Parse("#C98A42FF");
    Rgba lemon = Rgba.Parse("#E8B447FF");
    Rgba leaf = Rgba.Parse("#4F7155FF");
    RgbaImage image = new(width, height, clear);
    FillRect(image, 3, 15, 26, 13, ink);
    FillRect(image, 4, 16, 24, 11, wood);
    FillRect(image, 5, 20, 22, 2, ink);
    FillRect(image, 8, 11, 5, 5, lemon);
    FillRect(image, 14, 9, 5, 7, lemon);
    FillRect(image, 20, 11, 5, 5, lemon);
    image[10, 10] = leaf;
    image[17, 8] = leaf;
    image[22, 10] = leaf;
    return image;
}

static RgbaImage GenerateGuestSheet(AssetDefinition asset)
{
    RgbaImage image = new(asset.Width, asset.Height, Rgba.Parse("#00000000"));
    Rgba body = Rgba.Parse("#A65338FF");
    Rgba skin = Rgba.Parse("#E7B990FF");
    Rgba ink = Rgba.Parse("#292621FF");
    Rgba cream = Rgba.Parse("#FFF8E8FF");
    Rgba yellow = Rgba.Parse("#E8B447FF");
    Rgba river = Rgba.Parse("#557C78FF");
    for (int row = 0; row < asset.Rows; row++)
    for (int frame = 0; frame < asset.Columns; frame++)
    {
        int ox = frame * asset.FrameWidth;
        int oy = row * asset.FrameHeight;
        int bob = frame is 1 or 3 ? 1 : 0;
        FillRect(image, ox + 4, oy + 3 + bob, 8, 2, ink);
        FillRect(image, ox + 5, oy + 2 + bob, 6, 2, yellow);
        FillRect(image, ox + 4, oy + 5 + bob, 8, 6, ink);
        if (row != 3) FillRect(image, ox + 5, oy + 6 + bob, 6, 4, skin);
        FillRect(image, ox + 3, oy + 11 + bob, 10, 8, ink);
        FillRect(image, ox + 4, oy + 11 + bob, 8, 7, body);
        FillRect(image, ox + 6, oy + 12 + bob, 4, 4, cream);
        image[ox + 7, oy + 13 + bob] = river;
        int leftLeg = frame is 1 ? 4 : 5;
        int rightLeg = frame is 3 ? 10 : 9;
        FillRect(image, ox + leftLeg, oy + 18, 2, 4, river);
        FillRect(image, ox + rightLeg, oy + 18, 2, 4, river);
        image[ox + leftLeg - 1, oy + 22] = ink;
        image[ox + rightLeg, oy + 22] = ink;
    }
    return image;
}

static RgbaImage GenerateZestStand(int width, int height)
{
    RgbaImage image = new(width, height, Rgba.Parse("#00000000"));
    Rgba ink = Rgba.Parse("#292621FF"), charcoal = Rgba.Parse("#253333FF"), cream = Rgba.Parse("#FFF8E8FF"), yellow = Rgba.Parse("#E8B447FF");
    Rgba sun = Rgba.Parse("#EACB6AFF"), wood = Rgba.Parse("#71543AFF"), deepWood = Rgba.Parse("#4A3528FF"), orange = Rgba.Parse("#C98A42FF"), amber = Rgba.Parse("#D68E2EFF"), leaf = Rgba.Parse("#4F7155FF"), skin = Rgba.Parse("#E7B990FF");
    FillRect(image, 18, 2, 124, 30, deepWood); FillRect(image, 21, 4, 118, 26, orange); FillRect(image, 25, 7, 110, 20, cream);
    DrawWord(image, "ZEST", 56, 10, 2, ink);
    FillRect(image, 36, 12, 5, 8, leaf); FillRect(image, 40, 9, 7, 5, leaf); FillRect(image, 119, 11, 9, 9, yellow); FillRect(image, 122, 9, 3, 13, cream);
    FillRect(image, 12, 32, 136, 7, ink); FillRect(image, 15, 35, 130, 21, yellow);
    for (int x = 15; x < 145; x += 20) FillRect(image, x + 10, 35, 10, 20, cream);
    for (int x = 15; x < 145; x += 20) { FillRect(image, x, 52, 10, 6, yellow); FillRect(image, x + 10, 52, 10, 6, cream); }
    FillRect(image, 19, 55, 122, 4, ink); FillRect(image, 22, 58, 116, 31, charcoal);
    FillRect(image, 18, 56, 7, 49, deepWood); FillRect(image, 135, 56, 7, 49, deepWood); FillRect(image, 20, 58, 3, 45, wood); FillRect(image, 137, 58, 3, 45, wood);
    FillRect(image, 60, 61, 14, 8, ink); FillRect(image, 62, 62, 10, 7, yellow); FillRect(image, 62, 69, 10, 8, skin); FillRect(image, 59, 74, 16, 11, cream); FillRect(image, 65, 77, 4, 6, leaf);
    FillRect(image, 31, 64, 15, 19, cream); FillRect(image, 34, 68, 9, 12, yellow); FillRect(image, 28, 63, 21, 3, ink); FillRect(image, 30, 81, 17, 2, ink);
    FillRect(image, 106, 72, 21, 12, deepWood); FillRect(image, 108, 74, 17, 8, orange); FillRect(image, 111, 68, 5, 5, yellow); FillRect(image, 118, 66, 5, 7, sun); FillRect(image, 124, 69, 4, 5, yellow);
    FillRect(image, 12, 85, 136, 9, ink); FillRect(image, 15, 87, 130, 8, amber); FillRect(image, 20, 94, 120, 16, orange);
    for (int x = 28; x < 140; x += 18) FillRect(image, x, 95, 2, 14, amber);
    FillRect(image, 54, 95, 52, 4, sun); FillRect(image, 72, 99, 16, 11, cream); FillRect(image, 76, 101, 8, 6, yellow);
    FillRect(image, 17, 109, 126, 3, ink);
    return image;
}

static RgbaImage GenerateParkTree(int width, int height)
{
    RgbaImage image = new(width, height, Rgba.Parse("#00000000"));
    Rgba ink = Rgba.Parse("#292621FF"), deepLeaf = Rgba.Parse("#2F4A3CFF"), leaf = Rgba.Parse("#4F7155FF"), moss = Rgba.Parse("#738C55FF"), light = Rgba.Parse("#A6B85EFF"), wood = Rgba.Parse("#71543AFF"), deepWood = Rgba.Parse("#4A3528FF");
    FillRect(image, 31, 52, 18, 42, deepWood); FillRect(image, 35, 49, 12, 44, wood); FillRect(image, 28, 90, 10, 5, deepWood); FillRect(image, 45, 90, 12, 5, deepWood);
    FillRect(image, 9, 21, 62, 42, deepLeaf); FillRect(image, 4, 31, 72, 24, deepLeaf); FillRect(image, 17, 9, 47, 58, deepLeaf);
    FillRect(image, 10, 25, 26, 27, leaf); FillRect(image, 32, 14, 34, 32, leaf); FillRect(image, 43, 35, 27, 25, leaf); FillRect(image, 17, 48, 35, 18, leaf);
    FillRect(image, 14, 24, 15, 10, moss); FillRect(image, 37, 18, 19, 12, moss); FillRect(image, 51, 39, 15, 10, moss); FillRect(image, 24, 47, 16, 10, moss);
    FillRect(image, 18, 23, 7, 5, light); FillRect(image, 42, 17, 9, 5, light); FillRect(image, 55, 38, 7, 5, light); FillRect(image, 29, 49, 6, 4, light);
    FillRect(image, 38, 58, 4, 21, wood); FillRect(image, 28, 58, 12, 4, wood); FillRect(image, 42, 54, 12, 4, wood);
    return image;
}

static RgbaImage GenerateParkBench(int width, int height)
{
    RgbaImage image = new(width, height, Rgba.Parse("#00000000"));
    Rgba ink = Rgba.Parse("#292621FF"), charcoal = Rgba.Parse("#253333FF"), wood = Rgba.Parse("#71543AFF"), deepWood = Rgba.Parse("#4A3528FF"), orange = Rgba.Parse("#C98A42FF"), amber = Rgba.Parse("#D68E2EFF");
    FillRect(image, 4, 7, 56, 5, ink); FillRect(image, 7, 8, 50, 3, orange); FillRect(image, 7, 11, 50, 3, deepWood);
    FillRect(image, 4, 16, 56, 8, ink); FillRect(image, 7, 17, 50, 5, amber); FillRect(image, 7, 21, 50, 2, wood);
    FillRect(image, 7, 4, 5, 34, charcoal); FillRect(image, 52, 4, 5, 34, charcoal); FillRect(image, 9, 6, 2, 28, ink); FillRect(image, 53, 6, 2, 28, ink);
    FillRect(image, 5, 36, 10, 3, ink); FillRect(image, 49, 36, 10, 3, ink); FillRect(image, 18, 17, 2, 5, deepWood); FillRect(image, 42, 17, 2, 5, deepWood);
    return image;
}

static RgbaImage GenerateParkLampSign(int width, int height)
{
    RgbaImage image = new(width, height, Rgba.Parse("#00000000"));
    Rgba ink = Rgba.Parse("#292621FF"), charcoal = Rgba.Parse("#253333FF"), cream = Rgba.Parse("#FFF8E8FF"), yellow = Rgba.Parse("#E8B447FF"), sun = Rgba.Parse("#EACB6AFF"), wood = Rgba.Parse("#71543AFF"), amber = Rgba.Parse("#D68E2EFF"), leaf = Rgba.Parse("#4F7155FF");
    FillRect(image, 12, 3, 18, 4, ink); FillRect(image, 9, 7, 24, 5, ink); FillRect(image, 11, 11, 20, 19, ink); FillRect(image, 14, 13, 14, 14, cream); FillRect(image, 17, 15, 8, 9, sun);
    FillRect(image, 18, 29, 6, 63, charcoal); FillRect(image, 20, 30, 2, 59, ink); FillRect(image, 11, 88, 20, 7, ink);
    FillRect(image, 23, 40, 23, 4, ink); FillRect(image, 27, 43, 18, 26, wood); FillRect(image, 29, 45, 14, 22, amber); FillRect(image, 32, 49, 5, 6, leaf); FillRect(image, 36, 47, 5, 5, leaf); FillRect(image, 31, 60, 10, 3, cream);
    return image;
}

static RgbaImage GenerateParkPlanter(int width, int height)
{
    RgbaImage image = new(width, height, Rgba.Parse("#00000000"));
    Rgba ink = Rgba.Parse("#292621FF"), deepWood = Rgba.Parse("#4A3528FF"), rust = Rgba.Parse("#A65338FF"), orange = Rgba.Parse("#C98A42FF"), deepLeaf = Rgba.Parse("#2F4A3CFF"), leaf = Rgba.Parse("#4F7155FF"), moss = Rgba.Parse("#738C55FF"), yellow = Rgba.Parse("#E8B447FF"), cream = Rgba.Parse("#FFF8E8FF"), sun = Rgba.Parse("#EACB6AFF");
    FillRect(image, 4, 16, 40, 5, ink); FillRect(image, 7, 20, 34, 11, deepWood); FillRect(image, 9, 21, 30, 8, orange); FillRect(image, 12, 23, 24, 2, rust);
    FillRect(image, 8, 9, 8, 9, deepLeaf); FillRect(image, 14, 5, 9, 13, leaf); FillRect(image, 22, 3, 8, 15, moss); FillRect(image, 29, 7, 10, 11, leaf); FillRect(image, 36, 11, 6, 7, deepLeaf);
    FillRect(image, 10, 7, 5, 5, yellow); FillRect(image, 19, 3, 5, 5, cream); FillRect(image, 28, 5, 5, 5, sun); FillRect(image, 36, 9, 5, 5, cream);
    return image;
}

static RgbaImage GeneratePathEdge(int width, int height)
{
    Rgba path = Rgba.Parse("#D8C9AAFF"), ink = Rgba.Parse("#292621FF"), cream = Rgba.Parse("#F2EAD8FF"), grass = Rgba.Parse("#91A76FFF");
    RgbaImage image = new(width, height, path);
    FillRect(image, 0, 0, width, 4, grass); FillRect(image, 0, 4, width, 2, ink);
    for (int x = 0; x < width; x += 5) FillRect(image, x, 4, 4, 2, cream);
    image[3, 11] = cream; image[12, 13] = cream;
    return image;
}

static void DrawWord(RgbaImage image, string word, int x, int y, int scale, Rgba color)
{
    int cursor = x;
    foreach (char glyph in word)
    {
        DrawGlyph(image, glyph, cursor, y, scale, color);
        cursor += 6 * scale;
    }
}

static void DrawGlyph(RgbaImage image, char glyph, int x, int y, int scale, Rgba color)
{
    string[] rows = glyph switch
    {
        'Z' => ["11111", "00010", "00100", "01000", "10000", "10000", "11111"],
        'E' => ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
        'S' => ["11111", "10000", "10000", "11111", "00001", "00001", "11111"],
        'T' => ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
        _ => ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
    };
    for (int row = 0; row < rows.Length; row++)
    for (int column = 0; column < rows[row].Length; column++)
        if (rows[row][column] == '1') FillRect(image, x + column * scale, y + row * scale, scale, scale, color);
}

static void FillRect(RgbaImage image, int x, int y, int width, int height, Rgba color)
{
    for (int py = y; py < y + height; py++)
    for (int px = x; px < x + width; px++) image[px, py] = color;
}

static string Resolve(string root, string relative) => Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));

public sealed record Pipeline(int SchemaVersion, int SourceGridPixels, IReadOnlyList<string> Palette, IReadOnlyList<AssetDefinition> Assets);
public sealed record AssetDefinition(string Id, string Kind, string Source, string Runtime, int Width, int Height, int FrameWidth, int FrameHeight, int Columns, int Rows, int PivotX, int PivotY, string Generator);

public readonly record struct Rgba(byte R, byte G, byte B, byte A)
{
    public static Rgba Parse(string value)
    {
        string hex = value.TrimStart('#');
        if (hex.Length != 8) throw new FormatException($"Expected RRGGBBAA color, got '{value}'.");
        return new(Convert.ToByte(hex[0..2], 16), Convert.ToByte(hex[2..4], 16), Convert.ToByte(hex[4..6], 16), Convert.ToByte(hex[6..8], 16));
    }
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
}

public sealed class RgbaImage
{
    public RgbaImage(int width, int height, Rgba fill)
    {
        Width = width;
        Height = height;
        Pixels = Enumerable.Repeat(fill, checked(width * height)).ToArray();
    }
    public RgbaImage(int width, int height, Rgba[] pixels) { Width = width; Height = height; Pixels = pixels; }
    public int Width { get; }
    public int Height { get; }
    public Rgba[] Pixels { get; }
    public Rgba this[int x, int y] { get => Pixels[y * Width + x]; set => Pixels[y * Width + x] = value; }
}

public static class Png
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static void Write(string path, RgbaImage image)
    {
        using MemoryStream output = new();
        output.Write(Signature);
        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), image.Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), image.Height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(output, "IHDR", header);

        using MemoryStream raw = new();
        for (int y = 0; y < image.Height; y++)
        {
            raw.WriteByte(0);
            for (int x = 0; x < image.Width; x++)
            {
                Rgba pixel = image[x, y];
                raw.WriteByte(pixel.R); raw.WriteByte(pixel.G); raw.WriteByte(pixel.B); raw.WriteByte(pixel.A);
            }
        }
        using MemoryStream compressed = new();
        using (ZLibStream zlib = new(compressed, CompressionLevel.SmallestSize, leaveOpen: true)) raw.ToArray().AsSpan().CopyToStream(zlib);
        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", []);
        File.WriteAllBytes(path, output.ToArray());
    }

    public static RgbaImage Read(string path)
    {
        byte[] file = File.ReadAllBytes(path);
        if (file.Length < Signature.Length || !file.AsSpan(0, 8).SequenceEqual(Signature)) throw new InvalidDataException($"'{path}' is not a PNG.");
        int cursor = 8, width = 0, height = 0;
        using MemoryStream idat = new();
        while (cursor < file.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(file.AsSpan(cursor, 4)); cursor += 4;
            string type = Encoding.ASCII.GetString(file, cursor, 4); cursor += 4;
            ReadOnlySpan<byte> data = file.AsSpan(cursor, length); cursor += length + 4;
            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data[..4]);
                height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4));
                if (data[8] != 8 || data[9] != 6 || data[12] != 0) throw new InvalidDataException("Pipeline accepts non-interlaced RGBA8 PNG only.");
            }
            else if (type == "IDAT") idat.Write(data);
            else if (type == "IEND") break;
        }
        if (width <= 0 || height <= 0) throw new InvalidDataException("PNG has no valid IHDR.");
        idat.Position = 0;
        using ZLibStream zlib = new(idat, CompressionMode.Decompress);
        using MemoryStream decoded = new();
        zlib.CopyTo(decoded);
        byte[] scanlines = decoded.ToArray();
        int stride = checked(width * 4);
        if (scanlines.Length != checked((stride + 1) * height)) throw new InvalidDataException("Unexpected PNG scanline size.");
        byte[] pixels = new byte[stride * height];
        int sourceOffset = 0;
        for (int y = 0; y < height; y++)
        {
            int filter = scanlines[sourceOffset++];
            int rowOffset = y * stride;
            for (int x = 0; x < stride; x++)
            {
                byte raw = scanlines[sourceOffset++];
                byte left = x >= 4 ? pixels[rowOffset + x - 4] : (byte)0;
                byte up = y > 0 ? pixels[rowOffset - stride + x] : (byte)0;
                byte upperLeft = y > 0 && x >= 4 ? pixels[rowOffset - stride + x - 4] : (byte)0;
                pixels[rowOffset + x] = filter switch
                {
                    0 => raw,
                    1 => unchecked((byte)(raw + left)),
                    2 => unchecked((byte)(raw + up)),
                    3 => unchecked((byte)(raw + ((left + up) / 2))),
                    4 => unchecked((byte)(raw + Paeth(left, up, upperLeft))),
                    _ => throw new InvalidDataException($"Unsupported PNG filter {filter}."),
                };
            }
        }
        Rgba[] rgba = new Rgba[width * height];
        for (int index = 0; index < rgba.Length; index++) rgba[index] = new(pixels[index * 4], pixels[index * 4 + 1], pixels[index * 4 + 2], pixels[index * 4 + 3]);
        return new(width, height, rgba);
    }

    private static byte Paeth(byte a, byte b, byte c)
    {
        int p = a + b - c, pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static void WriteChunk(Stream output, string type, byte[] data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        output.Write(length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        output.Write(typeBytes); output.Write(data);
        Span<byte> crc = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typeBytes.Concat(data).ToArray()));
        output.Write(crc);
    }

    private static uint Crc32(byte[] bytes)
    {
        uint crc = 0xffffffff;
        foreach (byte value in bytes)
        {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ (0xedb88320u & unchecked((uint)-(int)(crc & 1)));
        }
        return ~crc;
    }

    private static void CopyToStream(this Span<byte> bytes, Stream output) => output.Write(bytes);
}
