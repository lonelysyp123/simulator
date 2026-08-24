#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
# shellcheck source=pointmap-common.sh
source "$ROOT/scripts/pointmap-common.sh"

RID="${RID:-linux-arm64}"
CONFIG="${CONFIG:-Debug}"
OUT="$ROOT/dist/$RID"
ARCHIVE="$ROOT/dist/EssSimulator-${RID}.tar.gz"

# 根目录点表固定取 standard 型号；设备型号点表（pointmaps/models）随发布携带，
# 运行期可在系统配置界面选型。

RUNTIME_FILES=(
  appsettings.json
  log4net.config
  autotest.json
)

copy_runtime_files() {
  echo "==> Copying runtime config (point map model: $DEFAULT_ROOT_MODEL)..."
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

echo "==> Publishing EssSimulator for Linux ($RID, $CONFIG, pointmap model=$DEFAULT_ROOT_MODEL, self-contained)..."
dotnet publish EssSimulator.csproj \
  -c "$CONFIG" \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

copy_runtime_files

cp "$ROOT/scripts/linux/start.sh" "$OUT/start.sh"
cp "$ROOT/scripts/linux/README-Linux.txt" "$OUT/README-Linux.txt"
chmod +x "$OUT/start.sh" "$OUT/EssSimulator"

echo "==> Creating archive: $ARCHIVE"
rm -f "$ARCHIVE"
tar -czf "$ARCHIVE" -C "$ROOT/dist" "$(basename "$OUT")"

echo "Done."
echo "  Point map model: $DEFAULT_ROOT_MODEL"
echo "  Folder:  $OUT"
echo "  Archive: $ARCHIVE"
echo "  Contents:"
ls -lh "$OUT/EssSimulator" "${RUNTIME_FILES[@]/#/$OUT/}" "$ARCHIVE"
