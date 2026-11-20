#!/usr/bin/env python3
"""Upgrade the optional Pokémon 1.4.0 ZIP with a reviewed neighborhood bundle.

Usage: python3 package_neighborhood.py OLD_1_4_ZIP BUILT_BUNDLE OUTPUT_ZIP
Requires OpenBonk 0.1.18-beta or newer for terraced elevation (content API 4). Existing output files
are never overwritten. The original ZIP and built bundle are read only.
"""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import tempfile
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo


FILE_LIMIT = 64 * 1024 * 1024
TOTAL_LIMIT = 128 * 1024 * 1024
BUNDLE_PATH = "assets/nimbo-props"
FIXED_TIMESTAMP = (2020, 1, 1, 0, 0, 0)
SPECIES = {"Beedrill", "Pidgey", "Rattata", "Raticate", "Fearow", "Arbok",
           "Nidoking", "Charizard", "Rhydon", "Gyarados"}
PROFILES = {
    1: (26, 6, .95, ("VendingCorner", "GachaCorner", "RamenFront", "PosterGarden")),
    2: (28, 7, .90, ("VendingCorner", "GachaCorner", "RamenFront", "LanternGate", "PosterGarden")),
    3: (30, 8, .85, ("RamenFront", "LanternGate", "PosterGarden", "ShrineCorner")),
}


def safe_path(name):
    return bool(name) and not name.startswith("/") and "\\" not in name and ":" not in name and \
        all(part not in ("", ".", "..") for part in name.split("/")) and \
        str(PurePosixPath(name)) == name


def inventory(payloads):
    return [{"path": name, "size": len(payloads[name]),
             "sha256": hashlib.sha256(payloads[name]).hexdigest()} for name in sorted(payloads)]


def read_package(source):
    payloads = {}
    with ZipFile(source) as archive:
        entries = archive.infolist()
        if len(entries) > 128:
            raise ValueError("Source package has too many entries")
        seen = set()
        total = 0
        for entry in entries:
            if entry.is_dir():
                if not safe_path(entry.filename.rstrip("/")):
                    raise ValueError("Invalid source directory")
                continue
            name = entry.filename
            if not safe_path(name) or name.lower() in seen:
                raise ValueError("Invalid or duplicate source path")
            seen.add(name.lower())
            limit = 65536 if name == "manifest.json" else FILE_LIMIT
            total += entry.file_size
            if not 0 < entry.file_size <= limit or total > TOTAL_LIMIT:
                raise ValueError("Source package exceeds content size limits")
            payloads[name] = archive.read(entry)
    if "manifest.json" not in payloads:
        raise ValueError("Expected manifest.json at the ZIP root")
    manifest = json.loads(payloads.pop("manifest.json").decode("utf-8"))
    declared = manifest.get("files")
    if not isinstance(declared, list) or len(declared) != len(payloads) or \
            sorted(declared, key=lambda row: row["path"]) != inventory(payloads):
        raise ValueError("Source package file inventory or hashes do not match")
    return manifest, payloads


