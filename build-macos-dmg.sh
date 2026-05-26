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
    EXPECTED_ARCH="arm64"
    FFMPEG_URL="https://www.osxexperts.net/ffmpeg81arm.zip"
    FFMPEG_SHA="ebb82529562b71170807bbc6b0e7eb4f0b13af8cbb0e085bb9e8f6fe709598ad"
    VOIKKO_URL="https://github.com/SubtitleEdit/support-files/releases/download/voikko-4.3-fi-2024-06/libvoikko-4.3.3-macos-arm64.zip"
    VOIKKO_SHA="21a131d6a972890384c7900f2aace608a6c8baa14da8c63863ee7433def9bb2b"
    DMG_FORMAT="UDBZ"
    ;;
  x64)
    RUNTIME="osx-x64"
    ARCH_LABEL="x64"
    EXPECTED_ARCH="x86_64"
    FFMPEG_URL="https://www.osxexperts.net/ffmpeg80intel.zip"
    FFMPEG_SHA="2d24d22db78c87f394a5822867acd5c5dc5e762cd261a44bd26923f3a5af3e07"
    VOIKKO_URL="https://github.com/SubtitleEdit/support-files/releases/download/voikko-4.3-fi-2024-06/libvoikko-4.3.3-macos-x64.zip"
    VOIKKO_SHA="fbd458122536708192a4f56f98f34d28a399547e628ce17d14d4cc6534b7a62b"
    DMG_FORMAT="UDZO"
    ;;
  *)
    echo "Usage: $0 [arm64|x64]" >&2
    exit 1
    ;;
esac

IINA_VERSION="v1.4.4"
IINA_SHA="dd0fc0bd4b37fb57a1c8d30d6e3201b3a64bafd29959fe56953964613237beb1"

VOIKKO_DICT_URL="https://github.com/SubtitleEdit/support-files/releases/download/voikko-4.3-fi-2024-06/dict.zip"
VOIKKO_DICT_SHA="98f26bb67e08288910fbf1aa92521f28bee79538ba86aefa267713333e7fa537"

PUBLISH_DIR="$REPO_ROOT/publish/macos-$ARCH"
APP_BUNDLE="$REPO_ROOT/SubtitleEdit-$ARCH_LABEL.app"
DMG_OUT="$REPO_ROOT/SubtitleEdit-macOS-$ARCH_LABEL.dmg"
DMG_STAGE="$REPO_ROOT/dmg-temp-$ARCH"
FFMPEG_TMP="$REPO_ROOT/ffmpeg-temp"
LIBMPV_TMP="$REPO_ROOT/libmpv-temp"
VOIKKO_DICT_TMP="$REPO_ROOT/voikko-dict-temp"
IINA_MNT="$REPO_ROOT/iina-mnt"

cd "$REPO_ROOT"

# Runs on success and on failure, so a broken build doesn't leave the IINA image mounted,
# temp folders behind, or Info.plist / packages.lock.json modified.
cleanup() {
  if [ -d "$IINA_MNT" ]; then
    hdiutil detach "$IINA_MNT" -quiet 2>/dev/null || true
  fi
  rm -rf "$APP_BUNDLE" "$DMG_STAGE" "$PUBLISH_DIR" "$FFMPEG_TMP" "$LIBMPV_TMP" "$VOIKKO_DICT_TMP" "$IINA_MNT"
  rm -f ffmpeg.zip iina.dmg libvoikko.zip voikko-dict.zip
  git restore installer/macBundle/update-plist-version.sh installer/macBundle/SubtitleEdit.app/Contents/Info.plist src/ui/packages.lock.json 2>/dev/null || true
}
trap cleanup EXIT

# Same version extraction as the CI build: "v5.3.0" -> 5.3.0.0, "v5.0.0-beta18" -> 5.0.0.18
version_line=$(grep -E 'public[[:space:]]+static[[:space:]]+string[[:space:]]+Version[[:space:]]*\{' src/ui/Logic/Config/Se.cs)
APP_VER_DISPLAY=$(printf '%s' "$version_line" | sed -E 's/.*"v([^"]+)".*/\1/')
numeric_part="${APP_VER_DISPLAY%%-*}"
suffix=""
case "$APP_VER_DISPLAY" in
  *-*) suffix="${APP_VER_DISPLAY#*-}" ;;
esac
dot_count=$(printf '%s' "$numeric_part" | tr -cd '.' | wc -c | tr -d ' ')
case "$dot_count" in
  0) numeric_part="${numeric_part}.0.0" ;;
  1) numeric_part="${numeric_part}.0" ;;
