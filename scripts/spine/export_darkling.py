"""Exports spine/darkling/darkling.spine into the mod: ActsFromThePast/monsters/darkling/darkling.{skel,atlas,png}.

    python scripts/spine/export_darkling.py

Uses spine/export.json (binary export, atlas packed like the other monsters: non-premultiplied, old atlas format) and the
4.2.11 editor, headless. Refuses to write if the animations the animator needs are missing. Re-export the mod in Godot
afterwards so it imports the new files.
"""
import json
import os
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
SPINE = os.environ.get("SPINE_CLI", r"C:\Program Files\Spine\Spine.com")
# the editor that wrote the project must read it: 4.2.11 made it, but saving it in a newer editor upgrades the file
VERSIONS = ("4.2.11", "4.2.39")
PROJECT = REPO / "spine" / "darkling" / "darkling.spine"
SETTINGS = REPO / "spine" / "export.json"
TARGET = REPO / "ActsFromThePast" / "monsters" / "darkling"
# what Darkling.GenerateAnimator refers to
NEEDED = {"Idle", "Attack", "Hit", "wither", "dead_loop", "regenerate"}


def run(args):
    p = subprocess.run([str(a) for a in args], capture_output=True, text=True, encoding="utf-8", errors="replace")
    return (p.stdout or "") + (p.stderr or "")


def main():
    out = Path(tempfile.mkdtemp(prefix="aftp_darkling_"))
    for version in VERSIONS:
        log = run([SPINE, "-u", version, "-i", PROJECT, "-o", out, "-e", SETTINGS])
        if "Complete." in log and (out / "darkling.skel").exists():
            break
    else:
        sys.exit("export failed:\n" + log[-1500:])
    print("exported with Spine", version)

    log = run([SPINE, "-u", version, "-i", PROJECT, "-o", out / "json", "-e", "json"])
    animations = set(json.load(open(out / "json" / "darkling.json", encoding="utf-8"))["animations"])
    if NEEDED - animations:
        sys.exit(f"the project lacks animations the animator uses: {sorted(NEEDED - animations)}")

    for name in ("darkling.skel", "darkling.atlas", "darkling.png"):
        shutil.copy2(out / name, TARGET / name)
    shutil.rmtree(out, ignore_errors=True)
    print("exported:", ", ".join(sorted(animations)))


main()
