#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=pointmap-common.sh
source "$ROOT/scripts/pointmap-common.sh"

OUT="$ROOT/dist/win-x64"
ZIP="$ROOT/dist/EssSimulator-win-x64.zip"

# 用法: ./scripts/publish-windows.sh [common|lc|battery]
# 或: POINTMAP_VERSION=lc ./scripts/publish-windows.sh
POINTMAP_VERSION="${POINTMAP_VERSION:-${1:-$DEFAULT_DEV_POINTMAP_VERSION}}"

RUNTIME_FILES=(
  appsettings.json
  log4net.config
  autotest.json
)

copy_runtime_files() {
  echo "==> Copying runtime config (point map: $POINTMAP_VERSION)..."
  for f in "${RUNTIME_FILES[@]}"; do
    cp -f "$ROOT/$f" "$OUT/$f"
    echo "    $f"
  done
  copy_pointmaps_to "$OUT" "$POINTMAP_VERSION"
  if [[ -d "$ROOT/docs" ]]; then
    rm -rf "$OUT/docs"
    cp -R "$ROOT/docs" "$OUT/docs"
    echo "    docs/"
  fi
}

validate_pointmap_version "$POINTMAP_VERSION"

cd "$ROOT"

echo "==> Publishing EssSimulator for Windows x64 (self-contained, pointmap=$POINTMAP_VERSION)..."
dotnet publish EssSimulator.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -o "$OUT"

copy_runtime_files

cp "$ROOT/scripts/windows/start.bat" "$OUT/start.bat"
cp "$ROOT/scripts/windows/README-Windows.txt" "$OUT/README-Windows.txt"

echo "==> Creating zip: $ZIP"
rm -f "$ZIP"
(cd "$ROOT/dist" && zip -r "$(basename "$ZIP")" win-x64)

echo "Done."
echo "  Point map: $POINTMAP_VERSION"
echo "  Folder: $OUT"
echo "  Zip:    $ZIP"
ls -lh "$OUT/EssSimulator.exe" "$ZIP"
