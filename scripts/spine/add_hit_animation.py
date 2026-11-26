"""Adds "Hit" and "Dead" animations to the legacy monster skeletons that lack them, and rebuilds their .skel.

    python scripts/spine/add_hit_animation.py cultist looter          # dry run into the scratch dir
    python scripts/spine/add_hit_animation.py --apply cultist looter  # also replaces ActsFromThePast/monsters/<dir>/<name>.skel
    python scripts/spine/add_hit_animation.py --apply --all

Why: the original game has no hit animation for these monsters, it shoves the sprite back and forth (STS1's stagger).
`Hit` is that shove as a root-bone animation: 20 px (on screen) away from the player, back to 0 in 0.3 s with a
quadratic ease-in. With it the monster's animator handles the "Hit" trigger like any other animation.
The animator plays every state on track 0, so a root-only animation would drop all other bones to their setup pose for
its duration. `Hit` therefore also contains the monster's idle cycle: it plays one idle loop with the shove on top, and
the idle state that follows loops on from there. Monsters whose animator switches between several idle states (the lice
curl up, Slaver Red loses its net, the Guardian goes defensive) get one hit animation per idle state, so a hit does
not undo the state (HitClosed, HitNoNet, HitDefensive); the C# animator wires them up with branches.

`Dead` is the death animation every vanilla monster has and the legacy ones never got. It is the idle pose held for 0.1 s:
the game starts the death effect when the current animation has run out (plus 0.5 s), and without a Dead animation that
is the rest of the idle loop. A pose held from the first idle frame, not an empty animation, because the animator replaces
the idle animation on track 0 and everything the new animation does not key would snap to the setup pose.

Pipeline (all headless): STS1 skeleton JSON (Spine 3.4.02) -> json_converter.py (3.x -> 4.2.11 JSON) -> add Hit ->
Spine 4.2.11 imports the JSON into a project -> Spine 4.2.11 exports binary. Needs Spine's 4.2.11 update to be cached in
~/Spine/updates (the launcher fetches it once) and the STS1 data in sts2mods/desktop-1.0.

The Spine editor refuses slot names that contain a path ("images/armleft": "Slot not found"), so "/" becomes "_" in slot
names (and the stray leading character a few STS1 slot names have is dropped). Attachment and region names keep theirs.
"""
import argparse
import copy
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
STS2MODS = REPO.parent
SPINE = os.environ.get("SPINE_CLI", r"C:\Program Files\Spine\Spine.com")
SPINE_VERSION = "4.2.11"
CONVERTER = STS2MODS / "testspine" / "json_converter.py"
STS1 = STS2MODS / "desktop-1.0" / "images" / "monsters"
MONSTERS = REPO / "ActsFromThePast" / "monsters"
SCRATCH = Path(os.environ.get("AFTP_SPINE_SCRATCH", tempfile.gettempdir())) / "aftp_spine"

STAGGER_SECONDS = 0.3
STAGGER_PIXELS = 20.0


def one(src, idle, **kw):
    """src: STS1 folder (below images/monsters). idle: the idle animation the Hit animation is built on."""
    return dict(src=src, hits=[("Hit", idle)], **kw)


