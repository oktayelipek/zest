$ErrorActionPreference = 'Stop'

$root = Resolve-Path (Join-Path $PSScriptRoot '..')
$servicePath = Join-Path $root 'src\Zest.Domain\Operations\LiveInterventions.cs'
$commandsPath = Join-Path $root 'src\Zest.Domain\DayCycle\DayCommands.cs'
$processorPath = Join-Path $root 'src\Zest.Domain\DayCycle\DayCommandProcessor.cs'
$economyPath = Join-Path $root 'src\Zest.Domain\Economy\LedgerEntry.cs'
$testPath = Join-Path $root 'tests\Zest.Domain.Tests\LiveInterventionTests.cs'
$docPath = Join-Path $root 'docs\architecture\e08-03-pressure-interventions.md'
$shotPath = Join-Path $root 'docs\artifacts\e08-03-emergency-restock.png'

foreach ($path in @($servicePath, $commandsPath, $processorPath, $economyPath, $testPath, $docPath, $shotPath)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing E08-03 deliverable: $path" }
}

$service = Get-Content -Raw -LiteralPath $servicePath
$commands = Get-Content -Raw -LiteralPath $commandsPath
$processor = Get-Content -Raw -LiteralPath $processorPath
$tests = Get-Content -Raw -LiteralPath $testPath
$doc = Get-Content -Raw -LiteralPath $docPath

foreach ($requirement in @('RequestEmergencyRestock', 'MaxEmergencyRestocksPerDay', 'premiumMultiplier <= 1m', 'CallExtraHelp', 'Capacity++', 'Capacity--', 'ChangeLivePrice', 'ApplyCurrentPrices')) {
    if (-not $service.Contains($requirement)) { throw "Missing E08-03 authoritative behavior: $requirement" }
}
foreach ($command in @('RequestEmergencyRestockCommand', 'CallExtraHelpCommand', 'ChangeLivePriceCommand')) {
    if (-not $commands.Contains($command) -or -not $processor.Contains($command)) { throw "Missing E08-03 command boundary: $command" }
}
foreach ($test in @('EmergencyRestockChargesPremiumAndArrivesAfterDelayWithDailyLimit', 'ExtraHelpAddsCapacityOnlyAfterArrivalAndPostsLaborCost', 'MiddayPriceChangeUsesExistingCustomerPriceSensitivity')) {
    if (-not $tests.Contains($test)) { throw "Missing E08-03 regression test: $test" }
}
if (-not $doc.Contains('universally dominant answer')) { throw 'E08-03 trade-offs are undocumented.' }
if ((Get-Item -LiteralPath $shotPath).Length -lt 100000) { throw 'E08-03 screenshot appears invalid.' }

Write-Output 'E08-03 delayed, costly operational pressure interventions validated.'
