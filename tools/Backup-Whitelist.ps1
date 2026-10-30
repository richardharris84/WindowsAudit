param(
    [Parameter(Mandatory)]
    [string]$Destination,
    [string[]]$AdditionalWhitelistPath = @()
)

$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$destinationPath = [IO.Path]::GetFullPath($Destination)
$relative = [IO.Path]::GetRelativePath($repository, $destinationPath)
if ($relative -eq '.' -or (-not [IO.Path]::IsPathRooted($relative) -and
    $relative -ne '..' -and -not $relative.StartsWith('..' + [IO.Path]::DirectorySeparatorChar))) {
    throw 'Choose a private backup destination outside the workspace.'
}

$candidates = @(Get-ChildItem -LiteralPath $repository -Recurse -File -Filter 'whitelist.csv')
$dataFolders = @(Join-Path $env:LOCALAPPDATA 'WindowsServiceAudit')
$packages = Join-Path $env:LOCALAPPDATA 'Packages'
if (Test-Path -LiteralPath $packages) {
    foreach ($package in Get-ChildItem -LiteralPath $packages -Directory -Filter 'BuiltbyRichardHarris.WindowsAudit_*') {
        $dataFolders += Join-Path $package.FullName 'LocalCache\Local\WindowsServiceAudit'
    }
}
foreach ($folder in $dataFolders) {
    $path = Join-Path $folder 'whitelist.csv'
    if (Test-Path -LiteralPath $path) { $candidates += Get-Item -LiteralPath $path }
    $settingsPath = Join-Path $folder 'settings.json'
    if (Test-Path -LiteralPath $settingsPath) {
        $settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
        if ([string]::IsNullOrWhiteSpace($settings.AuditDirectory)) {
            throw "Invalid saved audit folder setting: $settingsPath"
        }
        $configuredWhitelist = Join-Path $settings.AuditDirectory 'whitelist.csv'
        if (Test-Path -LiteralPath $configuredWhitelist) {
            $candidates += Get-Item -LiteralPath $configuredWhitelist
        } else {
            throw "Saved audit folder's whitelist is unavailable: $configuredWhitelist"
        }
    }
}
foreach ($path in $AdditionalWhitelistPath) { $candidates += Get-Item -LiteralPath $path }
$candidates = @($candidates | Sort-Object -Property FullName -Unique)
if ($candidates.Count -eq 0) { throw 'No whitelist files found to back up.' }

$folder = Join-Path $destinationPath ("whitelists-{0}-{1}" -f
    (Get-Date -Format 'yyyyMMdd-HHmmss'), [Guid]::NewGuid().ToString('N').Substring(0, 8))
New-Item -ItemType Directory -Path $folder -Force | Out-Null
$entries = @()
foreach ($file in $candidates) {
    $name = 'whitelist-{0:D2}.csv' -f ($entries.Count + 1)
    $sourceHash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    $copy = Join-Path $folder $name
    Copy-Item -LiteralPath $file.FullName -Destination $copy
    if ((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -ne $sourceHash) {
        throw "Backup verification failed for $($file.FullName)."
    }
    $entries += [pscustomobject]@{
        OriginalPath = $file.FullName
        BackupFile = $name
        SHA256 = $sourceHash
    }
}
$entries | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $folder 'manifest.json') -Encoding utf8
Write-Output "Verified $($entries.Count) whitelist backups in $folder"
Write-Output 'This backup is not encrypted. Keep it private; do not attach it to a public release.'
