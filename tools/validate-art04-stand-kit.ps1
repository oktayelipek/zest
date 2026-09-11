$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$ffprobe = (Get-Command ffprobe -ErrorAction Stop).Source
$codePaths = @(
    (Join-Path $root 'game\scripts\ProductionParkCanvas.cs'),
    (Join-Path $root 'game\scripts\ParkWorldView.cs'),
    (Join-Path $root 'game\scripts\Main.cs'),
    (Join-Path $root 'game\scripts\PreviewCapture.cs')
)
$assets = @(
    'prop_zest_stand_base_idle_v03.png',
    'prop_zest_stand_better_counter_idle_v02.png',
    'prop_zest_stand_electric_juicer_idle_v02.png',
    'prop_zest_stand_bigger_cooler_idle_v02.png'
)
$atlas = 'prop_zest_stand_upgrades_4x1_v01.png'
$deliverables = @(
    (Join-Path $root 'docs\art-direction\art-04-zest-stand-upgrade-visual-kit.md'),
    (Join-Path $root 'tools\Zest.AssetPipeline\NativeStandKit.cs')
) + $codePaths

foreach ($asset in $assets) {
    $deliverables += Join-Path $root "art-source\production\final-grid-v03\$asset"
    $deliverables += Join-Path $root "game\art\production\final-grid-v03\$asset"
}
$deliverables += Join-Path $root "art-source\production\final-grid-v03\$atlas"
$deliverables += Join-Path $root "game\art\production\final-grid-v03\$atlas"
$screenshots = @(
    (Join-Path $root 'docs\artifacts\art-04-better-counter.png'),
    (Join-Path $root 'docs\artifacts\art-04-electric-juicer-rush-menu.png'),
    (Join-Path $root 'docs\artifacts\art-04-bigger-cooler-sold-out.png')
)
$deliverables += $screenshots

foreach ($path in $deliverables) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing ART-04 deliverable: $path" }
}

$canvas = Get-Content -Raw -LiteralPath $codePaths[0]
$world = Get-Content -Raw -LiteralPath $codePaths[1]
$main = Get-Content -Raw -LiteralPath $codePaths[2]
$capture = Get-Content -Raw -LiteralPath $codePaths[3]
$contracts = @(
    'public enum StandUpgradeVisual { Base, BetterCounter, ElectricJuicer, BiggerCooler }',
    'public enum StandOperatingVisual { Normal, SoldOut, RushMenu }',
    'class ZestStandVisual',
    'SetPresentation(StandUpgradeVisual upgrade, StandOperatingVisual operatingState, int preparedBatchCount)',
    'StandGridRoot = "res://art/production/final-grid-v03/"',
    'prop_zest_stand_upgrades_4x1_v01.png',
    'RegionEnabled = true',
    'RegionRect = new Rect2((int)upgrade * 170, 0, 170, 136)',
    'PreparedBatchCue',
    'class PixelWorldText',
    'PREP +',
    'Native 5×7 world font',
    'Mathf.Round(Mathf.Sin',
    'TextureFilterEnum.Nearest'
)
foreach ($contract in $contracts) {
    if (-not $canvas.Contains($contract)) { throw "Missing ART-04 stand contract: $contract" }
}
$sourceAtlas = Join-Path $root "art-source\production\final-grid-v03\$atlas"
$runtimeAtlas = Join-Path $root "game\art\production\final-grid-v03\$atlas"
$atlasMetadata = & $ffprobe -v error -select_streams v:0 -show_entries stream=width,height,pix_fmt -of csv=p=0 $runtimeAtlas
if ($LASTEXITCODE -ne 0 -or -not $atlasMetadata.StartsWith('680,136,')) {
    throw "ART-04 sprite atlas must be 680x136: $runtimeAtlas ($atlasMetadata)"
}
if (-not ((Get-FileHash -Algorithm SHA256 -LiteralPath $sourceAtlas).Hash -eq (Get-FileHash -Algorithm SHA256 -LiteralPath $runtimeAtlas).Hash)) {
    throw 'ART-04 sprite atlas must be byte-identical to its native source.'
}
$atlasImport = "$runtimeAtlas.import"
if (-not (Test-Path -LiteralPath $atlasImport)) { throw "Missing Godot atlas import metadata: $atlasImport" }
if (-not (Get-Content -Raw -LiteralPath $atlasImport).Contains('detect_3d/compress_to=0')) {
    throw "ART-04 atlas has 3D compression enabled: $atlasImport"
}
if (-not $world.Contains('StandOperatingVisual.SoldOut') -or -not $world.Contains('StandOperatingVisual.RushMenu')) {
    throw 'World state does not drive sold-out and Rush Menu stand visuals.'
}
if (-not $main.Contains('RushMenuProductIds.Count > 0') -or -not $main.Contains('PreparedBatches.Count')) {
    throw 'Authoritative live state is not mapped to ART-04 presentation cues.'
}
foreach ($mode in @('art04-counter', 'art04-juicer', 'art04-cooler')) {
    if (-not $capture.Contains($mode)) { throw "Missing ART-04 capture mode: $mode" }
}

