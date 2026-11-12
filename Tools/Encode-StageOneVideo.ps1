<#
.SYNOPSIS
Creates a silent MJPEG AVI from a successful simulator validation capture.
.DESCRIPTION
Requires PowerShell on Windows, System.Drawing.Common and Python 3 (standard
library only). Timestamps in resultados.json are resampled to a constant frame
rate, so long pauses are retained. Images retain their aspect ratio.
.EXAMPLE
pwsh -File Tools/Encode-StageOneVideo.ps1 -RunDirectory Docs/Evidencias_Etapa_01/Run_YYYYMMDD_HHMMSS
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$RunDirectory,
    [ValidateRange(1, 100)][int]$ExpectedTestCount = 7,
    [ValidateRange(1, 60)][int]$FramesPerSecond = 4,
    [ValidateRange(64, 3840)][int]$MaxWidth = 960,
    [ValidateRange(1, 100)][int]$JpegQuality = 80,
    [ValidatePattern('^[a-zA-Z0-9_.-]+\.avi$')][string]$OutputName = 'video_recorrido.avi',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$projectPrefix = $projectRoot.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
$runPath = (Resolve-Path -LiteralPath $RunDirectory).Path
if (-not $runPath.StartsWith($projectPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'RunDirectory debe estar dentro de la carpeta de este proyecto.'
}
$reportPath = Join-Path $runPath 'resultados.json'
$report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
$acceptedStates = @('functional_checks_passed_visual_review_pending', 'passed', 'completed')
if ($report.state -notin $acceptedStates -or @($report.tests).Count -ne $ExpectedTestCount -or
    @($report.tests | Where-Object { -not $_.passed }).Count -gt 0 -or @($report.errors).Count -gt 0) {
    throw "Se requieren $ExpectedTestCount comprobaciones aprobadas y una ejecución sin errores para generar el video."
}
$frames = @($report.frames | Sort-Object elapsedSeconds)
if ($frames.Count -lt 2) { throw 'Se requieren al menos dos frames capturados.' }
$outputPath = Join-Path $runPath $OutputName
if ((Test-Path -LiteralPath $outputPath) -and -not $Force) {
    throw 'El video ya existe. Utiliza -Force para reemplazarlo.'
}
$pythonPath = (Get-Command python -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
Add-Type -AssemblyName System.Drawing.Common
$encoderPath = Join-Path $PSScriptRoot 'encode_stage_one_avi.py'
if (-not (Test-Path -LiteralPath $encoderPath)) { throw 'No se encuentra encode_stage_one_avi.py junto al script.' }

$tempPath = Join-Path $runPath ('.video_encode_' + [Guid]::NewGuid().ToString('N'))
$runPrefix = $runPath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
$tempPath = [System.IO.Path]::GetFullPath($tempPath)
if (-not $tempPath.StartsWith($runPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'La carpeta temporal debe permanecer dentro del run.'
}
[void][System.IO.Directory]::CreateDirectory($tempPath)
$temporaryFiles = [System.Collections.Generic.List[string]]::new()
$jpegCodec = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders() |
    Where-Object MimeType -EQ 'image/jpeg' | Select-Object -First 1
if ($null -eq $jpegCodec) { throw 'No está disponible el codificador JPEG de System.Drawing.' }
$manifestFrames = [System.Collections.Generic.List[object]]::new()
$width = 0
$height = 0

try {
    for ($index = 0; $index -lt $frames.Count; $index++) {
        $pngPath = [System.IO.Path]::GetFullPath((Join-Path $runPath $frames[$index].file))
        if (-not $pngPath.StartsWith($runPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            throw ('Un frame sale de la carpeta del run: ' + $frames[$index].file)
        }
        if (-not (Test-Path -LiteralPath $pngPath -PathType Leaf)) {
            throw ('Falta un frame capturado: ' + $frames[$index].file)
        }
        $image = [System.Drawing.Image]::FromFile($pngPath)
        $bitmap = $null
        $graphics = $null
        $parameters = $null
        try {
            $scale = [Math]::Min(1.0, $MaxWidth / [double]$image.Width)
            $frameWidth = [Math]::Max(1, [int][Math]::Round($image.Width * $scale))
            $frameHeight = [Math]::Max(1, [int][Math]::Round($image.Height * $scale))
            if ($index -eq 0) { $width = $frameWidth; $height = $frameHeight }
            if ($frameWidth -ne $width -or $frameHeight -ne $height) {
                throw 'La resolución de Game View cambió durante la captura; no se estirarán los frames.'
            }
            $bitmap = [System.Drawing.Bitmap]::new($width, $height, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            $graphics.Clear([System.Drawing.Color]::Black)
            $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
            $graphics.DrawImage($image, 0, 0, $width, $height)
            $parameters = [System.Drawing.Imaging.EncoderParameters]::new(1)
            $parameters.Param[0] = [System.Drawing.Imaging.EncoderParameter]::new(
                [System.Drawing.Imaging.Encoder]::Quality, [long]$JpegQuality)
            $jpegPath = Join-Path $tempPath ('frame_' + $index.ToString('D5') + '.jpg')
            $temporaryFiles.Add($jpegPath)
            $bitmap.Save($jpegPath, $jpegCodec, $parameters)
            $manifestFrames.Add([pscustomobject]@{
                file = $jpegPath
                elapsedSeconds = [double]$frames[$index].elapsedSeconds
            })
        }
        finally {
            if ($null -ne $parameters) { $parameters.Dispose() }
            if ($null -ne $graphics) { $graphics.Dispose() }
            if ($null -ne $bitmap) { $bitmap.Dispose() }
            $image.Dispose()
        }
    }
    $manifestPath = Join-Path $tempPath 'manifest.json'
    $temporaryFiles.Add($manifestPath)
    [pscustomobject]@{
        width = $width
        height = $height
        fps = $FramesPerSecond
        frames = $manifestFrames.ToArray()
        sourceState = $report.state
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
    & $pythonPath $encoderPath --manifest $manifestPath --output $outputPath
    if ($LASTEXITCODE -ne 0) { throw ('El codificador AVI terminó con código ' + $LASTEXITCODE) }
    Get-Item -LiteralPath $outputPath | Select-Object FullName, Length
}
finally {
    # Delete only the exact files created by this invocation; never recurse.
    foreach ($temporaryFile in $temporaryFiles) {
        $checkedPath = [System.IO.Path]::GetFullPath($temporaryFile)
        if (-not $checkedPath.StartsWith($tempPath + [System.IO.Path]::DirectorySeparatorChar,
                [StringComparison]::OrdinalIgnoreCase)) { throw 'Ruta temporal inesperada.' }
        if (Test-Path -LiteralPath $checkedPath) { Remove-Item -LiteralPath $checkedPath -Force }
    }
    if (Test-Path -LiteralPath $tempPath) { Remove-Item -LiteralPath $tempPath }
}
