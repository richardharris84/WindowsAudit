param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\branding')
)

$ErrorActionPreference = 'Stop'
if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') {
    throw 'Run this script with pwsh -STA -File .\media\Build-Branding.ps1.'
}
Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
$drawingPath = Join-Path $PSScriptRoot '..\src\WindowsServiceAudit.App\Assets\Broom.xaml'
$image = [Windows.Markup.XamlReader]::Parse([IO.File]::ReadAllText($drawingPath))
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
foreach ($size in @(1024, 300, 150, 71)) {
    $name = if ($size -eq 1024) { 'WindowsAudit-Logo.png' } else { "WindowsAudit-Logo-${size}x${size}.png" }
    $visual = [Windows.Media.DrawingVisual]::new()
    $context = $visual.RenderOpen()
    try {
        $context.PushTransform([Windows.Media.ScaleTransform]::new($size / 256.0, $size / 256.0))
        $context.DrawDrawing($image.Drawing)
        $context.Pop()
    } finally { $context.Close() }
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(
        $size, $size, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $stream = [IO.File]::Create((Join-Path $OutputDirectory $name))
    try { $encoder.Save($stream) } finally { $stream.Dispose() }
}
Get-ChildItem -LiteralPath $OutputDirectory -File -Filter 'WindowsAudit-Logo*.png'
