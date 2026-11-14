#!/usr/bin/env python3
"""Build a local-only Android launcher prototype for a generated audio plan.

The template and resulting APK contain game-derived data and must stay local.
This prototype is not a release build and uses Unity's debug signing setup.
"""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import struct
import subprocess
import tarfile
import zipfile


ROOT = Path(__file__).resolve().parent.parent
JAVA = ROOT / 'analysis/data-conversion/android'


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def entry_digest(archive, name):
    with archive.open(name) as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def verify(template, plan_path, expected, runtime, gradle):
    plan = json.loads(plan_path.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-steam-audio-offsets' or plan.get('template_sha256') != digest(template):
        raise ValueError('audio plan or template differs')
    rows = {row['entry']: row for row in plan['records']}
    if not rows or len(rows) != len(plan['records']):
        raise ValueError('audio plan contains duplicate or missing records')
    with zipfile.ZipFile(template) as base, zipfile.ZipFile(expected) as result, zipfile.ZipFile(runtime) as player:
        names = base.namelist()
        if len(names) != len(set(names)) or names != result.namelist():
            raise ValueError('template and expected archive entries differ')
        if not set(rows).issubset(names):
            raise ValueError('audio plan references missing entries')
        guid = base.read('unity_obb_guid')
        if len(guid) != 36 or guid != result.read('unity_obb_guid') or guid != player.read('assets/unity_obb_guid'):
            raise ValueError('runtime and OBB expansion identifiers differ')
        for name in names:
            if name in rows:
                row = rows[name]
                with base.open(name) as stream:
                    size = 0
                    while block := stream.read(65536):
                        size += len(block)
                        if any(block):
                            raise ValueError('audio template is not blank')
                if size != row['size'] or entry_digest(result, name) != row['sha256']:
                    raise ValueError('expected audio differs from plan')
            elif entry_digest(base, name) != entry_digest(result, name):
                raise ValueError('unrelated expected OBB entry differs')
        for name in ('ScriptingAssemblies.json', 'RuntimeInitializeOnLoads.json', 'Managed/Metadata/global-metadata.dat'):
            file = gradle / 'unityLibrary/src/main/assets/bin/Data' / name
            if not file.is_file() or digest(file) != entry_digest(player, 'assets/bin/Data/' + name):
                raise ValueError('Gradle project and runtime code metadata differ: ' + name)
        if 'lib/arm64-v8a/libil2cpp.so' not in player.namelist():
            raise ValueError('runtime lacks ARM64 IL2CPP library')
    return guid


def verify_packed(template, plan_path, intermediate):
    plan = json.loads(plan_path.read_text())
    if plan.get('schema') != 1 or plan.get('kind') != 'experimental-packed-steam-audio-offsets' or plan.get('template_sha256') != digest(template):
        raise ValueError('packed audio plan or template differs')
    allowed = {'assets/bin/Data/sharedassets1.resource', 'assets/bin/Data/sharedassets2.resource',
               'assets/bin/Data/sharedassets4.resource'}
    rows = plan.get('records')
    if not isinstance(rows, list) or not rows or any(r.get('entry') not in allowed for r in rows):
        raise ValueError('invalid packed audio records')
    with zipfile.ZipFile(template) as base, zipfile.ZipFile(intermediate) as result:
        names = base.namelist()
        if len(names) != len(set(names)) or names != result.namelist() or not {r['entry'] for r in rows}.issubset(names):
            raise ValueError('packed archive entries differ')
        for name in names:
            selected = sorted((r for r in rows if r['entry'] == name), key=lambda r: r['target_offset'])
            if not selected:
                if entry_digest(base, name) != entry_digest(result, name):
                    raise ValueError('unrelated packed archive entry differs')
                continue
            a, b = base.read(name), result.read(name)
            if len(a) != len(b): raise ValueError('packed resource length differs')
            end = 0
            for row in selected:
                start, size = row['target_offset'], row['size']
                if type(start) is not int or type(size) is not int or start < end or size <= 0 or start + size > len(a):
                    raise ValueError('invalid packed range')
                if a[end:start] != b[end:start] or any(a[start:start + size]) or hashlib.sha256(b[start:start + size]).hexdigest() != row['sha256']:
                    raise ValueError('packed payload differs')
                end = start + size
            if a[end:] != b[end:]: raise ValueError('unrelated packed resource data differs')


def verify_textures(template, plan_path, intermediate, cubemaps=False):
    plan = json.loads(plan_path.read_text())
    kind = 'experimental-converted-steam-cubemap-offsets' if cubemaps else 'experimental-converted-steam-texture-offsets'
    if plan.get('schema') != 1 or plan.get('kind') != kind or plan.get('template_sha256') != digest(template):
        raise ValueError('texture plan or template differs')
    rows = plan.get('records')
    if not isinstance(rows, list) or not rows or len(rows) > (32 if cubemaps else 5000):
        raise ValueError('invalid texture records')
    by_entry = {}
    results = {}
    total = 0
    for i, row in enumerate(rows):
        size = row.get('target_size')
        if row.get('id') != i or type(size) is not int or not 0 < size <= 64 * 1024 * 1024:
            raise ValueError('invalid texture record')
        quality = row.get('quality', False)
        if type(quality) is not bool or (quality and (cubemaps or row.get('normal', False) or row.get('target_format') != 47 or
                                                       type(row.get('width')) is not int or type(row.get('height')) is not int or
                                                       not 1 <= row['width'] <= 2048 or not 1 <= row['height'] <= 2048)):
            raise ValueError('invalid quality texture metadata')
        if cubemaps and size != 6 * 131072: raise ValueError('invalid cubemap size')
        total += size
        if total > 256 * 1024 * 1024: raise ValueError('texture size limit exceeded')
        fs = row.get('fragments')
        if not isinstance(fs, list) or not fs: raise ValueError('missing texture fragments')
        position = 0
        for frag in fs:
            entry, start, amount, offset = (frag.get(k) for k in ('entry', 'target_offset', 'size', 'converted_offset'))
            if (not isinstance(entry, str) or not re.fullmatch(r'assets/bin/Data/(?:sharedassets[12]\.assets\.split(?:0|[1-9]\d*)|[0-9a-f]{32})', entry) or
                    any(type(v) is not int for v in (start, amount, offset)) or start < 0 or amount <= 0 or offset != position or amount > size - position):
                raise ValueError('invalid texture fragment')
            if cubemaps and not entry.startswith('assets/bin/Data/sharedassets1.assets.split'): raise ValueError('invalid cubemap entry')
            by_entry.setdefault(entry, []).append((start, amount, offset, i))
            position += amount
        if position != size: raise ValueError('incomplete texture fragments')
        results[i] = bytearray(size)
    with zipfile.ZipFile(template) as base, zipfile.ZipFile(intermediate) as result:
        names = base.namelist()
        if len(names) != len(set(names)) or names != result.namelist() or not set(by_entry).issubset(names):
            raise ValueError('texture archive entries differ')
        for name in names:
            selected = sorted(by_entry.get(name, []))
            if not selected:
                if entry_digest(base, name) != entry_digest(result, name): raise ValueError('unrelated texture archive entry differs')
                continue
            a, b = base.read(name), result.read(name)
            if len(a) != len(b): raise ValueError('texture split length differs')
            end = 0
            for start, amount, offset, i in selected:
                if start < end or start + amount > len(a) or a[end:start] != b[end:start] or any(a[start:start + amount]):
                    raise ValueError('texture template range differs')
                results[i][offset:offset + amount] = b[start:start + amount]
                end = start + amount
            if a[end:] != b[end:]: raise ValueError('unrelated texture split bytes differ')
    if any(hashlib.sha256(results[i]).hexdigest() != row['target_sha256'] for i, row in enumerate(rows)):
        raise ValueError('converted texture checksum differs')


def verify_meshes(template, plan_path, intermediate, objects=False):
    plan = json.loads(plan_path.read_text())
    kind = 'experimental-exact-steam-object-offsets' if objects else 'experimental-steam-mesh-buffer-offsets'
    if plan.get('schema') != 1 or plan.get('kind') != kind or plan.get('template_sha256') != digest(template):
        raise ValueError('mesh plan or template differs')
    rows = plan.get('records')
    if not isinstance(rows, list) or not rows or len(rows) > 10000: raise ValueError('invalid mesh records')
    grouped = {}
    for row in rows:
        entry = row.get('entry')
        pattern = (r'(?:assets/bin/Data/(?:[0-9a-f]{32}|(?:sharedassets\d+\.assets|level\d+|resources\.assets)(?:\.split(?:0|[1-9]\d*))?)|assets/aa/StandaloneLinux64/localization-(?:assets-shared_assets_all|string-tables-[a-z0-9()_-]+_assets_all)\.bundle)'
                   if objects else r'assets/bin/Data/sharedassets[124]\.assets\.split(?:0|[1-9]\d*)')
        if not isinstance(entry, str) or not re.fullmatch(pattern, entry):
            raise ValueError('invalid mesh entry')
        grouped.setdefault(entry, []).append(row)
    with zipfile.ZipFile(template) as base, zipfile.ZipFile(intermediate) as result:
        names = base.namelist()
        if len(names) != len(set(names)) or names != result.namelist() or not set(grouped).issubset(names):
            raise ValueError('mesh archive entries differ')
        for name in names:
            if name not in grouped:
                if entry_digest(base, name) != entry_digest(result, name): raise ValueError('unrelated mesh archive entry differs')
                continue
            a, b = base.read(name), result.read(name)
            if len(a) != len(b): raise ValueError('mesh split length differs')
            end = 0
            for row in sorted(grouped[name], key=lambda r: r['target_offset']):
                start, size = row['target_offset'], row['size']
                if type(start) is not int or type(size) is not int or start < end or size <= 0 or start + size > len(a):
                    raise ValueError('invalid mesh range')
                if a[end:start] != b[end:start] or any(a[start:start + size]) or hashlib.sha256(b[start:start + size]).hexdigest() != row['sha256']:
                    raise ValueError('mesh payload differs')
                end = start + size
            if a[end:] != b[end:]: raise ValueError('unrelated mesh split bytes differ')


def verify_animations(template, plan_path, intermediate, partial_rgba=False):
    plan = json.loads(plan_path.read_text())
    kind = 'experimental-partial-rgba-texture-offsets' if partial_rgba else 'experimental-animation-fragment-mask'
    if plan.get('schema') != 1 or plan.get('kind') != kind or plan.get('template_sha256') != digest(template):
        raise ValueError('animation plan or template differs')
    rows = plan.get('records')
    if not isinstance(rows, list) or not rows or len(rows) > 5000: raise ValueError('invalid animation records')
    if partial_rgba:
        fragments = []
        for i, row in enumerate(rows):
            fmt, quality = row.get('target_format'), row.get('quality', False)
            if (row.get('id') != i or type(quality) is not bool or
                    not ((fmt == 4 and not quality) or (fmt in (45, 47) and quality)) or
                    not isinstance(row.get('fragments'), list) or not row['fragments']):
                raise ValueError('invalid partial texture record')
            for fragment in row['fragments']:
                offset, size, total = fragment.get('converted_offset'), fragment.get('size'), row.get('target_size')
                if any(type(x) is not int for x in (offset, size, total)) or offset < 0 or size <= 0 or offset + size > total: raise ValueError('invalid partial texture range')
                fragments.append(fragment)
        rows = fragments
        if len(rows) > 10000: raise ValueError('too many partial texture fragments')
    grouped = {}
    for row in rows:
        entry = row.get('entry')
        if not isinstance(entry, str) or not re.fullmatch(r'assets/bin/Data/(?:[0-9a-f]{32}|(?:sharedassets\d+\.assets|level\d+|resources\.assets)(?:\.split(?:0|[1-9]\d*))?)', entry):
            raise ValueError('invalid animation entry')
        grouped.setdefault(entry, []).append(row)
    with zipfile.ZipFile(template) as base, zipfile.ZipFile(intermediate) as result:
        names = base.namelist()
        if len(names) != len(set(names)) or names != result.namelist() or not set(grouped).issubset(names): raise ValueError('animation archive entries differ')
        for name in names:
            if name not in grouped:
                if entry_digest(base, name) != entry_digest(result, name): raise ValueError('unrelated animation archive entry differs')
                continue
            a, b = base.read(name), result.read(name)
            if len(a) != len(b): raise ValueError('animation entry length differs')
            end = 0
            for row in sorted(grouped[name], key=lambda r: r['target_offset']):
                start, size = row['target_offset'], row['size']
                if type(start) is not int or type(size) is not int or start < end or size <= 0 or start + size > len(a): raise ValueError('invalid animation range')
                bits = base64.b64decode(row['mask'], validate=True)
                if (len(bits) != (size + 7) // 8 or (size % 8 and bits[-1] >> (size % 8)) or
                        sum(x.bit_count() for x in bits) != row['masked_bytes'] or row['masked_bytes'] <= 0): raise ValueError('invalid animation mask')
                if a[end:start] != b[end:start] or hashlib.sha256(a[start:start + size]).hexdigest() != row['masked_sha256'] or hashlib.sha256(b[start:start + size]).hexdigest() != row['original_sha256']:
                    raise ValueError('animation fragment checksum differs')
                for i in range(size):
                    if bits[i // 8] & (1 << (i % 8)):
                        if a[start + i] != 0 or b[start + i] == 0: raise ValueError('invalid masked animation byte')
                    elif a[start + i] != b[start + i]: raise ValueError('unmasked animation byte differs')
                end = start + size
            if a[end:] != b[end:]: raise ValueError('unrelated animation entry bytes differ')


def property_value(value):
    result = []
    for c in str(value):
        if c in '\\ =:#!': result.append('\\' + c)
        elif ord(c) < 32 or ord(c) > 126:
            raw = c.encode('utf-16-be')
            result.extend('\\u%04x' % int.from_bytes(raw[i:i + 2], 'big') for i in range(0, len(raw), 2))
        else: result.append(c)
    return ''.join(result)


def replace_once(path, pattern, replacement, dotall=False):
    text = path.read_text(encoding='utf-8')
    text, count = re.subn(pattern, lambda _: replacement, text, flags=re.MULTILINE | (re.DOTALL if dotall else 0))
    if count != 1:
        raise ValueError('unexpected Gradle project structure: ' + path.name)
    path.write_text(text, encoding='utf-8')


def verify_research_platform(plan, platform):
    inventories = json.loads((ROOT / 'tools/launcher-steam-inputs.json').read_text(encoding='utf-8'))['platforms']
    selected = inventories[platform]; other = inventories['windows' if platform == 'linux' else 'linux']
    sources = json.loads(plan.read_text(encoding='utf-8')).get('sources')
    if not isinstance(sources, dict) or not sources: raise ValueError('research plan has no Steam sources')
    if any(selected.get(name, {}).get('sha256') != value for name, value in sources.items()):
        raise ValueError(platform + ' research plan uses incompatible Steam sources')
    if not any(other.get(name, {}).get('sha256') != value for name, value in sources.items()):
        raise ValueError('research plan does not distinguish Steam platforms')


def verify_windows_research(root, expected, runtime, gradle, partial_rgba=False):
    """Validate a second complete seven-stage plan against the same runtime."""
    canonical = root / 'canonical'
    template = root / 'plans/objects/template.obb'
    obj = canonical / 'objects-plan.json' if partial_rgba else root / 'plans/objects/plan.json'
    partial_template = root / 'plans/partial-rgba/template.obb'
    partial_plan = root / 'plans/partial-rgba/android-plan.json'
    partial_output = canonical / 'after-partial-rgba.obb'
    paths = {name: canonical / (name + '-plan.json') for name in ('animation', 'cubemap', 'mesh', 'texture', 'packed', 'audio')}
    outputs = {name: canonical / ('after-' + name + '.obb') for name in ('objects', 'animation', 'cubemap', 'mesh', 'texture', 'packed')}
    final = canonical / 'final.obb'
    for path in (template, obj, final, *paths.values(), *outputs.values(), *([partial_template, partial_plan, partial_output] if partial_rgba else [])):
        if path.is_symlink() or not path.is_file(): raise ValueError('incomplete Windows research inputs')
    if digest(final) != digest(expected): raise ValueError('Windows and Linux final OBBs differ')
    verify_research_platform(obj, 'windows')
    if partial_rgba: verify_animations(partial_template, partial_plan, partial_output, partial_rgba=True)
    verify_meshes(partial_output if partial_rgba else template, obj, outputs['objects'], objects=True)
    verify_animations(outputs['objects'], paths['animation'], outputs['animation'])
    verify_textures(outputs['animation'], paths['cubemap'], outputs['cubemap'], cubemaps=True)
    verify_meshes(outputs['cubemap'], paths['mesh'], outputs['mesh'])
    verify_textures(outputs['mesh'], paths['texture'], outputs['texture'])
    verify_packed(outputs['texture'], paths['packed'], outputs['packed'])
    verify(outputs['packed'], paths['audio'], final, runtime, gradle)
    if partial_rgba: paths['partial-rgba'] = partial_plan
    return partial_template if partial_rgba else template, obj, paths


def has_quality_textures(plan):
    return any(row.get('quality', False) for row in json.loads(plan.read_text(encoding='utf-8'))['records'])


def gradle_command(java, gradle_jar, runner=None, online=False):
    return ([runner] if runner else []) + [str(java), '-classpath', str(gradle_jar),
        'org.gradle.launcher.GradleMain', *([] if online else ['--offline']), '--no-daemon', ':launcher:assembleRelease']


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('gradle-project', 'unity-project', 'runtime-apk', 'audio-template', 'audio-plan', 'expected-obb', 'unity-editor', 'output'):
        p.add_argument('--' + name, type=Path, required=True)
    p.add_argument('--application-id', required=True)
    p.add_argument('--runner', help='optional executable such as steam-run')
    p.add_argument('--gradle-user-home', type=Path, help='existing local Gradle cache')
    p.add_argument('--gradle-online', action='store_true', help='allow Gradle to download missing dependencies')
    p.add_argument('--packed-plan', type=Path, help='optional packed-audio plan for the combined research mode')
    p.add_argument('--intermediate-obb', type=Path, help='canonical packed-audio restoration used by the single-audio plan')
    p.add_argument('--texture-plan', type=Path, help='optional converted texture plan for combined research mode')
    p.add_argument('--texture-intermediate-obb', type=Path, help='canonical texture restoration used by the packed plan')
    p.add_argument('--texture-library', type=Path, help='local ARM64 texture converter library')
    p.add_argument('--mesh-plan', type=Path, help='optional mesh plan for combined research mode')
    p.add_argument('--mesh-intermediate-obb', type=Path, help='canonical mesh restoration used by the texture plan')
    p.add_argument('--cubemap-plan', type=Path, help='optional sky cubemap plan for combined research mode')
    p.add_argument('--cubemap-intermediate-obb', type=Path, help='canonical cubemap restoration used by the mesh plan')
    p.add_argument('--animation-plan', type=Path, help='optional animation mask plan for combined research mode')
    p.add_argument('--animation-intermediate-obb', type=Path, help='canonical animation restoration used by the cubemap plan')
    p.add_argument('--object-plan', type=Path, help='optional exact object plan for combined research mode')
    p.add_argument('--object-intermediate-obb', type=Path, help='canonical object restoration used by the animation plan')
    p.add_argument('--partial-rgba-plan', type=Path, help='optional partial RGBA restoration plan')
    p.add_argument('--partial-rgba-intermediate-obb', type=Path, help='canonical partial RGBA restoration used by the object plan')
    p.add_argument('--windows-research', type=Path, help='optional validated Windows seven-stage research output')
    p.add_argument('--check', action='store_true')
    a = p.parse_args()
    gradle, project, runtime, template, plan, expected, editor = [x.expanduser().resolve(strict=True) for x in
        (a.gradle_project, a.unity_project, a.runtime_apk, a.audio_template, a.audio_plan, a.expected_obb, a.unity_editor)]
    if bool(a.packed_plan) != bool(a.intermediate_obb): p.error('packed plan and intermediate OBB must be supplied together')
    packed = a.packed_plan.expanduser().resolve(strict=True) if a.packed_plan else None
    intermediate = a.intermediate_obb.expanduser().resolve(strict=True) if a.intermediate_obb else None
    texture_args = (a.texture_plan, a.texture_intermediate_obb, a.texture_library)
    if any(texture_args) and (not all(texture_args) or not packed): p.error('texture mode requires all texture inputs and packed audio mode')
    texture_plan, texture_intermediate, texture_library = ([x.expanduser().resolve(strict=True) for x in texture_args] if all(texture_args) else (None, None, None))
    if bool(a.mesh_plan) != bool(a.mesh_intermediate_obb) or (a.mesh_plan and not texture_plan): p.error('mesh mode requires both mesh inputs and texture mode')
    mesh_plan = a.mesh_plan.expanduser().resolve(strict=True) if a.mesh_plan else None
    mesh_intermediate = a.mesh_intermediate_obb.expanduser().resolve(strict=True) if a.mesh_intermediate_obb else None
    if bool(a.cubemap_plan) != bool(a.cubemap_intermediate_obb) or (a.cubemap_plan and not mesh_plan): p.error('cubemap mode requires both cubemap inputs and mesh mode')
    cubemap_plan = a.cubemap_plan.expanduser().resolve(strict=True) if a.cubemap_plan else None
    cubemap_intermediate = a.cubemap_intermediate_obb.expanduser().resolve(strict=True) if a.cubemap_intermediate_obb else None
    if bool(a.animation_plan) != bool(a.animation_intermediate_obb) or (a.animation_plan and not cubemap_plan): p.error('animation mode requires both animation inputs and cubemap mode')
    animation_plan = a.animation_plan.expanduser().resolve(strict=True) if a.animation_plan else None
    animation_intermediate = a.animation_intermediate_obb.expanduser().resolve(strict=True) if a.animation_intermediate_obb else None
    if bool(a.object_plan) != bool(a.object_intermediate_obb) or (a.object_plan and not animation_plan): p.error('object mode requires both object inputs and animation mode')
    object_plan = a.object_plan.expanduser().resolve(strict=True) if a.object_plan else None
    object_intermediate = a.object_intermediate_obb.expanduser().resolve(strict=True) if a.object_intermediate_obb else None
    if bool(a.partial_rgba_plan) != bool(a.partial_rgba_intermediate_obb) or (a.partial_rgba_plan and not object_plan): p.error('partial RGBA mode requires both inputs and object mode')
    partial_plan = a.partial_rgba_plan.expanduser().resolve(strict=True) if a.partial_rgba_plan else None
    partial_intermediate = a.partial_rgba_intermediate_obb.expanduser().resolve(strict=True) if a.partial_rgba_intermediate_obb else None
    windows_research = a.windows_research.expanduser().resolve(strict=True) if a.windows_research else None
    if windows_research and (not windows_research.is_dir() or not object_plan): p.error('Windows research requires a directory and complete seven-stage Linux inputs')
    output = a.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be a new directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (ROOT, gradle, project, runtime, template, plan, expected, *([packed, intermediate] if packed else []), *([texture_plan, texture_intermediate, texture_library] if texture_plan else []), *([mesh_plan, mesh_intermediate] if mesh_plan else []), *([cubemap_plan, cubemap_intermediate] if cubemap_plan else []), *([animation_plan, animation_intermediate] if animation_plan else []), *([object_plan, object_intermediate] if object_plan else []), *([partial_plan, partial_intermediate] if partial_plan else []), *([windows_research] if windows_research else []))):
        p.error('output must be separate from source and inputs')
    if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*(?:\.[A-Za-z][A-Za-z0-9_]*)+', a.application_id, re.ASCII): p.error('invalid application ID')
    if a.runner and not shutil.which(a.runner): p.error('configured runner not found')
    android = editor / 'Data/PlaybackEngines/AndroidPlayer'
    java, gradle_jar = android / 'OpenJDK/bin/java', android / 'Tools/gradle/lib/gradle-launcher-7.6.jar'
    for path in (java, gradle_jar, android / 'SDK/build-tools/34.0.0/aapt2', gradle / 'settings.gradle', gradle / 'unityLibrary/build.gradle', gradle / 'launcher/build.gradle'):
        if not path.is_file(): p.error('required build input is missing: ' + path.name)
    marker = project / '.openbonk-workspace.json'
    if not marker.is_file() or marker.is_symlink() or json.loads(marker.read_text()) != {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}:
        p.error('Unity project must be an isolated workspace')
    version = project / 'ProjectSettings/ProjectVersion.txt'
    if not version.is_file() or 'm_EditorVersion: 2023.2.22f1' not in version.read_text().splitlines():
        p.error('Unity project requires version 2023.2.22f1')
    if a.gradle_user_home and not a.gradle_user_home.expanduser().resolve(strict=True).is_dir(): p.error('Gradle cache must be a directory')
    for path in gradle.rglob('*'):
        if path.is_symlink(): p.error('Gradle project must not contain symbolic links')
    if partial_plan: verify_animations(template, partial_plan, partial_intermediate, partial_rgba=True)
    if object_plan:
        verify_meshes(partial_intermediate or template, object_plan, object_intermediate, objects=True)
    if animation_plan:
        verify_animations(object_intermediate or template, animation_plan, animation_intermediate)
    if cubemap_plan:
        verify_textures(animation_intermediate or template, cubemap_plan, cubemap_intermediate, cubemaps=True)
    if mesh_plan:
        verify_meshes(cubemap_intermediate or template, mesh_plan, mesh_intermediate)
    if texture_plan:
        with texture_library.open('rb') as f: header = f.read(20)
        if len(header) != 20 or header[:6] != b'\x7fELF\x02\x01' or struct.unpack_from('<H', header, 18)[0] != 183: p.error('texture library must be ARM64 ELF')
        verify_textures(mesh_intermediate or template, texture_plan, texture_intermediate)
    if packed:
        verify_packed(texture_intermediate or template, packed, intermediate)
    guid = verify(intermediate or template, plan, expected, runtime, gradle)
    if windows_research: verify_research_platform(object_plan, 'linux')
    windows = verify_windows_research(windows_research, expected, runtime, gradle, partial_rgba=bool(partial_plan)) if windows_research else None
    quality_needed = bool((texture_plan and has_quality_textures(texture_plan)) or
                          (partial_plan and has_quality_textures(partial_plan)) or
                          (windows and (has_quality_textures(windows[2]['texture']) or
                                        (partial_plan and has_quality_textures(windows[2]['partial-rgba'])))))
    if quality_needed and (texture_library.stat().st_size > 64 * 1024 * 1024 or b'Java_com_openbonk_dataprep_SteamConvertedTextures_convertQualityMip' not in texture_library.read_bytes()):
        raise ValueError('Android texture library lacks the quality encoder')
    print('Local prototype prerequisites verified; output contains game data.', flush=True)
    if a.check:
        print('Check only; no APK was built.')
        return
    output.mkdir(mode=0o700)
    work = output / 'gradle'
    shutil.copytree(gradle, work, ignore=shutil.ignore_patterns('build', '.gradle', 'symbols'))
    library = work / 'unityLibrary'
    source = library / 'src/main/java'
    launcher = source / 'com/megabonk/launcher'
    dataprep = source / 'com/openbonk/dataprep'
    dataprep.mkdir(parents=True, exist_ok=True)
    for file in JAVA.glob('Steam*.java'): shutil.copy2(file, dataprep / file.name)
    shutil.copy2(JAVA / 'LauncherDiagnosticGenerator.java', launcher / 'LauncherDiagnosticGenerator.java')
    assets = library / 'src/main/assets/openbonk-diagnostic'
    assets.mkdir()
    (assets / 'marker').write_bytes(b'OPENBONK_PRIVATE_DIAGNOSTIC_V1')
    mode = b'OPENBONK_GENERATED_AUDIO_RESEARCH_V8' if partial_plan else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V7' if object_plan else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V6' if animation_plan else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V5' if cubemap_plan else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V4' if mesh_plan else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V3' if texture_plan else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V2' if packed else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V1'
    (assets / 'generated-audio-mode').write_bytes(mode)
    shutil.copyfile(template, assets / 'generated-audio-template.obb')
    shutil.copyfile(plan, assets / 'generated-audio-plan.json')
    (assets / 'generated-audio-plan.sha256').write_text(digest(plan), encoding='ascii')
    if packed:
        shutil.copyfile(packed, assets / 'packed-audio-plan.json')
        (assets / 'packed-audio-plan.sha256').write_text(digest(packed), encoding='ascii')
    if texture_plan:
        shutil.copyfile(texture_plan, assets / 'texture-plan.json')
        (assets / 'texture-plan.sha256').write_text(digest(texture_plan), encoding='ascii')
        dest = library / 'src/main/jniLibs/arm64-v8a/libopenbonk_texture.so'
        if dest.exists() or dest.is_symlink(): raise ValueError('unexpected existing texture library')
        shutil.copyfile(texture_library, dest)
    if mesh_plan:
        shutil.copyfile(mesh_plan, assets / 'mesh-plan.json')
        (assets / 'mesh-plan.sha256').write_text(digest(mesh_plan), encoding='ascii')
    if cubemap_plan:
        shutil.copyfile(cubemap_plan, assets / 'cubemap-plan.json')
        (assets / 'cubemap-plan.sha256').write_text(digest(cubemap_plan), encoding='ascii')
    if animation_plan:
        shutil.copyfile(animation_plan, assets / 'animation-plan.json')
        (assets / 'animation-plan.sha256').write_text(digest(animation_plan), encoding='ascii')
    if object_plan:
        shutil.copyfile(object_plan, assets / 'object-plan.json')
        (assets / 'object-plan.sha256').write_text(digest(object_plan), encoding='ascii')
    if partial_plan:
        shutil.copyfile(partial_plan, assets / 'partial-rgba-plan.json')
        (assets / 'partial-rgba-plan.sha256').write_text(digest(partial_plan), encoding='ascii')
    if windows:
        win_template, win_object, win_plans = windows
        dest = assets / 'windows'; dest.mkdir()
        (assets / 'dual-platform').write_bytes(b'OPENBONK_DUAL_RESEARCH_V1')
        (dest / 'generated-audio-mode').write_bytes(b'OPENBONK_GENERATED_AUDIO_RESEARCH_V8' if partial_plan else b'OPENBONK_GENERATED_AUDIO_RESEARCH_V7')
        shutil.copyfile(win_template, dest / 'generated-audio-template.obb')
        plan_files = {'generated-audio': win_plans['audio'], 'packed-audio': win_plans['packed'],
                      'texture': win_plans['texture'], 'mesh': win_plans['mesh'], 'cubemap': win_plans['cubemap'],
                      'animation': win_plans['animation'], 'object': win_object}
        if partial_plan: plan_files['partial-rgba'] = win_plans['partial-rgba']
        for name, path in plan_files.items():
            shutil.copyfile(path, dest / (name + '-plan.json'))
            (dest / (name + '-plan.sha256')).write_text(digest(path), encoding='ascii')
    if quality_needed:
        source = ROOT / 'third_party/etc2comp-39422c1.tar.gz'
        if source.is_symlink() or not source.is_file() or digest(source) != 'ba661f81c69fa4eecb2654afcacb0639617a619d964d6baf2310997c75f4381b':
            raise ValueError('quality encoder license source differs')
        notices = assets / 'notices'; notices.mkdir(exist_ok=True)
        with tarfile.open(source, 'r:gz') as archive:
            for name in ('LICENSE', 'AUTHORS'):
                member = archive.getmember('etc2comp-39422c1/' + name)
                if not member.isfile() or member.size > 64 * 1024: raise ValueError('invalid quality encoder notice')
                with archive.extractfile(member) as stream:
                    (notices / ('etc2comp-' + name + '.txt')).write_bytes(stream.read())
    (library / 'src/main/assets/unity_obb_guid').write_bytes(guid)
    result_hash = digest(expected)
    (launcher / 'SplitExpectedData.java').write_text('package com.megabonk.launcher;\nfinal class SplitExpectedData { static final String SHA256 = "' + result_hash + '", LEGACY_SHA256 = "", SECOND_LEGACY_SHA256 = "", THIRD_LEGACY_SHA256 = "", FOURTH_LEGACY_SHA256 = "", FIFTH_LEGACY_SHA256 = "", SIXTH_LEGACY_SHA256 = ""; }\n', encoding='ascii')
    replace_once(work / 'launcher/build.gradle', r'applicationId\s+"[^"\n]+"', 'applicationId "' + a.application_id + '"')
    replace_once(library / 'build.gradle', r'    afterEvaluate \{\n.*?\n    \}\n    sourceSets', '    sourceSets', dotall=True)
    ndk = str(android / 'NDK')
    if any(ord(c) < 32 for c in ndk): raise ValueError('invalid NDK path')
    for module in ('launcher', 'unityLibrary'):
        replace_once(work / module / 'build.gradle', r'^[ \t]*ndkPath[ \t]+[^\n]+$', '    ndkPath "' + ndk.replace('\\', '\\\\').replace('"', '\\"') + '"')
    (work / 'local.properties').write_text('sdk.dir=' + property_value(android / 'SDK') + '\n', encoding='ascii')
    properties = work / 'gradle.properties'
    replace_once(properties, r'^unityProjectPath=.*$', 'unityProjectPath=' + property_value(project))
    replace_once(properties, r'^android\.aapt2FromMavenOverride=.*$', 'android.aapt2FromMavenOverride=' + property_value(android / 'SDK/build-tools/34.0.0/aapt2'))
    jni = library / 'src/main/jniLibs/arm64-v8a/libil2cpp.so'
    if jni.exists() or jni.is_symlink(): raise ValueError('unexpected existing IL2CPP library')
    with zipfile.ZipFile(runtime) as apk, jni.open('xb') as target, apk.open('lib/arm64-v8a/libil2cpp.so') as source_file:
        shutil.copyfileobj(source_file, target)
    env = os.environ.copy()
    env['JAVA_HOME'] = str(android / 'OpenJDK')
    env['PATH'] = str(android / 'OpenJDK/bin') + os.pathsep + env.get('PATH', '')
    if a.gradle_user_home: env['GRADLE_USER_HOME'] = str(a.gradle_user_home.expanduser().resolve(strict=True))
    command = gradle_command(java, gradle_jar, a.runner, a.gradle_online)
    with (output / 'gradle.log').open('x', encoding='utf-8') as log:
        status = subprocess.run(command, cwd=work, env=env, stdout=log, stderr=subprocess.STDOUT).returncode
    if status: raise SystemExit(f'Gradle failed ({status}); local output and log retained at {output}')
    apk = work / 'launcher/build/outputs/apk/release/launcher-release.apk'
    if not apk.is_file() or not apk.stat().st_size: raise SystemExit('Gradle did not produce the expected APK')
    print('Local research APK created:', apk, flush=True)
    print('It contains game-derived data and must not be distributed.', flush=True)


if __name__ == '__main__':
    main()
