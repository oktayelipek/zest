$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot '..\game\project.godot'
$mainPath = Join-Path $PSScriptRoot '..\game\scripts\Main.cs'
$worldPath = Join-Path $PSScriptRoot '..\game\scripts\ParkWorldView.cs'
$stylePath = Join-Path $PSScriptRoot '..\game\scripts\ZestStyle.cs'

$project = Get-Content -Raw -LiteralPath $projectPath
$main = Get-Content -Raw -LiteralPath $mainPath
$world = Get-Content -Raw -LiteralPath $worldPath
$style = Get-Content -Raw -LiteralPath $stylePath

$requiredProjectSettings = @(
    'window/size/viewport_width=1920',
    'window/size/viewport_height=1080',
    'window/stretch/mode="canvas_items"',
    'window/stretch/scale_mode="fractional"',
    'textures/canvas_textures/default_texture_filter=0',
    '2d/snap/snap_2d_transforms_to_pixel=true',
    '2d/snap/snap_2d_vertices_to_pixel=true'
)

foreach ($setting in $requiredProjectSettings) {
    if (-not $project.Contains($setting)) {
        throw "Missing pixel foundation setting: $setting"
    }
}

if (-not $world.Contains('TextureFilterEnum.Nearest')) {
    throw 'World viewport must explicitly use nearest filtering.'
}
if (-not $style.Contains('BaseResolution = new(1920, 1080)') -or
    -not $style.Contains('LogicalWorldResolution = new(640, 360)') -or
    -not $style.Contains('WorldRenderScale = 3')) {
    throw 'Resolution contract must remain 1920x1080 UI/output with a 640x360 world at 3x baseline scale.'
}
if (-not $world.Contains('Size = ZestStyle.PixelRendering.LogicalWorldResolution')) {
    throw 'World viewport must use the explicit 640x360 logical resolution.'
}
if ($world.Contains('=> .88f') -or $world.Contains('=> 1.48f')) {
    throw 'Camera framings must not introduce fractional pixel zoom.'
}
if (-not $main.Contains('TextureFilterEnum.Linear')) {
    throw 'Native UI root must explicitly declare its filtering boundary.'
}

Write-Output 'TECHART-01 pixel rendering foundation validated.'
