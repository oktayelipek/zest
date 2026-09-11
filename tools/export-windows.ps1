param(
    [string]$GodotPath,
    [string]$OutputPath = "dist/Zest.exe"
)

$ErrorActionPreference = "Stop"

function Resolve-Godot {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath)) {
            throw "Godot was not found at -GodotPath '$ExplicitPath'."
        }
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    if ($env:ZEST_GODOT) {
        if (-not (Test-Path -LiteralPath $env:ZEST_GODOT)) {
            throw "ZEST_GODOT points to a file that does not exist: '$env:ZEST_GODOT'."
        }
        return (Resolve-Path -LiteralPath $env:ZEST_GODOT).Path
    }

    foreach ($candidate in @("godot4", "godot")) {
        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($command) { return $command.Source }
    }

    throw "Godot 4 .NET was not found. Pass -GodotPath or set ZEST_GODOT."
}

$root = Split-Path -Parent $PSScriptRoot
$godot = Resolve-Godot $GodotPath
$absoluteOutput = [System.IO.Path]::GetFullPath((Join-Path $root $OutputPath))
$outputDirectory = Split-Path -Parent $absoluteOutput
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

& $godot --headless --path (Join-Path $root "game") --export-release "Windows Desktop" $absoluteOutput
if ($LASTEXITCODE -ne 0) {
    throw "Godot export failed with exit code $LASTEXITCODE."
}

Write-Host "Exported Windows build: $absoluteOutput"
