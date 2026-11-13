#!/usr/bin/env python3
"""Compare named Unity objects in two local APK/OBB pairs.

Reports contain game-derived names and hashes and must stay private. Matching
names or object counts do not establish equivalent references or gameplay.
Only the serialized-file groups described below are inspected; asset bundles,
streamed payloads, code and unparsed groups are not checked.
Requires UnityPy.
"""
import argparse
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import re
import zipfile

import UnityPy

ROOT = Path(__file__).resolve().parent.parent
PREFIX = 'assets/bin/Data/'
SINGLE = re.compile(r'(?:[0-9a-f]{32}|globalgamemanagers|globalgamemanagers\.assets|sharedassets\d+\.assets|level\d+|resources\.assets)\Z')
SPLIT = re.compile(r'((?:globalgamemanagers\.assets|sharedassets\d+\.assets|level\d+|resources\.assets))\.split(0|[1-9]\d*)\Z')
NAMED = frozenset(('Texture2D', 'Mesh', 'AudioClip', 'AnimationClip', 'Sprite', 'GameObject', 'Material', 'AnimatorController'))
MAX_GROUP = 256 * 1024 * 1024


def asset_groups(archive):
    infos = archive.infolist()
    if len({i.filename for i in infos}) != len(infos): raise ValueError('duplicate archive entry')
    groups = defaultdict(list)
    for info in infos:
        if info.is_dir() or not info.filename.startswith(PREFIX): continue
        name = info.filename[len(PREFIX):]; split = SPLIT.fullmatch(name)
        if split: groups[split.group(1)].append((int(split.group(2)), info))
        elif SINGLE.fullmatch(name): groups[name].append((0, info))
    for name, parts in groups.items():
        parts.sort(key=lambda row: row[0])
        if [n for n, _ in parts] != list(range(len(parts))) or sum(i.file_size for _, i in parts) > MAX_GROUP:
            raise ValueError('invalid or oversized serialized asset group: ' + name)
    return {name: [info for _, info in parts] for name, parts in groups.items()}


def inventory(paths):
    counts = Counter(); names = defaultdict(Counter); scripts = Counter(); problems = []; groups_seen = 0; hashes = []
    for path in paths:
        with path.open('rb') as f: digest = hashlib.file_digest(f, 'sha256').hexdigest()
        with zipfile.ZipFile(path) as archive:
            groups = asset_groups(archive); hashes.append({'sha256': digest, 'entries': len(archive.infolist())})
            for group, parts in groups.items():
                groups_seen += 1
                try:
                    env = UnityPy.load(b''.join(archive.read(info) for info in parts))
                    if not env.objects: raise ValueError('no serialized objects')
                except Exception as error:
                    problems.append({'archive': len(hashes) - 1, 'group': group, 'error': type(error).__name__})
                    continue
                for obj in env.objects:
                    kind = obj.type.name; counts[kind] += 1
                    if kind not in NAMED and kind != 'MonoScript': continue
                    try:
                        item = obj.read()
                        if kind == 'MonoScript':
                            scripts[(item.m_AssemblyName, item.m_Namespace, item.m_ClassName)] += 1
                        else:
                            name = getattr(item, 'm_Name', '')
                            if isinstance(name, str) and name: names[kind][name] += 1
                    except Exception as error:
                        problems.append({'archive': len(hashes) - 1, 'group': group,
                                         'type': kind, 'path_id': obj.path_id, 'error': type(error).__name__})
    return counts, names, scripts, problems, groups_seen, hashes


def differences(reference, candidate):
    return {'reference_only': dict(sorted((reference - candidate).items())),
            'candidate_only': dict(sorted((candidate - reference).items()))}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('reference-apk', 'reference-obb', 'candidate-apk', 'candidate-obb', 'output'):
        p.add_argument('--' + name, type=Path, required=True)
    a = p.parse_args(); inputs = []
    for name in ('reference_apk', 'reference_obb', 'candidate_apk', 'candidate_obb'):
        path = getattr(a, name).expanduser()
        if path.is_symlink() or not path.is_file(): p.error(name + ' must be a regular file')
        inputs.append(path.resolve())
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(ROOT) or any(output == path for path in inputs): p.error('output must be outside the checkout and inputs')
    ref = inventory(inputs[:2]); new = inventory(inputs[2:])
    kinds = sorted(set(ref[1]) | set(new[1])); names = {kind: differences(ref[1][kind], new[1][kind]) for kind in kinds}
    # Encode the three MonoScript identity fields as JSON, avoiding ambiguous delimiters.
    scripts = [Counter({json.dumps(key, ensure_ascii=False): count for key, count in item.items()}) for item in (ref[2], new[2])]
    report = {'schema': 1, 'reference': {'archives': ref[5], 'groups': ref[4], 'object_counts': dict(ref[0]), 'problems': ref[3]},
              'candidate': {'archives': new[5], 'groups': new[4], 'object_counts': dict(new[0]), 'problems': new[3]},
              'named_objects': names, 'scripts': differences(*scripts),
              'limitations': 'Counts and names are not references or equivalence. Bundles, streamed payloads, code and unparsed objects are not compared.'}
    with output.open('x', encoding='utf-8') as f: json.dump(report, f, ensure_ascii=False, indent=2); f.write('\n')
    print(f'Compared {ref[4]} reference and {new[4]} candidate serialized groups; '
          f'{len(ref[3]) + len(new[3])} group/object read failures. Report remains private.')
    for kind in kinds:
        print(f'{kind}: reference-only occurrences {sum(names[kind]["reference_only"].values())}; '
              f'candidate-only occurrences {sum(names[kind]["candidate_only"].values())}')


if __name__ == '__main__': main()
