#!/usr/bin/env python3
# Draws the background of the .dmg window (build-trasmuta-macos.sh --dmg): an arrow from where Trasmuta.app sits to
# where the Applications link sits, and the instruction under it. Writes installer/macos/dmg-background.png (660x400,
# the window's size in points) and dmg-background@2x.png for Retina screens. Needs Pillow and a TrueType font:
#
#   python3 build/tools/make-dmg-background.py [path/to/Sans.ttf [path/to/Sans-Bold.ttf]]
#
# The icon positions here must match those in build-trasmuta-macos.sh (APP_X, LINK_X, ICON_Y).

import math
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

WIDTH, HEIGHT = 660, 400
APP_X, LINK_X, ICON_Y = 170, 490, 185
REGULAR = sys.argv[1] if len(sys.argv) > 1 else "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"
BOLD = sys.argv[2] if len(sys.argv) > 2 else "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
BLUE = (11, 102, 240)
INK = (28, 36, 52)
MUTED = (96, 108, 128)


def draw(final: int) -> Image.Image:
    # Drawn four times larger and scaled down: Pillow's lines have no anti-aliasing of their own.
    scale = final * 4
    w, h = WIDTH * scale, HEIGHT * scale
    image = Image.new("RGB", (w, h))
    top, bottom = (250, 252, 255), (226, 236, 252)
    d = ImageDraw.Draw(image)
    for y in range(h):
        t = y / (h - 1)
        d.line([(0, y), (w, y)], fill=tuple(round(a + (b - a) * t) for a, b in zip(top, bottom)))

    # A gentle arc between the two icons, ending in an arrow head pointing at Applications.
    left, right = (APP_X + 72) * scale, (LINK_X - 84) * scale
    base, lift = (ICON_Y - 8) * scale, 34 * scale
    points = []
    for i in range(61):
        t = i / 60
        x = left + (right - left) * t
        y = base - lift * math.sin(math.pi * t)
        points.append((x, y))
    d.line(points, fill=BLUE, width=6 * scale, joint="curve")
    (x1, y1), (x2, y2) = points[-4], points[-1]
    angle = math.atan2(y2 - y1, x2 - x1)
    size = 20 * scale
    tip = (x2 + math.cos(angle) * 6 * scale, y2 + math.sin(angle) * 6 * scale)
    wing = math.radians(28)
    d.polygon([
        tip,
        (tip[0] - size * math.cos(angle - wing), tip[1] - size * math.sin(angle - wing)),
        (tip[0] - size * math.cos(angle + wing), tip[1] - size * math.sin(angle + wing)),
    ], fill=BLUE)

    title = ImageFont.truetype(BOLD, 21 * scale)
    subtitle = ImageFont.truetype(REGULAR, 14 * scale)
    d.text((w / 2, 300 * scale), "Trascina Trasmuta nella cartella Applicazioni", font=title, fill=INK, anchor="mm")
    d.text((w / 2, 330 * scale), "Drag Trasmuta to the Applications folder", font=subtitle, fill=MUTED, anchor="mm")
    return image.resize((WIDTH * final, HEIGHT * final), Image.Resampling.LANCZOS)


if __name__ == "__main__":
    out = Path(__file__).resolve().parents[2] / "installer" / "macos"
    draw(1).save(out / "dmg-background.png", optimize=True)
    draw(2).save(out / "dmg-background@2x.png", optimize=True)
    print(f"Wrote {out / 'dmg-background.png'} and @2x")
