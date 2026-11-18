#!/usr/bin/env python3
"""Package the three original environment themes for the OpenBonk development build."""
import argparse
import hashlib
import json
import struct
import zlib
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo


ROOT = Path(__file__).resolve().parent
DISTRICTS = (
    ("terminal", (59, 89, 81), (231, 136, 57), (168, 168, 145), (48, 76, 75)),
    ("interchange", (49, 72, 94), (91, 202, 183), (116, 142, 156), (38, 57, 79)),
    ("relay", (73, 71, 106), (237, 123, 107), (127, 115, 152), (51, 45, 78)),
)


def png(size, pixel_at):
    def chunk(kind, payload):
        return struct.pack(">I", len(payload)) + kind + payload + struct.pack(">I", zlib.crc32(kind + payload))

    rows = bytearray()
    for y in range(size):
        rows.append(0)
        for x in range(size):
            rows.extend(pixel_at(x, y))
    header = struct.pack(">IIBBBBB", size, size, 8, 2, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(rows, 9)) + chunk(b"IEND", b"")


def shift(color, amount):
    return tuple(max(0, min(255, channel + amount)) for channel in color)


def roof_pixel(base, paint, x, y):
    if (x in range(9, 12) or x in range(116, 119)) and (y % 24) < 16:
        return paint
    if x in (0, 63, 127) or y in (0, 63, 127):
        return shift(base, -9)
    grain = (x * 17 + y * 23 + x * y * 3) % 7
    return shift(base, grain - 3)


def wall_pixel(base, glass, sign, x, y):
    bay, floor = x % 32, y % 32
    if floor in (0, 1, 30, 31):
        return shift(base, 16)
    if 6 <= bay <= 25 and 8 <= floor <= 23:
        if bay in (6, 25) or floor in (8, 23):
            return shift(base, -23)
        if (x // 32 + 2 * (y // 32)) % 9 == 3:
            return shift(sign, -10 if floor >= 18 else 0)
        return shift(glass, 9 if floor < 12 else 0)
    return shift(base, -4 if bay in (0, 31) else 0)


def icon_pixel(x, y):
    if x < 2 or y < 2 or x > 61 or y > 61:
        return (30, 40, 53)
    towers = ((7, 19, 29, 54), (22, 44, 15, 54), (47, 59, 34, 54))
    for left, right, top, bottom in towers:
        if left <= x <= right and top <= y <= bottom:
            if y in (top, top + 1):
                return (238, 168, 83)
            if x % 6 in (1, 2, 3) and y % 7 in (3, 4):
                return (76, 188, 168)
            return (51, 76, 92)
    if (31 <= x <= 33 and 7 <= y <= 14) or (27 <= x <= 37 and y == 9):
        return (238, 168, 83)
    return (123, 156, 179) if y < 55 else (57, 80, 100)


def build(destination, prop_bundle):
    payloads = {"main.lua": (ROOT / "main.lua").read_bytes(), "icon.png": png(64, icon_pixel)}
    bundle_path = "assets/nimbo-props"
    assets = prop_bundle.read_bytes()
    if not assets.startswith(b"UnityFS\x00") or len(assets) > 64 * 1024 * 1024:
        raise ValueError("Expected the nimbo-props Android bundle")
    payloads[bundle_path] = assets
    environments = []
    for stage, (name, deck, paint, concrete, glass) in enumerate(DISTRICTS, 1):
        ground = f"textures/{name}-roof.png"
        wall = f"textures/{name}-facade.png"
        payloads[ground] = png(128, lambda x, y: roof_pixel(deck, paint, x, y))
        payloads[wall] = png(128, lambda x, y: wall_pixel(concrete, glass, paint, x, y))
        environments.append({
            "stage": stage, "groundTexture": ground, "wallTexture": wall, "replaceDecorations": True,
        })

    manifest = {
        "api": 3,
        "kind": "content",
        "id": "openbonk.distrito-nimbo",
        "map": {
            "id": "distrito-nimbo",
            "name": "Distrito Nimbo",
            "description": "A rooftop district with three surface and facade palettes. Development preview.",
            "base": "Forest",
            "icon": "icon.png",
            "tileGridSize": 9,
            "volatility": 0.45,
            "slopeStrength": 0.25,
            "flatMapBias": 0.2,
            "stageDuration": 420,
            "hooks": ["stage_spawn_multiplier"],
            "stageEnvironments": environments,
            "bundle": bundle_path,
            "objects": [
                {"prefab": "TerminalHeatPump", "amount": 20, "scaleMin": 0.95, "scaleMax": 1.15,
                 "checkRadius": 2, "maxSlopeAngle": 5, "upOffset": 0},
                {"prefab": "BasaltHead", "amount": 12, "stages": [1, 2], "scaleMin": 0.95, "scaleMax": 1.4,
                 "checkRadius": 2.5, "maxSlopeAngle": 5, "upOffset": 0},
                {"prefab": "SignalAntenna", "amount": 22, "stages": [2, 3], "scaleMin": 1, "scaleMax": 1.3,
                 "checkRadius": 2, "maxSlopeAngle": 5, "upOffset": 0},
            ],
        },
        "files": [
            {"path": path, "size": len(data), "sha256": hashlib.sha256(data).hexdigest()}
            for path, data in payloads.items()
        ],
    }

    destination.parent.mkdir(parents=True, exist_ok=True)
    with ZipFile(destination, "x", compression=ZIP_DEFLATED) as archive:
        for name, data in [("manifest.json", (json.dumps(manifest, indent=2) + "\n").encode()), *payloads.items()]:
            entry = ZipInfo(name, (2020, 1, 1, 0, 0, 0))
            entry.compress_type = ZIP_DEFLATED
            archive.writestr(entry, data)
    print(destination)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("destination", type=Path)
    parser.add_argument("--props", type=Path, required=True)
    args = parser.parse_args()
    build(args.destination, args.props)
