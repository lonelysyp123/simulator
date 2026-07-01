#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=publish-common.sh
source "$(dirname "$0")/publish-common.sh"

EDITION="${EDITION:-$EDITION_COMMUNITY}"
RID="${RID:-linux-arm64}"
CONFIG="${CONFIG:-Release}"
OUT="$(dist_out_dir "$EDITION" "$RID")"
ARCHIVE="$(dist_archive_path "$EDITION" "$RID" tar.gz)"

validate_edition "$EDITION"
ensure_dist_layout

cd "$ROOT"

echo "==> Publishing EssSimulator [$EDITION] for Linux ($RID, $CONFIG, self-contained)..."
mkdir -p "$OUT"
dotnet publish EssSimulator.csproj \
  -c "$CONFIG" \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

copy_runtime_files "$OUT" "$EDITION"
copy_platform_files "$OUT" "$EDITION" linux
create_tar_archive "$EDITION" "$RID"

echo "Done."
echo "  Edition: $EDITION"
echo "  Folder:  $OUT"
echo "  Archive: $ARCHIVE"
echo "  Contents:"
ls -lh "$OUT/EssSimulator" "${RUNTIME_FILES[@]/#/$OUT/}" "$ARCHIVE"
