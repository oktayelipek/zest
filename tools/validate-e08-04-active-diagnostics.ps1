$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$servicePath = Join-Path $root 'src\Zest.Domain\Diagnostics\OperationalDiagnostics.cs'
$mainPath = Join-Path $root 'game\scripts\Main.cs'
$testPath = Join-Path $root 'tests\Zest.Domain.Tests\OperationalDiagnosticTests.cs'
$docPath = Join-Path $root 'docs\architecture\e08-04-active-diagnostics.md'
$shotPath = Join-Path $root 'docs\artifacts\e08-04-active-diagnostics.png'

foreach ($path in @($servicePath, $mainPath, $testPath, $docPath, $shotPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing E08-04 deliverable: $path" }
}
$service = Get-Content -Raw -LiteralPath $servicePath
$main = Get-Content -Raw -LiteralPath $mainPath
$tests = Get-Content -Raw -LiteralPath $testPath
$doc = Get-Content -Raw -LiteralPath $docPath

foreach ($signal in @('"QUEUE"', '"PRICE"', '"STOCK"', '"PRODUCT FIT"', 'LostCustomerFeed', 'YesterdayComparison', 'Bottleneck')) {
    if (-not $service.Contains($signal)) { throw "Missing E08-04 diagnostic signal: $signal" }
}
foreach ($surface in @('Pin customer', 'Watch queue', 'LOST CUSTOMER FEED', 'COMPARE YESTERDAY', 'BOTTLENECK LENS', 'RichTextLabel')) {
    if (-not $main.Contains($surface)) { throw "Missing E08-04 player observation surface: $surface" }
}
foreach ($test in @('ObservationProducesReadableSignalsWithoutMutatingSimulation', 'RepricingSurfacesPriceWatchWithoutExposingUtilityFormula')) {
    if (-not $tests.Contains($test)) { throw "Missing E08-04 regression test: $test" }
}
if (-not $doc.Contains('simulation outcomes remain untouched')) { throw 'E08-04 read-only contract is undocumented.' }
if ((Get-Item -LiteralPath $shotPath).Length -lt 100000) { throw 'E08-04 screenshot appears invalid.' }

Write-Output 'E08-04 read-only active observation and diagnostics validated.'
