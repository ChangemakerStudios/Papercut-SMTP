#!/usr/bin/env bash
# Builds an UNSIGNED Velopack package of the desktop shell on macOS: .app, .pkg installer and portable .zip.
# The .app holds the shell (Contents/MacOS/Papercut) and the service (Contents/MacOS/service/).
# Usage: ./pack.sh [osx-arm64|osx-x64] [version] [channel-postfix]
set -euo pipefail

RID="${1:-osx-arm64}"
VERSION="${2:-8.0.0-alpha.1}"
POSTFIX="${3:--alpha}"
VPK_VERSION="1.2.158"   # keep in step with the Velopack package in Papercut.Desktop.csproj and build.cake

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$HERE/../.." && pwd)"
REPO_ROOT="$(cd "$PROJECT_DIR/../.." && pwd)"
OUT="$HERE/out/$RID"
PUBLISH="$OUT/publish"
RELEASES="$OUT/releases"
WORK="$OUT/work"

[[ "$(uname)" == "Darwin" ]] || { echo "macOS only: Velopack needs codesign/xcrun/productbuild" >&2; exit 1; }

for tool in dotnet sips iconutil node; do
  command -v "$tool" >/dev/null || { echo "missing: $tool" >&2; exit 1; }
done

if ! command -v vpk >/dev/null || [[ "$(vpk --version 2>/dev/null | head -1)" != *"$VPK_VERSION"* ]]; then
  echo "==> installing vpk $VPK_VERSION"
  dotnet tool update -g vpk --version "$VPK_VERSION"
  export PATH="$PATH:$HOME/.dotnet/tools"
fi

rm -rf "$OUT"
mkdir -p "$PUBLISH" "$RELEASES" "$WORK"

echo "==> publishing the shell ($RID)"
dotnet publish "$PROJECT_DIR/Papercut.Desktop.csproj" \
  -c Release -f net10.0 -r "$RID" --self-contained true \
  -p:Version="$VERSION" \
  -o "$PUBLISH"

echo "==> publishing the service ($RID) into service/"
dotnet publish "$REPO_ROOT/src/Papercut.Service/Papercut.Service.csproj" \
  -c Release -r "$RID" --self-contained true \
  -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true \
  -p:Version="$VERSION" \
  -o "$PUBLISH/service"

echo "==> building Papercut.icns"
# the only square source in the repo is 128px, so large sizes are upscaled; a 1024px master is still needed
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
# CFBundleShortVersionString must be numeric: strip any prerelease suffix
SHORT_VERSION="${VERSION%%-*}"
sed -e "s/@VERSION@/$SHORT_VERSION/g" "$HERE/Info.plist" > "$WORK/Info.plist"
plutil -lint "$WORK/Info.plist"

echo "==> vpk pack (unsigned)"
vpk pack \
  --packId PapercutSMTP \
  --packVersion "$VERSION" \
  --packTitle "Papercut SMTP" \
  --packDir "$PUBLISH" \
  --mainExe Papercut \
  --icon "$WORK/Papercut.icns" \
  --plist "$WORK/Info.plist" \
  --channel "$RID$POSTFIX" \
  --releaseNotes "$REPO_ROOT/ReleaseNotesCurrent.md" \
  --outputDir "$RELEASES"

echo
echo "==> done: $RELEASES"
ls -la "$RELEASES"
cat <<NEXT

Next:
  1. Install:  open "$RELEASES"/*Setup.pkg
  2. Launch "Papercut SMTP" from Applications.
  3. Check:
     - Papercut icon in the MENU BAR, no Dock icon
     - "Open Papercut" shows the web UI; closing hides it; Exit quits and the service stops
       (ps aux | grep Papercut.Service)
     - send mail to localhost:2525 -> a notification appears
  4. Logs: ~/Library/Application Support/Papercut SMTP/Logs/
NEXT
