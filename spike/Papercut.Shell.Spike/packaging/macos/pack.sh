#!/usr/bin/env bash
# Builds an UNSIGNED Velopack package of the shell spike on macOS: .app, .pkg installer and portable .zip.
# Usage: ./pack.sh [osx-arm64|osx-x64] [version]
set -euo pipefail

RID="${1:-osx-arm64}"
VERSION="${2:-0.1.0}"
VPK_VERSION="1.2.158"   # keep in step with the Velopack package in Papercut.Shell.Spike.csproj

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$HERE/../.." && pwd)"
REPO_ROOT="$(cd "$PROJECT_DIR/../.." && pwd)"
OUT="$HERE/out/$RID"
PUBLISH="$OUT/publish"
RELEASES="$OUT/releases"
WORK="$OUT/work"

[[ "$(uname)" == "Darwin" ]] || { echo "macOS only: Velopack needs codesign/xcrun/productbuild" >&2; exit 1; }

for tool in dotnet sips iconutil; do
  command -v "$tool" >/dev/null || { echo "missing: $tool" >&2; exit 1; }
done

# vpk as a global dotnet tool, pinned
if ! command -v vpk >/dev/null || [[ "$(vpk --version 2>/dev/null | head -1)" != *"$VPK_VERSION"* ]]; then
  echo "==> installing vpk $VPK_VERSION"
  dotnet tool update -g vpk --version "$VPK_VERSION"
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

rm -rf "$OUT"
mkdir -p "$PUBLISH" "$RELEASES" "$WORK"

echo "==> publishing $RID"
dotnet publish "$PROJECT_DIR/Papercut.Shell.Spike.csproj" \
  -c Release -r "$RID" --self-contained true \
  -p:Version="$VERSION" \
  -o "$PUBLISH"

echo "==> building Papercut.icns"
# the only square source in the repo is 128px, so large sizes are upscaled -- fine for a spike;
# the real shell needs a 1024px master
SRC_PNG="$REPO_ROOT/graphics/Papercut-icon.png"
ICONSET="$WORK/Papercut.iconset"
mkdir -p "$ICONSET"
for size in 16 32 128 256 512; do
  sips -z "$size" "$size" "$SRC_PNG" --out "$ICONSET/icon_${size}x${size}.png" >/dev/null
  double=$((size * 2))
  sips -z "$double" "$double" "$SRC_PNG" --out "$ICONSET/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$WORK/Papercut.icns"

echo "==> writing Info.plist"
sed "s/@VERSION@/$VERSION/g" "$HERE/Info.plist" > "$WORK/Info.plist"
plutil -lint "$WORK/Info.plist"

echo "==> vpk pack (unsigned)"
vpk pack \
  --packId PapercutShellSpike \
  --packVersion "$VERSION" \
  --packTitle "Papercut Shell Spike" \
  --packDir "$PUBLISH" \
  --mainExe Papercut.Shell.Spike \
  --icon "$WORK/Papercut.icns" \
  --plist "$WORK/Info.plist" \
  --channel "$RID" \
  --outputDir "$RELEASES"

echo
echo "==> done: $RELEASES"
ls -la "$RELEASES"
cat <<EOF

Next:
  1. Install:  open "$RELEASES"/*Setup.pkg     (or: sudo installer -pkg <that .pkg> -target /)
  2. Launch "Papercut Shell Spike" from Applications (or Spotlight).
  3. Check, and note each result:
     - Papercut icon appears in the MENU BAR
     - NO Dock icon, and no app menu at the top of the screen
     - the window opens and the T/I cases behave as before
     - closing the window hides it; the menu bar icon reopens it; Exit quits
  4. Log: ~/Library/Application Support/Papercut.Shell.Spike/logs/
EOF
