[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ChangeId,
    [Parameter(Mandatory)] [string] $ResultEvidence,
    [ValidateSet('Completed','Cancelled','Blocked','In Review')]
    [string] $Status = 'Completed'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path $PSScriptRoot -Parent
$ledgerPath = Join-Path $root 'control\ACTIVE_CHANGES.csv'
$backupDirectory = Join-Path $root 'control\backups'
if (-not (Test-Path -LiteralPath $ledgerPath -PathType Leaf)) {
    throw "Active change ledger unavailable: $ledgerPath"
}

$rows = @(Import-Csv -LiteralPath $ledgerPath)
$matches = @($rows | Where-Object { $_.'Change ID' -eq $ChangeId -and $_.Status -notin @('Completed','Cancelled') })
if ($matches.Count -ne 1) {
    throw "Expected exactly one active row for $ChangeId; found $($matches.Count)."
}

New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$backupPath = Join-Path $backupDirectory "ACTIVE_CHANGES-$stamp.csv"
Copy-Item -LiteralPath $ledgerPath -Destination $backupPath

$now = [DateTimeOffset]::Now.ToString('o')
foreach ($row in $rows) {
    if ($row.'Change ID' -eq $ChangeId -and $row.Status -notin @('Completed','Cancelled')) {
        $row.Status = $Status
        $row.'Last Heartbeat' = $now
        if ($Status -in @('Completed','Cancelled')) { $row.'Completed At' = $now }
        $row.'Result / Evidence' = $ResultEvidence
    }
}

$temporary = "$ledgerPath.tmp"
$rows | Export-Csv -LiteralPath $temporary -NoTypeInformation -Encoding UTF8
Move-Item -LiteralPath $temporary -Destination $ledgerPath -Force
Write-Host "Updated $ChangeId. Recoverable backup: $backupPath" -ForegroundColor Green

