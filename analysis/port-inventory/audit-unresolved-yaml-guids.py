#!/usr/bin/env python3
"""Find unresolved references in exported Android scenes, prefabs and YAML data.

This is a static text audit, not Unity's object/type resolver.
Names and free text containing 'guid:' are intentionally excluded.
"""
from collections import Counter, defaultdict
from pathlib import Path
import re
import sys

project = Path(sys.argv[1])
roots = [project / "Assets", project / "Packages", project / "Library/PackageCache"]
known = set()
yaml_assets = {}
for root in roots:
    if not root.exists():
        continue
    for path in root.rglob("*.meta"):
        try:
            match = re.search(r"^guid: ([0-9a-f]{32})", path.read_text(errors="ignore"), re.M)
            if match:
                known.add(match.group(1))
                asset = Path(str(path)[:-5])
                if root == project / "Assets" and asset.is_file() and asset.suffix in (".prefab", ".asset", ".mat", ".renderTexture"):
                    yaml_assets[match.group(1)] = asset
        except OSError as error:
            raise SystemExit(f"Cannot read metadata {path}: {error}") from error

reference = re.compile(r"\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-f]{32})")
local_reference = re.compile(r"\{fileID:\s*(-?\d+)\}")
object_header = re.compile(r"^--- !u!\d+ &(-?\d+)", re.M)
asset_ids = {}
def is_yaml_asset(path):
    with path.open("rb") as stream:
        return stream.read(5) == b"%YAML"

groups = [
    ("Android scenes", list((project / "Assets/ScenesAndroid").glob("*.unity"))),
    ("exported prefabs", list((project / "Assets").rglob("*.prefab"))),
    ("YAML data assets", [path for path in (project / "Assets").rglob("*.asset") if is_yaml_asset(path)]),
]
errors_found = False
for label, paths in groups:
    counts = Counter()
    examples = defaultdict(list)
    missing_ids = Counter()
    missing_local = Counter()
    total = 0
    checked_external_ids = 0
    checked_local_ids = 0
    for path in paths:
        source = path.read_text(errors="ignore")
        refs = Counter(reference.findall(source))
        total += sum(refs.values())
        own_ids = set(object_header.findall(source))
        for file_id in local_reference.findall(source):
            if file_id == "0":
                continue
            checked_local_ids += 1
            if file_id not in own_ids:
                missing_local[(str(path.relative_to(project)), file_id)] += 1
        for (file_id, guid), count in refs.items():
            if guid in yaml_assets and file_id != "0":
                asset = yaml_assets[guid]
                if asset not in asset_ids:
                    contents = asset.read_text(errors="ignore")
                    asset_ids[asset] = set(object_header.findall(contents)) if contents.startswith("%YAML") else None
                ids = asset_ids[asset]
                if ids is not None:
                    checked_external_ids += count
                    if file_id not in ids:
                        missing_ids[(str(path.relative_to(project)), str(asset.relative_to(project)), file_id)] += count
            if guid in known or guid.startswith("0000000000000000"):
                continue
            counts[guid] += count
            if len(examples[guid]) < 3:
                examples[guid].append(str(path.relative_to(project)))
    print(f"{label}: files={len(paths)} references={total} unresolved_guids={len(counts)} occurrences={sum(counts.values())}")
    for guid, count in counts.most_common():
        print(f"  {guid} occurrences={count} examples={'; '.join(examples[guid])}")
    print(f"  local fileIDs checked={checked_local_ids} missing={sum(missing_local.values())}")
    for (source, file_id), count in missing_local.most_common():
        print(f"    {source} fileID={file_id} occurrences={count}")
    print(f"  external YAML fileIDs checked={checked_external_ids} missing={sum(missing_ids.values())}")
    for (source, asset, file_id), count in missing_ids.most_common():
        print(f"    {source} -> {asset} fileID={file_id} occurrences={count}")
    errors_found |= bool(counts or missing_local or missing_ids)
print("Limit: static textual references only; built-in GUIDs and non-YAML external fileIDs are excluded; object types, dynamic loads and runtime behavior are not validated.")
raise SystemExit(1 if errors_found else 0)
