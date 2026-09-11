$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$ffmpegCommand = Get-Command ffmpeg -ErrorAction Stop
$ffmpeg = $ffmpegCommand.Source
$source = Join-Path $root 'art-source\production'
$output = Join-Path $source 'final-grid-v02'
$runtime = Join-Path $root 'game\art\production\final-grid-v02'
New-Item -ItemType Directory -Force -Path $output, $runtime | Out-Null

function Invoke-FrameBuild([string] $inputName, [string] $filter, [string] $outputName) {
    $inputPath = Join-Path $source $inputName
    $outputPath = Join-Path $output $outputName
    if (-not (Test-Path -LiteralPath $inputPath)) { throw "Missing ART-12 source master: $inputPath" }
    & $ffmpeg -hide_banner -loglevel error -y -i $inputPath -vf $filter -frames:v 1 $outputPath
    if ($LASTEXITCODE -ne 0) { throw "Failed to build final-grid asset: $outputName" }
    Copy-Item -LiteralPath $outputPath -Destination (Join-Path $runtime $outputName) -Force
}

Invoke-FrameBuild 'art12_riverside_background_v01.png' 'scale=640:360:flags=neighbor,format=rgba' 'bg_riverside_640x360_v02.png'
Invoke-FrameBuild 'prop_zest_stand_hd_v01.png' 'scale=170:136:flags=neighbor,format=rgba' 'prop_zest_stand_idle_v02.png'
Invoke-FrameBuild 'chr_guest_hd_walk_v01.png' 'scale=192:192:flags=neighbor,format=rgba' 'chr_guest_walk_4x4_v02.png'
Invoke-FrameBuild 'art12_environment_props_atlas_v01.png' 'crop=410:311:455:740,scale=86:65:flags=neighbor,format=rgba' 'prop_park_bench_idle_v02.png'
Invoke-FrameBuild 'art12_environment_props_atlas_v01.png' 'crop=178:466:862:585,scale=43:112:flags=neighbor,format=rgba' 'prop_lamp_sign_idle_v02.png'
Invoke-FrameBuild 'art12_environment_props_atlas_v01.png' 'crop=264:229:1038:814,scale=58:50:flags=neighbor,format=rgba' 'prop_flower_planter_idle_v02.png'
Invoke-FrameBuild 'anim_bush_wind_hd_alpha_v02.png' 'crop=2172:408:0:132,scale=256:48:flags=neighbor,format=rgba' 'prop_bush_wind_4x1_v02.png'

$treeInput = Join-Path $source 'art12_environment_props_atlas_v01.png'
$treeOutput = Join-Path $output 'prop_park_tree_idle_v02.png'
& $ffmpeg -hide_banner -loglevel error -y -i $treeInput -filter_complex 'color=c=black@0:s=129x139,format=rgba[base];[0:v]crop=383:516:0:535,scale=103:139:flags=neighbor,format=rgba[left];[0:v]crop=95:466:383:585,scale=26:126:flags=neighbor,format=rgba[right];[base][left]overlay=0:0:format=auto[tmp];[tmp][right]overlay=103:13:format=auto' -frames:v 1 $treeOutput
if ($LASTEXITCODE -ne 0) { throw 'Failed to build final-grid tree asset.' }
Copy-Item -LiteralPath $treeOutput -Destination (Join-Path $runtime 'prop_park_tree_idle_v02.png') -Force

Write-Output 'ART-12 final-grid assets rebuilt and published. Run Godot import before validation.'