# skeleton name (the .skel file name) -> how to build its Hit animation(s). The STS1 folder was matched by comparing
# bone, slot, animation and attachment names with the shipped .skel.
# Not here: Bronze Orb (a plain sprite, no skeleton) and the monsters that already have a Hit animation.
SOURCES = {
    "cultist": one("theBottom/cultist", "waving"),
    "gremlin_fat": one("theBottom/fatGremlin", "animation"),
    "gremlin_mad": one("theBottom/angryGremlin", "idle"),
    "gremlin_shield": one("theBottom/femaleGremlin", "idle"),
    "gremlin_sneaky": one("theBottom/thiefGremlin", "animation"),
    "gremlin_wizard": one("theBottom/wizardGremlin", "animation"),
    "gremlin_nob": one("theBottom/nobGremlin", "animation"),
    "looter": one("theBottom/looter", "idle"),
    "slaver_blue": one("theBottom/blueSlaver", "idle"),
    "mugger": one("theCity/looterAlt", "idle"),
    "taskmaster": one("theCity/slaverMaster", "idle"),
    "torch_head": one("theCity/torchHead", "idle"),
    "collector": one("theCity/collector", "idle"),
    "bronze_automaton": one("theCity/automaton", "idle"),
    "exploder": one("theForest/exploder", "idle"),
    "maw": one("theForest/maw", "idle"),
    "repulsor": one("theForest/repulser", "idle"),
    # jawWorm and jawWormAlt differ only in the triangulation of their meshes; same art.
    "jaw_worm": one("theBottom/jawWorm", "idle"),
    # the animation state of the head runs at half speed, so half the duration gives the same 0.3 s on screen
    "giant_head": one("theForest/head", "idle_open", seconds=STAGGER_SECONDS / 2),
    "byrd_flying": one("theCity/byrd", "idle_flap", dir="byrd", src_file="flying.json"),
    "byrd_grounded": one("theCity/byrd", "idle", dir="byrd", src_file="grounded.json"),
    # animators with several idle states: one hit animation each
    "louse_green": dict(src="theBottom/louseGreen", hits=[("Hit", "idle"), ("HitClosed", "idle closed")]),
    "louse_red": dict(src="theBottom/louseRed", hits=[("Hit", "idle"), ("HitClosed", "idle closed")]),
    "slaver_red": dict(src="theBottom/redSlaver", hits=[("Hit", "idle"), ("HitNoNet", "idleNoNet")]),
    "guardian": dict(src="theBottom/boss/guardian", hits=[("Hit", "idle"), ("HitDefensive", "defensive")]),
}


def dead(src, idle, **kw):
    """A monster that already has hit animations and only needs Dead."""
    return dict(src=src, hits=[], idle=idle, **kw)


SOURCES.update({
    "acid_slime_large": dead("theBottom/slimeL", "Idle"),
    "acid_slime_medium": dead("theBottom/slimeM", "idle"),
    "acid_slime_small": dead("theBottom/slimeS", "idle"),
    "fungi_beast": dead("theBottom/fungi", "Idle"),
    "lagavulin": dead("theBottom/lagavulin", "Idle_1"),
    "sentry": dead("theBottom/sentry", "idle"),
    "slime_boss": dead("theBottom/boss/slime", "idle"),
    "spike_slime_large": dead("theBottom/slimeAltL", "idle"),
    "spike_slime_medium": dead("theBottom/slimeAltM", "idle"),
    "spike_slime_small": dead("theBottom/slimeAltS", "idle"),
    "awakened_one": dead("theForest/awakenedOne", "Idle_1"),
    "deca": dead("theForest/deca", "Idle"),
    "donu": dead("theForest/donu", "Idle"),
    "nemesis": dead("theForest/nemesis", "Idle"),
    "orb_walker": dead("theForest/orbWalker", "Idle"),
    "reptomancer": dead("theForest/mage", "Idle"),
    "dagger": dead("theForest/mage_dagger", "Idle"),
    "spiker": dead("theForest/spiker", "idle"),
    "spire_growth": dead("theForest/spireGrowth", "Idle"),
    "time_eater": dead("theForest/timeEater", "Idle"),
    "transient": dead("theForest/transient", "Idle"),
    "writhing_mass": dead("theForest/spaghetti", "Idle"),
    "bear": dead("theCity/bear", "Idle"),
    "book_of_stabbing": dead("theCity/stabBook", "Idle"),
    "centurion": dead("theCity/tank", "Idle"),
    "champ": dead("theCity/champ", "Idle"),
    "chosen": dead("theCity/chosen", "Idle"),
    "gremlin_leader": dead("theCity/gremlinleader", "Idle"),
    "mystic": dead("theCity/healer", "Idle"),
    "pointy": dead("theCity/pointy", "Idle"),
    "romeo": dead("theCity/romeo", "Idle"),
    "shelled_parasite": dead("theCity/shellMonster", "Idle"),
    "snake_plant": dead("theCity/snakePlant", "Idle"),
    "snecko": dead("theCity/reptile", "Idle"),
    "spheric_guardian": dead("theCity/sphere", "Idle"),
})
# Hexaghost has no skeleton and Bronze Orb is made by make_bronze_orb.py. The Darkling's animator wants a wither, a
# dead_loop and a regenerate animation (and triggers "Dead" itself), none of which the STS1 skeleton has: held poses.
SOURCES["darkling"] = dead("theForest/darkling", "Idle", no_dead=True, holds=["wither", "dead_loop", "regenerate"])


