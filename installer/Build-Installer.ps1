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

& $IsccPath "/DPublishDir=$publishDirectory" "/DOutputDir=$outputDirectory" (Join-Path $PSScriptRoot 'WindowsAudit.iss')
if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed with exit code $LASTEXITCODE." }

$setupPath = Join-Path $outputDirectory 'WindowsAudit-Setup.exe'
if (-not (Test-Path -LiteralPath $setupPath)) { throw "Installer was not created: $setupPath" }
Get-Item -LiteralPath $setupPath
