$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$skinPath = Join-Path $root 'game\scripts\ZestUiSkin.cs'
$mainPath = Join-Path $root 'game\scripts\Main.cs'
$docPath = Join-Path $root 'docs\ui\ui-11-hybrid-pixel-components.md'

foreach ($path in @($skinPath, $mainPath, $docPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing UI-11 deliverable: $path" }
}

$skin = Get-Content -Raw -LiteralPath $skinPath
$main = Get-Content -Raw -LiteralPath $mainPath
$doc = Get-Content -Raw -LiteralPath $docPath

$skinRequirements = @('TooltipPanel', 'font_disabled_color', '"normal"', '"hover"', '"pressed"', '"disabled"', '"focus"', 'AntiAliasing = false', 'StyleBoxFlat')
foreach ($requirement in $skinRequirements) {
    if (-not $skin.Contains($requirement)) { throw "Missing UI-11 skin contract: $requirement" }
}

$usageRequirements = @('Theme = ZestUiSkin.CreateTheme()', 'ZestUiSkin.ApplyButton', 'ToggleMode = true', 'SelectSpeed(speed)', 'ZestUiSkin.Tooltip')
foreach ($requirement in $usageRequirements) {
    if (-not $main.Contains($requirement)) { throw "Missing UI-11 component usage: $requirement" }
}

foreach ($term in @('Figma mapping', 'Selected control', 'Keyboard focus', 'Warning')) {
    if (-not $doc.Contains($term)) { throw "Missing UI-11 reusable rule documentation: $term" }
}

Write-Output 'UI-11 hybrid pixel skin and reusable component rules validated.'
