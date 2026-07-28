#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
# shellcheck source=publish-common.sh
source "$(dirname "$0")/publish-common.sh"

EDITION="$(normalize_edition "${EDITION:-$EDITION_COMMUNITY}")"
RID="win-x64"
OUT="$(dist_out_dir "$EDITION" "$RID")"
ZIP="$(dist_archive_path "$EDITION" "$RID" zip)"

validate_edition "$EDITION"
ensure_dist_layout

cd "$ROOT"

echo "==> Publishing EssSimulator [$EDITION] for Windows x64 (self-contained)..."
mkdir -p "$OUT"
dotnet publish EssSimulator.csproj \
  -c Release \
  -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true \
  -o "$OUT"

copy_runtime_files "$OUT" "$EDITION"
copy_platform_files "$OUT" "$EDITION" windows
create_zip_archive "$EDITION" "$RID"

echo "Done."
echo "  Edition: $EDITION"
echo "  Folder:  $OUT"
echo "  Zip:     $ZIP"
ls -lh "$OUT/EssSimulator.exe" "$ZIP"
