[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ChangeId,
    [Parameter(Mandatory)] [string] $NodeId,
    [Parameter(Mandatory)] [string] $Summary,
    [Parameter(Mandatory)] [string] $Worker,
    [string[]] $Repositories = @(),
    [string[]] $Projects = @(),
    [string[]] $Files = @(),
    [string[]] $SchemaContexts = @(),
    [string[]] $Contracts = @(),
    [string[]] $Dependencies = @(),
    [ValidateSet('Critical','High','Medium','Low','Not Assessed')]
    [string] $Risk = 'Not Assessed',
    [string] $Branch = '',
    [string] $Worktree = '',
    [switch] $Reserve
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$root = Split-Path $PSScriptRoot -Parent
$ledgerPath = Join-Path $root 'control\ACTIVE_CHANGES.csv'
if (-not (Test-Path -LiteralPath $ledgerPath -PathType Leaf)) {
    throw "Active change ledger unavailable: $ledgerPath"
}

function Split-Field([string] $value) {
    if ([string]::IsNullOrWhiteSpace($value)) { return @() }
    return @($value -split '\s*\|\s*' | Where-Object { $_ })
}

function Intersects([string[]] $left, [string[]] $right) {
    foreach ($a in $left) {
        foreach ($b in $right) {
            if ($a -ieq $b -or $a -like $b -or $b -like $a) { return $true }
        }
    }
    return $false
}

$architectureReasons = @()
if ($Repositories -contains 'Nexus.Developer') {
    if ($Projects | Where-Object { $_ -like 'Nexus.Products.*' }) {
        $architectureReasons += 'DEVELOPER must not reference a product domain project.'
    }
    if ($SchemaContexts | Where-Object { $_ -match 'product|chat|vault|trips|erp' }) {
        $architectureReasons += 'DEVELOPER must not mutate a product DbContext/schema.'
    }
}

$rows = @(Import-Csv -LiteralPath $ledgerPath)
$active = @($rows | Where-Object {
    $_.'Change ID' -ne $ChangeId -and $_.Status -notin @('Completed','Cancelled')
})

$dependencyHits = @()
$overlapHits = @()
$conflictHits = @()

foreach ($row in $active) {
    $rowId = $row.'Change ID'
    if (Intersects $Dependencies (Split-Field $row.'Node ID')) {
        $dependencyHits += "$rowId (declared dependency)"
    }

    $projectOverlap = Intersects $Projects (Split-Field $row.Projects)
    $fileOverlap = Intersects $Files (Split-Field $row.'Files / Globs')
    if ($projectOverlap -or $fileOverlap) {
        $overlapHits += "$rowId (project/file overlap)"
    }

    if (Intersects $SchemaContexts (Split-Field $row.'Schema Contexts')) {
        $conflictHits += "$rowId (shared schema/DbContext mutation)"
    }
    if (Intersects $Contracts (Split-Field $row.'Contracts / APIs')) {
        $conflictHits += "$rowId (shared contract/API mutation)"
    }
    if ($Risk -in @('Critical','High') -and $row.Risk -in @('Critical','High')) {
        $conflictHits += "$rowId (both high risk)"
    }
}

$verdict = 'CLEAR'
$reasons = @()
if ($dependencyHits.Count -gt 0) { $verdict = 'DEPENDENCY FOUND'; $reasons += $dependencyHits }
if ($overlapHits.Count -gt 0) { $verdict = 'OVERLAP FOUND'; $reasons += $overlapHits }
if ($conflictHits.Count -gt 0) { $verdict = 'CONFLICT FOUND'; $reasons += $conflictHits }
if ($architectureReasons.Count -gt 0) { $verdict = 'ARCHITECTURE CONFLICT'; $reasons += $architectureReasons }

Write-Host $verdict -ForegroundColor $(if ($verdict -eq 'CLEAR') { 'Green' } elseif ($verdict -like '*CONFLICT*') { 'Red' } else { 'Yellow' })
if ($reasons.Count -gt 0) { $reasons | ForEach-Object { Write-Host "- $_" } }

if ($verdict -in @('CONFLICT FOUND','ARCHITECTURE CONFLICT')) { exit 2 }

if ($Reserve) {
    if ($rows | Where-Object { $_.'Change ID' -eq $ChangeId -and $_.Status -notin @('Completed','Cancelled') }) {
        throw "An active reservation already exists for $ChangeId. Update it; do not duplicate it."
    }

    [pscustomobject]@{
        'Change ID' = $ChangeId
        'Node ID' = $NodeId
        'Milestone / Feature' = ''
        'Summary' = $Summary
        'Requested By' = 'Durai'
        'Worker' = $Worker
        'Repositories' = ($Repositories -join ' | ')
        'Projects' = ($Projects -join ' | ')
        'Files / Globs' = ($Files -join ' | ')
        'Schema Contexts' = ($SchemaContexts -join ' | ')
        'Contracts / APIs' = ($Contracts -join ' | ')
        'Status' = 'Reserved'
        'Preflight Verdict' = $verdict
        'Conflicts With' = ($overlapHits + $conflictHits -join ' | ')
        'Dependency On' = ($Dependencies -join ' | ')
        'Risk' = $Risk
        'Branch' = $Branch
        'Worktree' = $Worktree
        'Started At' = [DateTimeOffset]::Now.ToString('o')
        'Last Heartbeat' = [DateTimeOffset]::Now.ToString('o')
        'Completed At' = ''
        'Result / Evidence' = ''
        'Change Version' = '1.0'
        'Session / Chat' = ''
        'Notes' = ($reasons -join ' | ')
    } | Export-Csv -LiteralPath $ledgerPath -Append -NoTypeInformation -Encoding UTF8

    Write-Host "Reserved $ChangeId in $ledgerPath" -ForegroundColor Green
}
