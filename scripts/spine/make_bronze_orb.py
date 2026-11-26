"""Makes the Bronze Orb a Spine skeleton: one bone, one slot, one image, an idle, a Hit and a Dead animation.

    python scripts/spine/make_bronze_orb.py

The orb used to be a plain Sprite2D (with a hidden dummy SpineSprite so the scene would load), which cannot play
animations. The skeleton puts the same 145 x 145 image where the sprite was (centred 85 px above the creature, scale
1.1 in the scene), bobbing the way BronzeOrb.StartBobAnimation used to do in code, and gives it the same Hit shove the
other monsters have (see add_hit_animation.py). Even and odd orbs bob in opposite phase: idle / Hit and idle_odd / HitOdd.

Writes ActsFromThePast/monsters/bronze_orb/bronze_orb.skel and bronze_orb.atlas. Needs the cached Spine 4.2.11 update.
"""
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
SPINE = os.environ.get("SPINE_CLI", r"C:\Program Files\Spine\Spine.com")
SPINE_VERSION = "4.2.11"
DIR = REPO / "ActsFromThePast" / "monsters" / "bronze_orb"
SCRATCH = Path(os.environ.get("AFTP_SPINE_SCRATCH", tempfile.gettempdir())) / "aftp_spine" / "bronze_orb"

SPRITE_SCALE = 1.1          # scale of the SpineSprite in bronze_orb.tscn, same as the old Sprite2D
CENTER_HEIGHT = 85.0        # the old Sprite2D sat at (0, -85)
STAGGER_PIXELS = 20.0
STAGGER_SECONDS = 0.3
BOB_PIXELS = 6.0            # the bob moved +-6 px ...
BOB_SECONDS = 2.16          # ... with this period (360 * 6 ms), each half a sine in-out ease


def run(cmd):
    p = subprocess.run([str(c) for c in cmd], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return (p.stdout or "") + (p.stderr or "")


def main():
    width, height = Image.open(DIR / "bronze_orb.png").size
    units = round(STAGGER_PIXELS / SPRITE_SCALE, 3)
    t = STAGGER_SECONDS
    amplitude = round(BOB_PIXELS / SPRITE_SCALE, 3)
    half = BOB_SECONDS / 2

    def bob(first):
        """Vertical bob that starts at `first` (-amplitude or +amplitude; y is up in Spine), as sine in-out legs:
        cubic-bezier(0.37, 0, 0.63, 1), the usual approximation of a sine ease."""
        a, b = first, -first
        def leg(t0, v0, v1):
            return {"time": t0, "value": v0, "curve": [t0 + 0.37 * half, v0, t0 + 0.63 * half, v1]}
        return [leg(0, a, b), leg(half, b, a), {"time": BOB_SECONDS, "value": a}]

    def shove():
        return [{"time": 0, "value": units, "curve": [t / 3, units, t * 2 / 3, units * 2 / 3]},
                {"time": t, "value": 0}]

    skeleton = {
        "skeleton": {"spine": SPINE_VERSION, "x": -width / 2, "y": 0, "width": width, "height": height + CENTER_HEIGHT},
        "bones": [{"name": "root"}, {"name": "orb", "parent": "root", "y": round(CENTER_HEIGHT / SPRITE_SCALE, 3)}],
        "slots": [{"name": "orb", "bone": "orb", "attachment": "bronze_orb"}],
        "skins": [{"name": "default", "attachments": {"orb": {"bronze_orb": {"width": width, "height": height}}}}],
        "animations": {
            # even orbs rise first, odd orbs sink first. Hit is the bob plus the shove, so a hit does not interrupt it.
            "idle": {"bones": {"root": {"translatey": bob(-amplitude)}}},
            "idle_odd": {"bones": {"root": {"translatey": bob(amplitude)}}},
            "Hit": {"bones": {"root": {"translatey": bob(-amplitude), "translatex": shove()}}},
            "HitOdd": {"bones": {"root": {"translatey": bob(amplitude), "translatex": shove()}}},
            # the death animation every monster has: the first pose of the idle, held for 0.1 s
            "Dead": {"bones": {"root": {"translatey": [{"time": 0, "value": -amplitude}, {"time": 0.1, "value": -amplitude}]}}},
            "DeadOdd": {"bones": {"root": {"translatey": [{"time": 0, "value": amplitude}, {"time": 0.1, "value": amplitude}]}}},
        },
    }

    shutil.rmtree(SCRATCH, ignore_errors=True)
    SCRATCH.mkdir(parents=True)
    source = SCRATCH / "bronze_orb.json"
    json.dump(skeleton, open(source, "w", encoding="utf-8"), indent=1)

    out = run([SPINE, "-u", SPINE_VERSION, "-i", source, "-o", SCRATCH / "bronze_orb.spine", "-r", "bronze_orb"])
    if "Complete." not in out:
        sys.exit("Spine import failed:\n" + out[-1500:])
    out = run([SPINE, "-u", SPINE_VERSION, "-i", SCRATCH / "bronze_orb.spine", "-o", SCRATCH / "out", "-e", "binary"])
    if "Complete." not in out:
        sys.exit("Spine export failed:\n" + out[-1500:])
    shutil.copy2(SCRATCH / "out" / "bronze_orb.skel", DIR / "bronze_orb.skel")

    # one page, one region: the whole image
    (DIR / "bronze_orb.atlas").write_text(
        "bronze_orb.png\n"
        f"size: {width},{height}\n"
        "format: RGBA8888\n"
        "filter: Linear,Linear\n"
        "repeat: none\n"
        "bronze_orb\n"
        "  rotate: false\n"
        "  xy: 0, 0\n"
        f"  size: {width}, {height}\n"
        f"  orig: {width}, {height}\n"
        "  offset: 0, 0\n"
        "  index: -1\n", encoding="utf-8", newline="\n")
    print(f"bronze_orb.skel ({(DIR / 'bronze_orb.skel').stat().st_size} bytes) and bronze_orb.atlas written")


main()
