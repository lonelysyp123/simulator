#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=pointmap-common.sh
source "$ROOT/scripts/pointmap-common.sh"

OUT="$ROOT/dist/win-x64"
ZIP="$ROOT/dist/EssSimulator-win-x64.zip"

# 设备型号点表（pointmaps/models）随发布携带，运行期按选型解析。

RUNTIME_FILES=(
  appsettings.json
  log4net.config
  autotest.json
)

copy_runtime_files() {
  echo "==> Copying runtime config..."
  for f in "${RUNTIME_FILES[@]}"; do
    cp -f "$ROOT/$f" "$OUT/$f"
    echo "    $f"
  done
  copy_pointmaps_to "$OUT"
  if [[ -d "$ROOT/docs" ]]; then
    rm -rf "$OUT/docs"
    cp -R "$ROOT/docs" "$OUT/docs"
    echo "    docs/"
  fi
}

validate_device_models

cd "$ROOT"

# 构建 B/S 前端（Vue 3 + Vite）到 wwwroot/
build_frontend() {
  echo "==> Building web frontend (Vue3 + Vite)..."
  if [[ ! -d "$ROOT/web/node_modules" ]]; then
    echo "    installing npm dependencies..."
    (cd "$ROOT/web" && npm install)
  fi
  (cd "$ROOT/web" && npm run build)
  echo "    wwwroot/ populated."
}

build_frontend

echo "==> Cleaning previous output: $OUT"
rm -rf "$OUT"
mkdir -p "$OUT"

echo "==> Publishing EssSimulator for Windows x64 (self-contained, pointmap model=$DEFAULT_ROOT_MODEL)..."
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
echo "  Point map model: $DEFAULT_ROOT_MODEL"
echo "  Folder: $OUT"
echo "  Zip:    $ZIP"
ls -lh "$OUT/EssSimulator.exe" "$ZIP"
