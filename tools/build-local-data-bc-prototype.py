#!/usr/bin/env python3
"""Build one pinned, experimental assets/resource pair from local Steam data.

Research only. The supplied template still contains game content and is not a
distributable artifact. This is not a complete OpenBonk installer.
"""
import argparse
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('pc_data',type=Path)
parser.add_argument('template_dir',type=Path)
parser.add_argument('direct_manifest',type=Path)
parser.add_argument('rgba_manifest',type=Path)
parser.add_argument('etc_manifest',type=Path)
parser.add_argument('bc_manifest',type=Path)
parser.add_argument('output_dir',type=Path)
args=parser.parse_args()
if args.output_dir.exists(): parser.error('output already exists')
if not args.output_dir.parent.is_dir(): parser.error('output parent must already exist')
scripts=Path(__file__).resolve().parent.parent/'analysis/data-conversion'
with tempfile.TemporaryDirectory(prefix='.openbonk-local-build-',dir=args.output_dir.parent) as tmp:
    outline_template=Path(tmp)/'outline-template'
    texture_template=Path(tmp)/'texture-template'
    stage=Path(tmp)/'data'
    subprocess.run([sys.executable,str(scripts/'rebuild-outline-bc-prototype.py'),'restore',str(args.pc_data),
                    str(args.template_dir),str(outline_template)],check=True)
    subprocess.run([sys.executable,str(scripts/'cross-scene-mesh-bc-staged-prototype.py'),'restore',str(args.pc_data),
                    str(outline_template),str(texture_template)],check=True)
    subprocess.run([sys.executable,str(scripts/'combined-texture-bc-outline-prototype.py'),'restore',str(args.pc_data),
                    str(texture_template),str(args.direct_manifest),str(args.rgba_manifest),str(args.etc_manifest),str(args.bc_manifest),str(stage)],check=True)
    subprocess.run([sys.executable,str(scripts/'build-pc-audio-resource-prototype.py'),str(args.pc_data),
                    str(texture_template/'sharedassets1.assets'),str(stage/'sharedassets1.resource')],check=True)
    args.output_dir.mkdir()
    try:
        for name in ('sharedassets1.assets','sharedassets1.resource'):
            shutil.move(str(stage/name),str(args.output_dir/name))
    except Exception:
        shutil.rmtree(args.output_dir)
        raise
print('Experimental data pair created:',args.output_dir)
print('This is not a complete installer or a package cleared for distribution.')