def upgrade_manifest(original):
    if original.get("api") != 3 or original.get("kind") != "content" or \
            original.get("id") != "distrito-nimbo" or original.get("version") != "1.4.0":
        raise ValueError("Expected the Distrito Nimbo Pokémon 1.4.0 API 3 package")
    manifest = copy.deepcopy(original)
    world = manifest.get("map", {})
    if world.get("id") != "distrito-nimbo" or world.get("base") != "Forest" or \
            world.get("bundle") != BUNDLE_PATH:
        raise ValueError("Unexpected source map or bundle")
    environments = world.get("stageEnvironments", [])
    stages = [environment.get("stage") for environment in environments]
    if len(environments) != 3 or set(stages) != {1, 2, 3} or any(type(stage) is not int for stage in stages):
        raise ValueError("Expected exactly three distinct district stages")
    enemies = world.get("enemies", [])
    if len(enemies) != len(SPECIES) or {enemy.get("name") for enemy in enemies} != SPECIES:
        raise ValueError("Expected the ten-species Pokémon roster")
    manifest["api"] = 4
    manifest["version"] = "2.1.0"
    # Forest lasts 10/9/8 minutes; nine minutes per district restores the same
    # 27-minute total. The runtime scales each inherited stage's wave times,
    # wave durations and reinforcement checks by .9/1/1.125 respectively.
    world["stageDuration"] = 540
    world["description"] = ("Explore a sunny Japanese neighborhood with terraced streets, shops, "
                            "vending machines, gachapon, lanterns and signs across three Pokémon districts.")
    for environment in environments:
        if environment.get("replaceDecorations") is not True or not environment.get("architectureMaterial") or \
                "rooftops" in environment:
            raise ValueError("Street profiles require replacement decorations and tiled architecture")
        width, rise, coverage, frontages = PROFILES[environment["stage"]]
        environment["streetscape"] = {"tileWidth": width, "stepHeight": rise, "coverage": coverage,
                                     "frontages": ["Nimbo" + name for name in frontages]}
        environment["moaiVisual"] = "MewtwoStatue"
    for enemy in enemies:
        enemy["material"] = "Nimbo" + enemy["name"]
    # Absence means no random placement. Empty arrays are rejected by both
    # importers, and the old houses must not obstruct the new street fronts.
    world.pop("objects", None)
    return manifest


def prepare_payloads(original, payloads, bundle):
    if not bundle.startswith(b"UnityFS\x00") or len(bundle) > FILE_LIMIT:
        raise ValueError("Expected a reviewed nimbo-props Android UnityFS bundle under 64 MiB")
    manifest = upgrade_manifest(original)
    payloads = dict(payloads)
    if BUNDLE_PATH not in payloads:
        raise ValueError("Source package is missing the district bundle")
    payloads[BUNDLE_PATH] = bundle
    manifest["files"] = inventory(payloads)
    payloads["manifest.json"] = (json.dumps(manifest, indent=2, ensure_ascii=False, allow_nan=False) + "\n").encode("utf-8")
    if len(payloads["manifest.json"]) > 65536 or sum(map(len, payloads.values())) > TOTAL_LIMIT:
        raise ValueError("Upgraded package exceeds content size limits")
    return payloads


def write_archive(stream, payloads):
    with ZipFile(stream, "w", compression=ZIP_DEFLATED, compresslevel=9) as archive:
        for name in sorted(payloads):
            entry = ZipInfo(name, FIXED_TIMESTAMP)
            entry.create_system = 3
            entry.external_attr = 0o100644 << 16
            archive.writestr(entry, payloads[name], compress_type=ZIP_DEFLATED, compresslevel=9)


def build(source, bundle, destination):
    if destination.resolve() in (source.resolve(), bundle.resolve()):
        raise ValueError("Output must differ from both inputs")
    original, payloads = read_package(source)
    payloads = prepare_payloads(original, payloads, bundle.read_bytes())
    destination.parent.mkdir(parents=True, exist_ok=True)
    # Same-filesystem staging and an exclusive link leave an existing ZIP
    # untouched and never expose a partially written output package.
    with tempfile.NamedTemporaryFile(prefix=".nimbo-package-", dir=destination.parent, delete=False) as stream:
        staged = Path(stream.name)
        try:
            write_archive(stream, payloads)
            stream.flush()
            os.fsync(stream.fileno())
            os.link(staged, destination)
        finally:
            staged.unlink()
    print(destination)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("old_zip", type=Path, help="existing optional Pokémon 1.4.0 ZIP")
    parser.add_argument("bundle", type=Path, help="reviewed Android nimbo-props bundle")
    parser.add_argument("output_zip", type=Path, help="new 2.1.0 ZIP path")
    args = parser.parse_args()
    build(args.old_zip, args.bundle, args.output_zip)
