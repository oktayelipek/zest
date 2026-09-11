public static class VendorPoseCleanup
{
    private static readonly string[] PoseNames = ["prepare", "pour", "handoff", "reach", "cup-center"];

    public static void Run(string root)
    {
        string candidateFolder = Path.Combine(root, "art-source/concepts/art04/layer-candidates-v01");
        string sourceFolder = Path.Combine(root, "art-source/production/ai-layered-v01/vendor-poses-clean");
        Directory.CreateDirectory(sourceFolder);

        foreach (string pose in PoseNames)
        {
            string input = Path.Combine(candidateFolder, $"vendor-{pose}-rgba.png");
            RgbaImage image = Png.Read(input);
            bool[] exterior = FindExteriorNeutralPixels(image);
            int removed = 0;
            for (int i = 0; i < exterior.Length; i++)
            {
                if (!exterior[i]) continue;
                image.Pixels[i] = new Rgba(0, 0, 0, 0);
                removed++;
            }

            if (removed < image.Pixels.Length / 5 || removed > image.Pixels.Length * .85)
                throw new InvalidDataException($"Unexpected {pose} removal area: {removed}");

            (int x, int y, int width, int height) = AlphaBounds(image);
            string output = Path.Combine(sourceFolder, $"vendor-{pose}-clean.png");
            Png.Write(output, image);
            Console.WriteLine($"{pose,-8} removed={removed} alpha-bounds={width}x{height}+{x}+{y}");
        }
    }

    private static bool[] FindExteriorNeutralPixels(RgbaImage image)
    {
        bool[] visited = new bool[image.Pixels.Length];
        Queue<int> queue = new();
        void Visit(int x, int y)
        {
            if (x < 0 || y < 0 || x >= image.Width || y >= image.Height) return;
            int index = y * image.Width + x;
            if (visited[index]) return;
            Rgba pixel = image.Pixels[index];
            int maximum = Math.Max(pixel.R, Math.Max(pixel.G, pixel.B));
            int minimum = Math.Min(pixel.R, Math.Min(pixel.G, pixel.B));
            // Generated checkerboard is neutral grey/white. Restrict removal to the
            // exterior-connected component so eyes, shirt and cup details survive.
            if (pixel.A == 0 || minimum < 105 || maximum - minimum > 28) return;
            visited[index] = true;
            queue.Enqueue(index);
        }

        for (int x = 0; x < image.Width; x++) { Visit(x, 0); Visit(x, image.Height - 1); }
        for (int y = 0; y < image.Height; y++) { Visit(0, y); Visit(image.Width - 1, y); }
        while (queue.TryDequeue(out int index))
        {
            int x = index % image.Width;
            int y = index / image.Width;
            Visit(x - 1, y); Visit(x + 1, y); Visit(x, y - 1); Visit(x, y + 1);
        }
        return visited;
    }

    private static (int x, int y, int width, int height) AlphaBounds(RgbaImage image)
    {
        int left = image.Width, top = image.Height, right = -1, bottom = -1;
        for (int y = 0; y < image.Height; y++)
        for (int x = 0; x < image.Width; x++)
        {
            if (image[x, y].A == 0) continue;
            left = Math.Min(left, x); top = Math.Min(top, y);
            right = Math.Max(right, x); bottom = Math.Max(bottom, y);
        }
        if (right < left) throw new InvalidDataException("Cleaned pose is empty.");
        return (left, top, right - left + 1, bottom - top + 1);
    }
}
