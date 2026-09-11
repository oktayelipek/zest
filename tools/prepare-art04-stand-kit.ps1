$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$generatedRoot = Join-Path $root 'art-source\generative\art04'
$productionRoot = Join-Path $root 'art-source\concepts\art04\ai-cleaned-v01'
$finalGridRoot = Join-Path $root 'art-source\concepts\art04\downsampled-source-v01'
$ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source
New-Item -ItemType Directory -Force -Path $productionRoot, $finalGridRoot | Out-Null

if (-not ('Zest.ArtTools.BorderTransparency' -as [type])) {
    Add-Type -AssemblyName System.Drawing.Common
    $frameworkReferences = [string][AppContext]::GetData('TRUSTED_PLATFORM_ASSEMBLIES') -split [IO.Path]::PathSeparator
    Add-Type -ReferencedAssemblies $frameworkReferences -TypeDefinition @'
using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Zest.ArtTools
{
    public static class BorderTransparency
    {
        public static void Clean(string inputPath, string outputPath)
        {
            using var source = new Bitmap(inputPath);
            using var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.DrawImageUnscaled(source, 0, 0);
            }

            Rectangle bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            int stride = Math.Abs(data.Stride);
            byte[] pixels = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);

            int count = bitmap.Width * bitmap.Height;
            bool[] cleared = new bool[count];
            Queue queue = new Queue();
            for (int x = 0; x < bitmap.Width; x++)
            {
                Enqueue(x, 0);
                Enqueue(x, bitmap.Height - 1);
            }
            for (int y = 1; y < bitmap.Height - 1; y++)
            {
                Enqueue(0, y);
                Enqueue(bitmap.Width - 1, y);
            }

            while (queue.Count > 0)
            {
                int index = (int)queue.Dequeue();
                int x = index % bitmap.Width;
                int y = index / bitmap.Width;
                int offset = y * stride + x * 4;
                pixels[offset] = 0;
                pixels[offset + 1] = 0;
                pixels[offset + 2] = 0;
                pixels[offset + 3] = 0;
                if (x > 0) Enqueue(x - 1, y);
                if (x + 1 < bitmap.Width) Enqueue(x + 1, y);
                if (y > 0) Enqueue(x, y - 1);
                if (y + 1 < bitmap.Height) Enqueue(x, y + 1);
            }

            // Peel a narrow neutral fringe left by antialiasing against the generated checkerboard.
            for (int pass = 0; pass < 3; pass++)
            {
                ArrayList fringe = new ArrayList();
                for (int y = 1; y < bitmap.Height - 1; y++)
                for (int x = 1; x < bitmap.Width - 1; x++)
                {
                    int index = y * bitmap.Width + x;
                    if (cleared[index] || !IsNeutral(x, y, 38, 118)) continue;
                    if (cleared[index - 1] || cleared[index + 1] || cleared[index - bitmap.Width] || cleared[index + bitmap.Width])
                        fringe.Add(index);
                }
                foreach (int index in fringe)
                {
                    cleared[index] = true;
                    int x = index % bitmap.Width;
                    int y = index / bitmap.Width;
                    int offset = y * stride + x * 4;
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = pixels[offset + 3] = 0;
                }
            }

            // Remove isolated checkerboard/noise islands while preserving the connected stand artwork.
            bool[] componentSeen = new bool[count];
            for (int start = 0; start < count; start++)
            {
                if (componentSeen[start] || cleared[start] || pixels[(start / bitmap.Width) * stride + (start % bitmap.Width) * 4 + 3] == 0)
                    continue;
                Queue componentQueue = new Queue();
                ArrayList component = new ArrayList();
                componentSeen[start] = true;
                componentQueue.Enqueue(start);
                while (componentQueue.Count > 0)
                {
                    int index = (int)componentQueue.Dequeue();
                    component.Add(index);
                    int x = index % bitmap.Width;
                    int y = index / bitmap.Width;
                    Visit(index - 1, x > 0);
                    Visit(index + 1, x + 1 < bitmap.Width);
                    Visit(index - bitmap.Width, y > 0);
                    Visit(index + bitmap.Width, y + 1 < bitmap.Height);
                }
                if (component.Count >= 5000) continue;
                foreach (int index in component)
                {
                    cleared[index] = true;
                    int x = index % bitmap.Width;
                    int y = index / bitmap.Width;
                    int offset = y * stride + x * 4;
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = pixels[offset + 3] = 0;
                }

                void Visit(int index, bool inside)
                {
                    if (!inside || componentSeen[index] || cleared[index]) return;
                    int x = index % bitmap.Width;
                    int y = index / bitmap.Width;
                    if (pixels[y * stride + x * 4 + 3] == 0) return;
                    componentSeen[index] = true;
                    componentQueue.Enqueue(index);
                }
            }

            // The generated canvas has a guaranteed empty trim around the tightly framed stand.
            for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (y >= 62 && x >= 32 && x < bitmap.Width - 32) continue;
                int offset = y * stride + x * 4;
                pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = pixels[offset + 3] = 0;
            }

            Marshal.Copy(pixels, 0, data.Scan0, pixels.Length);
            bitmap.UnlockBits(data);
            bitmap.Save(outputPath, ImageFormat.Png);

            void Enqueue(int x, int y)
            {
                int index = y * bitmap.Width + x;
                if (cleared[index] || !IsNeutral(x, y, 26, 145)) return;
                cleared[index] = true;
                queue.Enqueue(index);
            }

            bool IsNeutral(int x, int y, int maximumChroma, int minimumChannel)
            {
                int offset = y * stride + x * 4;
                int b = pixels[offset];
                int g = pixels[offset + 1];
                int r = pixels[offset + 2];
                int maximum = Math.Max(r, Math.Max(g, b));
                int minimum = Math.Min(r, Math.Min(g, b));
                return pixels[offset + 3] > 0 && maximum - minimum <= maximumChroma && minimum >= minimumChannel;
            }
        }
    }
}
'@
}

$variants = @(
    @{ Id = 'better_counter'; Raw = 'prop_zest_stand_better_counter_generated_v01.png' },
    @{ Id = 'electric_juicer'; Raw = 'prop_zest_stand_electric_juicer_generated_v01.png' },
    @{ Id = 'bigger_cooler'; Raw = 'prop_zest_stand_bigger_cooler_generated_v01.png' }
)

foreach ($variant in $variants) {
    $inputPath = Join-Path $generatedRoot $variant.Raw
    $cleanName = "prop_zest_stand_$($variant.Id)_hd_v01.png"
    $finalName = "prop_zest_stand_$($variant.Id)_idle_v01.png"
    $cleanPath = Join-Path $productionRoot $cleanName
    $finalPath = Join-Path $finalGridRoot $finalName
    if (-not (Test-Path -LiteralPath $inputPath)) { throw "Missing ART-04 generated source: $inputPath" }
    [Zest.ArtTools.BorderTransparency]::Clean($inputPath, $cleanPath)
    & $ffmpeg -hide_banner -loglevel error -y -i $cleanPath -vf 'scale=170:136:flags=neighbor,format=rgba' -frames:v 1 $finalPath
    if ($LASTEXITCODE -ne 0) { throw "Failed to build ART-04 final-grid variant: $finalName" }
}

Write-Output "ART-04 concept references prepared: $($variants.Count) cleaned masters and downsample studies; nothing published to runtime."
