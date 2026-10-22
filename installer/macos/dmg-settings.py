# dmgbuild settings for Trasmuta-<version>-<runtime>.dmg (build/build-trasmuta-macos.sh --dmg): a window with the app,
# a link to Applications and an arrow between them (dmg-background.png, made by build/tools/make-dmg-background.py,
# whose icon positions match icon_locations below). dmgbuild writes the window's layout itself, without asking
# Finder, so it also works on a build server without a logged-in desktop.
#
#   dmgbuild -s installer/macos/dmg-settings.py -D app=…/Trasmuta.app -D background=…/background.tiff Trasmuta out.dmg

import os.path

app = defines["app"]  # noqa: F821 (dmgbuild provides it)
app_name = os.path.basename(app)

files = [app]
symlinks = {"Applications": "/Applications"}
icon = os.path.join(app, "Contents", "Resources", "trasmuta.icns")

format = "UDZO"
filesystem = "HFS+"

background = defines["background"]  # noqa: F821
window_rect = ((200, 140), (660, 400))
default_view = "icon-view"
show_status_bar = False
show_tab_view = False
show_toolbar = False
show_pathbar = False
show_sidebar = False
show_icon_preview = False
arrange_by = None
icon_size = 128
text_size = 13
icon_locations = {
    app_name: (170, 185),
    "Applications": (490, 185),
}
