[CmdletBinding()]
param(
    [string]$MakeAppxPath,
    [ValidatePattern('^\d+\.\d+\.\d+\.0$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src\WindowsServiceAudit.App\WindowsServiceAudit.App.csproj'
if (-not $Version) {
    $projectXml = [xml][IO.File]::ReadAllText($project)
    $Version = [string]$projectXml.Project.PropertyGroup.Version
}
if ($Version -notmatch '^\d+\.\d+\.\d+\.0$') {
    throw "MSIX Store version must have four numeric components and end in .0: $Version"
}
if ([Threading.Thread]::CurrentThread.ApartmentState -ne 'STA') {
    throw 'Run this script in STA mode: pwsh -STA -File .\packaging\Build-MSIX.ps1'
}

$manifest = [xml][IO.File]::ReadAllText((Join-Path $PSScriptRoot 'AppxManifest.xml'))
if ($manifest.Package.Properties.DisplayName -ne 'Windows Audit') {
    throw 'MSIX package display name must match the reserved Store name: Windows Audit.'
}

if (-not $MakeAppxPath) {
    $command = Get-Command makeappx.exe -ErrorAction SilentlyContinue
    if ($command) {
        $MakeAppxPath = $command.Source
    }
    else {
        $searchRoots = @(
            "${env:ProgramFiles(x86)}\Windows Kits\10\bin",
            (Join-Path $root 'artifacts\tools\sdk-buildtools\bin')
        )
        $MakeAppxPath = $searchRoots | Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { Get-ChildItem -LiteralPath $_ -Filter makeappx.exe -Recurse } |
            Where-Object { $_.Directory.Name -eq 'x64' } |
            Sort-Object FullName -Descending | Select-Object -First 1 -ExpandProperty FullName
    }
}
if (-not $MakeAppxPath -or -not (Test-Path -LiteralPath $MakeAppxPath)) {
    throw 'Install Windows SDK packaging tools, or pass -MakeAppxPath with the full path to makeappx.exe.'
}

$outputDirectory = Join-Path $root 'artifacts\msix'
$stage = Join-Path ([IO.Path]::GetTempPath()) "WindowsAudit-msix-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
try {
    & dotnet publish $project --configuration Release --runtime win-x64 --self-contained true `
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:DebugType=None -p:DebugSymbols=false "-p:Version=$Version" --output $stage
    if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }
    $readmePath = Join-Path $stage 'Assets\README.md'
    if (-not (Test-Path -LiteralPath $readmePath) -or
        (Get-FileHash -LiteralPath $readmePath).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root 'README.md')).Hash) {
        throw 'Published Getting Started README is missing or does not match the source.'
    }

    Add-Type -AssemblyName PresentationCore, PresentationFramework, WindowsBase
    $drawingPath = Join-Path $root 'src\WindowsServiceAudit.App\Assets\Broom.xaml'
    $image = [Windows.Markup.XamlReader]::Parse([IO.File]::ReadAllText($drawingPath))
    $assets = Join-Path $stage 'StoreAssets'
    New-Item -ItemType Directory -Path $assets | Out-Null
    foreach ($asset in @(
        @{ Name = 'StoreLogo'; Size = 50 },
        @{ Name = 'Square44x44Logo'; Size = 44 },
        @{ Name = 'Square150x150Logo'; Size = 150 }
    )) {
        $size = $asset.Size
        $visual = [Windows.Media.DrawingVisual]::new()
        $context = $visual.RenderOpen()
        $context.PushTransform([Windows.Media.ScaleTransform]::new($size / 256.0, $size / 256.0))
        $context.DrawDrawing($image.Drawing)
        $context.Pop()
        $context.Close()
        $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new(
            $size, $size, 96, 96, [Windows.Media.PixelFormats]::Pbgra32)
        $bitmap.Render($visual)
        $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
        $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
        $stream = [IO.File]::Create((Join-Path $assets "$($asset.Name).png"))
        try { $encoder.Save($stream) } finally { $stream.Dispose() }
    }

    $manifest.Package.Identity.Version = $Version
    $manifest.Save((Join-Path $stage 'AppxManifest.xml'))
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    $packagePath = Join-Path $outputDirectory "WindowsAudit_$($Version)_x64.msix"
    & $MakeAppxPath pack /d $stage /p $packagePath /o
    if ($LASTEXITCODE -ne 0) { throw "MSIX packaging failed with exit code $LASTEXITCODE." }
    $package = [IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $entry = $package.GetEntry('AppxManifest.xml')
        if (-not $entry) { throw 'Built MSIX is missing AppxManifest.xml.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $builtManifest = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($builtManifest.Package.Identity.Version -ne $Version -or
            $builtManifest.Package.Properties.DisplayName -ne 'Windows Audit') {
            throw "Built MSIX must have version $Version and reserved Store display name Windows Audit."
        }
        $entry = $package.GetEntry('Assets/README.md')
        if (-not $entry) { throw 'Built MSIX is missing the Getting Started README.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $readme = $reader.ReadToEnd() } finally { $reader.Dispose() }
        if ($readme -ne [IO.File]::ReadAllText((Join-Path $root 'README.md'))) {
            throw 'Built MSIX Getting Started README does not match the source.'
        }
    }
    finally { $package.Dispose() }
    Get-Item -LiteralPath $packagePath
}
finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
