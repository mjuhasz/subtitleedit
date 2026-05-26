#!/usr/bin/env bash
# Build an unsigned macOS DMG for SubtitleEdit.
# Usage: ./build-macos-dmg.sh [arm64|x64]   (default: arm64)

set -euo pipefail

ARCH="${1:-arm64}"
REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"

case "$ARCH" in
  arm64)
    RUNTIME="osx-arm64"
    ARCH_LABEL="ARM64"
    FFMPEG_URL="https://www.osxexperts.net/ffmpeg81arm.zip"
    FFMPEG_SHA="ebb82529562b71170807bbc6b0e7eb4f0b13af8cbb0e085bb9e8f6fe709598ad"
    DMG_FORMAT="UDBZ"
    ;;
  x64)
    RUNTIME="osx-x64"
    ARCH_LABEL="x64"
    FFMPEG_URL="https://www.osxexperts.net/ffmpeg80intel.zip"
    FFMPEG_SHA="2d24d22db78c87f394a5822867acd5c5dc5e762cd261a44bd26923f3a5af3e07"
    DMG_FORMAT="UDZO"
    ;;
  *)
    echo "Usage: $0 [arm64|x64]" >&2
    exit 1
    ;;
esac

IINA_VERSION="v1.4.2"
IINA_SHA="2e0fd89fbba1c92a6c115171e5b51904883bb497fbe513a6961d080fbab08ff6"

PUBLISH_DIR="$REPO_ROOT/publish/macos-$ARCH"
APP_BUNDLE="$REPO_ROOT/SubtitleEdit-$ARCH_LABEL.app"
DMG_OUT="$REPO_ROOT/SubtitleEdit-macOS-$ARCH_LABEL.dmg"
DMG_STAGE="$REPO_ROOT/dmg-temp-$ARCH"
FFMPEG_TMP="$REPO_ROOT/ffmpeg-temp"
LIBMPV_TMP="$REPO_ROOT/libmpv-temp"

cd "$REPO_ROOT"

echo "==> Publishing ($RUNTIME)..."
dotnet publish src/ui/UI.csproj -c Release -r "$RUNTIME" --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugSymbols=false \
  -p:DebugType=none \
  -o "$PUBLISH_DIR"
find "$PUBLISH_DIR" -name "*.pdb" -delete

echo "==> Downloading ffmpeg ($ARCH_LABEL)..."
curl -fL -o ffmpeg.zip "$FFMPEG_URL"
echo "$FFMPEG_SHA  ffmpeg.zip" | shasum -a 256 -c -
rm -rf "$FFMPEG_TMP" && mkdir -p "$FFMPEG_TMP"
unzip -o ffmpeg.zip -d "$FFMPEG_TMP"
rm ffmpeg.zip
if [ ! -f "$FFMPEG_TMP/ffmpeg" ]; then
  found=$(find "$FFMPEG_TMP" -maxdepth 3 -type f -name ffmpeg | head -1)
  [ -n "$found" ] && mv "$found" "$FFMPEG_TMP/ffmpeg"
fi
chmod +x "$FFMPEG_TMP/ffmpeg"

echo "==> Downloading libmpv from IINA $IINA_VERSION..."
curl -fL -o iina.dmg "https://github.com/iina/iina/releases/download/${IINA_VERSION}/IINA.${IINA_VERSION}.dmg"
echo "$IINA_SHA  iina.dmg" | shasum -a 256 -c -
hdiutil attach iina.dmg -nobrowse -readonly -mountpoint ./iina-mnt
rm -rf "$LIBMPV_TMP" && mkdir -p "$LIBMPV_TMP"
for dylib in ./iina-mnt/IINA.app/Contents/Frameworks/*.dylib; do
  name=$(basename "$dylib")
  case "$name" in libswift_*) continue ;; esac
  cp -L "$dylib" "$LIBMPV_TMP/$name"
done
hdiutil detach ./iina-mnt
rm iina.dmg
[ -f "$LIBMPV_TMP/libmpv.2.dylib" ] || { echo "ERROR: libmpv.2.dylib not found in IINA bundle" >&2; exit 1; }

echo "==> Updating Info.plist version..."
chmod +x ./installer/macBundle/update-plist-version.sh
./installer/macBundle/update-plist-version.sh

echo "==> Assembling app bundle..."
rm -rf "$APP_BUNDLE"
cp -R "./installer/macBundle/SubtitleEdit.app" "$APP_BUNDLE"
cp "$PUBLISH_DIR/SubtitleEdit" "$APP_BUNDLE/Contents/MacOS/"
cp "$PUBLISH_DIR/"*.dylib "$APP_BUNDLE/Contents/MacOS/" 2>/dev/null || true
chmod +x "$APP_BUNDLE/Contents/MacOS/SubtitleEdit"
cp "$FFMPEG_TMP/ffmpeg" "$APP_BUNDLE/Contents/MacOS/" && chmod +x "$APP_BUNDLE/Contents/MacOS/ffmpeg"
mkdir -p "$APP_BUNDLE/Contents/Frameworks"
cp "$LIBMPV_TMP/"*.dylib "$APP_BUNDLE/Contents/Frameworks/"
chmod -R 755 "$APP_BUNDLE/Contents/Frameworks/"
executable="$APP_BUNDLE/Contents/MacOS/SubtitleEdit"
if ! otool -l "$executable" | grep -q "@executable_path/../Frameworks"; then
  install_name_tool -add_rpath "@executable_path/../Frameworks" "$executable" || true
fi

echo "==> Ad-hoc signing app bundle..."
codesign --force --deep --sign - "$APP_BUNDLE"

echo "==> Creating DMG..."
rm -rf "$DMG_STAGE" && mkdir -p "$DMG_STAGE"
cp -R "$APP_BUNDLE" "$DMG_STAGE/Subtitle Edit.app"
cp "./installer/macBundle/THIRD_PARTY_LICENSES.txt" "$DMG_STAGE/"
cp "./installer/macBundle/README-macOS.txt" "$DMG_STAGE/"
cp "./installer/macBundle/fix-unsigned-app.sh" "$DMG_STAGE/" && chmod +x "$DMG_STAGE/fix-unsigned-app.sh"
ln -sf /Applications "$DMG_STAGE/Applications"
sync
hdiutil create -volname "SubtitleEdit $ARCH_LABEL" -srcfolder "$DMG_STAGE" -ov -format "$DMG_FORMAT" "$DMG_OUT"

echo "==> Cleaning up..."
rm -rf "$APP_BUNDLE" "$DMG_STAGE" "$PUBLISH_DIR" "$FFMPEG_TMP" "$LIBMPV_TMP"

echo ""
echo "Done: $DMG_OUT"
