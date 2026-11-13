#!/usr/bin/env python3
"""Build a personal local diagnostic APK/OBB from one pinned Steam build.

NOT FOR DISTRIBUTION: inputs, templates and outputs retain original game data
and reconstructed/third-party code. This uses an experimental DEX patch and a
local Android debug key, not the supported release build system. It does not
install anything or modify the Steam installation or an Android device.
"""
import argparse,hashlib,json,os,shutil,subprocess,sys,tempfile,zipfile
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
MANIFEST_ROOT=Path(os.environ.get('OPENBONK_MANIFEST_ROOT',str(ROOT/'build/android/share-source-experiment'))).expanduser()
BASE_APK='850e0a2901138a42bfe58167b4cbf6fd23c47d30734cf54594783c0acccee4e5'
SCRUBBED_BASE_APK='980c03b141a4d8dda5ff230d1bc880c4dbd2d5cad074899471813f6f30487581'
SCRUBBED_PAIR_APK='a2084de33d1522ace03f50c4d3e061bff4d5440f938ba7e16185fead4821ca1c'
SCRUBBED_PAIR_OBB='bade593b27a5ee3c624b2e9c9efef6f26bbe5bc4ac0c2b4d367c4cfae89b3017'
SCRUBBED_MESH_PAIR_APK='846a0477610bfc37fc83221135f67f30de7da701ecf7fefb29eca3118255308c'
SCRUBBED_MESH_PAIR_OBB='bad289bad4f106d314937a6a0a4e5790a0b0d608aece4f00416ac42168012e54'
HASHED_MESH_MANIFEST='44e200f8357cc8204e6d140828c4f6db05aaa1706501667d5b10ade321522b24'
SCRUBBED_ANIMATION_PAIR_APK='baf764c22cfbe47afaac34d97bc04c607b1329b8c8ecba076e65783c73f90981'
SCRUBBED_ANIMATION_PAIR_OBB='baf98c2c7cc11067de8276c08febc76404f07854191260101e920de1a991bf72'
HASHED_ANIMATION_MANIFEST='d906d270777cde93a6fa64448cec89f6ab7900289fe8ed7c9509d5c69d6ea0c8'
SCRUBBED_TEXTURE_PAIR_APK='b1a9b321609d49a9bfb98ada0c0c93f2bf770e81d48bdd0f5d0f4a7f3297674b'
SCRUBBED_TEXTURE_PAIR_OBB='bad91e6c18e64cb90a72b27064f06288fd174ca16b3224f6a45b87e6c33f376a'
HASHED_TEXTURE_MANIFEST='72c9d1bbe93ea0921ac04dec8ed1536cb9ca84a48447fb2738b4aff15b3ebaa9'
SCRUBBED_MATCHED_PAIR_APK='bba867e6587933d6c87466a4d41251208c8e707b6462831a2a432f2a62845e12'
SCRUBBED_MATCHED_PAIR_OBB='badc3f3c4607aac1853a31aaeb864e33a1e365bbbb37f38e3c880313ddf8c02a'
HASHED_MATCHED_MANIFEST='7e4bd817c3138434082496ba797c5ef216798357b7f8ef7bc6ecf1fed02061a7'
SCRUBBED_RGBA_PAIR_APK='e7d9a8e81d7e3f8b4f84d1dea28f61aedad9e82b497412353737579830ca7b0c'
SCRUBBED_RGBA_PAIR_OBB='baeb4b96ce7e96a0f1584605a5d501419db3d91a7364c55affbe2f5d7edd4f16'
HASHED_RGBA_MANIFEST='828fde47398ad39c9a0a42d2afba85111dd577f704f3fd5740c4ddcb5e79c76d'
SCRUBBED_EXTRA_PAIR_APK='c5e017fc2ab63a40aa65b8bf873f1f0c4addc44e05d6822f27b9f8ffdbdbf455'
SCRUBBED_EXTRA_PAIR_OBB='baf149b8ab6154dce137c583dca1b7a93a6d08ce057e88529ecca5a0527cc9b5'
HASHED_EXTRA_MANIFEST='34e87d8502957407489faca56ae44db02c1ed7d7eed9b2363ffdb44f44504f44'
SCRUBBED_FINAL_PAIR_APK='55a390aa3f831a5f329c54e453c3c4d83a8a8e593e58a698d4f06cb3b3b0c58d'
SCRUBBED_FINAL_PAIR_OBB='bada4b92f779ed21d3288f17d14e9821c64a617750ddf7b64dd7fc3cf261cd60'
HASHED_FINAL_MANIFEST='e2d7f81ff5d42db9b8e698b063212e3abf0cffdcbe27d12737fa78739370e6cd'
SCRUBBED_HASHED_AUDIO_PAIR_APK='d40546c53cfff0fd319df66a7b46c24af50884cf3a351ab1a8090ab892f105e3'
SCRUBBED_HASHED_AUDIO_PAIR_OBB='bafbe7ba6be507da28e65e87f0ff41f603e76a8badc7d14d1ad3b48a5ec4ce7a'
HASHED_AUDIO_MANIFEST='6e52736c3ee8a19ae933d5e2b2515cb8e8f271be5ae100e39f91b0fa5024318f'
SCRUBBED_SCENE_PAIR_APK='53acd53e75ab5b04b9a1129289de7514b3eed575b2104c3d5edac646d90f49ed'
SCRUBBED_SCENE_PAIR_OBB='bada442aa38b729666292968dac5b7771eeb620e045782356d7a1cf3ea9b7176'
SCENE_PARTICLE_MANIFEST='733b1073fba84e1c157a36b53f1f0f9d221f1bd3ff7963bda5adeed385b4e6be'
SCRUBBED_SCENE_REFS_APK='418bf6f5f8bffebe0da9724d6dc2eda283709c32408b407d47ccdd13ae53769c'
SCRUBBED_SCENE_REFS_OBB='baf36d26d1ed49517c68ce02f92ceb12a29ea7971185ac3ac7db2182d8ade7fd'
SCENE_REFERENCES_MANIFEST='cdcb0e41e6f1875e24c6375280f16d6aef7f89cc94a1120e63db4af176d43af0'
SCRUBBED_SCENE_MONO_APK='1420da7195c23eb4dd14c073c6ab4b1d9a0d9569de105b7d8e9355619e3bf89a'
SCRUBBED_SCENE_MONO_OBB='badfdfa5d894dfd65be29820948528178a3d447c90bc3b49fda9a1f10aa40c78'
SCENE_MONO_MANIFEST='b93e59027761154fe04a5a4d619284dc9099dfffa2d6fd2e1828aee6a7f088f4'
BASE_OBB='baf787dff6b987ca68c34f9351dd7866d8c269865069d43e8a7435ace555895d'
DATA_ASSET='9715a5ee8c6e589ece19b7996e48fa839eb1cf537e29d3a10e403f466fb760cd'
DATA_ASSET_REMAINING='b17ab5d19b53a2c5a076e5d18175a16ddeda9b68cf947add6b901b160466cec3'
DATA_ASSET_ALL='cc947601ab5e1d65c06afda412260f36d2d22bc66a818f47ac5b94c7c2227e7b'
DATA_ASSET_WATER_RGBA='b10fd4de8a0da336f39dff3dddbab994499b229fa5b3f443ab8adbcc16b2082e'
DATA_AUDIO='93dab7eea9314cea3a2895c5b7dcd8287e527a8c1f21c7157179298769963ba6'
SHARED0='637c514a66dd6ece82805b1265fdf1ed4079aba7fc4fa79282f09eb2c2c194dc'
SHARED0_ATLASES='11128b129eba663083a100b3b7c708c4335e05b2e0e457fce71d963ffd87b2a0'
SHARED0_CUBEMAP_TEMPLATE='a36b7152ad38ba4634eba085af73c01e5366a3168f7a0a0f4e86cd09e0250203'
SHARED0_CONVERTED_TEXTURES='5e76282651d6e6f120d1362277aea74a68bfa482ea8737101092f027d5d374b8'
SHARED0_CUBEMAP_WITH_TEXTURES='7a45c75fe6ac3334ba1ca3feb1c8cc3c2eb41157ea2f7c0e27800b6717861f90'
SHARED2_ATLASES='b5d6ab07a5ae164453dee414bf892a525b68dd053c9aa08fd80b93f7a75b68cb'
SHARED2_AUDIO_ASSETS='af9ac35192083879374d917614c9e2163e4bcd2d23fc6c604359c05e5d56c010'
SHARED2_AUDIO='6abc02d2702824b936add3ea2a6780b5b10e2954e1df7543b8cc79b5c9e1b1bc'
SHARED2_MATCHED_ATLASES='a1b5c15ff723ca7477ee76503916813167d72361b6fd7db43cd71c9c8b02bbc4'
SHARED2_MATCHED_AUDIO='42a617421d3feaca2907addb8362f0613ca48d5675171a677f8b669576a502e2'
SHARED2_MATCHED_TEMPLATE='30cdfbeb91acec299954bdbe38502980afa4962dc073e349ca1bc2120d5182da'
PACKAGE='com.megabonk.nativeport.splittest'
UNITY_ANDROID=Path(os.environ.get('OPENBONK_UNITY_EDITOR',str(Path.home()/'Unity/Hub/Editor/2023.2.22f1/Editor'))).expanduser()/'Data/PlaybackEngines/AndroidPlayer'
NEWTONSOFT_NOTICE_DIR=ROOT/'unity/OpenBonk/Library/PackageCache/com.unity.nuget.newtonsoft-json@3.0.2'
NEWTONSOFT_NOTICES={'assets/ThirdPartyNotices/Unity-Newtonsoft-License.md':'a5177bfecfea04d225e4a26a631db86721a03b4080350867847e71dc26b44f85','assets/ThirdPartyNotices/Unity-Newtonsoft-Third-Party-Notices.md':'f07d2ea4af53fd1c9800b345bb4fbfa3ab0181d8c87625d6c265cd65142df097'}
def sha(path):
 h=hashlib.sha256()
 with path.open('rb') as f:
  for b in iter(lambda:f.read(1024*1024),b''):h.update(b)
 return h.hexdigest()
