#requires -Version 7.4
[CmdletBinding()]
param(
    [string] $AppDirectory = (Join-Path $PSScriptRoot '../TSCC_WASD.Core/bin/Release/net10.0-windows'),
    [ValidateRange(1,60)][int] $Seconds = 15,
    [switch] $MapToDs4
)
$ErrorActionPreference = 'Stop'
$resolved = (Resolve-Path -LiteralPath $AppDirectory).Path
Add-Type -Path (Join-Path $resolved 'TSCC_WASD.Core.dll')
$reader = [TSCC_WASD.Core.Services.Input.Switch2InputReader]::new()
$pad = $null
$timer = [Diagnostics.Stopwatch]::StartNew()
$reports = 0
$polls = 0
$firstInputMs = $null
$missingAfterStart = 0
$outages = 0
$outageStartMs = $null
$longestOutageMs = 0.0
$buttonsSeen = [uint16]0
$lastStatus = ''
$leftMin = 32767; $leftMax = -32768
try {
    if ($MapToDs4) {
        Add-Type -Path (Join-Path $resolved 'Nefarius.ViGEm.Client.dll')
        $pad = [TSCC_WASD.Core.Services.VirtualControllers.DualShock4VirtualPad]::new()
        $connectionError = ''
        if (-not $pad.TryConnect([ref]$connectionError)) { throw $connectionError }
    }
    while ($timer.Elapsed.TotalSeconds -lt $Seconds) {
        $polls++
        $state = [TSCC_WASD.Core.Services.Input.State]::new()
        $status = $reader.Status
        if ($status -ne $lastStatus) { Write-Host $status; $lastStatus = $status }
        if ($reader.TryGetState([ref] $state)) {
            $reports++
            if ($null -eq $firstInputMs) { $firstInputMs = $timer.Elapsed.TotalMilliseconds }
            if ($null -ne $outageStartMs) {
                $longestOutageMs = [Math]::Max($longestOutageMs, $timer.Elapsed.TotalMilliseconds - $outageStartMs)
                $outageStartMs = $null
            }
            $buttonsSeen = $buttonsSeen -bor [uint16]$state.Gamepad.Buttons
            $leftMin = [Math]::Min($leftMin, $state.Gamepad.LeftThumbX)
            $leftMax = [Math]::Max($leftMax, $state.Gamepad.LeftThumbX)
        } elseif ($null -ne $firstInputMs) {
            $missingAfterStart++
            if ($null -eq $outageStartMs) {
                $outages++
                $outageStartMs = $timer.Elapsed.TotalMilliseconds
            }
        }
        if ($null -ne $pad) { $pad.PushState($state, 0.08) }
        Start-Sleep -Milliseconds 8
    }
} finally {
    if ($null -ne $outageStartMs) {
        $longestOutageMs = [Math]::Max($longestOutageMs, $timer.Elapsed.TotalMilliseconds - $outageStartMs)
    }
    $reader.Dispose()
    if ($null -ne $pad) { $pad.Dispose() }
}
# Samples are fresh-state polls, not unique USB reports; repeated state reads count.
[pscustomobject]@{
    Samples = $reports; Polls = $polls
    FirstInputMs = $firstInputMs
    MissingPollsAfterStart = $missingAfterStart
    Outages = $outages; LongestOutageMs = [Math]::Round($longestOutageMs, 2)
    ButtonsSeen = ('0x{0:X4}' -f $buttonsSeen)
    LeftXMin = $(if ($reports) { $leftMin } else { $null })
    LeftXMax = $(if ($reports) { $leftMax } else { $null })
}
if ($reports -eq 0) { throw 'No valid NS2 Pro report received.' }
