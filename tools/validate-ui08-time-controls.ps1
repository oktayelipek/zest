$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$mainPath = Join-Path $root 'game\scripts\Main.cs'
$clockPath = Join-Path $root 'src\Zest.Domain\Simulation\SimulationClock.cs'
$docPath = Join-Path $root 'docs\ui\world-first-live-interface.md'
$normalShot = Join-Path $root 'docs\artifacts\ui-08-time-controls.png'
$closingShot = Join-Path $root 'docs\artifacts\ui-08-closing-hour.png'

foreach ($path in @($mainPath, $clockPath, $docPath, $normalShot, $closingShot)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing UI-08 deliverable: $path" }
}

$main = Get-Content -Raw -LiteralPath $mainPath
$clock = Get-Content -Raw -LiteralPath $clockPath
$doc = Get-Content -Raw -LiteralPath $docPath

foreach ($requirement in @(
    'SpeedButton("Ⅱ", SimulationSpeed.Paused)',
    'SpeedButton("1×", SimulationSpeed.Normal)',
    'SpeedButton("2×", SimulationSpeed.Double)',
    'SpeedButton("4×", SimulationSpeed.Quadruple)',
    'SetSimulationSpeed',
    'case Key.Space',
    'OPEN 08:00',
    'CLOSE 18:00',
    'snapshot.SimTime >= 9 * 60 * 60',
    '_endDayButton.Visible = closingHour'
)) {
    if (-not $main.Contains($requirement)) { throw "Missing UI-08 time-control contract: $requirement" }
}

if (-not $clock.Contains('public void SetSpeed(SimulationSpeed speed)')) { throw 'UI-08 must use authoritative SimulationClock speed.' }
if ($main.Contains('Advance 2 hours')) { throw 'Debug time advance leaked into final UI.' }
if (-not $doc.Contains('Space` pauses and resumes the last active speed')) { throw 'UI-08 pause/resume behavior is undocumented.' }

foreach ($shot in @($normalShot, $closingShot)) {
    if ((Get-Item -LiteralPath $shot).Length -lt 100000) { throw "UI-08 screenshot appears invalid: $shot" }
}

Write-Output 'UI-08 compact time controls and closing-hour behavior validated.'
