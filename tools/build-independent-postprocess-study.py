#!/usr/bin/env python3
"""Compile Unity post-processing shaders in a project without game data.

This research build does not make any APK distributable. Unity and the package
have their own terms; inspect the supplied package and applicable licenses.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'analysis/port-inventory/OpenBonkPostprocessStudy.cs'
PINNED_BUNDLE_SHA256 = '7cbb023315a2ad6722bf11e23cc0139b0208dea1d81f3202ad7ebcbc0851eb80'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--unity', required=True, type=Path, help='Unity 2023.2.22f1 executable')
    parser.add_argument('--package', required=True, type=Path, help='local com.unity.postprocessing 3.4.0 package')
    parser.add_argument('--output', required=True, type=Path, help='new directory for the isolated project and output')
    parser.add_argument('--graphics-settings', type=Path, help='optional local Unity GraphicsSettings.asset to study project-dependent shader variants')
    parser.add_argument('--vulkan-pretransform', action='store_true', help='enable Vulkan pretransform while compiling the isolated shader bundle')
    parser.add_argument('--verify-pinned', action='store_true', help='require the historical post-processing bundle hash (requires --vulkan-pretransform)')
    parser.add_argument('--runner', help='optional executable used to launch Unity')
    parser.add_argument('--check', action='store_true')
    args = parser.parse_args()
    if args.verify_pinned and not args.vulkan_pretransform:
        parser.error('--verify-pinned requires --vulkan-pretransform')
    unity = args.unity.expanduser().resolve(strict=True)
    package = args.package.expanduser().resolve(strict=True)
    graphics = None
    if args.graphics_settings:
        path = args.graphics_settings.expanduser()
        if path.is_symlink() or not path.is_file() or path.stat().st_size > 1024 * 1024:
            parser.error('graphics settings must be a regular file under 1 MiB')
        graphics = path.resolve(strict=True)
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new path in an existing directory')
    output = output.parent.resolve() / output.name
    if any(output.is_relative_to(p) or p.is_relative_to(output) for p in (ROOT, package, unity, *([graphics] if graphics else []))):
        parser.error('output must be separate from checkout and inputs')
    if not unity.is_file() or not os.access(unity, os.X_OK):
        parser.error('Unity executable unavailable')
    if not package.is_dir() or not (package / 'package.json').is_file():
        parser.error('package directory unavailable')
    info = json.loads((package / 'package.json').read_text())
    if (info.get('name'), info.get('version')) != ('com.unity.postprocessing', '3.4.0'):
        parser.error('Unity post-processing package 3.4.0 is required')
    if any(p.is_symlink() for p in package.rglob('*')):
        parser.error('package must not contain symbolic links')
    if not SOURCE.is_file():
        parser.error('study editor script missing')
    runner = shutil.which(args.runner) if args.runner else None
    if args.runner and not runner:
        parser.error('runner unavailable')
    print('Study inputs found. The package contents and terms are not verified.', flush=True)
    if args.check:
        return
    output.mkdir(mode=0o700)
    project = output / 'project'
    editor = project / 'Assets/Editor'
    editor.mkdir(parents=True)
    packages = project / 'Packages'
    packages.mkdir()
    settings = project / 'ProjectSettings'
    settings.mkdir()
    bundles = output / 'bundles'
    bundles.mkdir()
    # Embedded packages otherwise compile their optional test sources, which
    # require NUnit. They are unrelated to the shader bundle.
    shutil.copytree(package, packages / 'com.unity.postprocessing',
                    ignore=shutil.ignore_patterns('Tests', 'Tests.meta'))
    (packages / 'manifest.json').write_text(json.dumps({'dependencies': {
        'com.unity.postprocessing': '3.4.0', 'com.unity.modules.assetbundle': '1.0.0',
        'com.unity.modules.imgui': '1.0.0', 'com.unity.modules.jsonserialize': '1.0.0'}}) + '\n')
    (settings / 'ProjectVersion.txt').write_text('m_EditorVersion: 2023.2.22f1\n')
    if graphics: shutil.copyfile(graphics, settings / 'GraphicsSettings.asset')
    shutil.copyfile(SOURCE, editor / 'OpenBonkPostprocessStudy.cs')
    env = os.environ.copy()
    env['OPENBONK_POSTPROCESS_OUTPUT'] = str(bundles)
    if args.vulkan_pretransform: env['OPENBONK_POSTPROCESS_VULKAN_PRETRANSFORM'] = '1'
    else: env.pop('OPENBONK_POSTPROCESS_VULKAN_PRETRANSFORM', None)
    log = output / 'unity.log'
    cmd = ([runner] if runner else []) + [str(unity), '-batchmode', '-nographics', '-quit',
           '-projectPath', str(project), '-executeMethod', 'OpenBonkPostprocessStudy.Build', '-logFile', str(log)]
    status = subprocess.run(cmd, env=env).returncode
    log_text = log.read_text(errors='replace') if log.is_file() else ''
    if status or 'OPENBONK_MINIMAL_POSTPROCESS_SUCCESS' not in log_text:
        raise SystemExit(f'Unity study failed; inspect {log}')
    if not any(line.startswith('Unity Editor version:') and line.split()[3] == '2023.2.22f1'
               for line in log_text.splitlines() if len(line.split()) >= 4):
        raise SystemExit(f'Unexpected Unity editor version; inspect {log}')
    result = bundles / 'postprocess-study'
    if not result.is_file() or not result.stat().st_size:
        raise SystemExit(f'Unity did not produce the shader bundle; inspect {log}')
    if args.verify_pinned:
        with result.open('rb') as stream:
            if hashlib.file_digest(stream, 'sha256').hexdigest() != PINNED_BUNDLE_SHA256:
                raise SystemExit(f'Generated shader bundle differs from the pinned build; inspect {log}')
        print('Pinned shader bundle hash verified.', flush=True)
    print('Shader study bundle:', result, flush=True)
    print('Local research only; source provenance and distribution rights remain to be reviewed.', flush=True)


if __name__ == '__main__':
    main()