foreach ($asset in $assets) {
    $source = Join-Path $root "art-source\production\final-grid-v03\$asset"
    $runtime = Join-Path $root "game\art\production\final-grid-v03\$asset"
    $metadata = & $ffprobe -v error -select_streams v:0 -show_entries stream=width,height,pix_fmt -of csv=p=0 $runtime
    if ($LASTEXITCODE -ne 0 -or -not $metadata.StartsWith('170,136,')) {
        throw "ART-04 final-grid asset must be 170x136: $runtime ($metadata)"
    }
    if ((Get-Item -LiteralPath $runtime).Length -lt 800) { throw "ART-04 runtime asset appears empty: $runtime" }
    if (-not ((Get-FileHash -Algorithm SHA256 -LiteralPath $source).Hash -eq (Get-FileHash -Algorithm SHA256 -LiteralPath $runtime).Hash)) {
        throw "ART-04 runtime must be byte-identical to its native source: $asset"
    }
    $import = "$runtime.import"
    if (-not (Test-Path -LiteralPath $import)) { throw "Missing Godot import metadata: $import" }
    if (-not (Get-Content -Raw -LiteralPath $import).Contains('detect_3d/compress_to=0')) {
        throw "ART-04 runtime asset has 3D compression enabled: $import"
    }
}
$pipelineSource = Get-Content -Raw -LiteralPath (Join-Path $root 'tools\Zest.AssetPipeline\NativeStandKit.cs')
foreach ($requirement in @('Width = 170', 'Height = 136', 'HashSet<Rgba> palette', 'off-palette pixels', 'RgbaImage image = new(Width, Height, Clear)')) {
    if (-not $pipelineSource.Contains($requirement)) { throw "Missing native-grid production contract: $requirement" }
}
if ($canvas.Contains('final-grid-v02/prop_zest_stand_') -or $canvas.Contains('_idle_v01.png')) {
    throw 'ART-04 runtime must not reference the rejected downsampled AI stand kit.'
}
foreach ($screenshot in $screenshots) {
    $metadata = & $ffprobe -v error -select_streams v:0 -show_entries stream=width,height -of csv=p=0 $screenshot
    if ($LASTEXITCODE -ne 0 -or $metadata -ne '1920,1080') { throw "ART-04 evidence must be 1920x1080: $screenshot ($metadata)" }
    if ((Get-Item -LiteralPath $screenshot).Length -lt 300000) { throw "ART-04 evidence appears empty: $screenshot" }
}

Write-Output 'ART-04 stand kit validated: one native 680x136 four-frame sprite atlas, 5x7 world text, operational cues, and 3 native 1080p captures.'
