#requires -Version 5.1
# Compatibility entry point: installers must be selected and run by the user.
$ErrorActionPreference = 'Stop'
Write-Host 'ViGEmBus is required; HidHide is optional and requires manual configuration.'
Write-Host 'ViGEmBus is end-of-life. Read the upstream notices before installation.'
Start-Process 'https://docs.nefarius.at/Downloads/'
