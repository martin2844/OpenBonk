#!/usr/bin/env python3
"""Build a pinned local OpenBonk diagnostic using tested research inputs.

LOCAL DIAGNOSTIC ONLY: this is not a public installer or a distributable APK.
It requires this workspace's unreviewed templates, one pinned Linux Steam build,
Unity's Android tools and a local debug key. It does not install to a device.
"""
import argparse,hashlib,os,shutil,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
PC_DEFAULT=Path.home()/'.local/share/Steam/steamapps/common/Megabonk/Megabonk_Data'
PAIR=ROOT/'build/android/share-source-experiment/shared-scene-mono-scrubbed-base'
SPLASH_PAIR=ROOT/'build/android/share-source-experiment/shared-splash-scrubbed-base'
UNITY=Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer'
PINS={
 'sharedassets0.assets':'01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a',
 'sharedassets0.assets.resS':'374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b',
 'sharedassets1.assets':'d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35',
 'sharedassets1.assets.resS':'43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea',
 'sharedassets1.resource':'28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c',
 'sharedassets2.assets':'e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da',
 'sharedassets2.assets.resS':'e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84',
 'sharedassets2.resource':'79f5dc7f17f979dc841accec3ed48744cfe78f787001f308b94632e2166503fa',
 'resources.assets':'a71923a420e792cd46c107eb8e607443ceb9357ef988e4efb73caafa3d35a366',
 'resources.assets.resS':'58b5f2965eb4a325f77fad08df75c036334e5625e58833fd02cf42fa8ee16d50',
 'level1':'fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551',
 'level1.resS':'bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6',
 'level2':'1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8',
 'level2.resS':'544f0a484be350056444c7776f157aab9da9f467df611ebdbf9a7f1da0181b51',
 'level4':'df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600',
}
PAIR_PINS={'base.apk':'1420da7195c23eb4dd14c073c6ab4b1d9a0d9569de105b7d8e9355619e3bf89a','base.obb':'badfdfa5d894dfd65be29820948528178a3d447c90bc3b49fda9a1f10aa40c78'}
SPLASH_PAIR_PINS={'base.apk':'5480eef6ed8a76aea6575881ddc2aef746dcf74587109f71d8ced0947f1cf020','base.obb':'badfdfa5d894dfd65be29820948528178a3d447c90bc3b49fda9a1f10aa40c78'}
SPLASH_PC_PINS={'globalgamemanagers.assets':'b3f7db2d847edd89f66e387a9bbc926b5c555314a23ca55a0531cf662c89bbe0','globalgamemanagers.assets.resS':'39dd38fcd8dd95d41d6d55fe16a448d29fe2c31d9a162c825c24c6d99a893d7a'}
SPLASH_TEMPLATE=ROOT/'build/android/share-source-experiment/unity-splash-template'
SPLASH_TEMPLATE_PINS={'globalgamemanagers.assets':'2e652dbff59cf547e44753d7236d52ff8f0dca03fa51fc38f2ed5edc10e93620','manifest.json':'9842c73a85d73cc35da4f93d6ef03ae5a6317fdd44d6dea41d801a7e63f3e9c6'}
FLAGS=['--experimental-terrain-animations','--experimental-shared1-textures','--experimental-shared1-four-textures','--experimental-water-rgba','--experimental-font-atlases','--experimental-font-metadata','--experimental-default-cubemap','--experimental-remaining-textures','--experimental-shared2-atlases','--experimental-shared2-audio','--experimental-shared2-textures','--experimental-shared2-meshes','--experimental-hashed-meshes','--experimental-hashed-animations','--experimental-hashed-textures','--experimental-hashed-matched','--experimental-hashed-rgba','--experimental-hashed-extra','--experimental-hashed-final','--experimental-hashed-audio','--experimental-scene-particles','--experimental-scene-references','--experimental-scene-mono','--include-newtonsoft-notices']
# Local Nix runtime dependencies for the workspace's current Python environment.
# Do not overwrite the user's search path. Other systems can supply their own.
LOCAL_LIBS=[Path('/nix/store/wgw25d181h0qks7qsfp1bh7c5nd3f0s7-zlib-1.3.2/lib'),Path('/nix/store/0ba5kj6r0zhfwd84l5vzccqvx7amlq2c-gcc-15.3.0-lib/lib')]
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
 return h.hexdigest()
def check_pins(root,pins,errors,label):
 for name,expected in pins.items():
  p=root/name
  try:
   if not p.is_file():errors.append(f'{label}: falta {p}')
   elif sha(p)!=expected:errors.append(f'{label}: versión o archivo incompatible: {p}')
  except OSError as exc:errors.append(f'{label}: no se puede leer {p}: {exc.strerror}')
