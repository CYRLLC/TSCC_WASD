#requires -Version 7.4
# Optional integration check: briefly creates a neutral virtual DS4, then removes it.
[CmdletBinding()]
# Release ZIPs are single-file, so point it at a build output folder, not an extracted ZIP.
param([string] $AppDirectory = (Join-Path $PSScriptRoot '../TSCC_WASD.App/bin/Release/net10.0-windows'))
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw 'Run using x64 PowerShell on Windows.'
}
$appPath = (Resolve-Path -LiteralPath $AppDirectory).Path
Add-Type -Path (Join-Path $appPath 'Nefarius.ViGEm.Client.dll')
Add-Type -Path (Join-Path $appPath 'TSCC_WASD.Core.dll')
$pad = $null
try {
    $pad = [TSCC_WASD.Core.Services.VirtualControllers.DualShock4VirtualPad]::new()
    $connectionError = ''
    if (-not $pad.TryConnect([ref] $connectionError)) { throw $connectionError }
    $pad.PushState([TSCC_WASD.Core.Services.Input.State]::new(), 0.08)
    $pad.Disconnect()
    if ($pad.IsConnected) { throw 'Virtual device remains connected.' }
    Write-Host 'PASS: virtual DS4 connect, neutral report, disconnect.'
} finally {
    if ($null -ne $pad) { $pad.Dispose() }
}
