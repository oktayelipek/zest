$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$scene = Join-Path $root 'game\scenes\art12_visual_benchmark.tscn'
$script = Join-Path $root 'game\scripts\Art12VisualBenchmark.cs'
$sourceStand = Join-Path $root 'art-source\benchmark\prop_zest_stand_hd_candidate_v01.png'
$runtimeStand = Join-Path $root 'game\art\benchmark\prop_zest_stand_hd_candidate_v01.png'
$target = Join-Path $root 'game\art\benchmark\art12_world_target_v01.png'
$standPreview = Join-Path $root 'docs\artifacts\art-12-stand-benchmark.png'
$targetPreview = Join-Path $root 'docs\artifacts\art-12-visual-benchmark.png'

foreach ($path in @($scene, $script, $sourceStand, $runtimeStand, $target, $standPreview, $targetPreview)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing ART-12 visual benchmark deliverable: $path" }
}

if (-not ((Get-Content -Raw -LiteralPath $script).Contains('key.Keycode != Key.Tab'))) {
    throw 'The benchmark must provide direct target/candidate comparison.'
}

Add-Type -AssemblyName System.Drawing
$bitmap = [System.Drawing.Bitmap]::FromFile($runtimeStand)
try {
    if ($bitmap.Width -lt 1024 -or $bitmap.Height -lt 768) { throw 'HD stand candidate resolution is below the benchmark floor.' }
    if ($bitmap.GetPixel(0, 0).A -ne 0 -or $bitmap.GetPixel($bitmap.Width - 1, 0).A -ne 0) {
        throw 'HD stand candidate must have genuine transparent alpha at its outer corners.'
    }
}
finally {
    $bitmap.Dispose()
}

foreach ($png in @($runtimeStand, $target)) {
    $import = "$png.import"
    if (-not (Test-Path -LiteralPath $import)) { throw "Missing Godot import metadata: $import" }
    $metadata = Get-Content -Raw -LiteralPath $import
    foreach ($setting in @('compress/mode=0', 'mipmaps/generate=false', 'detect_3d/compress_to=0')) {
        if (-not $metadata.Contains($setting)) { throw "Benchmark asset must use '$setting': $png" }
    }
}

Write-Output 'ART-12 visual benchmark and transparent HD stand candidate validated.'
