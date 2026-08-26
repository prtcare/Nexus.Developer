$target = 'C:\Personal\Nexus.Developer\src\Nexus.Developer.Client.Legacy\NexusDev.ps1'
$desktop = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktop 'Nexus Development.lnk'

if (-not (Test-Path $target)) {
    Write-Host "NexusDev.ps1 not found at $target" -ForegroundColor Red
    exit 1
}

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = 'powershell.exe'
$shortcut.Arguments = "-ExecutionPolicy Bypass -WindowStyle Hidden -File `"$target`""
$shortcut.WorkingDirectory = 'C:\Personal\Nexus.Developer\src\Nexus.Developer.Client.Legacy'
$shortcut.Description = 'Nexus Development Console'
$shortcut.Save()

Write-Host "Desktop shortcut created:" -ForegroundColor Green
Write-Host $shortcutPath
