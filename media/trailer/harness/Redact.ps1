param(
    [Parameter(Mandatory)]
    [string]$InputDirectory
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$root = [IO.Path]::GetFullPath($InputDirectory)
function Hide-Region($bitmap, $graphics, [int]$x, [int]$y, [int]$width, [int]$height) {
    $width = [Math]::Min($width, $bitmap.Width - $x)
    $height = [Math]::Min($height, $bitmap.Height - $y)
    $brush = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(238,242,242))
    try { $graphics.FillRectangle($brush, $x, $y, $width, $height) } finally { $brush.Dispose() }
}
function Pixelate-Region($bitmap, $graphics, [int]$x, [int]$y, [int]$width, [int]$height) {
    $width = [Math]::Min($width, $bitmap.Width - $x)
    $height = [Math]::Min($height, $bitmap.Height - $y)
    $small = [Drawing.Bitmap]::new([Math]::Max(1,[int]($width/45)),[Math]::Max(1,[int]($height/45)))
    $smallGraphics = [Drawing.Graphics]::FromImage($small)
    try {
        $smallGraphics.DrawImage($bitmap,[Drawing.Rectangle]::new(0,0,$small.Width,$small.Height),
            [Drawing.Rectangle]::new($x,$y,$width,$height),[Drawing.GraphicsUnit]::Pixel)
        $graphics.CompositingMode = [Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
        $graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
        $graphics.DrawImage($small,[Drawing.Rectangle]::new($x,$y,$width,$height))
    } finally { $smallGraphics.Dispose(); $small.Dispose() }
}
foreach ($kind in @('services','apps','startup','explorer')) {
    $bitmap = [Drawing.Bitmap]::new("$root\$kind-raw.png")
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    try {
        switch ($kind) {
            'services' {
                Pixelate-Region $bitmap $graphics 340 134 ($bitmap.Width-340) ($bitmap.Height-150)
            }
            'apps' {
                Hide-Region $bitmap $graphics 15 38 300 180
                Pixelate-Region $bitmap $graphics 334 219 1000 625
            }
            'startup' {
                Hide-Region $bitmap $graphics 15 38 300 180
                Pixelate-Region $bitmap $graphics 334 181 860 665
            }
            'explorer' {
                Hide-Region $bitmap $graphics 0 0 1100 44
                Hide-Region $bitmap $graphics 0 136 410 685
                Hide-Region $bitmap $graphics 1050 136 310 685
                Pixelate-Region $bitmap $graphics 414 164 636 654
            }
        }
        if ($kind -eq 'services') {
            $bitmap.Save("$root\$kind.png",[Drawing.Imaging.ImageFormat]::Png)
        } else {
            $crop = $bitmap.Clone([Drawing.Rectangle]::new(8,1,1344,841), $bitmap.PixelFormat)
            try { $crop.Save("$root\$kind.png",[Drawing.Imaging.ImageFormat]::Png) } finally { $crop.Dispose() }
        }
    } finally { $graphics.Dispose(); $bitmap.Dispose() }
}
