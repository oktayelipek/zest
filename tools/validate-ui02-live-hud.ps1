$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$mainPath = Join-Path $root 'game\scripts\Main.cs'
$iconPath = Join-Path $root 'game\scripts\PixelStatusIcon.cs'
$docPath = Join-Path $root 'docs\ui\world-first-live-interface.md'
$screenshotPath = Join-Path $root 'docs\artifacts\art-12-first-playable-pixel-world.png'

foreach ($path in @($mainPath, $iconPath, $docPath, $screenshotPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing UI-02 deliverable: $path" }
}

$main = Get-Content -Raw -LiteralPath $mainPath
$icon = Get-Content -Raw -LiteralPath $iconPath
$doc = Get-Content -Raw -LiteralPath $docPath

foreach ($requirement in @('HudGlyph.Clock', 'HudGlyph.Sun', 'HudGlyph.Coin', 'HudGlyph.Reputation', '_dayTime.Text', '_weather.Text', '_cashDelta.Text', '_reputation.Text')) {
    if (-not $main.Contains($requirement)) { throw "Missing UI-02 corner HUD contract: $requirement" }
}
foreach ($requirement in @('antialiased: false', 'CustomMinimumSize = new Vector2(22, 22)', 'DrawRect')) {
    if (-not $icon.Contains($requirement)) { throw "Missing UI-02 hybrid pixel icon contract: $requirement" }
}
foreach ($forbidden in @('_queueHud', '_stockHud', '_salesHud')) {
    if ($main.Contains($forbidden)) { throw "Global KPI must not return to live HUD: $forbidden" }
}
if (-not $doc.Contains('Urgency remains contextual')) { throw 'UI-02 contextual urgency behavior is undocumented.' }
if ((Get-Item -LiteralPath $screenshotPath).Length -lt 500000) { throw 'UI-02 1080p screenshot appears invalid.' }

Write-Output 'UI-02 game-first live HUD validated.'