def require(path,expected):
 if not path.is_file() or sha(path)!=expected:raise ValueError(f'missing or incompatible input: {path}')
def inside(path,root):
 try:path.relative_to(root);return True
 except ValueError:return False
def run(argv,env=None):
 print('+', ' '.join(str(x) for x in argv),flush=True)
 subprocess.run([str(x) for x in argv],check=True,env=env)
def archive_checks(apk,obb,shared0_hash,data_hash,shared2_hash=None,shared2_audio_hash=None,mesh_manifest=None,animation_manifest=None,texture_manifest=None,matched_manifest=None,rgba_manifest=None,extra_manifest=None,final_manifest=None,hashed_audio_manifest=None,scene_particle_manifest=None,include_newtonsoft_notices=False):
 with zipfile.ZipFile(apk) as az,zipfile.ZipFile(obb) as oz:
  names=oz.namelist();apk_names=az.namelist()
  if len(names)!=len(set(names)) or len(apk_names)!=len(set(apk_names)):raise ValueError('duplicate archive entries')
  pieces=[x for x in names if x.startswith('assets/bin/Data/sharedassets1.assets.split')]
  try:pieces.sort(key=lambda x:int(x.rsplit('split',1)[1]))
  except ValueError:raise ValueError('invalid asset fragment names')
  if not pieces or [int(x.rsplit('split',1)[1]) for x in pieces]!=list(range(len(pieces))):raise ValueError('missing asset fragments')
  digest=hashlib.sha256()
  for name in pieces:
   with oz.open(name) as f:
    for block in iter(lambda:f.read(1024*1024),b''):digest.update(block)
  if digest.hexdigest()!=data_hash:raise ValueError('packaged assets differ')
  audio='assets/bin/Data/sharedassets1.resource';digest=hashlib.sha256()
  with oz.open(audio) as f:
   for block in iter(lambda:f.read(1024*1024),b''):digest.update(block)
  if digest.hexdigest()!=DATA_AUDIO:raise ValueError('packaged audio differs')
  moved=[n for n in names if n.startswith('assets/bin/Data/sharedassets0.assets.split')]
  if not moved or any(n in apk_names for n in moved):raise ValueError('sharedassets0 fragments not relocated')
  try:moved.sort(key=lambda x:int(x.rsplit('split',1)[1]))
  except ValueError:raise ValueError('invalid sharedassets0 fragment names')
  if [int(x.rsplit('split',1)[1]) for x in moved]!=list(range(len(moved))):raise ValueError('missing sharedassets0 fragments')
  digest=hashlib.sha256()
  for name in moved:
   with oz.open(name) as f:
    for block in iter(lambda:f.read(1024*1024),b''):digest.update(block)
  if digest.hexdigest()!=shared0_hash:raise ValueError('packaged sharedassets0 differs')
  if shared2_hash is not None:
   shared2=[n for n in names if n.startswith('assets/bin/Data/sharedassets2.assets.split')]
   try:shared2.sort(key=lambda x:int(x.rsplit('split',1)[1]))
   except ValueError:raise ValueError('invalid sharedassets2 fragment names')
   if not shared2 or [int(x.rsplit('split',1)[1]) for x in shared2]!=list(range(len(shared2))):raise ValueError('missing sharedassets2 fragments')
   digest=hashlib.sha256()
   for name in shared2:
    with oz.open(name) as f:
     for block in iter(lambda:f.read(1048576),b''):digest.update(block)
   if digest.hexdigest()!=shared2_hash:raise ValueError('packaged sharedassets2 differs')
  if shared2_audio_hash is not None:
   digest=hashlib.sha256()
   with oz.open('assets/bin/Data/sharedassets2.resource') as f:
    for block in iter(lambda:f.read(1048576),b''):digest.update(block)
   if digest.hexdigest()!=shared2_audio_hash:raise ValueError('packaged sharedassets2 audio differs')
  if mesh_manifest is not None:
   records=json.loads(mesh_manifest.read_text())['meshes']
   if len(records)!=50 or len({r['file'] for r in records})!=50:raise ValueError('invalid hashed mesh manifest')
   for r in records:
    name='assets/bin/Data/'+r['file']
    if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r['original_sha256']:raise ValueError('packaged hashed mesh differs')
  if animation_manifest is not None:
   records=json.loads(animation_manifest.read_text())['animations']
   if len(records)!=108 or len({r['file'] for r in records})!=108:raise ValueError('invalid hashed animation manifest')
   for r in records:
    name='assets/bin/Data/'+r['file']
    if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r['original_sha256']:raise ValueError('packaged hashed animation differs')
  if texture_manifest is not None:
   records=json.loads(texture_manifest.read_text())['textures']
   if len(records)!=56 or len({r['file'] for r in records})!=56:raise ValueError('invalid hashed texture manifest')
   for r in records:
    name='assets/bin/Data/'+r['file']
    if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r['original_sha256']:raise ValueError('packaged hashed texture differs')
  if matched_manifest is not None:
   records=json.loads(matched_manifest.read_text())['textures']
   if len(records)!=53 or len({r['file'] for r in records})!=53:raise ValueError('invalid matched texture manifest')
   for r in records:
    name='assets/bin/Data/'+r['file']
    if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r['converted_sha256']:raise ValueError('packaged matched texture differs')
  if rgba_manifest is not None:
   records=json.loads(rgba_manifest.read_text())['textures']
   if len(records)!=10 or len({r['file'] for r in records})!=10:raise ValueError('invalid RGBA texture manifest')
   for r in records:
    name='assets/bin/Data/'+r['file']
    if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r['converted_sha256']:raise ValueError('packaged RGBA texture differs')
  if extra_manifest is not None:
   records=json.loads(extra_manifest.read_text())['textures']
   if len(records)!=13 or len({r['file'] for r in records})!=13:raise ValueError('invalid extra texture manifest')
   for r in records:
    name='assets/bin/Data/'+r['file']
    if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r['converted_sha256']:raise ValueError('packaged extra texture differs')
  if final_manifest is not None:
   records=json.loads(final_manifest.read_text())['textures']
   if len(records)!=2 or len({r['file'] for r in records})!=2:raise ValueError('invalid final texture manifest')
   for r in records:
    name='assets/bin/Data/'+r['file']
    if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r['converted_sha256']:raise ValueError('packaged final texture differs')
  if hashed_audio_manifest is not None:
   records=json.loads(hashed_audio_manifest.read_text())['clips']
   if len(records)!=63 or len({r['file'] for r in records})!=63:raise ValueError('invalid hashed audio manifest')
   for r in records:
    for suffix,key in (('', 'asset_sha256'),('.resource', 'resource_sha256')):
     name='assets/bin/Data/'+r['file']+suffix
     if name not in names or hashlib.sha256(oz.read(name)).hexdigest()!=r[key]:raise ValueError('packaged hashed audio differs')
  if scene_particle_manifest is not None:
   records=json.loads(scene_particle_manifest.read_text())['scenes']
   if len(records)!=3 or {r['scene'] for r in records}!={'level1','level2','level4'}:raise ValueError('invalid scene particle manifest')
   for r in records:
    prefix='assets/bin/Data/'+r['scene']+'.split';parts=sorted((n for n in names if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
    if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))):raise ValueError('scene fragments missing')
    digest=hashlib.sha256()
    for name in parts:digest.update(oz.read(name))
    if digest.hexdigest()!=r['original_sha256']:raise ValueError('packaged scene differs')
  if 'assets/ThirdPartyNotices/Lua.txt' not in apk_names:raise ValueError('missing Lua notice')
  if include_newtonsoft_notices:
   for name,expected in NEWTONSOFT_NOTICES.items():
    if name not in apk_names or hashlib.sha256(az.read(name)).hexdigest()!=expected:raise ValueError('missing or altered Newtonsoft notice')
  obb_hash=sha(obb)
  if obb_hash.encode() not in az.read('classes.dex'):raise ValueError('APK does not reference packaged OBB hash')
 return len(pieces),len(moved),obb_hash
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('pc_data',type=Path);p.add_argument('output_dir',type=Path)
p.add_argument('--template',type=Path,default=MANIFEST_ROOT/'full-matched-stripped')
p.add_argument('--experimental-terrain-animations',action='store_true',help='reconstruct terrain and 12 animation objects from Steam')
p.add_argument('--windows-terrain-four',type=Path,help='experimentally source terrain, animations and 63 textures from Windows')
p.add_argument('--windows-core-assets',type=Path,help='experimentally source selected core textures, meshes, cubemaps and audio from Windows')
p.add_argument('--windows-hashed-assets',type=Path,help='experimentally source selected hashed assets from Windows')
p.add_argument('--windows-shared0',type=Path,help='experimentally source selected shared0 textures, cubemap and font assets from Windows')
p.add_argument('--windows-remaining-assets',type=Path,help='experimentally source the water normal from Windows')
p.add_argument('--terrain-animation-template',type=Path,default=MANIFEST_ROOT/'shared1-terrain-animation-template')
p.add_argument('--experimental-shared1-textures',action='store_true',help='also reconstruct 59 visually matched textures (requires experimental terrain and animations)')
p.add_argument('--shared1-texture-template',type=Path,default=MANIFEST_ROOT/'shared1-last-textures-template')
p.add_argument('--experimental-shared1-four-textures',action='store_true',help='also reconstruct four remaining textures (requires experimental shared1 textures)')
p.add_argument('--shared1-four-texture-template',type=Path,default=MANIFEST_ROOT/'shared1-four-textures-template')
p.add_argument('--experimental-water-rgba',action='store_true',help='use more accurate RGBA32 water normals (requires experimental shared1 four textures)')
p.add_argument('--experimental-shared2-atlases',action='store_true',help='reconstruct two font atlases from sharedassets2')
p.add_argument('--windows-shared2',type=Path,help='experimentally source shared2 textures, meshes, atlases and audio from Windows')
p.add_argument('--experimental-shared2-audio',action='store_true',help='retarget sharedassets2 audio from Steam (requires experimental shared2 atlases)')
p.add_argument('--experimental-shared2-textures',action='store_true',help='reconstruct 72 additional name-matched textures (requires experimental shared2 atlases)')
p.add_argument('--shared2-texture-template',type=Path,default=MANIFEST_ROOT/'shared2-matched72-template')
p.add_argument('--experimental-shared2-meshes',action='store_true',help='reconstruct 251 sharedassets2 mesh buffers (requires experimental shared2 textures)')
p.add_argument('--shared2-mesh-template',type=Path,default=MANIFEST_ROOT/'shared2-mesh-template')
p.add_argument('--shared2-atlas-template',type=Path,default=MANIFEST_ROOT/'shared2-atlases-template')
p.add_argument('--manifest-root',type=Path,default=MANIFEST_ROOT)
p.add_argument('--font-template',type=Path,default=MANIFEST_ROOT/'shared0-font-template')
p.add_argument('--experimental-font-atlases',action='store_true',help='also reconstruct 94 font atlases; lossy experimental conversion')
p.add_argument('--atlas-template',type=Path,default=MANIFEST_ROOT/'shared0-atlas-template')
p.add_argument('--experimental-font-metadata',action='store_true',help='also reconstruct serialized font metadata (requires --experimental-font-atlases)')
p.add_argument('--metadata-template',type=Path,default=MANIFEST_ROOT/'shared0-metadata-template')
p.add_argument('--experimental-default-cubemap',action='store_true',help='also reconstruct the default sky cubemap (requires experimental font metadata)')
p.add_argument('--cubemap-template',type=Path,default=MANIFEST_ROOT/'shared0-cubemap-template')
p.add_argument('--experimental-remaining-textures',action='store_true',help='also reconstruct Fox_Default and Emission (requires experimental default cubemap)')
p.add_argument('--remaining-textures-template',type=Path,default=MANIFEST_ROOT/'shared0-last-textures-template')
p.add_argument('--decoder',type=Path,default=Path(os.environ.get('OPENBONK_CRUNCH_DECODER',str(ROOT/'build/tooling/unpack-unity-crunch-cubemap-unity'))).expanduser())
p.add_argument('--base-apk',type=Path,default=ROOT/'build/android/Megabonk-arm64-split-test.apk')
p.add_argument('--scrubbed-base-apk',type=Path,help='use pinned local research template with sharedassets0 fragments zeroed')
p.add_argument('--scrubbed-base-pair',type=Path,help='use pinned local research APK/OBB with sharedassets0/1/2 payloads zeroed')
p.add_argument('--scrubbed-hashed-mesh-pair',type=Path,help='use pinned zeroed APK/OBB including hashed mesh buffers')
p.add_argument('--experimental-hashed-meshes',action='store_true',help='restore 50 hashed mesh buffers for the scrubbed hashed-mesh pair')
p.add_argument('--hashed-mesh-template',type=Path,default=MANIFEST_ROOT/'hashed-mesh-template')
p.add_argument('--scrubbed-hashed-animation-pair',type=Path,help='use pinned APK/OBB with hashed meshes and animations zeroed')
p.add_argument('--experimental-hashed-animations',action='store_true',help='restore 108 hashed animations with unreviewed Android-derived patches')
p.add_argument('--hashed-animation-template',type=Path,default=MANIFEST_ROOT/'hashed-animation-template')
p.add_argument('--hashed-animation-hybrid-manifest',type=Path,help='use a pinned local mixed Steam/Android animation manifest')
p.add_argument('--scrubbed-hashed-texture-pair',type=Path,help='use pinned APK/OBB with exact hashed textures, animations and meshes zeroed')
p.add_argument('--experimental-hashed-textures',action='store_true',help='restore 56 exact hashed textures from Steam')
p.add_argument('--hashed-texture-template',type=Path,default=MANIFEST_ROOT/'hashed-exact-textures-template')
p.add_argument('--scrubbed-hashed-matched-pair',type=Path,help='use pinned APK/OBB with matched hashed textures also zeroed')
p.add_argument('--experimental-hashed-matched',action='store_true',help='restore 53 lossy name-matched hashed textures from Steam')
p.add_argument('--hashed-matched-template',type=Path,default=MANIFEST_ROOT/'hashed-matched53-template')
p.add_argument('--scrubbed-hashed-rgba-pair',type=Path,help='use pinned APK/OBB with ten further texture payloads zeroed')
p.add_argument('--experimental-hashed-rgba',action='store_true',help='restore ten name-matched textures as uncompressed RGBA')
p.add_argument('--hashed-rgba-template',type=Path,default=MANIFEST_ROOT/'hashed-excluded-rgba-template')
p.add_argument('--scrubbed-hashed-extra-pair',type=Path,help='use pinned APK/OBB with thirteen additional texture candidates zeroed')
p.add_argument('--experimental-hashed-extra',action='store_true',help='restore thirteen additional texture candidates from Steam')
p.add_argument('--hashed-extra-template',type=Path,default=MANIFEST_ROOT/'hashed-extra-candidates-template')
p.add_argument('--scrubbed-hashed-final-pair',type=Path,help='use pinned APK/OBB with two further texture candidates zeroed')
p.add_argument('--experimental-hashed-final',action='store_true',help='restore the final two texture candidates from Steam')
p.add_argument('--hashed-final-template',type=Path,default=MANIFEST_ROOT/'hashed-final-candidates-template')
p.add_argument('--scrubbed-hashed-audio-pair',type=Path,help='use pinned APK/OBB with 63 audio streams zeroed')
p.add_argument('--experimental-hashed-audio',action='store_true',help='restore 63 name-matched audio candidates from Steam')
p.add_argument('--hashed-audio-template',type=Path,default=MANIFEST_ROOT/'hashed-audio-template')
p.add_argument('--scrubbed-scene-pair',type=Path,help='use pinned APK/OBB with 397 scene particle bodies zeroed')
p.add_argument('--experimental-scene-particles',action='store_true',help='restore 397 exact scene particle bodies from Steam')
p.add_argument('--scene-particle-template',type=Path,default=MANIFEST_ROOT/'scene-particle-bodies-template')
p.add_argument('--scrubbed-scene-refs-pair',type=Path,help='use pinned APK/OBB with 39 further particle bodies zeroed')
p.add_argument('--experimental-scene-references',action='store_true',help='restore 39 particle bodies preserving Android references')
p.add_argument('--scene-references-template',type=Path,default=MANIFEST_ROOT/'scene-particle-references-template')
p.add_argument('--scrubbed-scene-mono-pair',type=Path,help='use pinned APK/OBB with scene component bodies zeroed')
p.add_argument('--experimental-scene-mono',action='store_true',help='restore 3927 MonoBehaviour bodies retaining Android headers')
p.add_argument('--experimental-unity-splash',action='store_true',help='restore the Unity splash texture from pinned PC data; lossy local experiment')
p.add_argument('--unity-splash-template',type=Path,default=MANIFEST_ROOT/'unity-splash-template')
p.add_argument('--scene-mono-template',type=Path,default=MANIFEST_ROOT/'scene-monobehaviour-bodies-template')
p.add_argument('--base-obb',type=Path,default=ROOT/'build/android/Megabonk-arm64-split-test.main.obb')
p.add_argument('--build-tools',type=Path,default=Path(os.environ.get('OPENBONK_ANDROID_SDK_ROOT',str(UNITY_ANDROID/'SDK'))).expanduser()/'build-tools/34.0.0')
p.add_argument('--java-home',type=Path,default=Path(os.environ.get('OPENBONK_JAVA_HOME',str(UNITY_ANDROID/'OpenJDK'))).expanduser())
p.add_argument('--debug-keystore',type=Path,default=Path(os.environ.get('OPENBONK_DEBUG_KEYSTORE',str(Path.home()/'.android/debug.keystore'))).expanduser())
p.add_argument('--include-newtonsoft-notices',action='store_true',help='include two pinned local Unity Newtonsoft notice documents; not a complete notice set')
p.add_argument('--experimental-export-rebuilt-only',action='store_true',help='export selected reconstructed files without packaging an old base; local diagnostic only')
p.add_argument('--experimental-leave-font-metadata-blank',action='store_true',help='leave old font metadata blank for the semantic reconstruction path')
p.add_argument('--runner',default=os.environ.get('OPENBONK_TOOL_RUNNER','steam-run' if shutil.which('steam-run') else 'none'),help='executable wrapper for Android SDK tools; use none for no wrapper')
a=p.parse_args();out=a.output_dir.resolve();pc=a.pc_data.resolve()
if a.windows_shared2 and not all((a.experimental_shared2_atlases,a.experimental_shared2_audio,a.experimental_shared2_textures,a.experimental_shared2_meshes)):p.error('--windows-shared2 requires all four shared2 options')
if a.windows_shared2 and (not a.windows_shared2.is_dir() or out.is_relative_to(a.windows_shared2.resolve()) or a.windows_shared2.resolve().is_relative_to(out)):p.error('Windows data must exist and be separate from output')
if a.windows_terrain_four and not (a.experimental_terrain_animations and a.experimental_shared1_four_textures):p.error('--windows-terrain-four requires terrain animations and four textures')
if a.windows_terrain_four and (not a.windows_terrain_four.is_dir() or out.is_relative_to(a.windows_terrain_four.resolve()) or a.windows_terrain_four.resolve().is_relative_to(out)):p.error('Windows data must exist and be separate from output')
if a.windows_core_assets and (not a.windows_core_assets.is_dir() or out.is_relative_to(a.windows_core_assets.resolve()) or a.windows_core_assets.resolve().is_relative_to(out)):p.error('Windows data must exist and be separate from output')
if a.windows_hashed_assets and (not a.windows_hashed_assets.is_dir() or out.is_relative_to(a.windows_hashed_assets.resolve()) or a.windows_hashed_assets.resolve().is_relative_to(out)):p.error('Windows data must exist and be separate from output')
if a.windows_shared0 and (not a.windows_shared0.is_dir() or out.is_relative_to(a.windows_shared0.resolve()) or a.windows_shared0.resolve().is_relative_to(out)):p.error('Windows data must exist and be separate from output')
if a.windows_shared0 and not (a.experimental_font_atlases and a.experimental_font_metadata and a.experimental_default_cubemap and a.experimental_remaining_textures and a.experimental_leave_font_metadata_blank):p.error('--windows-shared0 requires the converted textures and blank font metadata mode')
if a.windows_remaining_assets and (not a.windows_remaining_assets.is_dir() or out.is_relative_to(a.windows_remaining_assets.resolve()) or a.windows_remaining_assets.resolve().is_relative_to(out)):p.error('Windows data must exist and be separate from output')
if a.windows_remaining_assets and not a.experimental_water_rgba:p.error('--windows-remaining-assets requires --experimental-water-rgba')
if a.hashed_animation_hybrid_manifest and (not a.experimental_export_rebuilt_only or not a.experimental_hashed_animations):p.error('--hashed-animation-hybrid-manifest requires rebuilt-only export and hashed animations')
if a.experimental_leave_font_metadata_blank and (not a.experimental_export_rebuilt_only or not a.experimental_remaining_textures):p.error('--experimental-leave-font-metadata-blank requires rebuilt-only export and remaining textures')
if a.experimental_export_rebuilt_only:
 required=('experimental_font_atlases','experimental_font_metadata','experimental_default_cubemap','experimental_remaining_textures','experimental_terrain_animations','experimental_shared1_textures','experimental_shared1_four_textures','experimental_water_rgba','experimental_shared2_atlases','experimental_shared2_audio','experimental_shared2_textures','experimental_shared2_meshes','experimental_hashed_meshes','experimental_hashed_animations','experimental_hashed_textures','experimental_hashed_matched','experimental_hashed_rgba','experimental_hashed_extra','experimental_hashed_final','experimental_hashed_audio')
 if not all(getattr(a,k) for k in required):p.error('rebuilt-only export requires all shared and hashed reconstruction options')
 if any((a.scrubbed_base_apk,a.scrubbed_base_pair,a.scrubbed_hashed_mesh_pair,a.scrubbed_hashed_animation_pair,a.scrubbed_hashed_texture_pair,a.scrubbed_hashed_matched_pair,a.scrubbed_hashed_rgba_pair,a.scrubbed_hashed_extra_pair,a.scrubbed_hashed_final_pair,a.scrubbed_hashed_audio_pair,a.scrubbed_scene_pair,a.scrubbed_scene_refs_pair,a.scrubbed_scene_mono_pair,a.experimental_scene_particles,a.experimental_scene_references,a.experimental_scene_mono,a.experimental_unity_splash,a.include_newtonsoft_notices)):p.error('rebuilt-only export does not package a base or scenes')
