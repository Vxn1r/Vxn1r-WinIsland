$ErrorActionPreference = 'Stop'

$source = $PSScriptRoot
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\WinIsland'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\WinIsland'
$startupDir = [Environment]::GetFolderPath('Startup')
$exe = Join-Path $source 'WinIsland.exe'

if (-not (Test-Path -LiteralPath $exe)) {
    throw 'WinIsland.exe was not found. Extract the complete WinIsland release ZIP before running Install.bat.'
}

$running = Get-Process -Name 'WinIsland' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host 'Close WinIsland before installing an update, then run Install.bat again.' -ForegroundColor Yellow
    exit 1
}

New-Item -ItemType Directory -Path $installDir -Force | Out-Null
Get-ChildItem -LiteralPath $source -Force | Copy-Item -Destination $installDir -Recurse -Force
New-Item -ItemType Directory -Path $startMenuDir -Force | Out-Null

$wsh = New-Object -ComObject WScript.Shell
$shortcut = $wsh.CreateShortcut((Join-Path $startMenuDir 'WinIsland.lnk'))
$shortcut.TargetPath = Join-Path $installDir 'WinIsland.exe'
$shortcut.WorkingDirectory = $installDir
$shortcut.Description = 'WinIsland Windows control center'
$shortcut.Save()

$uninstallShortcut = $wsh.CreateShortcut((Join-Path $startMenuDir 'Uninstall WinIsland.lnk'))
$uninstallShortcut.TargetPath = 'powershell.exe'
$uninstallShortcut.Arguments = '-NoProfile -ExecutionPolicy Bypass -File "' + (Join-Path $installDir 'Uninstall.ps1') + '"'
$uninstallShortcut.WorkingDirectory = $installDir
$uninstallShortcut.Description = 'Uninstall WinIsland'
$uninstallShortcut.Save()

$startupShortcutPath = Join-Path $startupDir 'WinIsland.lnk'
$startAtSignIn = Read-Host 'Start WinIsland automatically when you sign in? [Y/N]'
if ($startAtSignIn -match '^(?i)y(es)?$') {
    $startupShortcut = $wsh.CreateShortcut($startupShortcutPath)
    $startupShortcut.TargetPath = Join-Path $installDir 'WinIsland.exe'
    $startupShortcut.WorkingDirectory = $installDir
    $startupShortcut.Description = 'Start WinIsland when this Windows user signs in'
    $startupShortcut.Save()
    Write-Host 'Automatic startup is enabled for this Windows user.' -ForegroundColor Green
}
else {
    if (Test-Path -LiteralPath $startupShortcutPath) {
        Remove-Item -LiteralPath $startupShortcutPath -Force
    }
    Write-Host 'Automatic startup is disabled.' -ForegroundColor DarkGray
}

Write-Host 'WinIsland is installed for your Windows account.' -ForegroundColor Green
Write-Host 'Open Start, search for WinIsland, and launch it.'
Start-Process -FilePath (Join-Path $installDir 'WinIsland.exe')
