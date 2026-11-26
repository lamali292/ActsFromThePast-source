"""Makes an editable Spine project of the Darkling, to author its collapse and regrow animations in the Spine editor.

    python scripts/spine/make_darkling_project.py

Writes spine/darkling/darkling.spine and spine/darkling/images/ (Spine 4.2.11 project; open it with the 4.2.11 editor).
It holds everything the STS1 skeleton has (Idle, Attack, old_Attack, Hit) and the three animations the mod's animator
uses but no skeleton ever had. They are generated here, as a starting point to edit in the Spine editor:

    wither        0.5 s: from the normal look to the half dead look (then dead_loop)
    dead_loop     looping while it is half dead: the idle motion with the half dead look
    regenerate    0.5 s: back from the half dead look to the normal one (then Idle)

The half dead look is the body squashed to 80 % of its width and 60 % of its height (root bone scale), grayer (the body
slots get a two color tint: their dark color, which is where the near black art sits, is lifted to a dark gray) and
with dimmed eyes.

RUNNING THIS AGAIN OVERWRITES THE PROJECT, including anything edited in the Spine editor.

When you are done, export with scripts/spine/export_darkling.py, which writes the .skel, .atlas and .png into
ActsFromThePast/monsters/darkling. Do not export with "Dead": the animator triggers "Dead" itself and maps it to wither.
"""
import copy
import json
import os
import shutil
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import add_hit_animation as pipeline  # noqa: E402  (the shared Spine helpers and the STS1 -> 4.2.11 conversion)

REPO = pipeline.REPO
OUT = REPO / "spine" / "darkling"
SRC = pipeline.STS1 / "theForest" / "darkling"
NAME = "darkling"


SQUASH = (0.8, 0.6)          # width, height of the half dead Darkling, relative to the normal one
TRANSITION = 0.5             # seconds of wither and regenerate
BODY_SLOTS = ["body", "littleBody"]
EYE_SLOTS = ["eye1", "eye2", "eye3", "eye4"]
NORMAL = dict(light="ffffffff", dark="000000")
HALF_DEAD = dict(light="ffffffeb", dark="303030")   # the near black body turns gray; (light is the usual tint)
EYE_ALPHA = (1.0, 0.45)      # normal, half dead


def ease(t0, v0, t1, v1):
    """cubic-bezier(0.42, 0, 0.58, 1), as the four control values Spine's JSON wants."""
    d = t1 - t0
    return [t0 + 0.42 * d, v0, t0 + 0.58 * d, v1]


def transition(idle, to_half_dead):
    """The first idle pose held for TRANSITION seconds while the half dead look fades in (or out)."""
    a, b = (1.0, 1.0), SQUASH
    start, end = (a, b) if to_half_dead else (b, a)
    anim = pipeline.dead_animation(idle, TRANSITION)
    root = anim.setdefault("bones", {}).setdefault("root", {})
    root.pop("scale", None)
    root["scalex"] = [{"time": 0, "value": start[0], "curve": ease(0, start[0], TRANSITION, end[0])},
                      {"time": TRANSITION, "value": end[0]}]
    root["scaley"] = [{"time": 0, "value": start[1], "curve": ease(0, start[1], TRANSITION, end[1])},
                      {"time": TRANSITION, "value": end[1]}]
    look = (NORMAL, HALF_DEAD) if to_half_dead else (HALF_DEAD, NORMAL)
    alpha = EYE_ALPHA if to_half_dead else EYE_ALPHA[::-1]
    slots = anim.setdefault("slots", {})
    for name in BODY_SLOTS:
        slots.setdefault(name, {})["rgba2"] = [{"time": 0, **look[0]}, {"time": TRANSITION, **look[1]}]
    for name in EYE_SLOTS:
        slots.setdefault(name, {})["alpha"] = [{"time": 0, "value": alpha[0]}, {"time": TRANSITION, "value": alpha[1]}]
    return anim


def half_dead_loop(idle):
    """The idle motion with the half dead look: the root scale of every key is squashed and the slot colors held."""
    anim = copy.deepcopy(idle)
    root = anim.setdefault("bones", {}).setdefault("root", {})
    keys = root.get("scale") or [{"time": 0}]
    for key in keys:
        key["x"], key["y"] = key.get("x", 1) * SQUASH[0], key.get("y", 1) * SQUASH[1]
    root["scale"] = keys
    end = max(k["time"] for tl in anim["bones"].values() for ks in tl.values() for k in ks)
    slots = anim.setdefault("slots", {})
    for name in BODY_SLOTS:
        slots.setdefault(name, {})["rgba2"] = [{"time": 0, **HALF_DEAD}, {"time": end, **HALF_DEAD}]
    for name in EYE_SLOTS:
        slots.setdefault(name, {})["alpha"] = [{"time": 0, "value": EYE_ALPHA[1]}, {"time": end, "value": EYE_ALPHA[1]}]
    return anim


def add_half_dead_animations(skeleton):
    # a slot needs its dark color in the setup pose before an animation can tint with it
    for slot in skeleton["slots"]:
        if slot["name"] in BODY_SLOTS:
            slot["dark"] = NORMAL["dark"]
    animations = skeleton["animations"]
    idle = animations["Idle"]
    animations["wither"] = transition(idle, True)
    animations["dead_loop"] = half_dead_loop(idle)
    animations["regenerate"] = transition(idle, False)


def main():
    work = pipeline.SCRATCH / "darkling_project"
    shutil.rmtree(work, ignore_errors=True)
    work.mkdir(parents=True)

    # the STS1 skeleton in the shape the editor accepts
    prepared, original, _, _ = pipeline.convert(NAME, pipeline.SOURCES[NAME], work)
    skeleton = json.load(open(prepared, encoding="utf-8"))
    skeleton["skeleton"]["images"] = "./images/"
    add_half_dead_animations(skeleton)

    shutil.rmtree(OUT, ignore_errors=True)
    OUT.mkdir(parents=True)

    # the images of the atlas, one file per region
    code, out = pipeline.run([pipeline.SPINE, "-u", pipeline.SPINE_VERSION, "-i", SRC, "-o", OUT / "images",
                              "-c", SRC / "skeleton.atlas"])
    if "Complete." not in out:
        sys.exit("unpacking the atlas failed:\n" + out[-1500:])

    source = OUT / f"{NAME}.json"
    json.dump(skeleton, open(source, "w", encoding="utf-8"), indent=1)
    code, out = pipeline.run([pipeline.SPINE, "-u", pipeline.SPINE_VERSION, "-i", source, "-o", OUT / f"{NAME}.spine",
                              "-r", NAME])
    source.unlink()
    if "Complete." not in out or not (OUT / f"{NAME}.spine").exists():
        sys.exit("project import failed:\n" + out[-1500:])
    print("animations:", sorted(skeleton["animations"]))
    print("project:", OUT / f"{NAME}.spine")


main()
