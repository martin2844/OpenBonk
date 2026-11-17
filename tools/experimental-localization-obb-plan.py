#!/usr/bin/env python3
"""Generate a private OBB localization reconstruction experiment.

Output contains game-derived data and must remain local. This is an isolated
research stage, not yet integrated into a launcher or release build.
"""
import argparse
import base64
import copy
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import shutil
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parent.parent
INPUTS = ROOT / 'tools/launcher-steam-inputs.json'
PATTERN = re.compile(r'localization-(?:assets-shared_assets_all|string-tables-[a-z0-9()_-]+_assets_all)\.bundle\Z')

def sha(data): return hashlib.sha256(data).hexdigest()

def write_pair(archive, masked, converted, info, left=None, right=None):
    # ZipFile mutates ZipInfo while writing (including CRC and sizes). Each
    # output must own its copy or the second write corrupts the first index.
    if left is not None and right is not None:
        masked.writestr(copy.copy(info), left); converted.writestr(copy.copy(info), right)
    elif left is None and right is None:
        with archive.open(info) as inp, masked.open(copy.copy(info), 'w') as dst1, converted.open(copy.copy(info), 'w') as dst2:
            while block := inp.read(1024*1024): dst1.write(block); dst2.write(block)
    else: raise ValueError('both replacement payloads are required')

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--obb', type=Path, required=True); p.add_argument('--steam-data', type=Path, required=True)
    p.add_argument('--platform', choices=('linux','windows'), required=True); p.add_argument('--output', type=Path, required=True)
    p.add_argument('--small-fragments', action='store_true', help='experimentally match exact fragments as small as 12 bytes')
    a=p.parse_args(); obb=a.obb.resolve(strict=True); steam=a.steam_data.resolve(strict=True); out=a.output.absolute()
    if not obb.is_file() or not steam.is_dir() or out.exists() or out.is_symlink() or not out.parent.is_dir() or out.resolve().is_relative_to(ROOT) or obb.is_relative_to(out.resolve()) or steam.is_relative_to(out.resolve()) or out.resolve().is_relative_to(steam):
        p.error('output must be new and separate from inputs and checkout')
    spec=importlib.util.spec_from_file_location('cabplan',ROOT/'tools/experimental-localization-cab-plan.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    sources=json.loads(INPUTS.read_text())['platforms'][a.platform]
    folder='StandaloneLinux64' if a.platform=='linux' else 'StandaloneWindows64'; prefix='StreamingAssets/aa/'+folder+'/'
    selected=sorted(x for x in sources if x.startswith(prefix) and PATTERN.fullmatch(x[len(prefix):]))
    if len(selected)!=18: raise ValueError('unexpected localization source inventory')
    out.mkdir(mode=0o700)
    try:
        rows=[]; total=0
        with tempfile.TemporaryDirectory(dir=out) as temp, zipfile.ZipFile(obb) as archive, zipfile.ZipFile(out/'template.obb','x') as masked, zipfile.ZipFile(out/'converted.obb','x') as converted:
            infos=archive.infolist();names=[i.filename for i in infos]
            if len(names)!=len(set(names)): raise ValueError('duplicate OBB entries')
            mappings={'assets/aa/Android/'+x[len(prefix):]:x for x in selected}
            if not mappings.keys() <= set(names):raise ValueError('missing Android localization bundle')
            for info in infos:
                if info.is_dir():continue
                if info.file_size<0 or info.file_size>1024*1024*1024: raise ValueError('OBB entry too large')
                source=mappings.get(info.filename)
                if source:
                    if info.file_size>32*1024*1024: raise ValueError('localization bundle too large')
                    raw=archive.read(info)
                    source_path=steam/source
                    if not source_path.is_file() or not source_path.resolve().is_relative_to(steam): raise ValueError('missing Steam localization bundle')
                    scratch=Path(temp)/'target.bundle';scratch.write_bytes(raw)
                    template, plan, target, count, removed=module.build(scratch,source_path,a.small_fragments)
                    rows.append({'entry':info.filename,'source':source,'plan_sha256':sha(plan),'plan':base64.b64encode(plan).decode('ascii')});total+=removed
                    write_pair(archive,masked,converted,info,template,target)
                else:
                    write_pair(archive,masked,converted,info)
        with (out/'template.obb').open('rb') as stream:
            template_hash=hashlib.file_digest(stream,'sha256').hexdigest()
        plan={'schema':1,'kind':'experimental-localization-cab-obb','platform':a.platform,'template_sha256':template_hash,'records':rows}
        (out/'plan.json').write_text(json.dumps(plan,separators=(',',':'))+'\n',encoding='utf-8')
        print(f'Private OBB stage: {len(rows)} bundles, {total} matching nonzero CAB bytes masked; other assets remain.')
    except BaseException:
        shutil.rmtree(out);raise

if __name__=='__main__': main()
