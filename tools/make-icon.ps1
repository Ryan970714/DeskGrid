# Renders the app icon (a rounded tile holding a 2x2 grid) into a multi-size .ico.
# Usage: powershell -NoProfile -File tools/make-icon.ps1
param([string]$OutFile = "$PSScriptRoot\..\src\DeskGrid\Assets\DeskGrid.ico")

Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase

function Render([int]$size) {
    $visual = New-Object System.Windows.Media.DrawingVisual
    $dc = $visual.RenderOpen()
    $s = [double]$size
    $tile = New-Object System.Windows.Media.LinearGradientBrush ([System.Windows.Media.Color]::FromRgb(0x4F, 0x8D, 0xF7)), ([System.Windows.Media.Color]::FromRgb(0x25, 0x5F, 0xE0)), 90
    $radius = $s * 0.2237
    $dc.DrawRoundedRectangle($tile, $null, (New-Object System.Windows.Rect 0, 0, $s, $s), $radius, $radius)

    $white = New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.Color]::FromArgb(0xF2, 0xFF, 0xFF, 0xFF))
    $padding = $s * 0.2; $gap = $s * 0.08
    $cell = ($s - 2 * $padding - $gap) / 2
    foreach ($row in 0, 1) {
        foreach ($column in 0, 1) {
            $rect = New-Object System.Windows.Rect ($padding + $column * ($cell + $gap)), ($padding + $row * ($cell + $gap)), $cell, $cell
            $dc.DrawRoundedRectangle($white, $null, $rect, $cell * 0.25, $cell * 0.25)
        }
    }
    $dc.Close()

    $bitmap = New-Object System.Windows.Media.Imaging.RenderTargetBitmap $size, $size, 96, 96, ([System.Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = New-Object System.IO.MemoryStream
    $encoder.Save($stream)
    return , $stream.ToArray()
}

# ICO with PNG-compressed images (supported since Windows Vista).
$sizes = 16, 20, 24, 32, 40, 48, 64, 256
$images = foreach ($size in $sizes) { , (Render $size) }
$out = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter $out
$writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $dimension = if ($sizes[$i] -ge 256) { 0 } else { $sizes[$i] }
    $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($image in $images) { $writer.Write($image) }
$writer.Flush()
New-Item -ItemType Directory -Force (Split-Path $OutFile) | Out-Null
[System.IO.File]::WriteAllBytes($OutFile, $out.ToArray())
"Wrote $OutFile"
