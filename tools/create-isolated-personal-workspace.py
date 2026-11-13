#!/usr/bin/env python3
"""Create an isolated local research workspace, not a distribution package.

The copied inputs retain game and third-party material. Steam, Python, Android
SDK, Java and the signing key remain external prerequisites.
"""
import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[1]


def sha(path):
    with path.open('rb') as f: return hashlib.file_digest(f, 'sha256').hexdigest()


def source_files():
    files = set()
    for directory in ('tools', 'analysis/data-conversion', 'analysis/port-inventory'):
        folder = ROOT/directory
        for suffix in ('*.py', '*.json'):
            files.update(p.relative_to(ROOT) for p in folder.glob(suffix) if p.is_file())
    third_party = ROOT/'third_party/texture2ddecoder-unitycrunch'
    files.update(p.relative_to(ROOT) for p in third_party.iterdir() if p.is_file())
    files.add(Path('analysis/data-conversion/unpack-unity-crunch-cubemap.cpp'))
    files.add(Path('tools/requirements-conversion.txt'))
    return files


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('output', type=Path, help='new directory for a private local research copy')
    a = p.parse_args(); out = a.output.expanduser().absolute()
    if out.exists() or out.is_symlink() or not out.parent.is_dir(): p.error('output must be new')
    if ROOT.resolve().is_relative_to(out.resolve()): p.error('output must not contain the source project')
    inventory = json.loads((ROOT/'tools/isolated-workspace-inputs.json').read_text())
    data = inventory['files']
    if len(data) != 106: raise ValueError('unexpected input inventory')
    spec = importlib.util.spec_from_file_location('isolated_runtime_inputs', ROOT/'tools/build-current-runtime-diagnostic.py')
    runtime = importlib.util.module_from_spec(spec); spec.loader.exec_module(runtime)
    for path, expected in (runtime.PINS | runtime.WINDOWS_LAUNCHER_PINS).items():
        rel = str(path.relative_to(ROOT))
        if data.get(rel) != expected:
            raise ValueError('isolated inventory does not match runtime pin: '+rel)
    paths = source_files() | {Path(n) for n in data}
    records = {}
    for rel in sorted(paths):
        if rel.is_absolute() or any(part in ('', '.', '..') for part in rel.parts): raise ValueError('invalid relative path')
        source = ROOT/rel
        if not source.is_file() or not source.resolve().is_relative_to(ROOT.resolve()): raise ValueError('missing or external input: '+str(rel))
        digest = sha(source)
        if str(rel) in data and digest != data[str(rel)]: raise ValueError('incompatible pinned input: '+str(rel))
        records[str(rel)] = digest
    out.mkdir(mode=0o700)
    try:
        for name, digest in records.items():
            dst=out/name; dst.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(ROOT/name,dst)
            if sha(dst) != digest: raise ValueError('input changed while copying: '+name)
        (out/'LOCAL_RESEARCH_COPY.json').write_text(json.dumps({
            'kind':'local research only, not for distribution',
            'files':records,
            'external':['Steam game data','Python environment','Android SDK','Java','signing key'],
        },indent=2,sort_keys=True)+'\n')
        (out/'README_LOCAL.txt').write_text(
            'Private local research copy; do not distribute.\n'
            'Use OPENBONK_SHADER_BUNDLES pointing to build/android/pinned-shader-cache-test.\n'
            'Supply a compatible Steam copy, Python environment, Android SDK, Java and debug key.\n'
            'The copy supports the cached-shader flow; it does not include the Unity project.\n'
        )
    except BaseException:
        shutil.rmtree(out); raise
    print(f'Private local workspace created: {out} ({len(records)} files).')
    print('Contains game and third-party material; not for distribution.')


if __name__ == '__main__': main()
