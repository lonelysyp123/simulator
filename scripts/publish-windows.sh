#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/dist/win-x64"
ZIP="$ROOT/dist/EssSimulator-win-x64.zip"

cd "$ROOT"

echo "==> Publishing EssSimulator for Windows x64 (self-contained)..."
dotnet publish EssSimulator.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -o "$OUT"

cp "$ROOT/scripts/windows/start.bat" "$OUT/start.bat"
cp "$ROOT/scripts/windows/README-Windows.txt" "$OUT/README-Windows.txt"

echo "==> Creating zip: $ZIP"
rm -f "$ZIP"
(cd "$ROOT/dist" && zip -r "$(basename "$ZIP")" win-x64)

echo "Done."
echo "  Folder: $OUT"
echo "  Zip:    $ZIP"
ls -lh "$OUT/EssSimulator.exe" "$ZIP"
