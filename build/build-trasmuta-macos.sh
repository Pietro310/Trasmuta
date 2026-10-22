#!/bin/sh
# Builds Trasmuta.app for this Mac (Apple Silicon or Intel) and, with --install, copies it to ~/Applications.
# A shell version of the macOS part of build-portable.ps1, so no PowerShell is needed. The bundled engines (7-Zip,
# rhwp) are copied from an installed Filee.app when there is one; otherwise they are left out and the app offers
# to download what it needs.
#
#   build/build-trasmuta-macos.sh            build into $TMPDIR/trasmuta-build/Trasmuta.app
#   build/build-trasmuta-macos.sh --install  build, then replace ~/Applications/Trasmuta.app
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd -P)
export PATH="/usr/local/share/dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1
case "$(uname -m)" in arm64) runtime=osx-arm64 ;; *) runtime=osx-x64 ;; esac
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Directory.Build.props")

# Assembled outside the repository: Documents may sync through iCloud Drive, whose file-provider attributes make
# codesign refuse the bundle.
out="${TMPDIR:-/tmp}/trasmuta-build"
app="$out/Trasmuta.app"
macos="$app/Contents/MacOS"
resources="$app/Contents/Resources"
rm -rf "$app"
mkdir -p "$macos" "$resources"

for project in Filee.App Filee.Cli; do
    dotnet publish "$root/src/$project/$project.csproj" -c Release -r "$runtime" --self-contained true \
        "-p:Version=$version" -p:FileeRelease=true -o "$macos" -nologo -v quiet
done
# The process name is what macOS shows in menus and uses for the Services port (NSPortName in Info.plist).
mv "$macos/Filee" "$macos/Trasmuta"
chmod +x "$macos/Trasmuta" "$macos/filee-cli"

filee_engines="$HOME/Applications/Filee.app/Contents/MacOS/engines"
[ -d "$filee_engines" ] || filee_engines="/Applications/Filee.app/Contents/MacOS/engines"
if [ -d "$filee_engines" ]; then
    /usr/bin/ditto "$filee_engines" "$macos/engines"
    echo "Engines copied from $filee_engines"
fi

sed -e "s/__VERSION__/$version/" -e "s/__BUNDLE_VERSION__/$version/" "$root/installer/macos/Info.plist" \
    > "$app/Contents/Info.plist"
plutil -lint "$app/Contents/Info.plist" >/dev/null
for localization in "$root"/installer/macos/*.lproj; do
    cp -R "$localization" "$resources/"
done

iconset="$out/trasmuta.iconset"
rm -rf "$iconset" && mkdir -p "$iconset"
for size in 16 32 128 256 512; do
    sips -z "$size" "$size" "$root/src/Filee.App/Assets/Icons/filee.png" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z "$double" "$double" "$root/src/Filee.App/Assets/Icons/filee.png" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$resources/trasmuta.icns"
rm -rf "$iconset"
cp "$root/LICENSE" "$root/THIRD-PARTY-NOTICES.md" "$resources/"

# Ad-hoc signature: every file in Contents/MacOS, deepest first, then the main executable and the bundle.
# Quarantine and Finder attributes left by downloads or copies make codesign refuse the bundle.
xattr -cr "$app"
entitlements="$root/installer/macos/entitlements.plist"
find "$macos" -type f ! -path "$macos/Trasmuta" | awk '{ print length, $0 }' | sort -rn | cut -d' ' -f2- |
    while IFS= read -r file; do codesign --force --sign - --entitlements "$entitlements" "$file" 2>/dev/null; done
codesign --force --sign - --entitlements "$entitlements" "$macos/Trasmuta"
codesign --force --sign - --entitlements "$entitlements" "$app"
codesign --verify --deep --strict "$app"
echo "Built $app ($version, $runtime)"

if [ "${1:-}" = "--install" ]; then
    osascript -e 'quit app id "com.trasmuta.app"' 2>/dev/null || true
    rm -rf "$HOME/Applications/Trasmuta.app"
    mkdir -p "$HOME/Applications"
    /usr/bin/ditto "$app" "$HOME/Applications/Trasmuta.app"
    echo "Installed $HOME/Applications/Trasmuta.app"
fi
