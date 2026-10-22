#!/bin/sh
set -eu

source_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)
data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
install_dir="$data_dir/trasmuta/application"
bin_dir="$HOME/.local/bin"
desktop_file="$data_dir/applications/com.trasmuta.app.desktop"
if [ -e "$install_dir" ] || [ -L "$install_dir" ]; then
    printf '%s\n' "An installation already exists at $install_dir. Run its uninstall.sh first."
    exit 1
fi
if [ -e "$bin_dir/trasmuta" ] || [ -L "$bin_dir/trasmuta" ]; then
    printf '%s\n' "$bin_dir/trasmuta already exists. Remove or rename it before installing Trasmuta."
    exit 1
fi
if [ -e "$desktop_file" ] || [ -L "$desktop_file" ]; then
    printf '%s\n' "$desktop_file already exists. Remove or rename it before installing Trasmuta."
    exit 1
fi
command -v python3 >/dev/null
test -x "$source_dir/Trasmuta"
test -x "$source_dir/filee-cli"
mkdir -p "$data_dir/trasmuta" "$bin_dir" "$data_dir/applications"
cp -R "$source_dir" "$install_dir"
printf '%s\n' 'com.trasmuta.app' > "$install_dir/.trasmuta-package"
ln -s "$install_dir/filee-cli" "$bin_dir/trasmuta"
python3 - "$install_dir" "$desktop_file" <<'PY'
import pathlib
import sys

app, desktop = map(pathlib.Path, sys.argv[1:])
def quoted_exec(value):
    value = value.replace('%', '%%')
    for character in ('\\', '"', '`', '$'):
        value = value.replace(character, '\\' + character)
    return '"' + value.replace('\\', '\\\\').replace('\n', '\\n').replace('\r', '\\r') + '"'

def desktop_value(value):
    return str(value).replace('\\', '\\\\').replace('\n', '\\n').replace('\r', '\\r')

desktop.write_text(
    '[Desktop Entry]\nType=Application\nName=Trasmuta\n'
    'Comment=Convert files with Trasmuta\n'
    f'Exec={quoted_exec(str(app / "Trasmuta"))} --convert %F\n'
    f'Icon={desktop_value(app / "trasmuta.png")}\n'
    'Terminal=false\nCategories=Utility;\nX-Trasmuta-Managed=true\n', encoding='utf-8')
PY
if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$data_dir/applications"
fi
printf '%s\n' "Installed Trasmuta at $install_dir" "CLI: $bin_dir/trasmuta (add $bin_dir to PATH if needed)."
