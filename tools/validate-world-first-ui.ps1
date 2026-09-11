$ErrorActionPreference = 'Stop'
$mainPath = Join-Path $PSScriptRoot '..\game\scripts\Main.cs'
$worldPath = Join-Path $PSScriptRoot '..\game\scripts\ParkWorldView.cs'
$main = Get-Content -Raw -LiteralPath $mainPath
$world = Get-Content -Raw -LiteralPath $worldPath

$forbidden = @(
    'Advance 2 hours',
    'ActionButton("Neighborhood"',
    'ActionButton("Business"',
    'ActionButton("Observe"',
    'ActionButton("Track guest"'
)
foreach ($item in $forbidden) {
    if ($main.Contains($item)) { throw "Player-facing legacy UI remains: $item" }
}

$required = @(
    'SpeedButton("Ⅱ", SimulationSpeed.Paused)',
    'SpeedButton("1×", SimulationSpeed.Normal)',
    'SpeedButton("2×", SimulationSpeed.Double)',
    'SpeedButton("4×", SimulationSpeed.Quadruple)',
    'WorldSelectionRequested += HandleWorldSelection',
    'ProductRow(',
    'ShowManagement('
)
foreach ($item in $required) {
    if (-not $main.Contains($item)) { throw "Missing world-first UI contract: $item" }
}
if (-not $world.Contains('SetProductStatus') -or -not $world.Contains('QUEUE')) {
    throw 'World-space queue and stock feedback must remain implemented.'
}

Write-Output 'World-first live UI contract validated.'