def run(cmd, **kw):
    p = subprocess.run([str(c) for c in cmd], capture_output=True, text=True, encoding="utf-8", errors="replace", **kw)
    return p.returncode, (p.stdout or "") + (p.stderr or "")


def strip(name):
    r"""The slot name the editor accepts. It refuses names with a path in them ("images/armleft": "Slot not found"), and
    a few STS1 slot names have a stray leading character (a backslash or a quote). The "/" becomes "_"
    instead of being cut off, because the data has pairs like "images/antleft" and "antleft" that must stay apart."""
    return re.sub(r"[\\/]", "_", re.sub(r"^[^A-Za-z0-9]*", "", name))


def strip_slot_names(j):
    for slot in j["slots"]:
        slot["name"] = strip(slot["name"])
    for skin in j["skins"]:
        skin["attachments"] = {strip(k): v for k, v in skin["attachments"].items()}
    for anim in j["animations"].values():
        if "slots" in anim:
            anim["slots"] = {strip(k): v for k, v in anim["slots"].items()}
        for key in ("attachments", "deform"):  # per-attachment timelines: skin -> slot -> attachment
            for skin_name, per_slot in anim.get(key, {}).items():
                anim[key][skin_name] = {strip(k): v for k, v in per_slot.items()}
        for entry in anim.get("drawOrder", []):
            for offset in entry.get("offsets", []):
                offset["slot"] = strip(offset["slot"])
    return j


def sprite_scale(directory, stem):
    """The SpineSprite's scale in the monster's scene: skeleton units * scale = pixels on screen."""
    text = (MONSTERS / directory / f"{stem}.tscn").read_text(encoding="utf-8")
    found = re.search(r'\[node name="Visuals" type="SpineSprite".*?\nscale = Vector2\(([\d.]+),', text, re.S)
    return float(found.group(1)) if found else 1.0


def hit_animation(idle, units, seconds):
    """The idle animation plus a root shove right by `units` that returns to 0: x(t) = units * (1 - (t / T)^2), which a
    bezier with control points (T/3, units) and (2T/3, units * 2/3) reproduces exactly."""
    hit = copy.deepcopy(idle)
    root = hit.setdefault("bones", {}).setdefault("root", {})
    translate = root.get("translate")
    if translate is not None and all(k.get("x", 0) == 0 and k.get("y", 0) == 0 for k in translate):
        del root["translate"]  # a root translation that never moves anything (Bronze Automaton)
    if "translate" in root or "translatex" in root:
        raise RuntimeError("the idle animation really moves the root bone")
    root["translatex"] = [
        {"time": 0, "value": units, "curve": [seconds / 3, units, seconds * 2 / 3, units * 2 / 3]},
        {"time": seconds, "value": 0},
    ]
    return hit


def dead_animation(idle, seconds=0.1):
    """The first pose of the idle animation, held. Every timeline keeps its first key (moved to time 0, without a curve)
    and gets a copy of it at `seconds`; events are dropped so nothing plays a sound."""
    def hold(node):
        if isinstance(node, list):
            if node and all(isinstance(k, dict) and "time" in k for k in node):
                first = {k: v for k, v in node[0].items() if k != "curve"}
                first["time"] = 0
                return [first, {**first, "time": seconds}]
            return [hold(x) for x in node]
        if isinstance(node, dict):
            return {k: hold(v) for k, v in node.items() if k != "events"}
        return node
    return hold(copy.deepcopy(idle))


