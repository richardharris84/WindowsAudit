[CmdletBinding()]
param(
    [string]$IsccPath
)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

if (-not $IsccPath) {
    $compiler = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($compiler) {
        $IsccPath = $compiler.Source
    }
    else {
        $candidates = @(
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
            "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
        )
        $IsccPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
    }
}
if (-not $IsccPath -or -not (Test-Path -LiteralPath $IsccPath)) {
    throw 'Install Inno Setup 6, or pass -IsccPath with the full path to ISCC.exe.'
}

$publishDirectory = Join-Path $root 'artifacts\publish\win-x64'
$outputDirectory = Join-Path $root 'artifacts\installer'
$project = Join-Path $root 'src\WindowsServiceAudit.App\WindowsServiceAudit.App.csproj'

& dotnet publish $project --configuration Release --runtime win-x64 --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false --output $publishDirectory
if ($LASTEXITCODE -ne 0) { throw "Publish failed with exit code $LASTEXITCODE." }
$readmePath = Join-Path $publishDirectory 'Assets\README.md'
if (-not (Test-Path -LiteralPath $readmePath) -or
    (Get-FileHash -LiteralPath $readmePath).Hash -ne (Get-FileHash -LiteralPath (Join-Path $root 'README.md')).Hash) {
    throw 'Published Getting Started README is missing or does not match the source.'
}

$stage = Join-Path ([IO.Path]::GetTempPath()) "WindowsAudit-installer-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    # Keep Inno's resource updates outside folders watched by workspace sync tools.
    & $IsccPath "/DPublishDir=$publishDirectory" "/DOutputDir=$stage" (Join-Path $PSScriptRoot 'WindowsAudit.iss')
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed with exit code $LASTEXITCODE." }

    $stagedSetupPath = Join-Path $stage 'WindowsAudit-Setup.exe'
    $installer = Get-Item -LiteralPath $stagedSetupPath
    if ($installer.Length -eq 0) { throw "Installer is empty: $stagedSetupPath" }
    $expectedVersion = (Get-Item -LiteralPath (Join-Path $publishDirectory 'WindowsAudit.exe')).VersionInfo.FileVersion
    $installerVersion = $installer.VersionInfo.ProductVersion.Trim()
    if ($installerVersion -ne $expectedVersion) {
        throw "Installer version '$installerVersion' does not match published version '$expectedVersion'."
    }

    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
    $setupPath = Join-Path $outputDirectory 'WindowsAudit-Setup.exe'
    Copy-Item -LiteralPath $stagedSetupPath -Destination $setupPath -Force
    Get-Item -LiteralPath $setupPath
}
finally {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
}
