# Regenerates YnyrWASD.App/Assets/YnyrWASD.ico: a rounded blue tile with four face buttons.
# Run with Windows PowerShell or pwsh on Windows (uses System.Drawing).
[CmdletBinding()]
param([string] $OutFile)
$ErrorActionPreference = 'Stop'
# Windows PowerShell 5.1 leaves $PSScriptRoot empty inside param() defaults.
if (-not $OutFile) { $OutFile = Join-Path $PSScriptRoot '../YnyrWASD.App/Assets/YnyrWASD.ico' }
Add-Type -AssemblyName System.Drawing

function New-IconPng([int] $size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)
    $r = [single]($size * 0.22)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $s = [single]($size - 1)
    $path.AddArc(0, 0, $r * 2, $r * 2, 180, 90)
    $path.AddArc($s - $r * 2, 0, $r * 2, $r * 2, 270, 90)
    $path.AddArc($s - $r * 2, $s - $r * 2, $r * 2, $r * 2, 0, 90)
    $path.AddArc(0, $s - $r * 2, $r * 2, $r * 2, 90, 90)
    $path.CloseFigure()
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush (New-Object System.Drawing.Point 0, 0),
        (New-Object System.Drawing.Point $size, $size),
        ([System.Drawing.Color]::FromArgb(255, 70, 120, 255)), ([System.Drawing.Color]::FromArgb(255, 30, 60, 170))
    $g.FillPath($brush, $path)
    # Four face buttons in a diamond.
    $d = [single]($size * 0.2)
    $c = [single]($size / 2)
    $o = [single]($size * 0.2)
    $white = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::White)
    foreach ($p in @(@(0, -1), @(1, 0), @(0, 1), @(-1, 0))) {
        $g.FillEllipse($white, $c + $p[0] * $o - $d / 2, $c + $p[1] * $o - $d / 2, $d, $d)
    }
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    return , $ms.ToArray()
}

$sizes = 16, 24, 32, 48, 64, 256
$images = foreach ($size in $sizes) { , (New-IconPng $size) }
$out = New-Object System.IO.MemoryStream
$w = New-Object System.IO.BinaryWriter $out
$w.Write([uint16]0); $w.Write([uint16]1); $w.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dim = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $w.Write([byte]$dim); $w.Write([byte]$dim); $w.Write([byte]0); $w.Write([byte]0)
    $w.Write([uint16]1); $w.Write([uint16]32)
    $w.Write([uint32]$images[$i].Length); $w.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($img in $images) { $w.Write($img) }
$w.Flush()
New-Item -ItemType Directory -Force (Split-Path $OutFile) | Out-Null
[System.IO.File]::WriteAllBytes($OutFile, $out.ToArray())
Write-Host "Wrote $OutFile"