def convert(stem, spec, work):
    directory = spec.get("dir", stem)
    src = STS1 / spec["src"] / spec.get("src_file", "skeleton.json")
    converted = work / "converted.json"
    code, out = run([sys.executable, CONVERTER, src, converted])
    if code != 0 or not converted.exists():
        raise RuntimeError("json_converter failed:\n" + out)
    j = json.load(open(converted, encoding="utf-8"))
    original_animations = set(j["animations"])
    strip_slot_names(j)
    units = round(STAGGER_PIXELS / sprite_scale(directory, stem), 3)
    for hit_name, idle_name in spec["hits"]:
        if hit_name in original_animations:
            raise RuntimeError(f"already has a {hit_name} animation")
        if idle_name not in j["animations"]:
            raise RuntimeError(f"no idle animation {idle_name!r}")
        j["animations"][hit_name] = hit_animation(j["animations"][idle_name], units, spec.get("seconds", STAGGER_SECONDS))
    idle_for_dead = spec.get("dead_idle") or (spec["hits"][0][1] if spec["hits"] else spec["idle"])
    held = ["Dead"] if not spec.get("no_dead") else []
    held += spec.get("holds", [])  # animations the monster's animator already uses but the skeleton never had
    for name in held:
        if name in original_animations:
            raise RuntimeError(f"already has a {name} animation")
        j["animations"][name] = dead_animation(j["animations"][idle_for_dead])
    prepared = work / f"{stem}.json"
    json.dump(j, open(prepared, "w", encoding="utf-8"), indent=1)
    return prepared, original_animations, len(j["bones"]), len(j["slots"])


def build(stem, spec, apply):
    directory = spec.get("dir", stem)
    work = SCRATCH / stem
    shutil.rmtree(work, ignore_errors=True)
    work.mkdir(parents=True)
    prepared, original, bone_count, slot_count = convert(stem, spec, work)

    project = work / f"{stem}.spine"
    code, out = run([SPINE, "-u", SPINE_VERSION, "-i", prepared, "-o", project, "-r", stem])
    if "Complete." not in out or not project.exists():
        raise RuntimeError("Spine import failed:\n" + out[-1500:])

    for kind in ("binary", "json"):
        code, out = run([SPINE, "-u", SPINE_VERSION, "-i", project, "-o", work / kind, "-e", kind])
        if "Complete." not in out:
            raise RuntimeError(f"Spine {kind} export failed:\n" + out[-1500:])

    expected = original | {hit for hit, _ in spec["hits"]} | ({"Dead"} if not spec.get("no_dead") else set())         | set(spec.get("holds", []))
    exported = json.load(open(work / "json" / f"{stem}.json", encoding="utf-8"))
    if set(exported["animations"]) != expected:
        raise RuntimeError(f"animations differ: {sorted(exported['animations'])} vs {sorted(expected)}")
    if len(exported["slots"]) != slot_count:
        raise RuntimeError(f"slot count differs: {len(exported['slots'])} vs {slot_count}")
    if len(exported["bones"]) != bone_count:
        raise RuntimeError(f"bone count differs: {len(exported['bones'])} vs {bone_count}")

    skel = work / "binary" / f"{stem}.skel"
    shipped = MONSTERS / directory / f"{stem}.skel"
    shipped_bytes = shipped.read_bytes()
    missing = [a for a in original if a.encode() not in shipped_bytes]
    if missing:
        raise RuntimeError(f"the shipped skeleton lacks animations of the STS1 source: {missing}")
    if apply:
        shutil.copy2(skel, shipped)
    return sorted(original), skel.stat().st_size, len(shipped_bytes)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("monsters", nargs="*")
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--apply", action="store_true", help="replace the shipped .skel files")
    args = ap.parse_args()
    names = list(SOURCES) if args.all else args.monsters
    if not names:
        ap.error("name monsters or pass --all")
    failed = 0
    for name in names:
        try:
            animations, new_size, old_size = build(name, SOURCES[name], args.apply)
            hits = [h for h, _ in SOURCES[name]["hits"]] + (["Dead"] if not SOURCES[name].get("no_dead") else [])                 + SOURCES[name].get("holds", [])
            print(f"ok   {name:18} {animations} + {hits}   .skel {old_size} -> {new_size} bytes"
                  + ("  (applied)" if args.apply else ""), flush=True)
        except Exception as e:  # keep going: one broken monster should not stop the rest
            failed += 1
            print(f"FAIL {name:18} {str(e).splitlines()[0] if str(e) else e!r}", flush=True)
    sys.exit(1 if failed else 0)


if __name__ == "__main__":
    main()
