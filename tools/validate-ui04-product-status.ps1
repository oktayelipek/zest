$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$mainPath = Join-Path $root 'game\scripts\Main.cs'
$worldPath = Join-Path $root 'game\scripts\ParkWorldView.cs'
$iconPath = Join-Path $root 'game\scripts\PixelStatusIcon.cs'
$docPath = Join-Path $root 'docs\ui\world-first-live-interface.md'
$standShot = Join-Path $root 'docs\artifacts\ui-04-stand-context.png'
$detailsShot = Join-Path $root 'docs\artifacts\ui-04-inventory-details.png'
$syncShot = Join-Path $root 'docs\artifacts\ui-04-paused-world-sync.png'

foreach ($path in @($mainPath, $worldPath, $iconPath, $docPath, $standShot, $detailsShot, $syncShot)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing UI-04 deliverable: $path" }
}

$main = Get-Content -Raw -LiteralPath $mainPath
$world = Get-Content -Raw -LiteralPath $worldPath
$icon = Get-Content -Raw -LiteralPath $iconPath
$doc = Get-Content -Raw -LiteralPath $docPath

foreach ($requirement in @('HudGlyph.Lemon', 'HudGlyph.Berry', '"SOLD OUT"', '"FRESH"', 'CurrentPrice("berry")', 'SellableServings("berry")', 'ShowInventoryDetails', 'Back to stand')) {
    if (-not $main.Contains($requirement)) { throw "Missing UI-04 stand product contract: $requirement" }
}
foreach ($requirement in @('case HudGlyph.Lemon', 'case HudGlyph.Berry')) {
    if (-not $icon.Contains($requirement)) { throw "Missing UI-04 product glyph: $requirement" }
}
foreach ($requirement in @('SetProductStatus(stock, IsProductDisabled("classic")', '"CLASSIC OFF"', '"SOLD OUT"')) {
    if (-not ($main + $world).Contains($requirement)) { throw "Missing UI-04 world/panel synchronization contract: $requirement" }
}
if (-not $doc.Contains('Inventory is not a permanent dashboard')) { throw 'UI-04 contextual product behavior is undocumented.' }

foreach ($shot in @($standShot, $detailsShot, $syncShot)) {
    if ((Get-Item -LiteralPath $shot).Length -lt 100000) { throw "UI-04 screenshot appears invalid: $shot" }
}

Write-Output 'UI-04 contextual inventory and product status validated.'
