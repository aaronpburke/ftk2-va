param(
    [Parameter(Mandatory = $true)]
    [string]$InputDir,

    [string]$OutputDir = $InputDir,

    [ValidateRange(0, 10)]
    [int]$Quality = 6
)

$ErrorActionPreference = "Stop"

Write-Host "========================================="
Write-Host "Voice Acting Audio Converter"
Write-Host "========================================="
Write-Host "Input:   $InputDir"
Write-Host "Output:  $OutputDir"
Write-Host "Quality: $Quality (OGG Vorbis)"
Write-Host "========================================="
Write-Host ""

if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    Write-Error "ffmpeg is not installed or is not on PATH. Install it from https://ffmpeg.org/."
}

if (-not (Test-Path -LiteralPath $InputDir -PathType Container)) {
    Write-Error "Input directory does not exist: $InputDir"
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$total = 0
$converted = 0
$skipped = 0
$failed = 0

Get-ChildItem -LiteralPath $InputDir -Filter "*.wav" -File -Recurse | ForEach-Object {
    $total++
    $outputFile = Join-Path $OutputDir ($_.BaseName + ".ogg")

    if (Test-Path -LiteralPath $outputFile) {
        Write-Host "[SKIP] $($_.Name) (already exists)"
        $skipped++
        return
    }

    Write-Host -NoNewline "Converting: $($_.Name) ... "

    & ffmpeg -i $_.FullName `
        -c:a libvorbis `
        -q:a $Quality `
        -ac 1 `
        -ar 44100 `
        -map_metadata -1 `
        $outputFile `
        -y `
        -loglevel error

    if ($LASTEXITCODE -eq 0) {
        Write-Host "[OK]"
        $converted++
    }
    else {
        Write-Host "[FAIL]"
        $failed++
    }
}

Write-Host ""
Write-Host "========================================="
Write-Host "Conversion Complete"
Write-Host "========================================="
Write-Host "Total:     $total"
Write-Host "Converted: $converted"
Write-Host "Skipped:   $skipped"
Write-Host "Failed:    $failed"
Write-Host "========================================="

if ($failed -eq 0) {
    exit 0
}

exit 1
