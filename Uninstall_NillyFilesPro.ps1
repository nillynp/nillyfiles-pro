# ============================================================================
# NillyFiles Pro — Clean Uninstaller (Zero Admin · HKCU only)
# created by Nilly graphics (Sotiris Christos) — https://nillygraphics.com/
# ============================================================================
param([switch]$Silent)

$ErrorActionPreference = 'SilentlyContinue'
Get-Process NillyFilesPro -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

foreach ($k in @(
    'HKCU:\Software\Classes\Applications\NillyFilesPro.exe',
    'HKCU:\Software\Classes\Directory\shell\NillyFilesPro'
)) {
    if (Test-Path $k) { Remove-Item -Path $k -Recurse -Force }
}
Remove-Item (Join-Path ([Environment]::GetFolderPath('Desktop')) 'NillyFiles Pro.lnk') -Force
Remove-Item (Join-Path ([Environment]::GetFolderPath('Programs')) 'Nilly graphics\NillyFiles Pro.lnk') -Force

if (-not $Silent) { Write-Host '  Το NillyFiles Pro απεγκαταστάθηκε καθαρά.' -ForegroundColor Green }
