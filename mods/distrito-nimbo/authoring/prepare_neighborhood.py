"""Merge Nimbo props, add solid collision boxes, and bake vertex ambient occlusion.

Run with Blender, for example:
  blender -b --python prepare_neighborhood.py -- \
    --input-old nimbo-props-pokeballs.json --input-new japanese-props.json \
    --collision collision-contract.json --output neighborhood/props.json

Input geometry is already in Unity metres, facing +Z. Supplemental boxes below
come from named parts in japanese_streetscape.py, whose Blender coordinates are
converted by (-x, z, -y). Only new prop colors and collider metadata change.
The adjacent *.validation.json records inputs, collision envelopes and AO data.
"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import sys

from mathutils import Vector
from mathutils.bvhtree import BVHTree


AXES = "xyz"
BOUNDS_MARGIN = 0.125

# (named Blender part, center XYZ, dimensions XYZ). Keep these physical solids
# separate: never create a box spanning an awning, gate opening or cluster.
EXTRA_SOLIDS = {
    "NimboVendingCorner": [
        ("Concrete service pad", (0, 0, .045), (4.8, 2.15, .09)),
        ("Bottle return bin", (1.65, -.2, .62), (.52, .64, 1.1)),
        ("Planter stone", (-1.82, -.42, .32), (.65, .72, .64)),
        ("Canopy post left", (-2.15, .65, 1.47), (.12, .12, 2.85)),
        ("Canopy post right", (2.15, .65, 1.47), (.12, .12, 2.85)),
    ],
    "NimboGachaCorner": [
        ("Capsule kiosk base", (0, 0, .07), (4.2, 1.8, .14)),
        ("Empty capsule return", (1.25, .15, .64), (.68, .67, 1.05)),
        ("Kiosk upright left", (-1.82, .48, 1.42), (.13, .15, 2.7)),
        ("Kiosk upright right", (1.82, .48, 1.42), (.13, .15, 2.7)),
    ],
    "NimboRamenFront": [
        # The contract's main shop box intentionally closes the interior.
        # Stools, curtains, hanging signs and lanterns remain nonblocking.
        ("Shop foundation", (0, .4, .11), (5.8, 2.8, .22)),
        ("Planter stone", (-2.55, -1.38, .32), (.55, .72, .64)),
    ],
    "NimboLanternGate": [
        ("Gate footing left", (-2, 0, .17), (.60, .65, .34)),
        ("Gate footing right", (2, 0, .17), (.60, .65, .34)),
    ],
    "NimboPosterGarden": [
        ("Community notice backing", (0, .64, 1.73), (3.9, .22, 2.06)),
        ("Planter stone left", (-2.1, .04, .32), (.7, .72, .64)),
        ("Planter stone right", (2.1, .04, .32), (.7, .72, .64)),
    ],
    "NimboShrineCorner": [
        ("Shrine lowest step", (0, 0, .13), (4.7, 3.3, .26)),
        ("Shrine upper step", (0, .22, .36), (4.0, 2.7, .2)),
    ],
}


def read_json(path):
    return json.loads(path.read_text(encoding="utf-8"))


def digest(value):
    return hashlib.sha256(json.dumps(value, separators=(",", ":"),
                                     allow_nan=False).encode()).hexdigest()


def vector_dict(values):
    return dict(zip(AXES, values))


def xyz(value):
    values = tuple(value[k] for k in AXES)
    if any(isinstance(x, bool) or not isinstance(x, (int, float)) or
           not math.isfinite(x) for x in values):
        raise ValueError("non-finite or non-numeric vector")
    return values


def geometry_digest(prop):
    return digest({k: prop[k] for k in ("points", "normals")})


def validate_geometry(prop):
    name = prop["name"]
    points, normals, colors = (prop[k] for k in ("points", "normals", "colors"))
    if not points or len(points) % 3 or len(normals) != len(points) or len(colors) != len(points):
        raise ValueError(f"{name}: triangle and attribute arrays do not match")
    for point in points:
        xyz(point)
    for normal in normals:
        n = xyz(normal)
        if sum(v * v for v in n) < 1e-12:
            raise ValueError(f"{name}: zero normal")
    for color in colors:
        if any(isinstance(color[k], bool) or not isinstance(color[k], (int, float)) or
               not math.isfinite(color[k]) or not 0 <= color[k] <= 1 for k in "rgba"):
            raise ValueError(f"{name}: invalid color")
        if color["a"] != 1:
            raise ValueError(f"{name}: non-opaque vertex color")
    lo = {k: min(p[k] for p in points) for k in AXES}
    hi = {k: max(p[k] for p in points) for k in AXES}
    return {"name": name, "vertices": len(points), "triangles": len(points) // 3,
            "arraysMatch": True, "finite": True, "opaque": True,
            "boundsMin": lo, "boundsMax": hi}


def collision_boxes(name, contract):
    boxes = []
    labels = []
    for i, box in enumerate(contract):
        if len(box["center"]) != 3 or len(box["size"]) != 3:
            raise ValueError(f"{name}: malformed source collision box")
        boxes.append({"center": vector_dict(box["center"]), "size": vector_dict(box["size"])})
        labels.append(f"Existing contract solid {i + 1}")
    for label, (x, y, z), (sx, sy, sz) in EXTRA_SOLIDS[name]:
        boxes.append({"center": vector_dict((-x, z, -y)), "size": vector_dict((sx, sz, sy))})
        labels.append(label)
    return boxes, labels


def validate_collision(boxes, labels, geometry):
    if not 0 < len(boxes) <= 16:
        raise ValueError(f"{geometry['name']}: collision box count outside 1..16")
    receipt = []
    for box, label in zip(boxes, labels):
        xyz(box["center"])
        if any(v <= 0 for v in xyz(box["size"])):
            raise ValueError(f"{geometry['name']}: nonpositive collision box")
        lo = {k: box["center"][k] - box["size"][k] / 2 for k in AXES}
        hi = {k: box["center"][k] + box["size"][k] / 2 for k in AXES}
        if any(lo[k] < geometry["boundsMin"][k] - BOUNDS_MARGIN or
               hi[k] > geometry["boundsMax"][k] + BOUNDS_MARGIN for k in AXES):
            raise ValueError(f"{geometry['name']}: {label} exceeds visual bounds + {BOUNDS_MARGIN}m")
        receipt.append({"part": label, **box, "boundsMin": lo, "boundsMax": hi,
                        "insideVisualBoundsWithMargin": True})
    if geometry["name"] == "NimboLanternGate":
        # Both footings are outside +/-1.7m. The complete central aperture must
        # remain clear at every height, including the overhanging signs/roof.
        if any(box["center"]["x"] - box["size"]["x"] / 2 < 1.7 - 1e-8 and
               box["center"]["x"] + box["size"]["x"] / 2 > -1.7 + 1e-8 for box in boxes):
            raise ValueError("gate collision crosses the 3.4m clear aperture")
    return receipt


def bake_ao(prop, samples, distance, epsilon, minimum):
    points = [Vector(xyz(point)) for point in prop["points"]]
    tree = BVHTree.FromPolygons(points,
                               [(i, i + 1, i + 2) for i in range(0, len(points), 3)],
                               all_triangles=True, epsilon=0.0)
    # Fixed cosine-weighted hemisphere samples. No scene or gameplay RNG and
    # no dependence on prop/triangle ordering. Duplicate corners share a bake.
    hemisphere = []
    golden_angle = math.pi * (3 - math.sqrt(5))
    for i in range(samples):
        r = math.sqrt((i + .5) / samples)
        angle = i * golden_angle
        hemisphere.append((r * math.cos(angle), r * math.sin(angle), math.sqrt(1 - r * r)))
    cache = {}
    factors = []
    for i, point in enumerate(points):
        key = (*xyz(prop["points"][i]), *xyz(prop["normals"][i]))
        if key not in cache:
            normal = Vector(xyz(prop["normals"][i])).normalized()
            reference = Vector((0, 1, 0)) if abs(normal.y) < .95 else Vector((1, 0, 0))
            tangent = reference.cross(normal).normalized()
            bitangent = normal.cross(tangent)
            origin = point + normal * epsilon
            blocked = 0
            for x, y, z in hemisphere:
                ray = tangent * x + bitangent * y + normal * z
                hit, _, _, _ = tree.ray_cast(origin, ray, distance)
                blocked += hit is not None
            cache[key] = max(minimum, min(1.0, 1 - (1 - minimum) * blocked / samples))
        factor = cache[key]
        factors.append(factor)
        color = prop["colors"][i]
        for channel in "rgb":
            color[channel] *= factor
    return {"samples": samples, "maxDistanceMetres": distance,
            "surfaceOffsetMetres": epsilon, "minimumAllowedFactor": minimum,
            "minimumFactor": min(factors), "maximumFactor": max(factors),
            "meanFactor": sum(factors) / len(factors),
            "shadedVertices": sum(f < 1 for f in factors), "uniqueSurfaceSamples": len(cache),
            "rayCount": len(cache) * samples, "alphaUnchanged": True}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input-old", type=Path, required=True)
    parser.add_argument("--input-new", type=Path, required=True)
    parser.add_argument("--collision", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--ao-samples", type=int, default=8)
    parser.add_argument("--ao-distance", type=float, default=1.5)
    parser.add_argument("--ao-offset", type=float, default=.01)
    parser.add_argument("--ao-minimum", type=float, default=.65)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:])
    if not 1 <= args.ao_samples <= 256 or not math.isfinite(args.ao_distance) or args.ao_distance <= 0 or \
            not math.isfinite(args.ao_offset) or not 0 < args.ao_offset < args.ao_distance or \
            not math.isfinite(args.ao_minimum) or not 0 <= args.ao_minimum <= 1:
        parser.error("invalid AO settings")
    input_paths = [args.input_old, args.input_new, args.collision]
    if args.output.resolve() in [p.resolve() for p in input_paths]:
        parser.error("output must not overwrite an input")
    old_package, new_package, contract = map(read_json, input_paths)
    old_props, new_props = old_package["props"], new_package["props"]
    names = [p["name"] for p in old_props + new_props]
    if len(names) != len(set(names)):
        raise ValueError("duplicate prop name across input packages")
    if {p["name"] for p in new_props} != set(EXTRA_SOLIDS) or set(contract["prefabs"]) != set(EXTRA_SOLIDS):
        raise ValueError("new props and collision contract must match the six inspected Japanese clusters")
    if contract.get("units") != "metres" or contract.get("front") != "+Z":
        raise ValueError("collision contract must use Unity metres and +Z frontage")
    old_digest = digest(old_props)
    receipts = []
    for prop in old_props:
        receipt = validate_geometry(prop)
        receipt.update({"source": "legacy", "preservedExactly": True,
                        "defaultCollisionPreserved": True, "ambientOcclusionModified": False})
        receipts.append(receipt)
    for prop in new_props:
        receipt = validate_geometry(prop)
        original_geometry = geometry_digest(prop)
        boxes, labels = collision_boxes(prop["name"], contract["prefabs"][prop["name"]])
        receipt["colliders"] = validate_collision(boxes, labels, receipt)
        prop["colliders"] = boxes
        receipt["ao"] = bake_ao(prop, args.ao_samples, args.ao_distance, args.ao_offset, args.ao_minimum)
        validate_geometry(prop)
        if original_geometry != geometry_digest(prop):
            raise ValueError("ambient occlusion changed geometry")
        receipt.update({"source": "japanese", "geometryUnchanged": True,
                        "geometrySha256": original_geometry, "colliderCount": len(boxes),
                        "ambientOcclusionModified": True})
        if prop["name"] == "NimboLanternGate":
            receipt["clearApertureWidthMetres"] = 3.4
        receipts.append(receipt)
        print(f"Prepared {prop['name']}: {receipt['triangles']} triangles, {len(boxes)} solids, "
              f"AO mean {receipt['ao']['meanFactor']:.3f}", flush=True)
    if old_digest != digest(old_props):
        raise ValueError("legacy props changed")
    package = {**old_package, "props": old_props + new_props}
    encoded = json.dumps(package, separators=(",", ":"), allow_nan=False).encode()
    validation = {
        "schema": 1, "coordinates": "Unity (-Blender X, Blender Z, -Blender Y), metres",
        "inputs": [{"path": str(p.resolve()), "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
                   for p in input_paths],
        "output": str(args.output.resolve()), "outputSha256": hashlib.sha256(encoded).hexdigest(),
        "legacyProps": len(old_props), "newProps": len(new_props), "propCount": len(names),
        "legacyPropsUnchanged": True, "collisionBoundsMarginMetres": BOUNDS_MARGIN,
        "nonblocking": ["awnings", "hanging signs", "lanterns", "ramen stools", "gate aperture"],
        "props": receipts,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(encoded)
    receipt_path = args.output.with_suffix(".validation.json")
    receipt_path.write_text(json.dumps(validation, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    print(f"Merged {len(names)} props into {args.output}; validation: {receipt_path}", flush=True)


if __name__ == "__main__":
    main()
