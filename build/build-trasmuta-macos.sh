#!/bin/sh
# Builds Trasmuta.app (Apple Silicon or Intel) and, with --install, copies it to ~/Applications; with --zip, packs
# it as Trasmuta-<version>-<runtime>.zip for a release. A shell version of the macOS part of build-portable.ps1.
#
#   build/build-trasmuta-macos.sh            build into $TMPDIR/trasmuta-build/Trasmuta.app
#   build/build-trasmuta-macos.sh --install  build, then replace ~/Applications/Trasmuta.app
#   build/build-trasmuta-macos.sh --zip      build, then pack the zip next to it (what the Release workflow does)
#
# Optional settings (the Release workflow sets them):
#   TRASMUTA_RUNTIME  osx-arm64 or osx-x64 (default: this Mac's)
#   TRASMUTA_ENGINES  folder with the bundled engines (7zip, rhwp), as fetch-portable-engines.ps1 makes it.
#                     Default: those of an installed Filee.app, else none (the app offers downloads instead).
#   TRASMUTA_OUT      where to build (default: $TMPDIR/trasmuta-build)
#   TRASMUTA_VERSION  version to stamp instead of Directory.Build.props' (to try an update from an older one)
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd -P)
export PATH="/usr/local/share/dotnet:$PATH" DOTNET_CLI_TELEMETRY_OPTOUT=1
case "$(uname -m)" in arm64) runtime=osx-arm64 ;; *) runtime=osx-x64 ;; esac
runtime="${TRASMUTA_RUNTIME:-$runtime}"
version=$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$root/Directory.Build.props")
version="${TRASMUTA_VERSION:-$version}"

# Assembled outside the repository: Documents may sync through iCloud Drive, whose file-provider attributes make
# codesign refuse the bundle.
out="${TRASMUTA_OUT:-${TMPDIR:-/tmp}/trasmuta-build}"
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

engines="${TRASMUTA_ENGINES:-}"
if [ -z "$engines" ]; then
    engines="$HOME/Applications/Filee.app/Contents/MacOS/engines"
    [ -d "$engines" ] || engines="/Applications/Filee.app/Contents/MacOS/engines"
fi
if [ -d "$engines" ]; then
    /usr/bin/ditto "$engines" "$macos/engines"
    echo "Engines copied from $engines"
elif [ -n "${TRASMUTA_ENGINES:-}" ]; then
    echo "TRASMUTA_ENGINES is not a folder: $TRASMUTA_ENGINES" >&2
    exit 1
fi

sed -e "s/__VERSION__/$version/" -e "s/__BUNDLE_VERSION__/$version/" "$root/installer/macos/Info.plist" \
    > "$app/Contents/Info.plist"
plutil -lint "$app/Contents/Info.plist" >/dev/null
for localization in "$root"/installer/macos/*.lproj; do
    cp -R "$localization" "$resources/"
done

iconset="$out/trasmuta.iconset"
rm -rf "$iconset" && mkdir -p "$iconset"
# From the 1024-pixel master, so the Retina sizes stay sharp.
master="$root/installer/macos/AppIcon-1024.png"
for size in 16 32 128 256 512; do
    sips -z "$size" "$size" "$master" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z "$double" "$double" "$master" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
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
# An ad-hoc signature is identified by its exact hash, which changes with every build: macOS would forget the
# Accessibility permission at each update. Requiring only the bundle identifier keeps it across builds.
requirement='designated => identifier "com.trasmuta.app"'
codesign --force --sign - --entitlements "$entitlements" -r="$requirement" "$macos/Trasmuta"
codesign --force --sign - --entitlements "$entitlements" -r="$requirement" "$app"
codesign --verify --deep --strict "$app"
echo "Built $app ($version, $runtime)"

if [ "${1:-}" = "--zip" ]; then
    zip="$out/Trasmuta-$version-$runtime.zip"
    rm -f "$zip"
    # ditto keeps the signature (stored in extended attributes for the assemblies) and the executable bits.
    /usr/bin/ditto -c -k --keepParent "$app" "$zip"
    echo "Packed $zip"
fi

if [ "${1:-}" = "--install" ]; then
    osascript -e 'quit app id "com.trasmuta.app"' 2>/dev/null || true
    rm -rf "$HOME/Applications/Trasmuta.app"
    mkdir -p "$HOME/Applications"
    /usr/bin/ditto "$app" "$HOME/Applications/Trasmuta.app"
    echo "Installed $HOME/Applications/Trasmuta.app"
fi
