#!/usr/bin/env python3
"""Compile and verify pinned Unity package shaders for local diagnostics.

This is not a distributable package; Unity and third-party terms require review.
"""
import argparse,hashlib,importlib.util,json,os,shutil,signal,subprocess,sys,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];PROJECT=ROOT/'unity/OpenBonk'
EDITOR=Path(os.environ.get('OPENBONK_UNITY_EDITOR',str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser()/'Unity'
RUNNER=os.environ.get('OPENBONK_TOOL_RUNNER','steam-run' if shutil.which('steam-run') else 'none')
PREFIX=[] if RUNNER=='none' else [RUNNER]
SOURCE=ROOT/'analysis/port-inventory/OpenBonkComputeStudy.cs';TEMP_SCRIPT=PROJECT/'Assets/Editor/OpenBonkComputeStudy.cs';META=Path(str(TEMP_SCRIPT)+'.meta')
MANIFEST_SHA='ad767897929ffb988fd841832d320fe0a6136cd9c6d668fdea045c4f1124c018'
RENDER_MANIFEST_SHA='ce07ca801c3107d55d5058d605a11c081a8e5a6ca151ff970663181d3d1dc5ad'
FOG_MANIFEST_SHA='f5c6d801ef3bc40de5bfe709a07bf8f179dbc3f50715fc0db46953d1b6461390'
LOCAL_MANIFEST_SHA='8c17255c338cdfcc5abe02f4d23c46edd498a4800802f57cad200cd05e25f0c8'
REFERENCE_MANIFEST_SHA='28e11a2f01e8cc56475e12507b1747db61ac0b99ad4339b3428340d243e81a74'
INSTANCING_MANIFEST_SHA='856e2b6801c7a45483e80c296cf3e439c9ce8ba062339914c4161c558214d241'
INSTANCING_REFERENCE_SHA='a45ad32e40a0e84788818dd20c21f06c56a93fe95b27d3572fe95980e39998df'
FEATURE_MANIFEST_SHA='68937ba74996405d19dd5ac3a9b2bf977c989e6f561706f4d4bf40d931ee7f22'
BUILTIN_MANIFEST_SHA='c46fce9e463d02580c4fe748116f3761e1851e8fba5306a32e6bc876181dc108'
FEATURE_TEMP=PROJECT/'Assets/OpenBonkShaderFeatureProbeTemp'
BUILTIN_TEMP=PROJECT/'Assets/OpenBonkBuiltinProbeTemp'
GRAPHICS=PROJECT/'ProjectSettings/GraphicsSettings.asset'
def sha(path):
 with path.open('rb') as f:return hashlib.file_digest(f,'sha256').hexdigest()
def unity_in_use():
 for proc in Path('/proc').iterdir():
  if not proc.name.isdigit() or int(proc.name)==os.getpid():continue
  try:
   args=(proc/'cmdline').read_bytes().split(b'\0')
   if (any(os.path.realpath(os.fsdecode(arg))==str(EDITOR.resolve()) for arg in args if arg)
       and str(PROJECT).encode() in args):return True
  except (OSError,PermissionError):pass
 return False

def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('manifest',type=Path);p.add_argument('output',type=Path);p.add_argument('--render-manifest',type=Path);p.add_argument('--fog-manifest',type=Path);p.add_argument('--local-manifest',type=Path);p.add_argument('--reference-manifest',type=Path);p.add_argument('--instancing-manifest',type=Path);p.add_argument('--instancing-reference-manifest',type=Path);p.add_argument('--feature-manifest',type=Path);p.add_argument('--builtin-manifest',type=Path);a=p.parse_args();out=a.output.absolute()
 if out.exists() or out.is_symlink() or not out.parent.is_dir():p.error('output must be new')
 for src in (PROJECT,a.manifest,SOURCE):
  src=src.resolve()
  if out.resolve().is_relative_to(src) or src.is_relative_to(out.resolve()):p.error('output must be separate from inputs')
 if not EDITOR.is_file() or not SOURCE.is_file() or (PREFIX and not shutil.which(RUNNER)):p.error('Unity, source or configured runner missing')
 if TEMP_SCRIPT.exists() or TEMP_SCRIPT.is_symlink() or META.exists() or META.is_symlink():p.error('temporary editor script already exists')
 if FEATURE_TEMP.exists() or FEATURE_TEMP.is_symlink() or Path(str(FEATURE_TEMP)+'.meta').exists() or Path(str(FEATURE_TEMP)+'.meta').is_symlink():p.error('temporary feature material folder already exists')
 if BUILTIN_TEMP.exists() or BUILTIN_TEMP.is_symlink() or Path(str(BUILTIN_TEMP)+'.meta').exists() or Path(str(BUILTIN_TEMP)+'.meta').is_symlink():p.error('temporary built-in material folder already exists')
 if unity_in_use():p.error('Unity is already running this project')
 if not a.manifest.is_file() or sha(a.manifest)!=MANIFEST_SHA:p.error('incompatible shader manifest')
 if a.render_manifest and (not a.render_manifest.is_file() or sha(a.render_manifest)!=RENDER_MANIFEST_SHA):p.error('incompatible render shader manifest')
 if a.fog_manifest and (not a.fog_manifest.is_file() or sha(a.fog_manifest)!=FOG_MANIFEST_SHA):p.error('incompatible fog shader manifest')
 if a.local_manifest and (not a.local_manifest.is_file() or sha(a.local_manifest)!=LOCAL_MANIFEST_SHA):p.error('incompatible local shader manifest')
 if a.reference_manifest and (not a.reference_manifest.is_file() or sha(a.reference_manifest)!=REFERENCE_MANIFEST_SHA):p.error('incompatible reference shader manifest')
 if a.instancing_manifest and (not a.instancing_manifest.is_file() or sha(a.instancing_manifest)!=INSTANCING_MANIFEST_SHA):p.error('incompatible instancing shader manifest')
 if a.instancing_reference_manifest and (not a.instancing_reference_manifest.is_file() or sha(a.instancing_reference_manifest)!=INSTANCING_REFERENCE_SHA):p.error('incompatible instancing reference manifest')
 if a.feature_manifest and (not a.feature_manifest.is_file() or sha(a.feature_manifest)!=FEATURE_MANIFEST_SHA):p.error('incompatible feature shader manifest')
 if a.builtin_manifest and (not a.builtin_manifest.is_file() or sha(a.builtin_manifest)!=BUILTIN_MANIFEST_SHA):p.error('incompatible built-in shader manifest')
 manifest=json.loads(a.manifest.read_text());expected={row['name']:row for row in manifest['objects']}
 if len(expected)!=12:p.error('invalid shader inventory')
 source_bytes=SOURCE.read_bytes();graphics_before=GRAPHICS.read_bytes();created=False
 try:
  fd=os.open(TEMP_SCRIPT,os.O_WRONLY|os.O_CREAT|os.O_EXCL,0o600);created=True
  with os.fdopen(fd,'wb') as f:f.write(source_bytes)
  with tempfile.TemporaryDirectory(prefix='.openbonk-compute-',dir=out.parent) as tmp:
   tmp=Path(tmp);bundles=tmp/'bundles';bundles.mkdir();log=tmp/'unity.log';env=os.environ.copy();env['OPENBONK_COMPUTE_STUDY_OUT']=str(bundles)
   nix_xml=Path('/nix/store/5f5fnvixyi4zlkp122ypzkmjnvphi4bs-libxml2-2.13.9/lib')
   if nix_xml.is_dir():env['LD_LIBRARY_PATH']=str(nix_xml)+(':'+env['LD_LIBRARY_PATH'] if env.get('LD_LIBRARY_PATH') else '')
   cmd=PREFIX+[str(EDITOR),'-batchmode','-nographics','-quit','-projectPath',str(PROJECT),'-executeMethod','OpenBonkComputeStudy.Build','-logFile',str(log)]
   proc=subprocess.Popen(cmd,env=env,start_new_session=True)
   try:code=proc.wait()
   except BaseException:
    if proc.poll() is None:
     try:os.killpg(proc.pid,signal.SIGTERM)
     except ProcessLookupError:pass
     proc.wait()
    raise
   text=log.read_text(errors='replace') if log.exists() else ''
   if code or 'OPENBONK_COMPUTE_STUDY_OK 12' not in text or 'OPENBONK_RENDER_STUDY_OK 24' not in text or 'OPENBONK_LOCAL_STUDY_OK 17' not in text or 'OPENBONK_INSTANCE_STUDY_OK 17' not in text or 'OPENBONK_SHADER_FEATURE_PROBE_OK' not in text or 'OPENBONK_BUILTIN_PROBE_OK' not in text or 'error CS' in text:
    errors=[line for line in text.splitlines() if 'error CS' in line or 'Exception:' in line]
    raise ValueError('Unity shader compilation failed: '+'\n'.join(errors[:20] or text.splitlines()[-25:]))
   if GRAPHICS.read_bytes()!=graphics_before:raise ValueError('Unity did not restore graphics settings')
   # Match serialized shader objects, not bundle wrapper bytes.
   spec=importlib.util.spec_from_file_location('shader_template',ROOT/'analysis/data-conversion/shared1-compute-shaders-template.py');module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
   actual=module.sources(bundles)
   if set(actual)!=set(expected):raise ValueError('generated shader inventory differs')
   for name,data in actual.items():
    if len(data)!=expected[name]['size'] or hashlib.sha256(data).hexdigest()!=expected[name]['sha256']:raise ValueError('generated shader differs: '+name)
   if a.render_manifest:
    render_expected={row['name']:row for row in json.loads(a.render_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('render_template',ROOT/'analysis/data-conversion/shared1-render-shaders-template.py');render=importlib.util.module_from_spec(spec);spec.loader.exec_module(render)
    render_actual=render.sources(bundles)
    if len(render_expected)!=21 or set(render_actual)!=set(render_expected):raise ValueError('generated render shader inventory differs')
    for name,data in render_actual.items():
     if len(data)!=render_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=render_expected[name]['sha256']:raise ValueError('generated render shader differs: '+name)
   if a.fog_manifest:
    fog_expected={row['name']:row for row in json.loads(a.fog_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('fog_template',ROOT/'analysis/data-conversion/shared1-fog-shaders-template.py');fog=importlib.util.module_from_spec(spec);spec.loader.exec_module(fog)
    fog_actual=fog.sources(bundles)
    if len(fog_expected)!=3 or set(fog_actual)!=set(fog_expected):raise ValueError('generated fog shader inventory differs')
    for name,data in fog_actual.items():
     if len(data)!=fog_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=fog_expected[name]['sha256']:raise ValueError('generated fog shader differs: '+name)
   if a.local_manifest:
    local_expected={row['name']:row for row in json.loads(a.local_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('local_template',ROOT/'analysis/data-conversion/shared1-local-shaders-template.py');local=importlib.util.module_from_spec(spec);spec.loader.exec_module(local)
    local_actual=local.sources(bundles)
    if len(local_expected)!=9 or set(local_actual)!=set(local_expected):raise ValueError('generated local shader inventory differs')
    for name,data in local_actual.items():
     if len(data)!=local_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=local_expected[name]['sha256']:raise ValueError('generated local shader differs: '+name)
   if a.reference_manifest:
    ref_expected={row['name']:row for row in json.loads(a.reference_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('reference_template',ROOT/'analysis/data-conversion/shared1-reference-shaders-template.py');reference=importlib.util.module_from_spec(spec);spec.loader.exec_module(reference)
    ref_actual=reference.sources(bundles)
    if len(ref_expected)!=5 or set(ref_actual)!=set(ref_expected):raise ValueError('generated reference shader inventory differs')
    for name,(data,tree) in ref_actual.items():
     if len(data)!=ref_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=ref_expected[name]['generated_sha256']:raise ValueError('generated reference shader differs: '+name)
   if a.instancing_manifest:
    instance_expected={row['name']:row for row in json.loads(a.instancing_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('instance_template',ROOT/'analysis/data-conversion/shared1-instancing-shaders-template.py');instance=importlib.util.module_from_spec(spec);spec.loader.exec_module(instance)
    instance_actual=instance.sources(bundles)
    if len(instance_expected)!=2 or set(instance_actual)!=set(instance_expected):raise ValueError('generated instancing shader inventory differs')
    for name,data in instance_actual.items():
     if len(data)!=instance_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=instance_expected[name]['sha256']:raise ValueError('generated instancing shader differs: '+name)
   if a.instancing_reference_manifest:
    instance_ref_expected={row['name']:row for row in json.loads(a.instancing_reference_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('instance_ref_template',ROOT/'analysis/data-conversion/shared1-instancing-reference-template.py');instance_ref=importlib.util.module_from_spec(spec);spec.loader.exec_module(instance_ref)
    instance_ref_actual=instance_ref.sources(bundles)
    if len(instance_ref_expected)!=1 or set(instance_ref_actual)!=set(instance_ref_expected):raise ValueError('generated instancing reference inventory differs')
    for name,(data,tree) in instance_ref_actual.items():
     if len(data)!=instance_ref_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=instance_ref_expected[name]['generated_sha256']:raise ValueError('generated instancing reference differs: '+name)
   if a.feature_manifest:
    feature_expected={row['name']:row for row in json.loads(a.feature_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('feature_template',ROOT/'analysis/data-conversion/shared1-feature-shaders-template.py');feature=importlib.util.module_from_spec(spec);spec.loader.exec_module(feature)
    feature_actual=feature.sources(bundles)
    if len(feature_expected)!=2 or set(feature_actual)!=set(feature_expected):raise ValueError('generated feature shader inventory differs')
    for name,data in feature_actual.items():
     if len(data)!=feature_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=feature_expected[name]['sha256']:raise ValueError('generated feature shader differs: '+name)
   if a.builtin_manifest:
    builtin_expected={row['name']:row for row in json.loads(a.builtin_manifest.read_text())['objects']}
    spec=importlib.util.spec_from_file_location('builtin_template',ROOT/'analysis/data-conversion/builtin-shaders-template.py');builtin=importlib.util.module_from_spec(spec);spec.loader.exec_module(builtin)
    builtin_actual=builtin.sources(bundles)
    if len(builtin_expected)!=3 or set(builtin_actual)!=set(builtin_expected):raise ValueError('generated built-in shader inventory differs')
    for name,data in builtin_actual.items():
     if len(data)!=builtin_expected[name]['size'] or hashlib.sha256(data).hexdigest()!=builtin_expected[name]['sha256']:raise ValueError('generated built-in shader differs: '+name)
   out.mkdir(mode=0o700)
   try:
    for name in actual:shutil.copyfile(bundles/name.lower(),out/name.lower())
    shutil.copyfile(bundles/'postprocess-study',out/'postprocess-study')
    shutil.copyfile(bundles/'localshader-study',out/'localshader-study')
    shutil.copyfile(bundles/'localshader-instance',out/'localshader-instance')
    if a.feature_manifest:shutil.copyfile(bundles/'feature-probe',out/'feature-probe')
    if a.builtin_manifest:shutil.copyfile(bundles/'builtin-probe',out/'builtin-probe')
    shutil.copyfile(log,out/'unity.log')
    (out/'report.json').write_text(json.dumps({'kind':'local research only, not for distribution','shaders':len(actual),'manifest_sha256':MANIFEST_SHA},indent=2)+'\n')
   except BaseException:shutil.rmtree(out);raise
 finally:
  if created:
   if TEMP_SCRIPT.is_file() and TEMP_SCRIPT.read_bytes()==source_bytes:
    TEMP_SCRIPT.unlink();META.unlink(missing_ok=True)
   else:print('Temporary editor script changed; retained with metadata:',TEMP_SCRIPT,file=sys.stderr)
  if GRAPHICS.read_bytes()!=graphics_before:
   with tempfile.NamedTemporaryFile(prefix='openbonk-graphics-recovery-',suffix='.asset',dir=out.parent,delete=False) as f:
    f.write(graphics_before);recovery=f.name
   raise RuntimeError('Graphics settings changed; original saved for recovery: '+recovery)
 print('Verified 12 Unity compute shaders; local diagnostic only.')
if __name__=='__main__':main()
