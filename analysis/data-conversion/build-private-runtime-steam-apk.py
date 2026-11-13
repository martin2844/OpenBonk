#!/usr/bin/env python3
"""Stage a private recompiled-runtime APK with the experimental Steam launcher."""
import copy
import hashlib
from pathlib import Path
import sys
import zipfile

RUNTIME_SHAS = {'f50aff5a9af4ae78968d4c56e721e961ae69c94e21cf012715fe07e6cfd8b260',
                'f30a535b8f36151d0a80714e869d45ed7270aee17c55e415ad3d65240ce56ca4',
                '5543620d74804f198d8c864dc45962c83b97d82787f13ea21bd7b3f93b19c37f',
                '435618a799dc8821f713d28f3005d2338315916cc3fd604ad3ac521e9e7691be',
                '8eca98041e4d38ead18fe49a50d352936f6fae0344c71b4ada4bece1b5387735',
                '590a68f00c9459e1c709a8abe89061a25a818ebe7f4c6f1305f9f17c681f9d58',
                'f6f6c256fc274b34957c67a10f2c70deed22231e42b0a4908853bb90e7677de4',
                '29d8b0a9b0ea951c0051a2ec7c48f0b7afccd3f55ec80df80533e59274f64410',
                'dabfdee55c8bd2c56d1f2213a70aa5764465b99366e8143350df073ea74be301',
                'dee2b0c36667e845f42acc1793a395b0fa0030025d4a212c467f03829dc86672',
                '31a4913f3004b13c6c91acaf312f55f53a87c89ed3a49c056053eb99a78cfb60',
                '9b39fabb6995b5f36d65875e4762df5b3cd08a554545463be17bee20545e2a92', 'cfc632ec59451bbc1e3db4b25a4f451912dfa3d059da92b591484bc08897e574'}
OBB_SHA = '18280c2798fa40248a312575fe1eeabd167951fbd05282a9f9d7e2b0779e6499'
VIDEO_ONLY_SHA = '5acfe4f4b62c97e972e46525977375378cabdd09e33d5311f89bb637e2abb931'
VIDEO_ONLY_NO_PROBES_SHA = 'c367ae38db2ba09fc3ddb1de428bb1782664916905565bb68b1e850ac4ee09f5'
PLAN_SHA = '3a0621e1de16a038e0d41f77fb839d7c04ee2a963f0bdfd1917eb0b68ddcd1b1'

def sha(path):
    with path.open('rb') as stream: return hashlib.file_digest(stream, 'sha256').hexdigest()

