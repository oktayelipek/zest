$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$mainPath = Join-Path $root 'game\scripts\Main.cs'
$docPath = Join-Path $root 'docs\ui\world-first-live-interface.md'
$customerShot = Join-Path $root 'docs\artifacts\ui-03-customer-context.png'
$managementShot = Join-Path $root 'docs\artifacts\ui-03-management-layer.png'

foreach ($path in @($mainPath, $docPath, $customerShot, $managementShot)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing UI-03 deliverable: $path" }
}

$main = Get-Content -Raw -LiteralPath $mainPath
$doc = Get-Content -Raw -LiteralPath $docPath

foreach ($requirement in @(
    'LiveDestination.Stand',
    'LiveDestination.Customers',
    'LiveDestination.Notebook',
    'LiveDestination.Map',
    'SelectDestination',
    'ReturnToWorld',
    'Pin customer',
    'case Key.Key1',
    'case Key.Key2',
    'case Key.N',
    'case Key.M',
    'case Key.Escape'
)) {
    if (-not $main.Contains($requirement)) { throw "Missing UI-03 navigation contract: $requirement" }
}

foreach ($forbidden in @('NEIGHBORHOOD / BUSINESS / OBSERVE', 'TRACK GUEST</Button>')) {
    if ($main.Contains($forbidden)) { throw "Web-style global navigation must not return: $forbidden" }
}

if (-not $doc.Contains('The hotbar is navigation, not a web tab strip')) {
    throw 'UI-03 hotbar behavior is undocumented.'
}

foreach ($shot in @($customerShot, $managementShot)) {
    if ((Get-Item -LiteralPath $shot).Length -lt 100000) { throw "UI-03 screenshot appears invalid: $shot" }
}

Write-Output 'UI-03 world-first navigation validated.'
