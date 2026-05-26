#!/bin/bash
# Batch convert WAV files to OGG Vorbis for voice acting.
#
# Usage:
#   ./convert_audio.sh <input_dir> [output_dir] [quality]
#
# Arguments:
#   input_dir   Directory containing .wav files (searched recursively)
#   output_dir  Output directory for .ogg files (default: same as input)
#   quality     OGG quality 0-10 (default: 6, ~128 kbps — recommended for dialogue)
#
# Requires: ffmpeg (https://ffmpeg.org/)
#
# Example:
#   ./convert_audio.sh recordings/ VoiceAssets/NPC_BARMAID/ 6

set -euo pipefail

INPUT_DIR="${1:-.}"
OUTPUT_DIR="${2:-$INPUT_DIR}"
QUALITY="${3:-6}"

echo "========================================="
echo "Voice Acting Audio Converter"
echo "========================================="
echo "Input:   $INPUT_DIR"
echo "Output:  $OUTPUT_DIR"
echo "Quality: $QUALITY (OGG Vorbis)"
echo "========================================="
echo ""

if ! command -v ffmpeg &> /dev/null; then
    echo "Error: ffmpeg is not installed."
    echo "Install it from https://ffmpeg.org/ or via your package manager."
    exit 1
fi

if [ ! -d "$INPUT_DIR" ]; then
    echo "Error: Input directory does not exist: $INPUT_DIR"
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

total=0
converted=0
skipped=0
failed=0

while IFS= read -r -d '' file; do
    ((total++))
    filename=$(basename "$file")
    basename="${filename%.*}"
    output_file="$OUTPUT_DIR/${basename}.ogg"

    if [ -f "$output_file" ]; then
        echo "[SKIP] $filename (already exists)"
        ((skipped++))
        continue
    fi

    echo -n "Converting: $filename ... "

    if ffmpeg -i "$file" \
        -c:a libvorbis \
        -q:a "$QUALITY" \
        -ac 1 \
        -ar 44100 \
        -map_metadata -1 \
        "$output_file" \
        -y \
        -loglevel error 2>&1; then
        echo "[OK]"
        ((converted++))
    else
        echo "[FAIL]"
        ((failed++))
    fi

done < <(find "$INPUT_DIR" -type f -iname "*.wav" -print0)

echo ""
echo "========================================="
echo "Conversion Complete"
echo "========================================="
echo "Total:     $total"
echo "Converted: $converted"
echo "Skipped:   $skipped"
echo "Failed:    $failed"
echo "========================================="

exit 0