def main():
    if len(sys.argv) not in (5, 6): raise ValueError('usage: SCRIPT RUNTIME_APK GRADLE_APK TARGET_OBB NEW_UNSIGNED_APK [REDACTED_LIB_DIRECTORY]')
    runtime, gradle, obb, output = map(Path, sys.argv[1:5])
    redacted = None
    if len(sys.argv) == 6:
        directory = Path(sys.argv[5])
        pins = {'libgame.so': '2962415ebbdb0a95b68813aa4f07dcd47b29ce1cd5514e6b8fd3712f086a6d13',
                'libil2cpp.so': 'aa9f72691e59988ffa31484de25f683f51d49b325c37af7610017240bba83d62',
                'libswappywrapper.so': '4834dc594210f5d7495b1914f3728625ca820ea562f012f9eb944254863564c5'}
        runtime_hash = sha(runtime)
        if runtime_hash == '29d8b0a9b0ea951c0051a2ec7c48f0b7afccd3f55ec80df80533e59274f64410':
            pins['libswappywrapper.so'] = '40ef0e5aa0d708a15e6657fe1f7c82d16ab97cc71c69d98e3380926a96d64f4b'
        elif runtime_hash == 'dabfdee55c8bd2c56d1f2213a70aa5764465b99366e8143350df073ea74be301':
            pins['libswappywrapper.so'] = '757030e04c90fe82fc02d889a51556f3a615fe1d1c67f5bc25ce5e5f5f17b3ad'
        elif runtime_hash == 'dee2b0c36667e845f42acc1793a395b0fa0030025d4a212c467f03829dc86672':
            pins['libswappywrapper.so'] = '0a76441eba01f731e54e45d4e80ec7dda767f7232c315691a2273b03213dc7e7'
        elif runtime_hash == '31a4913f3004b13c6c91acaf312f55f53a87c89ed3a49c056053eb99a78cfb60':
            pins['libswappywrapper.so'] = '0ca2d756ef011e1e7abd9399a32338fe2b0ba1e438548970dcd513de30346008'
        elif runtime_hash == '9b39fabb6995b5f36d65875e4762df5b3cd08a554545463be17bee20545e2a92':
            pins['libil2cpp.so'] = 'bcf9025aada495df557d02342020700722d870024c28dd5dc998658bf439be97'
            pins['libswappywrapper.so'] = '977509b60c99663de8f50f49bbe0b56ef3b3eff06f52f12b9d4eeaedd9b0e90a'
        elif runtime_hash == 'cfc632ec59451bbc1e3db4b25a4f451912dfa3d059da92b591484bc08897e574':
            pins['libil2cpp.so'] = '3ba53aa588ab8093b76507449209bb48ccdaf0e319ae7bff7b68c4bbc7285bfe'
            pins['libswappywrapper.so'] = '0f86a532dda81bf8fc4bb778bf504a7a6b914aaafe0e9ee6f2f52e957e514186'
        elif runtime_hash != 'f6f6c256fc274b34957c67a10f2c70deed22231e42b0a4908853bb90e7677de4':
            raise ValueError('redacted libraries require their pinned runtime')
        redacted = {}
        for name, expected in pins.items():
            path = directory / name
            if path.is_symlink() or not path.is_file() or sha(path) != expected: raise ValueError('unexpected redacted library')
            redacted['lib/arm64-v8a/' + name] = path
    if not runtime.is_file() or sha(runtime) not in RUNTIME_SHAS or not obb.is_file():
        raise ValueError('unexpected runtime or OBB')
    video_only = sha(runtime) in {'5543620d74804f198d8c864dc45962c83b97d82787f13ea21bd7b3f93b19c37f',
                                  '435618a799dc8821f713d28f3005d2338315916cc3fd604ad3ac521e9e7691be',
                                  '8eca98041e4d38ead18fe49a50d352936f6fae0344c71b4ada4bece1b5387735',
                                  '590a68f00c9459e1c709a8abe89061a25a818ebe7f4c6f1305f9f17c681f9d58',
                                  'f6f6c256fc274b34957c67a10f2c70deed22231e42b0a4908853bb90e7677de4',
                                  '29d8b0a9b0ea951c0051a2ec7c48f0b7afccd3f55ec80df80533e59274f64410',
                                  'dabfdee55c8bd2c56d1f2213a70aa5764465b99366e8143350df073ea74be301',
                                  'dee2b0c36667e845f42acc1793a395b0fa0030025d4a212c467f03829dc86672',
                                  '31a4913f3004b13c6c91acaf312f55f53a87c89ed3a49c056053eb99a78cfb60',
                                  '9b39fabb6995b5f36d65875e4762df5b3cd08a554545463be17bee20545e2a92', 'cfc632ec59451bbc1e3db4b25a4f451912dfa3d059da92b591484bc08897e574'}
    obb_hash = sha(obb)
    if video_only:
        if obb_hash not in {VIDEO_ONLY_SHA, VIDEO_ONLY_NO_PROBES_SHA}: raise ValueError('unexpected runtime or OBB')
        if obb_hash == VIDEO_ONLY_NO_PROBES_SHA and sha(runtime) not in {'435618a799dc8821f713d28f3005d2338315916cc3fd604ad3ac521e9e7691be', '8eca98041e4d38ead18fe49a50d352936f6fae0344c71b4ada4bece1b5387735', '590a68f00c9459e1c709a8abe89061a25a818ebe7f4c6f1305f9f17c681f9d58', 'f6f6c256fc274b34957c67a10f2c70deed22231e42b0a4908853bb90e7677de4', '29d8b0a9b0ea951c0051a2ec7c48f0b7afccd3f55ec80df80533e59274f64410', 'dabfdee55c8bd2c56d1f2213a70aa5764465b99366e8143350df073ea74be301', 'dee2b0c36667e845f42acc1793a395b0fa0030025d4a212c467f03829dc86672', '31a4913f3004b13c6c91acaf312f55f53a87c89ed3a49c056053eb99a78cfb60', '9b39fabb6995b5f36d65875e4762df5b3cd08a554545463be17bee20545e2a92', 'cfc632ec59451bbc1e3db4b25a4f451912dfa3d059da92b591484bc08897e574'}: raise ValueError('reduced base requires current runtime')
        expected_obb = obb_hash
    else:
        expected_obb = OBB_SHA
        if obb_hash != expected_obb: raise ValueError('unexpected runtime or OBB')
    if not gradle.is_file() or output.exists() or output.is_symlink() or not output.parent.is_dir(): raise ValueError('invalid build or output')
    try:
        with zipfile.ZipFile(runtime) as base, zipfile.ZipFile(gradle) as built, zipfile.ZipFile(obb) as data:
            for z in (base, built, data):
                if len(z.namelist()) != len(set(z.namelist())): raise ValueError('duplicate entries')
            overlap = set(base.namelist()) & set(data.namelist())
            if len(overlap) != 126 or any(not n.startswith('assets/bin/Data/') for n in overlap): raise ValueError('unexpected embedded data inventory')
            additions = {n for n in built.namelist() if n.startswith('assets/openbonk-diagnostic/') or n in ('lib/arm64-v8a/libopenbonk_texture.so','lib/arm64-v8a/libopenbonk_zlib.so')}
            notices = {'README.md','bcdec-dual-license.txt','crn_decomp.h-notice.txt','crnlib.h-notice.txt','etcpak-BSD-3-Clause.txt','etcpak-wrapper-MIT.txt','texture2ddecoder-MIT.txt','zlib-1.3.2-license.txt','Lua-5.4.9.txt'}
            expected = {'assets/openbonk-diagnostic/shared0.zip','assets/openbonk-diagnostic/base.obb','assets/openbonk-diagnostic/marker',
                        'assets/openbonk-diagnostic/plans.zip','assets/openbonk-diagnostic/openbonk_mascot.png','lib/arm64-v8a/libopenbonk_texture.so','lib/arm64-v8a/libopenbonk_zlib.so'}
            expected.add('assets/openbonk-diagnostic/runtime-video-only' if video_only else 'assets/openbonk-diagnostic/runtime-rebase.zip')
            expected |= {'assets/openbonk-diagnostic/notices/' + n for n in notices}
            scene_plan = 'assets/openbonk-diagnostic/scene-component-plan.bin'
            scene_base = '6212c14a17747e1abd5fb34cbf6ea2ee13c1c766f4ece5f74d1657d1be5b260d'
            shared_plan = 'assets/openbonk-diagnostic/shared-component-plan.bin'
            shared_base = 'd1cd0e69e2690cb7e289962c5646a4aa3ef3b9b1a60e1838c4e4edf8bbb83431'
            material_plan = 'assets/openbonk-diagnostic/material-plan.bin'
            material_base = '902dfed8c1dff91fe38b7a75acf72bb94b20fac36b121d145e7bdcce09f58e22'
            has_scene_plan = scene_plan in additions
            has_shared_plan = shared_plan in additions
            has_material_plan = material_plan in additions
            base_sha = sha_bytes(built.read('assets/openbonk-diagnostic/base.obb'))
            if has_scene_plan:
                expected.add(scene_plan)
                if sha_bytes(built.read(scene_plan)) != 'a35bf01205a78f486bb3f062e83f8649ca975aa0ef43acceda1047554b8d7317':
                    raise ValueError('scene component plan differs')
            if has_shared_plan:
                expected.add(shared_plan)
                if sha_bytes(built.read(shared_plan)) != '16ba7e7f6192831f5ba201b5347896f16f14f9dc31352107e8fd5a43132ccf0b':
                    raise ValueError('shared component plan differs')
            if has_material_plan:
                expected.add(material_plan)
                if sha_bytes(built.read(material_plan)) != '59a579b040fd13880a1ae1dff053eb06291df5f52b1b074aadeeab5e46a72593':
                    raise ValueError('material plan differs')
            if (base_sha in {scene_base, shared_base, material_base}) != has_scene_plan or (base_sha in {shared_base, material_base}) != has_shared_plan or (base_sha == material_base) != has_material_plan:
                raise ValueError('scene component plan/base mismatch')
            if additions != expected or (video_only and built.read('assets/openbonk-diagnostic/runtime-video-only') != b'OPENBONK_VIDEO_ONLY_V1') or (not video_only and sha_bytes(built.read('assets/openbonk-diagnostic/runtime-rebase.zip')) != PLAN_SHA):
                raise ValueError('unexpected experimental launcher payload')
            if sha_bytes(built.read('assets/openbonk-diagnostic/notices/Lua-5.4.9.txt')) != 'd14fe4a91721fd1d0f6650137594ee83c692f422864a00e69bd6a9b42cdda005':
                raise ValueError('Lua notice differs')
            if built.read('assets/openbonk-diagnostic/marker') != b'OPENBONK_PRIVATE_DIAGNOSTIC_V1': raise ValueError('bad marker')
            if additions & set(base.namelist()): raise ValueError('launcher payload collides with runtime')
            guid = data.read('unity_obb_guid')
            if len(guid) != 36 or len(base.read('assets/unity_obb_guid')) != 36: raise ValueError('bad OBB GUID')
            if 'classes.dex' not in built.namelist() or 'classes.dex' not in base.namelist(): raise ValueError('missing DEX')
            dex = built.read('classes.dex')
            if dex.count(expected_obb.encode('ascii')) != 1: raise ValueError('launcher does not pin the expected OBB')
            with zipfile.ZipFile(output, 'x', allowZip64=True) as out:
                for info in base.infolist():
                    if info.filename.startswith('META-INF/') or info.filename in overlap: continue
                    payload = (dex if info.filename == 'classes.dex' else guid if info.filename == 'assets/unity_obb_guid' else redacted[info.filename].read_bytes() if redacted and info.filename in redacted else base.read(info))
                    out.writestr(copy.copy(info), payload)
                for name in sorted(additions): out.writestr(copy.copy(built.getinfo(name)), built.read(name))
    except BaseException:
        output.unlink(missing_ok=True)
        raise
    print('Staged private recompiled-runtime Steam launcher; not distributable.')

def sha_bytes(data): return hashlib.sha256(data).hexdigest()

if __name__ == '__main__': main()
