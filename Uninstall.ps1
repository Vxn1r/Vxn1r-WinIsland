$ErrorActionPreference = 'Stop'
$installDir = Join-Path $env:LOCALAPPDATA 'Programs\WinIsland'
$startMenuDir = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs\WinIsland'
$startupShortcut = Join-Path ([Environment]::GetFolderPath('Startup')) 'WinIsland.lnk'

$running = Get-Process -Name 'WinIsland' -ErrorAction SilentlyContinue
if ($running) {
    $running | Stop-Process -Force
}

if (Test-Path -LiteralPath $startupShortcut) {
    Remove-Item -LiteralPath $startupShortcut -Force
}
if (Test-Path -LiteralPath $startMenuDir) {
    Remove-Item -LiteralPath $startMenuDir -Recurse -Force
}
if (Test-Path -LiteralPath $installDir) {
    Remove-Item -LiteralPath $installDir -Recurse -Force
}
Write-Host 'WinIsland has been removed from this Windows user account.' -ForegroundColor Green
