#!/bin/sh
set -eu

data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
install_dir="$data_dir/trasmuta/application"
desktop_file="$data_dir/applications/com.trasmuta.app.desktop"
python3 - "$data_dir" "$install_dir" <<'PY'
import pathlib
import sys

root, target = map(pathlib.Path, sys.argv[1:])
expected = root.resolve() / 'trasmuta' / 'application'
if target.is_symlink() or target.resolve() != expected:
    raise SystemExit('Refusing to remove an installation outside its expected directory.')
if (target / '.trasmuta-package').read_text(encoding='utf-8').strip() != 'com.trasmuta.app':
    raise SystemExit('The directory is not an installation created by Trasmuta.')
PY
"$install_dir/Trasmuta" --uninstall-cleanup
if [ -L "$HOME/.local/bin/trasmuta" ] && [ "$(readlink "$HOME/.local/bin/trasmuta")" = "$install_dir/filee-cli" ]; then
    rm -- "$HOME/.local/bin/trasmuta"
fi
if [ -f "$desktop_file" ] && grep -q '^X-Trasmuta-Managed=true$' "$desktop_file"; then
    rm -- "$desktop_file"
fi
rm -rf -- "$install_dir"
printf '%s\n' 'Trasmuta was removed. Your settings and conversion results were kept.'
