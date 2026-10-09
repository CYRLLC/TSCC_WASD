#requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
if (-not $IsWindows -or -not [Environment]::Is64BitProcess) {
    throw 'Package on Windows using x64 PowerShell.'
}
$repoRoot = Split-Path $PSScriptRoot -Parent
Push-Location $repoRoot
try {
    function Invoke-DotNet([string[]] $Arguments) {
        & dotnet @Arguments
        if ($LASTEXITCODE -ne 0) { throw "dotnet failed ($LASTEXITCODE): $Arguments" }
    }
    [xml] $props = Get-Content -Raw 'Directory.Build.props'
    $version = [string] $props.Project.PropertyGroup.Version
    if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Expected a three-part version.' }
    $artifactRoot = Join-Path $repoRoot 'artifacts'
    $stage = Join-Path $artifactRoot ('stage-' + [guid]::NewGuid().ToString('N'))
    $packageName = "YnyrWASD-$version-win-x64"
    $packageDir = Join-Path $stage $packageName
    New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
    Invoke-DotNet @('restore', 'YnyrWASD.sln', '--locked-mode', '-p:NuGetAuditMode=all', '-warnaserror')
    Invoke-DotNet @('build', 'YnyrWASD.sln', '-c', 'Release', '--no-restore', '-warnaserror')
    Invoke-DotNet @('test', 'YnyrWASD.sln', '-c', 'Release', '--no-build', '--logger', 'trx')
    Invoke-DotNet @('publish', 'YnyrWASD.App/YnyrWASD.App.csproj', '-c', 'Release',
        '--no-restore', '--self-contained', 'false', '-p:PlatformTarget=x64',
        '-p:DebugType=None', '-p:DebugSymbols=false', '-o', $packageDir)
    foreach ($name in @('README.md', 'LICENSE', 'THIRD-PARTY-NOTICES.md', 'CHANGELOG.md',
        'CONTRIBUTING.md', 'SECURITY.md', 'PLAN.md')) {
        Copy-Item -LiteralPath (Join-Path $repoRoot $name) -Destination $packageDir
    }
    Copy-Item -LiteralPath (Join-Path $repoRoot 'docs') -Destination $packageDir -Recurse
    Copy-Item -LiteralPath (Join-Path $repoRoot 'examples') -Destination $packageDir -Recurse
    Copy-Item -LiteralPath (Join-Path $repoRoot 'licenses') -Destination $packageDir -Recurse
    foreach ($required in @('YnyrWASD.App.exe', 'YnyrWASD.App.dll',
        'YnyrWASD.App.runtimeconfig.json', 'Nefarius.ViGEm.Client.dll', 'LICENSE')) {
        if (-not (Test-Path -LiteralPath (Join-Path $packageDir $required))) {
            throw "Missing package file: $required"
        }
    }
    if (Get-ChildItem -LiteralPath $packageDir -Filter '*SharpDX*' -Recurse) {
        throw 'Obsolete SharpDX files found.'
    }
    $zip = Join-Path $artifactRoot ($packageName + '.zip')
    Compress-Archive -LiteralPath $packageDir -DestinationPath $zip -Force
    $hash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    "$hash  $packageName.zip" | Set-Content -LiteralPath ($zip + '.sha256') -Encoding utf8
    Write-Host "Package: $zip"
    Write-Host "SHA256: $hash"
} finally {
    Pop-Location
}
