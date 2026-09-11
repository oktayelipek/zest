public static class CustomerPoseCleanup
{
    private static readonly string[] PoseNames = ["west", "receive", "north-b", "west-b", "south-a", "leave"];

    public static void Run(string root)
    {
        string candidateFolder = Path.Combine(root, "art-source/concepts/art05/layer-candidates-v01");
        string outputFolder = Path.Combine(root, "art-source/production/ai-layered-v01/customer-poses-clean");
        Directory.CreateDirectory(outputFolder);
        foreach (string pose in PoseNames)
        {
            RgbaImage image = Png.Read(Path.Combine(candidateFolder, $"customer-{pose}-rgba.png"));
            bool[] exterior = FindExteriorNeutralPixels(image);
            int removed = 0;
            for (int i = 0; i < exterior.Length; i++)
            {
                if (!exterior[i]) continue;
                image.Pixels[i] = new Rgba(0, 0, 0, 0);
                removed++;
            }
            if (removed < image.Pixels.Length / 5 || removed > image.Pixels.Length * .9)
                throw new InvalidDataException($"Unexpected customer {pose} removal area: {removed}");
            (int x, int y, int width, int height) = AlphaBounds(image);
            Png.Write(Path.Combine(outputFolder, $"customer-{pose}-clean.png"), image);
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
            Rgba p = image.Pixels[index];
            int max = Math.Max(p.R, Math.Max(p.G, p.B));
            int min = Math.Min(p.R, Math.Min(p.G, p.B));
            if (p.A == 0 || min < 105 || max - min > 28) return;
            visited[index] = true;
            queue.Enqueue(index);
        }
        for (int x = 0; x < image.Width; x++) { Visit(x, 0); Visit(x, image.Height - 1); }
        for (int y = 0; y < image.Height; y++) { Visit(0, y); Visit(image.Width - 1, y); }
        while (queue.TryDequeue(out int index))
        {
            int x = index % image.Width, y = index / image.Width;
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
        if (right < left) throw new InvalidDataException("Cleaned customer pose is empty.");
        return (left, top, right - left + 1, bottom - top + 1);
    }
}
