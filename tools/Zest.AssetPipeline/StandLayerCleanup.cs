public static class StandLayerCleanup
{
    public static void Run(string root)
    {
        string source = Path.Combine(root, "art-source/concepts/art04/layer-candidates-v01/stand-no-vendor-rgba.png");
        var image = Png.Read(source);
        var visited = new bool[image.Pixels.Length];
        var queue = new Queue<int>();
        void Visit(int x, int y)
        {
            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return;
            int i = y * image.Width + x;
            if (visited[i]) return;
            var p = image.Pixels[i];
            int max = Math.Max(p.R, Math.Max(p.G, p.B));
            int min = Math.Min(p.R, Math.Min(p.G, p.B));
            // Only exterior-connected light neutral checkerboard can be removed.
            if (p.A != 0 && (min < 115 || max - min > 30)) return;
            visited[i] = true;
            queue.Enqueue(i);
        }
        for (int x = 0; x < image.Width; x++) { Visit(x, 0); Visit(x, image.Height - 1); }
        for (int y = 0; y < image.Height; y++) { Visit(0, y); Visit(image.Width - 1, y); }
        while (queue.TryDequeue(out int i))
        {
            int x = i % image.Width, y = i / image.Width;
            Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
        }
        int removed = 0;
        for (int i = 0; i < visited.Length; i++)
            if (visited[i]) { image.Pixels[i] = new Rgba(0, 0, 0, 0); removed++; }
        if (removed < image.Pixels.Length / 10 || removed > image.Pixels.Length * .6)
            throw new InvalidDataException($"Unexpected removal area: {removed}");
        string folder = Path.Combine(root, "art-source/production/ai-layered-v01");
        string runtime = Path.Combine(root, "game/art/production/ai-layered-v01");
        Directory.CreateDirectory(folder); Directory.CreateDirectory(runtime);
        string output = Path.Combine(folder, "stand-body.png");
        Png.Write(output, image);
        File.Copy(output, Path.Combine(runtime, "stand-body.png"), true);
        // The single generated body cannot correctly occlude an independent vendor.
        // Republish its front fascia as a foreground layer; the source pixels stay identical.
        var fascia = new RgbaImage(image.Width, image.Height, new Rgba(0, 0, 0, 0));
        const int fasciaTop = 720;
        for (int y = fasciaTop; y < image.Height; y++)
        for (int x = 0; x < image.Width; x++)
            fascia[x, y] = image[x, y];
        string fasciaOutput = Path.Combine(folder, "stand-front-occluder.png");
        Png.Write(fasciaOutput, fascia);
        File.Copy(fasciaOutput, Path.Combine(runtime, "stand-front-occluder.png"), true);
        var proof = new RgbaImage(image.Width, image.Height, new Rgba(78, 112, 90, 255));
        for (int i = 0; i < image.Pixels.Length; i++)
            if (image.Pixels[i].A != 0) proof.Pixels[i] = image.Pixels[i];
        Png.Write(Path.Combine(root, "docs/artifacts/stand-body-alpha-proof.png"), proof);
        var reread = Png.Read(output);
        if (reread.Pixels.Count(p => p.A == 0) != removed) throw new InvalidDataException("Alpha roundtrip failed.");
        Console.WriteLine($"RGBA alpha verified: {image.Width}x{image.Height}; {removed} transparent pixels. Retained pixels unchanged.");
    }
}
