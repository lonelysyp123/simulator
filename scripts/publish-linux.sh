#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
RID="${RID:-linux-arm64}"
CONFIG="${CONFIG:-Debug}"
OUT="$ROOT/dist/$RID"
ARCHIVE="$ROOT/dist/EssSimulator-${RID}.tar.gz"

# 单文件发布时 dotnet 可能漏拷内容文件，发布后再显式复制一次以确保可运行。
RUNTIME_FILES=(
  appsettings.json
  log4net.config
  emu.csv
  em.csv
  bms_bank.csv
  bms_rack.csv
  lc.csv
  autotest.json
)

copy_runtime_files() {
  echo "==> Copying runtime config and point maps..."
  for f in "${RUNTIME_FILES[@]}"; do
    cp -f "$ROOT/$f" "$OUT/$f"
    echo "    $f"
  done
  if [[ -d "$ROOT/docs" ]]; then
    rm -rf "$OUT/docs"
    cp -R "$ROOT/docs" "$OUT/docs"
    echo "    docs/"
  fi
}

cd "$ROOT"

echo "==> Publishing EssSimulator for Linux ($RID, $CONFIG, self-contained)..."
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
echo "  Folder:  $OUT"
echo "  Archive: $ARCHIVE"
echo "  Contents:"
ls -lh "$OUT/EssSimulator" "${RUNTIME_FILES[@]/#/$OUT/}" "$ARCHIVE"
