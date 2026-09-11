$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'tools\Zest.AssetPipeline\Zest.AssetPipeline.csproj'

& dotnet run --project $project -c Release -- validate
if ($LASTEXITCODE -ne 0) { throw 'ART-10 manifest/source/runtime validation failed.' }

$runtimeRoot = Join-Path $repoRoot 'game\art\pixel'
$pngFiles = Get-ChildItem -LiteralPath $runtimeRoot -Filter '*.png' -Recurse
if ($pngFiles.Count -lt 3) { throw 'ART-10 requires tile, prop, and character proof assets.' }

foreach ($png in $pngFiles) {
    $importPath = "$($png.FullName).import"
    if (-not (Test-Path -LiteralPath $importPath)) {
        throw "Godot import metadata missing for $($png.Name). Open/scan the project once."
    }
    $metadata = Get-Content -Raw -LiteralPath $importPath
    if (-not $metadata.Contains('importer="texture"')) { throw "$($png.Name) is not using the texture importer." }
    if (-not $metadata.Contains('compress/mode=0')) { throw "$($png.Name) must use lossless compression." }
    if (-not $metadata.Contains('mipmaps/generate=false')) { throw "$($png.Name) must disable mipmaps." }
    if (-not $metadata.Contains('detect_3d/compress_to=0')) { throw "$($png.Name) must disable automatic 3D texture conversion." }
}

Write-Output "ART-10 Godot import metadata validated for $($pngFiles.Count) assets."