if a.experimental_unity_splash and not a.scrubbed_scene_mono_pair:p.error('--experimental-unity-splash requires a splash-scrubbed scene MonoBehaviour base pair')
if a.experimental_font_metadata and not a.experimental_font_atlases:p.error('--experimental-font-metadata requires --experimental-font-atlases')
if a.experimental_default_cubemap and not a.experimental_font_metadata:p.error('--experimental-default-cubemap requires --experimental-font-metadata')
if a.experimental_remaining_textures and not a.experimental_default_cubemap:p.error('--experimental-remaining-textures requires --experimental-default-cubemap')
if a.experimental_shared1_textures and not a.experimental_terrain_animations:p.error('--experimental-shared1-textures requires --experimental-terrain-animations')
if a.experimental_shared1_four_textures and not a.experimental_shared1_textures:p.error('--experimental-shared1-four-textures requires --experimental-shared1-textures')
if a.experimental_water_rgba and not a.experimental_shared1_four_textures:p.error('--experimental-water-rgba requires --experimental-shared1-four-textures')
if a.experimental_shared2_audio and not a.experimental_shared2_atlases:p.error('--experimental-shared2-audio requires --experimental-shared2-atlases')
if a.experimental_shared2_textures and not a.experimental_shared2_atlases:p.error('--experimental-shared2-textures requires --experimental-shared2-atlases')
if a.experimental_shared2_meshes and not a.experimental_shared2_textures:p.error('--experimental-shared2-meshes requires --experimental-shared2-textures')
if a.scrubbed_base_pair and a.scrubbed_base_apk:p.error('--scrubbed-base-pair cannot be combined with --scrubbed-base-apk')
if a.scrubbed_base_pair and not a.experimental_shared2_audio:p.error('--scrubbed-base-pair requires --experimental-shared2-audio')
if a.scrubbed_hashed_mesh_pair and (a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-mesh-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_hashed_animation_pair and (a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-animation-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_hashed_texture_pair and (a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-texture-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_hashed_matched_pair and (a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-matched-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_hashed_rgba_pair and (a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-rgba-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_hashed_extra_pair and (a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-extra-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_hashed_final_pair and (a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-final-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_hashed_audio_pair and (a.scrubbed_hashed_final_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-hashed-audio-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_scene_pair and (a.scrubbed_hashed_audio_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-scene-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_scene_refs_pair and (a.scrubbed_scene_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-scene-refs-pair cannot be combined with other scrubbed inputs')
if a.scrubbed_scene_mono_pair and (a.scrubbed_scene_refs_pair or a.scrubbed_scene_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_base_pair or a.scrubbed_base_apk):p.error('--scrubbed-scene-mono-pair cannot be combined with other scrubbed inputs')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_mesh_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_meshes):p.error('a scrubbed hashed-mesh pair and --experimental-hashed-meshes must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_animations):p.error('a scrubbed hashed-animation pair and --experimental-hashed-animations must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_textures):p.error('a scrubbed hashed-texture pair and --experimental-hashed-textures must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_matched):p.error('a scrubbed matched pair and --experimental-hashed-matched must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_rgba):p.error('a scrubbed RGBA pair and --experimental-hashed-rgba must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_extra):p.error('a scrubbed extra pair and --experimental-hashed-extra must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_final):p.error('a scrubbed final pair and --experimental-hashed-final must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_hashed_audio):p.error('a scrubbed audio pair and --experimental-hashed-audio must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_scene_particles):p.error('a scrubbed scene pair and --experimental-scene-particles must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair)!=bool(a.experimental_scene_references):p.error('a scrubbed scene references pair and --experimental-scene-references must be used together')
if not a.experimental_export_rebuilt_only and bool(a.scrubbed_scene_mono_pair)!=bool(a.experimental_scene_mono):p.error('--scrubbed-scene-mono-pair and --experimental-scene-mono must be used together')
if a.experimental_hashed_meshes and not a.experimental_shared2_audio:p.error('--experimental-hashed-meshes requires --experimental-shared2-audio')
if out.exists() or not out.parent.is_dir():p.error('output must not exist and its parent must exist')
if inside(out,pc) or inside(pc,out):p.error('output must be separate from the Steam data directory')
base_apk=a.scrubbed_scene_mono_pair/'base.apk' if a.scrubbed_scene_mono_pair else a.scrubbed_scene_refs_pair/'base.apk' if a.scrubbed_scene_refs_pair else a.scrubbed_scene_pair/'base.apk' if a.scrubbed_scene_pair else a.scrubbed_hashed_audio_pair/'base.apk' if a.scrubbed_hashed_audio_pair else a.scrubbed_hashed_final_pair/'base.apk' if a.scrubbed_hashed_final_pair else a.scrubbed_hashed_extra_pair/'base.apk' if a.scrubbed_hashed_extra_pair else a.scrubbed_hashed_rgba_pair/'base.apk' if a.scrubbed_hashed_rgba_pair else a.scrubbed_hashed_matched_pair/'base.apk' if a.scrubbed_hashed_matched_pair else a.scrubbed_hashed_texture_pair/'base.apk' if a.scrubbed_hashed_texture_pair else a.scrubbed_hashed_animation_pair/'base.apk' if a.scrubbed_hashed_animation_pair else a.scrubbed_hashed_mesh_pair/'base.apk' if a.scrubbed_hashed_mesh_pair else a.scrubbed_base_pair/'base.apk' if a.scrubbed_base_pair else a.scrubbed_base_apk if a.scrubbed_base_apk else a.base_apk
base_obb=a.scrubbed_scene_mono_pair/'base.obb' if a.scrubbed_scene_mono_pair else a.scrubbed_scene_refs_pair/'base.obb' if a.scrubbed_scene_refs_pair else a.scrubbed_scene_pair/'base.obb' if a.scrubbed_scene_pair else a.scrubbed_hashed_audio_pair/'base.obb' if a.scrubbed_hashed_audio_pair else a.scrubbed_hashed_final_pair/'base.obb' if a.scrubbed_hashed_final_pair else a.scrubbed_hashed_extra_pair/'base.obb' if a.scrubbed_hashed_extra_pair else a.scrubbed_hashed_rgba_pair/'base.obb' if a.scrubbed_hashed_rgba_pair else a.scrubbed_hashed_matched_pair/'base.obb' if a.scrubbed_hashed_matched_pair else a.scrubbed_hashed_texture_pair/'base.obb' if a.scrubbed_hashed_texture_pair else a.scrubbed_hashed_animation_pair/'base.obb' if a.scrubbed_hashed_animation_pair else a.scrubbed_hashed_mesh_pair/'base.obb' if a.scrubbed_hashed_mesh_pair else a.scrubbed_base_pair/'base.obb' if a.scrubbed_base_pair else a.base_obb
if not a.experimental_export_rebuilt_only:require(base_apk,'5480eef6ed8a76aea6575881ddc2aef746dcf74587109f71d8ced0947f1cf020' if a.experimental_unity_splash else SCRUBBED_SCENE_MONO_APK if a.scrubbed_scene_mono_pair else SCRUBBED_SCENE_REFS_APK if a.scrubbed_scene_refs_pair else SCRUBBED_SCENE_PAIR_APK if a.scrubbed_scene_pair else SCRUBBED_HASHED_AUDIO_PAIR_APK if a.scrubbed_hashed_audio_pair else SCRUBBED_FINAL_PAIR_APK if a.scrubbed_hashed_final_pair else SCRUBBED_EXTRA_PAIR_APK if a.scrubbed_hashed_extra_pair else SCRUBBED_RGBA_PAIR_APK if a.scrubbed_hashed_rgba_pair else SCRUBBED_MATCHED_PAIR_APK if a.scrubbed_hashed_matched_pair else SCRUBBED_TEXTURE_PAIR_APK if a.scrubbed_hashed_texture_pair else SCRUBBED_ANIMATION_PAIR_APK if a.scrubbed_hashed_animation_pair else SCRUBBED_MESH_PAIR_APK if a.scrubbed_hashed_mesh_pair else SCRUBBED_PAIR_APK if a.scrubbed_base_pair else SCRUBBED_BASE_APK if a.scrubbed_base_apk else BASE_APK);require(base_obb,SCRUBBED_SCENE_MONO_OBB if a.scrubbed_scene_mono_pair else SCRUBBED_SCENE_REFS_OBB if a.scrubbed_scene_refs_pair else SCRUBBED_SCENE_PAIR_OBB if a.scrubbed_scene_pair else SCRUBBED_HASHED_AUDIO_PAIR_OBB if a.scrubbed_hashed_audio_pair else SCRUBBED_FINAL_PAIR_OBB if a.scrubbed_hashed_final_pair else SCRUBBED_EXTRA_PAIR_OBB if a.scrubbed_hashed_extra_pair else SCRUBBED_RGBA_PAIR_OBB if a.scrubbed_hashed_rgba_pair else SCRUBBED_MATCHED_PAIR_OBB if a.scrubbed_hashed_matched_pair else SCRUBBED_TEXTURE_PAIR_OBB if a.scrubbed_hashed_texture_pair else SCRUBBED_ANIMATION_PAIR_OBB if a.scrubbed_hashed_animation_pair else SCRUBBED_MESH_PAIR_OBB if a.scrubbed_hashed_mesh_pair else SCRUBBED_PAIR_OBB if a.scrubbed_base_pair else BASE_OBB)
for tool in ((a.decoder,) if a.experimental_export_rebuilt_only else (a.build_tools/'zipalign',a.build_tools/'apksigner',a.java_home/'bin/java',a.debug_keystore,a.decoder)):
 if not tool.is_file():p.error(f'missing local tool or debug keystore: {tool}')
if a.runner!='none' and shutil.which(a.runner) is None:p.error(f'runner not found: {a.runner}')
runner=[] if a.runner=='none' else [a.runner]
env=os.environ.copy();env['JAVA_HOME']=str(a.java_home);env['PATH']=str(a.java_home/'bin')+os.pathsep+env.get('PATH','')
with tempfile.TemporaryDirectory(prefix='.openbonk-diagnostic-',dir=out.parent) as tmp:
 tmp=Path(tmp);data=tmp/'data';terrain_data=tmp/'terrain_data';extra_texture_data=tmp/'extra_texture_data';four_texture_data=tmp/'four_texture_data';font_data=tmp/'font_data';shared2_data=tmp/'shared2_data';shared2_texture_data=tmp/'shared2_texture_data';shared2_mesh_data=tmp/'shared2_mesh_data';shared2_audio_data=tmp/'shared2_audio_data';hashed_mesh_data=tmp/'hashed_mesh_data';hashed_animation_data=tmp/'hashed_animation_data';hashed_texture_data=tmp/'hashed_texture_data';hashed_matched_data=tmp/'hashed_matched_data';hashed_rgba_data=tmp/'hashed_rgba_data';hashed_extra_data=tmp/'hashed_extra_data';hashed_final_data=tmp/'hashed_final_data';hashed_audio_data=tmp/'hashed_audio_data';scene_particle_data=tmp/'scene_particle_data';scene_refs_data=tmp/'scene_refs_data';scene_mono_data=tmp/'scene_mono_data';metadata_data=tmp/'metadata_data';cubemap_data=tmp/'cubemap_data';texture_data=tmp/'texture_data';unsigned=tmp/'unsigned.apk';aligned=tmp/'aligned.apk';signed=tmp/'OpenBonk-diagnostic.apk';obb=tmp/f'main.1.{PACKAGE}.obb'
 data_template=a.template
 data_hash=DATA_ASSET_ALL if a.experimental_shared1_four_textures else DATA_ASSET_REMAINING if a.experimental_shared1_textures else DATA_ASSET
 if a.experimental_terrain_animations:
  terrain_asset=a.terrain_animation_template/'sharedassets1.assets'
  if a.experimental_shared1_textures:
   shared1_texture_asset=a.shared1_texture_template/'sharedassets1.assets'
   if a.experimental_shared1_four_textures:
    stage_pc=a.windows_terrain_four.resolve() if a.windows_terrain_four else pc
    stage_args=['--windows-source'] if a.windows_terrain_four else []
    run([sys.executable,ROOT/'analysis/data-conversion/shared1-four-textures-prototype.py','restore',stage_pc,a.shared1_four_texture_template/'sharedassets1.assets',four_texture_data,'--manifest',a.shared1_four_texture_template/'textures-manifest.json',*stage_args])
    shared1_texture_asset=four_texture_data/'sharedassets1.assets'
   stage_pc=a.windows_terrain_four.resolve() if a.windows_terrain_four else pc
   stage_args=['--windows-source'] if a.windows_terrain_four else []
   run([sys.executable,ROOT/'analysis/data-conversion/shared1-remaining-textures-prototype.py','restore',stage_pc,shared1_texture_asset,extra_texture_data,'--manifest',a.shared1_texture_template/'textures-manifest.json']+(['--four-textures'] if a.experimental_shared1_four_textures else [])+stage_args)
   terrain_asset=extra_texture_data/'sharedassets1.assets'
  stage_pc=a.windows_terrain_four.resolve() if a.windows_terrain_four else pc
  stage_args=['--windows-source'] if a.windows_terrain_four else []
  run([sys.executable,ROOT/'analysis/data-conversion/shared1-terrain-animation-prototype.py','restore',stage_pc,terrain_asset,terrain_data,'--manifest',a.terrain_animation_template/'objects-manifest.json']+(['--all-textures'] if a.experimental_shared1_four_textures else ['--converted-textures'] if a.experimental_shared1_textures else [])+stage_args)
  data_template=terrain_data
 core_pc=a.windows_core_assets.resolve() if a.windows_core_assets else pc
 core_args=['--windows-source'] if a.windows_core_assets else []
 run([sys.executable,ROOT/'tools/build-local-data-matched-prototype.py',core_pc,data_template,a.manifest_root,a.decoder,data]+(['--experimental-all-textures'] if a.experimental_shared1_four_textures else ['--experimental-remaining-textures'] if a.experimental_shared1_textures else [])+core_args)
 require(data/'sharedassets1.assets',data_hash);require(data/'sharedassets1.resource',DATA_AUDIO)
 if a.experimental_water_rgba:
  water_asset=tmp/'water-rgba.assets'
  water_pc=a.windows_remaining_assets.resolve() if a.windows_remaining_assets else pc
  water_source=['--windows-source'] if a.windows_remaining_assets else []
  run([sys.executable,ROOT/'analysis/data-conversion/shared1-water-normal-quality.py',water_pc,data/'sharedassets1.assets',water_asset,*water_source])
  require(water_asset,DATA_ASSET_WATER_RGBA);os.replace(water_asset,data/'sharedassets1.assets');data_hash=DATA_ASSET_WATER_RGBA
 if a.experimental_font_atlases:
  shared0_pc=a.windows_shared0.resolve() if a.windows_shared0 else pc
  shared0_source=['--windows-source'] if a.windows_shared0 else []
  shared0_hash='e92b4561faf7be547bef42a874067ba3d688947db0bfa0fbd966532792e2e7b1' if a.experimental_leave_font_metadata_blank else SHARED0_CONVERTED_TEXTURES if a.experimental_remaining_textures else SHARED0_ATLASES
  atlas_asset=a.atlas_template/'sharedassets0.assets'
  if a.experimental_font_metadata:
   metadata_asset=a.metadata_template/'sharedassets0.assets'
   if a.experimental_default_cubemap:
    cubemap_asset=a.cubemap_template/'sharedassets0.assets';cubemap_hash=SHARED0_CUBEMAP_TEMPLATE;variant=[]
    if a.experimental_remaining_textures:
     run([sys.executable,ROOT/'analysis/data-conversion/shared0-remaining-textures-prototype.py','restore',shared0_pc,a.remaining_textures_template/'sharedassets0.assets',texture_data,'--manifest',a.remaining_textures_template/'textures-manifest.json']+shared0_source)
     cubemap_asset=texture_data/'sharedassets0.assets';cubemap_hash=SHARED0_CUBEMAP_WITH_TEXTURES;variant=['--converted-textures']
    run([sys.executable,ROOT/'analysis/data-conversion/shared0-default-cubemap-prototype.py','restore',shared0_pc,cubemap_asset,cubemap_data,'--template-sha256',cubemap_hash]+variant+shared0_source)
    metadata_asset=cubemap_data/'sharedassets0.assets'
   if a.experimental_leave_font_metadata_blank:
    atlas_asset=metadata_asset
   else:
    run([sys.executable,ROOT/'analysis/data-conversion/shared0-font-metadata-prototype.py','restore',pc,metadata_asset,metadata_data,'--manifest',a.metadata_template/'metadata-manifest.json']+(['--converted-textures'] if a.experimental_remaining_textures else []))
    atlas_asset=metadata_data/'sharedassets0.assets'
  run([sys.executable,ROOT/'analysis/data-conversion/shared0-atlas-fonts-prototype.py','restore',shared0_pc,atlas_asset,a.font_template/'font-manifest.json',font_data,'--atlas-manifest',a.atlas_template/'atlas-manifest.json']+(['--converted-textures'] if a.experimental_remaining_textures else [])+(['--leave-font-metadata-blank'] if a.experimental_leave_font_metadata_blank else [])+shared0_source)
 else:
  shared0_hash=SHARED0
  run([sys.executable,ROOT/'analysis/data-conversion/shared0-fonts-prototype.py','restore',pc,a.font_template/'sharedassets0.assets',font_data,'--manifest',a.font_template/'font-manifest.json'])
 require(font_data/'sharedassets0.assets',shared0_hash)
 shared2_args=[]
 shared2_hash=None
 if a.experimental_shared2_atlases:
  shared2_pc=a.windows_shared2.resolve() if a.windows_shared2 else pc
  shared2_source=['--windows-source'] if a.windows_shared2 else []
  shared2_asset=a.shared2_atlas_template/'sharedassets2.assets'
  if a.experimental_shared2_textures:
   shared2_texture_asset=a.shared2_texture_template/'sharedassets2.assets'
   if a.experimental_shared2_meshes:
    run([sys.executable,ROOT/'analysis/data-conversion/shared2-mesh-buffers-prototype.py','restore',shared2_pc,a.shared2_mesh_template/'sharedassets2.assets',shared2_mesh_data,'--manifest',a.shared2_mesh_template/'mesh-manifest.json',*shared2_source])
    require(shared2_mesh_data/'sharedassets2.assets',SHARED2_MATCHED_TEMPLATE);shared2_texture_asset=shared2_mesh_data/'sharedassets2.assets'
   run([sys.executable,ROOT/'analysis/data-conversion/shared2-matched-textures-prototype.py','restore',shared2_pc,shared2_texture_asset,shared2_texture_data,'--manifest',a.shared2_texture_template/'textures-manifest.json',*shared2_source])
   shared2_asset=shared2_texture_data/'sharedassets2.assets'
  run([sys.executable,ROOT/'analysis/data-conversion/shared2-font-atlases-prototype.py','restore',shared2_pc,shared2_asset,shared2_data,'--manifest',a.shared2_atlas_template/'atlases-manifest.json']+(['--matched-textures'] if a.experimental_shared2_textures else [])+shared2_source)
  shared2_hash=SHARED2_MATCHED_ATLASES if a.experimental_shared2_textures else SHARED2_ATLASES
  require(shared2_data/'sharedassets2.assets',shared2_hash)
  if a.experimental_shared2_audio:
   run([sys.executable,ROOT/'analysis/data-conversion/shared2-audio-retarget-prototype.py',shared2_pc,shared2_data/'sharedassets2.assets',shared2_audio_data]+(['--matched-textures'] if a.experimental_shared2_textures else [])+shared2_source)
   shared2_hash=SHARED2_MATCHED_AUDIO if a.experimental_shared2_textures else SHARED2_AUDIO_ASSETS
   require(shared2_audio_data/'sharedassets2.assets',shared2_hash);require(shared2_audio_data/'sharedassets2.resource',SHARED2_AUDIO)
   shared2_args=[shared2_audio_data/'sharedassets2.assets',shared2_audio_data/'sharedassets2.resource']
  else:shared2_args=[shared2_data/'sharedassets2.assets']
 hashed_pc=a.windows_hashed_assets.resolve() if a.windows_hashed_assets else pc
 hashed_source=['--windows-source'] if a.windows_hashed_assets else []
 mesh_manifest=None
 if a.experimental_hashed_meshes:
  mesh_manifest=a.hashed_mesh_template/'manifest.json';require(mesh_manifest,HASHED_MESH_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-mesh-buffers-prototype.py','restore',hashed_pc,a.hashed_mesh_template,hashed_mesh_data]+hashed_source)
 animation_manifest=None
 if a.experimental_hashed_animations:
  animation_manifest=a.hashed_animation_hybrid_manifest if a.hashed_animation_hybrid_manifest else a.hashed_animation_template/'manifest.json'
  require(animation_manifest,'716bf1773ce218878cf83494dc3b4d05344ea9b64bf960a96a9a3a8043eef7a1' if a.hashed_animation_hybrid_manifest else HASHED_ANIMATION_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-animation-prototype.py','restore',hashed_pc,a.hashed_animation_template,hashed_animation_data]+(['--hybrid-manifest',a.hashed_animation_hybrid_manifest] if a.hashed_animation_hybrid_manifest else [])+hashed_source)
 texture_manifest=None
 if a.experimental_hashed_textures:
  texture_manifest=a.hashed_texture_template/'manifest.json';require(texture_manifest,HASHED_TEXTURE_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-exact-textures-prototype.py','restore',hashed_pc,a.hashed_texture_template,hashed_texture_data]+hashed_source)
 matched_manifest=None
 if a.experimental_hashed_matched:
  matched_manifest=a.hashed_matched_template/'manifest.json';require(matched_manifest,HASHED_MATCHED_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-matched-textures-prototype.py','restore',hashed_pc,a.hashed_matched_template,hashed_matched_data]+hashed_source)
 rgba_manifest=None
 if a.experimental_hashed_rgba:
  rgba_manifest=a.hashed_rgba_template/'manifest.json';require(rgba_manifest,HASHED_RGBA_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-excluded-rgba-template.py','restore',hashed_pc,a.hashed_rgba_template,hashed_rgba_data]+hashed_source)
 extra_manifest=None
 if a.experimental_hashed_extra:
  extra_manifest=a.hashed_extra_template/'manifest.json';require(extra_manifest,HASHED_EXTRA_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-extra-candidates-template.py','restore',hashed_pc,a.hashed_extra_template,hashed_extra_data]+hashed_source)
 final_manifest=None
 if a.experimental_hashed_final:
  final_manifest=a.hashed_final_template/'manifest.json';require(final_manifest,HASHED_FINAL_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-final-candidates-template.py','restore',hashed_pc,a.hashed_final_template,hashed_final_data]+hashed_source)
 hashed_audio_manifest=None
 if a.experimental_hashed_audio:
  hashed_audio_manifest=a.hashed_audio_template/'manifest.json';require(hashed_audio_manifest,HASHED_AUDIO_MANIFEST)
  run([sys.executable,ROOT/'analysis/data-conversion/hashed-audio-template.py','restore',hashed_pc,a.hashed_audio_template,hashed_audio_data]+hashed_source)
 if a.experimental_export_rebuilt_only:
  files={'sharedassets0.assets':font_data/'sharedassets0.assets','sharedassets1.assets':data/'sharedassets1.assets','sharedassets1.resource':data/'sharedassets1.resource','sharedassets2.assets':shared2_audio_data/'sharedassets2.assets','sharedassets2.resource':shared2_audio_data/'sharedassets2.resource'}
  dirs=[hashed_mesh_data,hashed_animation_data,hashed_texture_data,hashed_matched_data,hashed_rgba_data,hashed_extra_data,hashed_final_data,hashed_audio_data]
  out.mkdir()
  try:
   for name,src in files.items():shutil.copyfile(src,out/name)
   for i,src in enumerate(dirs):shutil.copytree(src,out/f'hashed-{i}')
   report={'kind':'local rebuilt files only, not for distribution','files':{name:sha(out/name) for name in files},'hashed_directory_count':len(dirs),'limitations':['Files retain game content and are not redistributable.','Only one PC build and local templates are supported.','No old APK/OBB was packaged.']}
   (out/'diagnostic-report.json').write_text(json.dumps(report,indent=2)+'\n')
  except BaseException:shutil.rmtree(out);raise
  print('Local rebuilt-only files created:',out,'NOT FOR DISTRIBUTION',flush=True)
  sys.exit(0)
 scene_particle_manifest=None
 if a.experimental_scene_particles:
  scene_particle_manifest=a.scene_particle_template/'manifest.json';require(scene_particle_manifest,SCENE_PARTICLE_MANIFEST)
  scene_template=a.scene_particle_template
  if a.experimental_scene_references:
   require(a.scene_references_template/'manifest.json',SCENE_REFERENCES_MANIFEST)
   refs_template=a.scene_references_template
   if a.experimental_scene_mono:
    require(a.scene_mono_template/'manifest.json',SCENE_MONO_MANIFEST)
    run([sys.executable,ROOT/'analysis/data-conversion/scene-monobehaviour-bodies-template.py','restore',pc,a.scene_mono_template,scene_mono_data])
    shutil.copyfile(a.scene_references_template/'manifest.json',scene_mono_data/'manifest.json');refs_template=scene_mono_data
   run([sys.executable,ROOT/'analysis/data-conversion/scene-particle-references-template.py','restore',pc,refs_template,scene_refs_data])
   shutil.copyfile(scene_particle_manifest,scene_refs_data/'manifest.json');scene_template=scene_refs_data
  run([sys.executable,ROOT/'analysis/data-conversion/scene-particle-bodies-template.py','restore',pc,scene_template,scene_particle_data])
 run([sys.executable,ROOT/'analysis/port-inventory/relocate-split-probe.py',base_apk,base_obb,unsigned,obb,data,font_data/'sharedassets0.assets']+shared2_args+([hashed_mesh_data] if a.experimental_hashed_meshes else [])+([hashed_animation_data] if a.experimental_hashed_animations else [])+([hashed_texture_data] if a.experimental_hashed_textures else [])+([hashed_matched_data] if a.experimental_hashed_matched else [])+([hashed_rgba_data] if a.experimental_hashed_rgba else [])+([hashed_extra_data] if a.experimental_hashed_extra else [])+([hashed_final_data] if a.experimental_hashed_final else [])+([hashed_audio_data] if a.experimental_hashed_audio else [])+([scene_particle_data] if a.experimental_scene_particles else []))
 if a.experimental_unity_splash:
  splash_data=tmp/'unity-splash-data';splash_apk=tmp/'unsigned-with-splash.apk'
  run([sys.executable,ROOT/'analysis/data-conversion/unity-splash-template.py','restore',pc,a.unity_splash_template,splash_data])
  require(splash_data/'globalgamemanagers.assets','6c4768f32e18a14ce3a9d945bd96b417db33df5bbe4a336d9ee26c38ea4bde58')
  run([sys.executable,ROOT/'analysis/data-conversion/patch-unity-splash-apk-probe.py',unsigned,splash_data/'globalgamemanagers.assets',splash_apk,'--restore-blank'])
  unsigned=splash_apk
 if a.include_newtonsoft_notices:
  with_notices=tmp/'unsigned-with-notices.apk'
  run([sys.executable,ROOT/'analysis/port-inventory/attach-newtonsoft-notices.py',unsigned,NEWTONSOFT_NOTICE_DIR,with_notices])
  unsigned=with_notices
 run(runner+[a.build_tools/'zipalign','-p','4',unsigned,aligned],env)
 run(runner+[a.build_tools/'apksigner','sign','--ks',a.debug_keystore,'--ks-key-alias','androiddebugkey','--ks-pass','pass:android','--key-pass','pass:android','--out',signed,aligned],env)
 run(runner+[a.build_tools/'zipalign','-c','4',signed],env)
 run(runner+[a.build_tools/'apksigner','verify','--verbose',signed],env)
 pieces,moved,obb_hash=archive_checks(signed,obb,shared0_hash,data_hash,shared2_hash,SHARED2_AUDIO if a.experimental_shared2_audio else None,mesh_manifest,animation_manifest,texture_manifest,matched_manifest,rgba_manifest,extra_manifest,final_manifest,hashed_audio_manifest,scene_particle_manifest,a.include_newtonsoft_notices)
 if a.experimental_unity_splash:
  with zipfile.ZipFile(signed) as apkcheck:
   prefix='assets/bin/Data/globalgamemanagers.assets.split';parts=sorted((n for n in apkcheck.namelist() if n.startswith(prefix)),key=lambda n:int(n[len(prefix):]))
   if not parts or [int(n[len(prefix):]) for n in parts]!=list(range(len(parts))) or hashlib.sha256(b''.join(apkcheck.read(n) for n in parts)).hexdigest()!='6c4768f32e18a14ce3a9d945bd96b417db33df5bbe4a336d9ee26c38ea4bde58':raise ValueError('packaged Unity splash asset differs')
 report={'kind':'local diagnostic only, not for distribution','package':PACKAGE,'apk_sha256':sha(signed),'obb_sha256':obb_hash,'data_sha256':data_hash,'audio_sha256':DATA_AUDIO,'sharedassets0_sha256':shared0_hash,'reconstructed_fonts':7,'reconstructed_font_atlases':94 if a.experimental_font_atlases else 0,'reconstructed_font_metadata_objects':7 if a.experimental_font_metadata else 0,'asset_fragments':pieces,'relocated_fragments':moved,'signing':'local Android debug keystore','limitations':['Contains original game data and reconstructed or third-party code.','Experimental DEX hash patch; not a release build or a user-side installer.','Pinned to one PC build and local templates.','Font atlas conversion is lossy when enabled; language coverage is incomplete.','Gameplay, assets, rights, updates and compatibility are not fully verified.']}
 report['reconstructed_default_cubemaps']=1 if a.experimental_default_cubemap else 0
 report['reconstructed_remaining_textures']=2 if a.experimental_remaining_textures else 0
 report['reconstructed_terrain_objects']=1 if a.experimental_terrain_animations else 0
 report['reconstructed_animation_objects']=12 if a.experimental_terrain_animations else 0
 report['reconstructed_shared1_additional_textures']=59 if a.experimental_shared1_textures else 0
 report['reconstructed_shared1_final_textures']=4 if a.experimental_shared1_four_textures else 0
 report['water_normal_format']='RGBA32' if a.experimental_water_rgba else 'original diagnostic format'
 report['base_apk_shared0']='zeroed fragments' if (a.scrubbed_base_apk or a.scrubbed_base_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair) else 'original fragments'
 report['base_obb_shared1_shared2']='zeroed' if (a.scrubbed_base_pair or a.scrubbed_hashed_mesh_pair or a.scrubbed_hashed_animation_pair or a.scrubbed_hashed_texture_pair or a.scrubbed_hashed_matched_pair or a.scrubbed_hashed_rgba_pair or a.scrubbed_hashed_extra_pair or a.scrubbed_hashed_final_pair or a.scrubbed_hashed_audio_pair or a.scrubbed_scene_pair or a.scrubbed_scene_refs_pair or a.scrubbed_scene_mono_pair) else 'original'
 report['reconstructed_hashed_meshes']=50 if a.experimental_hashed_meshes else 0
 report['reconstructed_hashed_animations']=108 if a.experimental_hashed_animations else 0
 report['reconstructed_hashed_textures']=56 if a.experimental_hashed_textures else 0
 report['reconstructed_hashed_matched_textures']=53 if a.experimental_hashed_matched else 0
 report['reconstructed_hashed_rgba_textures']=10 if a.experimental_hashed_rgba else 0
 report['reconstructed_hashed_extra_textures']=13 if a.experimental_hashed_extra else 0
 report['reconstructed_hashed_final_textures']=2 if a.experimental_hashed_final else 0
 report['retargeted_hashed_audio_clips']=63 if a.experimental_hashed_audio else 0
 report['reconstructed_scene_particles']=(436 if a.experimental_scene_references else 397) if a.experimental_scene_particles else 0
 report['reconstructed_scene_monobehaviours']=3927 if a.experimental_scene_mono else 0
 report['reconstructed_unity_splash']=1 if a.experimental_unity_splash else 0
 if a.experimental_unity_splash:report['limitations'].append('Unity splash texture conversion is lossy; its visual result and rights require review.')
 report['newtonsoft_notice_documents']=2 if a.include_newtonsoft_notices else 0
 if a.include_newtonsoft_notices:report['limitations'].append('Newtonsoft notices added for review; the complete set of notices and binary provenance are not verified.')
 if a.experimental_scene_mono:report['limitations'].append('Android component headers and unreviewed scene objects remain.')
 if a.experimental_scene_references:report['limitations'].append('Scene GameObject and sub-emitter references remain in templates; provenance unreviewed.')
 if a.experimental_hashed_audio:report['limitations'].append('63 matched audio candidates require playback and audible equivalence checks; one was measured in an isolated test.')
 if a.experimental_hashed_final:report['limitations'].append('Final sprite and weapon texture candidates need visual and device memory validation.')
 if a.experimental_hashed_extra:report['limitations'].append('Thirteen additional texture candidates need visual and memory validation.')
 if a.experimental_hashed_rgba:report['limitations'].append('Ten uncompressed name-matched textures need visual and device memory validation.')
 if a.experimental_hashed_matched:report['limitations'].append('53 name-matched hashed textures are lossy and need in-game validation.')
 if a.experimental_hashed_animations:report['limitations'].append('Animation patches contain Android-derived data with unreviewed rights.')
 report['reconstructed_shared2_atlases']=2 if a.experimental_shared2_atlases else 0
 report['retargeted_shared2_audio_clips']=124 if a.experimental_shared2_audio else 0
 report['reconstructed_shared2_additional_textures']=72 if a.experimental_shared2_textures else 0
 report['reconstructed_shared2_mesh_buffers']=251 if a.experimental_shared2_meshes else 0
 if a.experimental_shared2_atlases:report['limitations'].append('Two sharedassets2 font atlases use lossy conversion and need visual validation.')
 if a.experimental_shared2_audio:report['limitations'].append('124 PC audio streams need playback verification on Android devices.')
 if a.experimental_shared2_textures:report['limitations'].append('72 name-matched sharedassets2 textures are not fully validated in game.')
 if a.experimental_remaining_textures:report['limitations'].append('Fox_Default and Emission conversions are lossy; distant texture quality requires further testing.')
 if a.experimental_shared1_textures:report['limitations'].append('59 visually matched textures are lossy and not fully validated in game.')
 if a.experimental_shared1_four_textures:report['limitations'].append('Four additional texture matches and conversions require in-game validation, especially water appearance.')
 report_path=tmp/'diagnostic-report.json';report_path.write_text(json.dumps(report,indent=2)+'\n')
 os.mkdir(out)
 try:
  for path in (signed,obb,report_path):shutil.move(str(path),str(out/path.name))
 except Exception:shutil.rmtree(out);raise
print('Local diagnostic created:',out,flush=True)
print('NOT FOR DISTRIBUTION: contains game data; no device was modified.',flush=True)
