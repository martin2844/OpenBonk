#!/usr/bin/env python3
"""Build an importable example using only the Python standard library."""
import hashlib
import json
import struct
import zlib
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile, ZipInfo

ROOT = Path(__file__).resolve().parent


def chunk(kind, data):
    return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xffffffff)


def icon():
    # Original procedural pixel art; the ZIP does not need game assets.
    pixels = bytearray()
    for y in range(64):
        pixels.append(0)
        for x in range(64):
            border = x < 3 or y < 3 or x >= 61 or y >= 61
            path = abs(x - (31 + (y - 32) // 5)) < 5 and y > 19
            tree = any(abs(x - cx) + abs(y - cy) < 10 for cx, cy in ((15, 17), (45, 14), (14, 43), (49, 40)))
            color = (47, 31, 54, 255) if border else (222, 176, 96, 255) if path else (28, 94, 67, 255) if tree else (100, 171, 94, 255)
            pixels.extend(color)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", 64, 64, 8, 6, 0, 0, 0)) + chunk(b"IDAT", zlib.compress(pixels, 9)) + chunk(b"IEND", b"")


def main():
    payloads = {"main.lua": (ROOT / "main.lua").read_bytes(), "icon.png": icon()}
    manifest = {
        "api": 3, "kind": "content", "id": "openbonk.example.forest",
        "map": {
            "id": "example-forest", "name": "Example Forest",
            "description": "A slightly denser forest with tougher enemies.",
            "base": "Forest", "icon": "icon.png", "tileGridSize": 9,
            "volatility": 1.1, "slopeStrength": 0.25, "flatMapBias": 0.1,
            "stageDuration": 300, "hooks": [
                "stage_spawn_multiplier", "stage_enemy_health_multiplier",
                "stage_enemy_speed_multiplier", "stage_enemy_damage_multiplier",
            ],
        },
        "files": [{"path": name, "size": len(data), "sha256": hashlib.sha256(data).hexdigest()} for name, data in payloads.items()],
    }
    destination = ROOT / "example-forest.zip"
    with ZipFile(destination, "w", compression=ZIP_DEFLATED) as archive:
        for name, data in [("manifest.json", (json.dumps(manifest, indent=2) + "\n").encode()), *payloads.items()]:
            info = ZipInfo(name, (2020, 1, 1, 0, 0, 0))
            info.compress_type = ZIP_DEFLATED
            archive.writestr(info, data)
    print(destination)


if __name__ == "__main__":
    main()
