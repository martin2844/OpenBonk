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
    ("terminal", (247, 229, 192), (219, 171, 96), (131, 144, 148), (73, 145, 171)),
    ("interchange", (220, 239, 220), (151, 192, 154), (100, 137, 142), (72, 156, 163)),
    ("relay", (238, 229, 249), (201, 174, 221), (125, 114, 151), (104, 133, 182)),
)
LANDMARKS = ("TerminalDepot", "InterchangePavilion", "RelayBeacon")
REWARDS = (
    ("platform-pass", "Pase de andén", "Aumenta un 12 % la velocidad de movimiento.", "MoveSpeedMultiplier", .12),
    ("transit-crystal", "Cristal de paso", "Aumenta un 18 % la velocidad de los proyectiles.", "ProjectileSpeedMultiplier", .18),
    ("relay-core", "Núcleo del relé", "Aumenta un 20 % la duración de los ataques.", "DurationMultiplier", .20),
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
    if (x in range(7, 11) or x in range(117, 121)) and (y % 32) < 24:
        return paint
    # Staggered concrete slabs, with the seams less bright than the route marks.
    if y % 32 == 0 or (x + (16 if y // 32 % 2 else 0)) % 32 == 0:
        return shift(base, -20)
    grain = (x * 17 + y * 23 + x * y * 3) % 6
    variation = ((x + (16 if y // 32 % 2 else 0)) // 32 + y // 32 * 2) % 4
    return shift(base, grain + 3 * variation - 8)


def wall_pixel(base, glass, sign, x, y):
    bay, floor = x % 32, y % 32
    if floor in (0, 31):
        return shift(base, -40)
    if floor in (1, 2, 29, 30):
        return shift(base, 12)
    if 3 <= bay <= 28 and 6 <= floor <= 23:
        if bay in (3, 4, 27, 28, 15, 16) or floor in (6, 7, 22, 23):
            return (40, 59, 78)
        if floor + (bay % 15) in (14, 15, 16):
            return shift(glass, 32)
        if (x // 32 + 2 * (y // 32)) % 11 == 6:
            return shift(sign, -9 if floor >= 18 else 0)
        return shift(glass, 10 if floor < 14 else -11)
    if 6 <= bay <= 25 and floor == 24:
        return shift(base, -25)
    brick = 5 if (y % 8 == 0 or (x + 7 * (y // 8 % 2)) % 14 == 0) else 0
    return shift(base, -brick)


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


def reward_pixel(stage, x, y):
    dx, dy = x - 32, y - 32
    if max(abs(dx), abs(dy)) >= 29:
        return (23, 29, 50)
    if stage == 1 and abs(dx + dy // 4) < 22 and abs(dy) < 13:
        if -6 < dy < 6 and dx in range(-18, 19) and dx % 6 < 3:
            return (125, 71, 60)
        return (249, 185, 102) if dy < 7 else (212, 126, 72)
    if stage == 2 and abs(dx) * 3 + abs(dy) * 2 < 55:
        return (134, 236, 225) if dx < 5 else (44, 158, 168)
    if stage == 3 and abs(dx) + abs(dy) < 27:
        if abs(dx) < 4 or abs(dy) < 4:
            return (236, 210, 255)
        return (152, 107, 230) if dx < 0 else (100, 79, 185)
    return (48, 59, 83)


def enemy(identifier, name, mesh_prefix, movement, *, hp, damage, xp, speed, cost, scale, radius, attack=None):
    clips = ("walk", "idle", "charge") if movement == "charge" else ("walk", "idle")
    return {
        "id": identifier, "name": name, "material": "NimboProp", "movement": movement,
        "hp": hp, "damage": damage, "xp": xp, "speed": speed, "cost": cost,
        "scale": scale, "radius": radius, "fps": 12,
        **({"attack": attack, "attackMaterial": "NimboWarning"} if attack else {}),
        **{clip: {"prefix": mesh_prefix + clip.title(), "frames": 12} for clip in clips},
    }


def build(destination, prop_bundle):
    payloads = {"main.lua": (ROOT / "main.lua").read_bytes(), "icon.png": png(64, icon_pixel)}
    bundle_path = "assets/nimbo-props"
    assets = prop_bundle.read_bytes()
    if not assets.startswith(b"UnityFS\x00") or len(assets) > 64 * 1024 * 1024:
        raise ValueError("Expected the nimbo-props Android bundle")
    payloads[bundle_path] = assets
    environments = []
    rewards = []
    for stage, (name, deck, paint, concrete, glass) in enumerate(DISTRICTS, 1):
        ground = f"textures/{name}-roof.png"
        wall = f"textures/{name}-facade.png"
        payloads[ground] = png(128, lambda x, y: roof_pixel(deck, paint, x, y))
        payloads[wall] = png(128, lambda x, y: wall_pixel(concrete, glass, paint, x, y))
        environments.append({
            "stage": stage, "groundTexture": ground, "wallTexture": wall, "replaceDecorations": True,
            "architectureMaterial": "NimboTerrain", "skyMaterial": "NimboDaySky",
            "potVisual": "CargoCapsule", "silverPotVisual": "SilverCargoCapsule",
            "arenaLandmark": LANDMARKS[stage - 1], "potBreakFx": "NimboCapsuleBurst",
        })
        identifier, title, details, stat, bonus = REWARDS[stage - 1]
        reward_icon = f"textures/{identifier}.png"
        payloads[reward_icon] = png(64, lambda x, y: reward_pixel(stage, x, y))
        rewards.append({"id": identifier, "name": title, "description": details, "icon": reward_icon,
                        "stat": stat, "bonus": bonus})

    manifest = {
        "api": 3,
        "kind": "content",
        "id": "openbonk.distrito-nimbo",
        "map": {
            "id": "distrito-nimbo",
            "name": "Distrito Nimbo",
            "description": "Climb switchback streets and cross terraced plazas through three city districts.",
            "base": "Forest",
            "icon": "icon.png",
            "tileGridSize": 15,
            "volatility": 0.8,
            "slopeStrength": 0,
            "flatMapBias": 0,
            "stageDuration": 420,
            "hooks": ["stage_spawn_multiplier"],
            "stageEnvironments": environments,
            "enemies": [
                enemy("beedrill", "Beedrill", "Beedrill", "charge",
                      hp=9, damage=13, xp=1, speed=5, cost=1, scale=1.35, radius=.59),
                enemy("pidgey", "Pidgey", "Pidgey", "fly",
                      hp=6, damage=9, xp=1, speed=6, cost=.9, scale=1.7, radius=.24),
                enemy("charizard", "Charizard", "Charizard", "charge",
                      hp=160, damage=24, xp=8, speed=5.4, cost=9, scale=2.9, radius=.70),
                enemy("rattata", "Rattata", "Rattata", "ground",
                      hp=16, damage=15, xp=2, speed=6.7, cost=1.1, scale=1.75, radius=.36),
                enemy("gyarados", "Gyarados", "Gyarados", "fly",
                      hp=230, damage=25, xp=11, speed=4.5, cost=10, scale=2.26, radius=.30, attack="sweep"),
                enemy("raticate", "Raticate", "Raticate", "ground",
                      hp=18, damage=18, xp=2, speed=6.3, cost=1.3, scale=1.65, radius=.32),
                enemy("rhydon", "Rhydon", "Rhydon", "ground",
                      hp=340, damage=31, xp=15, speed=4.5, cost=12, scale=2.7, radius=.69, attack="pulse"),
                enemy("fearow", "Fearow", "Fearow", "fly",
                      hp=72, damage=18, xp=5, speed=6.4, cost=5, scale=2.2, radius=.45),
                enemy("arbok", "Arbok", "Arbok", "charge",
                      hp=105, damage=23, xp=6, speed=5.8, cost=6, scale=2.35, radius=.57),
                enemy("nidoking", "Nidoking", "Nidoking", "charge",
                      hp=150, damage=28, xp=8, speed=5.1, cost=7, scale=2.6, radius=.68),
            ],
            "encounters": [{
                "stage": 1,
                "start": ["beedrill"],
                "reserve": ["pidgey"],
                "swarm": ["pidgey"],
                "minibosses": ["fearow"],
                "overtime": ["beedrill", "pidgey"],
                "boss": "charizard", "reward": rewards[0],
            }, {
                "stage": 2,
                "start": ["rattata"],
                "reserve": ["pidgey", "beedrill"],
                "swarm": ["rattata", "pidgey"],
                "minibosses": ["arbok"],
                "overtime": ["rattata", "pidgey", "beedrill"],
                "boss": "gyarados", "reward": rewards[1],
            }, {
                "stage": 3,
                "start": ["raticate", "beedrill"],
                "reserve": ["rattata"],
                "swarm": ["raticate"],
                "minibosses": ["nidoking"],
                "overtime": ["raticate", "beedrill", "rattata"],
                "boss": "rhydon", "reward": rewards[2],
            }],
            "bundle": bundle_path,
            "objects": [
                {"prefab": "TerminalHeatPump", "amount": 36, "scaleMin": 0.95, "scaleMax": 1.15,
                 "checkRadius": 2, "maxSlopeAngle": 5, "upOffset": 0},
                {"prefab": "BasaltHead", "amount": 12, "stages": [1, 2], "scaleMin": 0.95, "scaleMax": 1.4,
                 "checkRadius": 2.5, "maxSlopeAngle": 5, "upOffset": 0},
                {"prefab": "SignalAntenna", "amount": 22, "stages": [2, 3], "scaleMin": 1, "scaleMax": 1.3,
                 "checkRadius": 2, "maxSlopeAngle": 5, "upOffset": 0},
                {"prefab": "SageAccessHouse", "amount": 56, "stages": [1, 2], "scaleMin": 1.7, "scaleMax": 2.1,
                 "checkRadius": 4.5, "maxSlopeAngle": 3, "upOffset": 0},
                {"prefab": "LilacAccessHouse", "amount": 42, "stages": [1, 3], "scaleMin": 1.55, "scaleMax": 2.0,
                 "checkRadius": 4.5, "maxSlopeAngle": 3, "upOffset": 0},
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
