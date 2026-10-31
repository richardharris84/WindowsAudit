param(
    [string]$FfmpegPath = 'ffmpeg',
    [string]$FfprobePath = 'ffprobe',
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\artifacts\trailer'),
    [string]$FramesDirectory = (Join-Path $PSScriptRoot 'frames'),
    [string]$ThumbnailPath = (Join-Path $PSScriptRoot 'thumbnail.png'),
    [switch]$Overwrite
)

$ErrorActionPreference = 'Stop'
$encoder = (Get-Command $FfmpegPath -ErrorAction Stop).Source
$probe = (Get-Command $FfprobePath -ErrorAction Stop).Source
$frames = [IO.Path]::GetFullPath($FramesDirectory)
$timeline = Join-Path $frames 'timeline.txt'
$total = 0.0
foreach ($line in Get-Content -LiteralPath $timeline) {
    if ($line -match "^file '([^']+)'$") {
        if (-not (Test-Path -LiteralPath (Join-Path $frames $Matches[1]))) {
            throw "Missing scene: $($Matches[1])"
        }
    } elseif ($line -match '^duration ([\d.]+)$') {
        $total += [double]::Parse($Matches[1], [Globalization.CultureInfo]::InvariantCulture)
    }
}
if ([Math]::Abs($total - 60) -gt 0.000001) { throw "Expected a 60-second timeline; found $total." }
$output = [IO.Path]::GetFullPath($OutputDirectory)
New-Item -ItemType Directory -Path $output -Force | Out-Null
$video = Join-Path $output 'WindowsAudit-Trailer-60s-Store.mp4'
$thumbnail = Join-Path $output 'WindowsAudit-Trailer-60s-Thumbnail.png'
if (-not $Overwrite -and ((Test-Path -LiteralPath $video) -or (Test-Path -LiteralPath $thumbnail))) {
    throw 'Output already exists. Choose another directory or explicitly pass -Overwrite.'
}
$overwriteFlag = if ($Overwrite) { '-y' } else { '-n' }
$normalized = Join-Path $output "normalized-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $normalized | Out-Null
try {
    Copy-Item -LiteralPath $timeline -Destination (Join-Path $normalized 'timeline.txt')
    $sceneNames = Get-Content -LiteralPath $timeline |
        ForEach-Object { if ($_ -match "^file '([^']+)'$") { $Matches[1] } } |
        Select-Object -Unique
    foreach ($name in $sceneNames) {
        & $encoder -hide_banner -loglevel error -y -i (Join-Path $frames $name) `
            -vf 'format=rgb24' -frames:v 1 -update 1 (Join-Path $normalized $name)
        if ($LASTEXITCODE -ne 0) { throw "Scene normalization failed: $name" }
    }
& $encoder -hide_banner -loglevel warning $overwriteFlag -f concat -safe 0 -i (Join-Path $normalized 'timeline.txt') `
    -f lavfi -i 'anullsrc=channel_layout=stereo:sample_rate=48000' `
    -map 0:v:0 -map 1:a:0 -vf 'fps=30,format=yuv420p' -r 30 -fps_mode cfr `
    -c:v libx264 -preset medium -profile:v high -level:v 4.1 -pix_fmt yuv420p `
    -b:v 50M -minrate 50M -maxrate 50M -bufsize 50M -g 15 -keyint_min 15 -bf 2 `
    -sc_threshold 0 -x264-params 'nal-hrd=cbr:filler=1:force-cfr=1:open-gop=0:b-adapt=0:cabac=1' `
    -tag:v avc1 -c:a aac -profile:a aac_low -b:a 384k -ar 48000 -ac 2 `
    -af 'atrim=duration=59.978666667' -t 60 `
    -movflags '+faststart+negative_cts_offsets' -use_editlist 0 -avoid_negative_ts disabled $video
if ($LASTEXITCODE -ne 0) { throw 'FFmpeg trailer encoding failed.' }
}
finally {
    Remove-Item -LiteralPath $normalized -Recurse -Force
}

$metadata = & $probe -v error -show_streams -show_format -of json $video
if ($LASTEXITCODE -ne 0) { throw 'FFprobe validation failed.' }
$info = ($metadata -join "`n") | ConvertFrom-Json
$videoStream = @($info.streams | Where-Object codec_type -eq 'video')
$audioStream = @($info.streams | Where-Object codec_type -eq 'audio')
$culture = [Globalization.CultureInfo]::InvariantCulture
if ($videoStream.Count -ne 1 -or $audioStream.Count -ne 1 -or
    $videoStream[0].codec_name -ne 'h264' -or $videoStream[0].codec_tag_string -ne 'avc1' -or
    $videoStream[0].profile -ne 'High' -or $videoStream[0].pix_fmt -ne 'yuv420p' -or
    $videoStream[0].width -ne 1920 -or $videoStream[0].height -ne 1080 -or
    $videoStream[0].r_frame_rate -ne '30/1' -or [int]$videoStream[0].nb_frames -ne 1800 -or
    $audioStream[0].codec_name -ne 'aac' -or $audioStream[0].profile -ne 'LC' -or
    [int]$audioStream[0].sample_rate -ne 48000 -or $audioStream[0].channels -ne 2) {
    throw 'Encoded trailer does not match the required video/audio shape.'
}
foreach ($item in @($info.format, $videoStream[0], $audioStream[0])) {
    if ([Math]::Abs([double]::Parse($item.duration, $culture) - 60) -gt 0.001 -or
        [Math]::Abs([double]::Parse($item.start_time, $culture)) -gt 0.001) {
        throw 'Expected all streams and the container to start at zero and last exactly 60 seconds.'
    }
}
& $encoder -hide_banner -loglevel error -xerror -i $video -f null -
if ($LASTEXITCODE -ne 0) { throw 'Trailer failed full decoding.' }
Copy-Item -LiteralPath $ThumbnailPath -Destination $thumbnail -Force
Get-FileHash -LiteralPath $video, $thumbnail -Algorithm SHA256
