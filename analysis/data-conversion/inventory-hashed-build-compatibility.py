#!/usr/bin/env python3
"""Check byte compatibility of previously reconstructed hashed OBB entries.

Equal archive entry bytes do not establish provenance, visual fidelity, or
compatibility with other changed scene and code references.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

ROOT = Path(__file__).resolve().parents[2]
MANIFESTS = {'meshes':('hashed-mesh-template','meshes'), 'animations':('hashed-animation-template','animations'), 'exact_textures':('hashed-exact-textures-template','textures'), 'matched_textures':('hashed-matched53-template','textures'), 'rgba_textures':('hashed-excluded-rgba-template','textures'), 'extra_textures':('hashed-extra-candidates-template','textures'), 'final_textures':('hashed-final-candidates-template','textures'), 'audio':('hashed-audio-template','clips')}


def digest(data): return hashlib.sha256(data).hexdigest()


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('old',type=Path);p.add_argument('converted',type=Path);p.add_argument('new',type=Path);p.add_argument('report',type=Path)
    a=p.parse_args(); out={}
    with zipfile.ZipFile(a.old) as old,zipfile.ZipFile(a.converted) as converted,zipfile.ZipFile(a.new) as new:
        for z in (old,converted,new):
            if len(z.namelist())!=len(set(z.namelist())):raise ValueError('duplicate archive entry')
        for category,(folder,key) in MANIFESTS.items():
            manifest=json.loads((ROOT/'build/android/share-source-experiment'/folder/'manifest.json').read_text());records=[]
            for item in manifest[key]:
                name=item['file']; entries=['assets/bin/Data/'+name]+(['assets/bin/Data/'+name+'.resource'] if category=='audio' else [])
                d=[]
                for entry in entries:
                    if any(entry not in z.namelist() for z in (old,converted,new)):
                        d.append({'entry':entry,'status':'missing'});continue
                    od,cd,nd=(z.read(entry) for z in (old,converted,new))
                    d.append({'entry':entry,'old_sha256':digest(od),'converted_sha256':digest(cd),'new_sha256':digest(nd),'status':'identical_base' if od==nd else 'different_base','converted_changed':od!=cd})
                records.append({'file':name,'name':item.get('name',item.get('target_name','')),'eligible':all(x['status']=='identical_base' for x in d) and any(x.get('converted_changed') for x in d),'entries':d})
            out[category]={'total':len(records),'eligible':sum(x['eligible'] for x in records),'records':records}
    with a.report.open('x') as f:json.dump({'kind':'local research only; not proof of redistribution rights','categories':out},f,indent=2);f.write('\n')
    for category,info in out.items():print(category,info['eligible'],'of',info['total'],'eligible')


if __name__=='__main__':main()
