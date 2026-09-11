param(
    [switch]$NativeGround,
    [string]$GodotPath
)

$project = Join-Path $PSScriptRoot '..\game'
if ([string]::IsNullOrWhiteSpace($GodotPath)) { $GodotPath = $env:ZEST_GODOT }
if ([string]::IsNullOrWhiteSpace($GodotPath))
{
    $candidate = Get-Command godot, godot4 -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($null -ne $candidate) { $GodotPath = $candidate.Source }
}
if ([string]::IsNullOrWhiteSpace($GodotPath) -or -not (Test-Path -LiteralPath $GodotPath))
{
    throw "Godot .NET executable was not found. Put 'godot' on PATH, set ZEST_GODOT, or pass -GodotPath <path>."
}

if ($NativeGround) { $env:ZEST_NATIVE_GROUND = '1' } else { Remove-Item Env:ZEST_NATIVE_GROUND -ErrorAction SilentlyContinue }
& $GodotPath --path $project