esac
revision=$(printf '%s' "$suffix" | grep -oE '[0-9]+$' || true)
APP_VER_FULL="${numeric_part}.${revision:-0}"

echo "==> Publishing ($RUNTIME, version $APP_VER_DISPLAY / $APP_VER_FULL)..."
dotnet publish src/ui/UI.csproj -c Release -r "$RUNTIME" --self-contained true \
  -p:PublishSingleFile=true \
  -p:DebugSymbols=false \
  -p:DebugType=none \
  -p:Version="$APP_VER_FULL" \
  -p:FileVersion="$APP_VER_FULL" \
  -p:InformationalVersion="$APP_VER_DISPLAY" \
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
hdiutil attach iina.dmg -nobrowse -readonly -mountpoint "$IINA_MNT"
rm -rf "$LIBMPV_TMP" && mkdir -p "$LIBMPV_TMP"
for dylib in "$IINA_MNT"/IINA.app/Contents/Frameworks/*.dylib; do
  name=$(basename "$dylib")
  case "$name" in libswift_*) continue ;; esac
  cp -L "$dylib" "$LIBMPV_TMP/$name"
done
hdiutil detach "$IINA_MNT"
rm iina.dmg
[ -f "$LIBMPV_TMP/libmpv.2.dylib" ] || { echo "ERROR: libmpv.2.dylib not found in IINA bundle" >&2; exit 1; }

# libvoikko for Finnish spell check: the dylib goes to Contents/Frameworks with libmpv, the
# dictionary to Contents/Resources/voikko, where VoikkoSpellChecker looks for them.
echo "==> Downloading libvoikko + voikko-fi dictionary ($ARCH_LABEL)..."
curl -fL -o libvoikko.zip "$VOIKKO_URL"
echo "$VOIKKO_SHA  libvoikko.zip" | shasum -a 256 -c -
unzip -o libvoikko.zip -d "$LIBMPV_TMP"
rm libvoikko.zip
archs=$(lipo -archs "$LIBMPV_TMP/libvoikko.1.dylib")
[ "$archs" = "$EXPECTED_ARCH" ] || { echo "ERROR: libvoikko archs are '$archs', expected '$EXPECTED_ARCH'" >&2; exit 1; }
curl -fL -o voikko-dict.zip "$VOIKKO_DICT_URL"
echo "$VOIKKO_DICT_SHA  voikko-dict.zip" | shasum -a 256 -c -
rm -rf "$VOIKKO_DICT_TMP" && mkdir -p "$VOIKKO_DICT_TMP"
unzip -o voikko-dict.zip -d "$VOIKKO_DICT_TMP"
rm voikko-dict.zip
[ -f "$VOIKKO_DICT_TMP/5/mor-standard/mor.vfst" ] || { echo "ERROR: voikko dictionary layout changed" >&2; exit 1; }

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
mkdir -p "$APP_BUNDLE/Contents/Resources/voikko"
cp -R "$VOIKKO_DICT_TMP/5" "$APP_BUNDLE/Contents/Resources/voikko/"
executable="$APP_BUNDLE/Contents/MacOS/SubtitleEdit"
if ! otool -l "$executable" | grep -q "@executable_path/../Frameworks"; then
  install_name_tool -add_rpath "@executable_path/../Frameworks" "$executable" || true
fi

echo "==> Ad-hoc signing app bundle..."
# .gitkeep only exists so git tracks the empty Contents/MacOS folder; codesign treats
# every file under MacOS/ as code, so leaving it in place fails signing. .DS_Store can
# arrive the same way and is unsignable too.
find "$APP_BUNDLE" \( -name ".gitkeep" -o -name ".DS_Store" \) -delete
codesign --force --deep --sign - "$APP_BUNDLE"

echo "==> Creating DMG..."
rm -rf "$DMG_STAGE" && mkdir -p "$DMG_STAGE"
cp -R "$APP_BUNDLE" "$DMG_STAGE/Subtitle Edit.app"
cp "./LICENSE" "$DMG_STAGE/LICENSE"
cp "./installer/macBundle/THIRD_PARTY_LICENSES.txt" "$DMG_STAGE/"
cp "./installer/macBundle/README-macOS.txt" "$DMG_STAGE/"
cp "./installer/macBundle/fix-unsigned-app.sh" "$DMG_STAGE/" && chmod +x "$DMG_STAGE/fix-unsigned-app.sh"
ln -sf /Applications "$DMG_STAGE/Applications"
sync
hdiutil create -volname "SubtitleEdit $ARCH_LABEL" -srcfolder "$DMG_STAGE" -ov -format "$DMG_FORMAT" "$DMG_OUT"

echo ""
echo "Done: $DMG_OUT"