def inside(a,b):
 try:a.relative_to(b);return True
 except ValueError:return False
p=argparse.ArgumentParser(description=__doc__,formatter_class=argparse.RawDescriptionHelpFormatter)
p.add_argument('--pc-data',type=Path,default=PC_DEFAULT,help='Megabonk_Data directory from the compatible Steam Linux installation')
p.add_argument('--output',type=Path,default=ROOT/'build/android/OpenBonk-personal-diagnostic',help='new output directory; never overwritten')
p.add_argument('--unity-android',type=Path,default=UNITY,help='Unity AndroidPlayer directory containing SDK and OpenJDK')
p.add_argument('--debug-keystore',type=Path,default=Path.home()/'.android/debug.keystore',help='existing Android debug keystore')
p.add_argument('--runner',default='steam-run',help='wrapper for Android tools, or none for direct execution')
p.add_argument('--check',action='store_true',help='check pinned Steam files and local tools without building')
p.add_argument('--experimental-unity-splash',action='store_true',help='use the splash-scrubbed research base and reconstruct the splash texture locally')
a=p.parse_args();pc=a.pc_data.expanduser().resolve();out=a.output.expanduser().resolve();unity=a.unity_android.expanduser().resolve();keystore=a.debug_keystore.expanduser().resolve();errors=[]
if out.exists():errors.append(f'la salida ya existe: {out}')
if not out.parent.is_dir():errors.append(f'no existe la carpeta padre de salida: {out.parent}')
if inside(out,pc) or inside(pc,out):errors.append('la salida debe estar separada de la instalación de Steam')
check_pins(pc,PINS,errors,'Steam');check_pins(SPLASH_PAIR if a.experimental_unity_splash else PAIR,SPLASH_PAIR_PINS if a.experimental_unity_splash else PAIR_PINS,errors,'plantilla local')
if a.experimental_unity_splash:
 check_pins(pc,SPLASH_PC_PINS,errors,'Steam splash')
 check_pins(SPLASH_TEMPLATE,SPLASH_TEMPLATE_PINS,errors,'plantilla splash')
for f in (ROOT/'tools/build-local-diagnostic.py',ROOT/'build/tooling/unpack-unity-crunch-cubemap-unity',unity/'SDK/build-tools/34.0.0/zipalign',unity/'SDK/build-tools/34.0.0/apksigner',unity/'OpenJDK/bin/java',keystore):
 if not f.is_file():errors.append(f'falta herramienta o archivo local: {f}')
if a.runner!='none' and not shutil.which(a.runner):errors.append(f'falta el ejecutor {a.runner} en PATH')
env=os.environ.copy();libs=[str(x) for x in LOCAL_LIBS if x.is_dir()]
if env.get('LD_LIBRARY_PATH'):libs.append(env['LD_LIBRARY_PATH'])
if libs:env['LD_LIBRARY_PATH']=os.pathsep.join(libs)
probe=subprocess.run([sys.executable,'-c','import PIL.Image; import UnityPy'],env=env,capture_output=True,text=True)
if probe.returncode:errors.append('el Python actual no puede cargar Pillow y UnityPy; revisá el entorno y LD_LIBRARY_PATH: '+(probe.stderr.strip().splitlines()[-1] if probe.stderr.strip() else 'error desconocido'))
if errors:
 for e in errors:print('ERROR:',e,file=sys.stderr)
 raise SystemExit(2)
print(f'{len(PINS) + (len(SPLASH_PC_PINS) if a.experimental_unity_splash else 0)} archivos fijados de Steam verificados; herramientas locales encontradas.',flush=True)
print('Diagnóstico local: contiene datos y código sin revisar; no distribuir.',flush=True)
if a.check:
 print('Prevalidación limitada: las demás plantillas y datos se comprueban durante la construcción.',flush=True)
 raise SystemExit(0)
cmd=[sys.executable,str(ROOT/'tools/build-local-diagnostic.py'),str(pc),str(out),*FLAGS,*(['--experimental-unity-splash'] if a.experimental_unity_splash else []),'--scrubbed-scene-mono-pair',str(SPLASH_PAIR if a.experimental_unity_splash else PAIR),'--build-tools',str(unity/'SDK/build-tools/34.0.0'),'--java-home',str(unity/'OpenJDK'),'--debug-keystore',str(keystore),'--runner',a.runner]
result=subprocess.run(cmd,env=env)
if result.returncode:raise SystemExit(result.returncode)
print('Resultado:',out,flush=True)
print('No se instaló ni modificó ningún dispositivo.',flush=True)
