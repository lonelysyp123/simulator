#!/usr/bin/env bash
# 商业版 macOS 发布（默认 osx-arm64；可 RID=osx-x64）
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=publish-common.sh
source "$(dirname "$0")/publish-common.sh"

EDITION="$(normalize_edition "${EDITION:-$EDITION_COMMUNITY}")"
RID="${RID:-osx-arm64}"
CONFIG="${CONFIG:-Release}"
OUT="$(dist_out_dir "$EDITION" "$RID")"
ARCHIVE="$(dist_archive_path "$EDITION" "$RID" tar.gz)"

validate_edition "$EDITION"
ensure_dist_layout

cd "$ROOT"

echo "==> Publishing EssSimulator [$EDITION] for macOS ($RID, $CONFIG, self-contained)..."
mkdir -p "$OUT"
dotnet publish EssSimulator.csproj \
  -c "$CONFIG" \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

copy_runtime_files "$OUT" "$EDITION"
copy_platform_files "$OUT" "$EDITION" osx
create_tar_archive "$EDITION" "$RID"

echo "Done."
echo "  Edition: $EDITION"
echo "  Folder:  $OUT"
echo "  Archive: $ARCHIVE"
echo "  Contents:"
ls -lh "$OUT/EssSimulator" "${RUNTIME_FILES[@]/#/$OUT/}" "$ARCHIVE"
