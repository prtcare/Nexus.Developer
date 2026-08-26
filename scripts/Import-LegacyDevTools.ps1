[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $SourceRoot = 'C:\Personal\DevTools',
    [string] $DestinationRoot = (Split-Path $PSScriptRoot -Parent)
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$required = [ordered]@{
    'NexusDev.ps1'        = 'src\Nexus.Developer.Client.Legacy\NexusDev.ps1'
    'LayerShell.ps1'      = 'src\Nexus.Developer.Client.Legacy\LayerShell.ps1'
    'config\layers.json' = 'src\Nexus.Developer.Client.Legacy\config\layers.json'
    'notify.ps1'          = 'tools\local\notify.ps1'
    'checkpoint.ps1'      = 'tools\local\checkpoint.ps1'
    'verify.ps1'          = 'tools\local\verify.ps1'
}

$optional = [ordered]@{
    'start-dev.ps1' = 'tools\local\start-dev.ps1'
}

$source = [System.IO.Path]::GetFullPath($SourceRoot)
$destination = [System.IO.Path]::GetFullPath($DestinationRoot)

if (-not (Test-Path -LiteralPath $source -PathType Container)) {
    throw "DevTools source folder not found: $source"
}

if ($destination.StartsWith($source, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Destination must not be inside the DevTools source folder.'
}

$missing = @($required.Keys | Where-Object {
    -not (Test-Path -LiteralPath (Join-Path $source $_) -PathType Leaf)
})

if ($missing.Count -gt 0) {
    throw "Required DevTools files are missing: $($missing -join ', '). Nothing was copied."
}

$map = [ordered]@{}
foreach ($entry in $required.GetEnumerator()) { $map[$entry.Key] = $entry.Value }
foreach ($entry in $optional.GetEnumerator()) {
    if (Test-Path -LiteralPath (Join-Path $source $entry.Key) -PathType Leaf) {
        $map[$entry.Key] = $entry.Value
    }
}

$rows = @()
foreach ($entry in $map.GetEnumerator()) {
    $sourceFile = Join-Path $source $entry.Key
    $destinationFile = Join-Path $destination $entry.Value
    $destinationDirectory = Split-Path $destinationFile -Parent

    if ($PSCmdlet.ShouldProcess($destinationFile, "Copy verified DevTools file from $sourceFile")) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
        Copy-Item -LiteralPath $sourceFile -Destination $destinationFile -Force

        $sourceHash = (Get-FileHash -LiteralPath $sourceFile -Algorithm SHA256).Hash
        $destinationHash = (Get-FileHash -LiteralPath $destinationFile -Algorithm SHA256).Hash

        if ($sourceHash -ne $destinationHash) {
            throw "Hash verification failed for $($entry.Key). Source remains untouched."
        }

        $rows += [pscustomobject]@{
            Source = $sourceFile
            Destination = $destinationFile
            Sha256 = $sourceHash
            Verified = $true
            CopiedAt = [DateTimeOffset]::Now.ToString('o')
        }
    }
}

if (-not $WhatIfPreference) {
    $reportDirectory = Join-Path $destination 'control'
    New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $reportPath = Join-Path $reportDirectory "migration-report-$stamp.csv"
    $rows | Export-Csv -LiteralPath $reportPath -NoTypeInformation -Encoding UTF8
    Write-Host "Copied and verified $($rows.Count) file(s). Source was not modified." -ForegroundColor Green
    Write-Host "Report: $reportPath" -ForegroundColor Green
}

