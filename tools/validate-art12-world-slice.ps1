$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$scenePath = Join-Path $root 'game\scenes\art12_playable.tscn'
$worldPath = Join-Path $root 'game\scripts\ParkWorldView.cs'
$canvasPath = Join-Path $root 'game\scripts\ProductionParkCanvas.cs'
$manifestPath = Join-Path $root 'art-source\pipeline.json'
$screenshotPath = Join-Path $root 'docs\artifacts\art-12-first-playable-pixel-world.png'
$productionAssets = @(
    (Join-Path $root 'game\art\production\final-grid-v02\bg_riverside_640x360_v02.png'),
    (Join-Path $root 'game\art\production\final-grid-v02\prop_zest_stand_idle_v02.png'),
    (Join-Path $root 'game\art\production\final-grid-v02\chr_guest_walk_4x4_v02.png'),
    (Join-Path $root 'game\art\production\final-grid-v02\prop_park_tree_idle_v02.png'),
    (Join-Path $root 'game\art\production\final-grid-v02\prop_park_bench_idle_v02.png'),
    (Join-Path $root 'game\art\production\final-grid-v02\prop_lamp_sign_idle_v02.png'),
    (Join-Path $root 'game\art\production\final-grid-v02\prop_flower_planter_idle_v02.png'),
    (Join-Path $root 'game\art\production\final-grid-v02\prop_bush_wind_4x1_v02.png')
)

foreach ($path in @($scenePath, $worldPath, $canvasPath, $manifestPath, $screenshotPath) + $productionAssets) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing ART-12 deliverable: $path" }
}

$scene = Get-Content -Raw -LiteralPath $scenePath
$world = Get-Content -Raw -LiteralPath $worldPath
$canvas = Get-Content -Raw -LiteralPath $canvasPath
$manifest = Get-Content -Raw -LiteralPath $manifestPath | ConvertFrom-Json

if (-not $scene.Contains('StartInLiveStudy = true')) { throw 'ART-12 scene must open directly into the playable live slice.' }
if ($world.Contains('Node3D') -or $world.Contains('MeshInstance3D')) { throw 'ART-12 must not retain the low-poly 3D greybox world.' }

$worldRequirements = @(
    'TextureFilterEnum.Nearest',
    'Camera2D',
    'PositionSmoothingEnabled = false',
    'YSortEnabled = true',
    'QueuePositions',
    'WorldSelectionRequested'
)
foreach ($requirement in $worldRequirements) {
    if (-not ($world.Contains($requirement) -or $canvas.Contains($requirement))) {
        throw "Missing ART-12 world contract: $requirement"
    }
}

$movementRequirements = @('RegionEnabled = true', 'direction.X < 0 ? 1 : 2', 'direction.Y < 0 ? 3 : 0', '_precisePosition += direction * 36f * (float)delta', 'Position = _precisePosition.Round()', 'AddAnimatedProp("WindBush"', 'sprite.Play("idle")', 'Scale = Vector2.One', 'RuntimeScale = .035f', 'SetDirection(int row, bool walking)')
foreach ($requirement in $movementRequirements) {
    if (-not $canvas.Contains($requirement)) { throw "Missing directional movement contract: $requirement" }
}

$requiredAssets = @('grass_base', 'path_edge_base', 'zest_stand_base', 'lemon_crate', 'guest_base_walk', 'park_tree_leafy', 'park_bench_wood', 'park_lamp_sign', 'park_planter_flowers')
$assetIds = @($manifest.assets | ForEach-Object { $_.id })
foreach ($assetId in $requiredAssets) {
    if ($assetIds -notcontains $assetId) { throw "ART-12 manifest entry is missing: $assetId" }
}

foreach ($assetPath in $productionAssets) {
    if ((Get-Item -LiteralPath $assetPath).Length -lt 4000) { throw "Production asset appears empty or invalid: $assetPath" }
    $importPath = "$assetPath.import"
    if (-not (Test-Path -LiteralPath $importPath)) { throw "Missing Godot import metadata: $importPath" }
    $import = Get-Content -Raw -LiteralPath $importPath
    if (-not $import.Contains('detect_3d/compress_to=0')) { throw "2D production asset has 3D compression enabled: $importPath" }
}

if ((Get-Item -LiteralPath $screenshotPath).Length -lt 500000) { throw 'ART-12 production screenshot appears empty or invalid.' }

# Production-layer customer sprites intentionally use a proportional .035 scale
# from their shared 800x1400 source canvas; nearest filtering remains the crispness gate.

Write-Output "ART-12 production playable slice validated with $($productionAssets.Count) final-grid assets, animated wind bush, walking guest, and $($requiredAssets.Count) manifest fallbacks."
