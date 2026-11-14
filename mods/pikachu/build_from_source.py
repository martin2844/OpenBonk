#!/usr/bin/env python3
"""Build a local Pikachu mod ZIP from a separately obtained source FBX.

Outputs contain third-party character data. This does not establish rights to
the model or character. The original FBX, Blender and Unity are not included.
"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys


ROOT = Path(__file__).resolve().parent
REPO = ROOT.parent.parent


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    for name in ('source-fbx', 'blender', 'unity', 'output'):
        parser.add_argument('--' + name, required=True, type=Path)
    parser.add_argument('--runner', help='optional executable wrapper for the Unity editor')
    args = parser.parse_args()
    source, blender, unity = (getattr(args, name).expanduser().resolve(strict=True)
                              for name in ('source_fbx', 'blender', 'unity'))
    if any(not path.is_file() for path in (source, blender, unity)):
        parser.error('FBX and executables must be regular files')
    if not os.access(blender, os.X_OK) or not os.access(unity, os.X_OK):
        parser.error('Blender and Unity must be executable')
    if args.runner and not shutil.which(args.runner): parser.error('runner not found')
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir():
        parser.error('output must be a new directory in an existing directory')
    output = output.parent.resolve() / output.name
    if output.is_relative_to(REPO) or any(path.is_relative_to(output) for path in (source, blender, unity)):
        parser.error('output must be separate from the repository and inputs')
    output.mkdir(mode=0o700)
    work = output / 'authoring'; local_source = work / 'mods/pikachu/source'
    local_source.mkdir(parents=True)
    shutil.copy2(source, local_source / 'PikachuAnim.fbx')
    env = os.environ.copy(); env['OPENBONK_SKIP_STUDY_RENDERS'] = '1'

    def run(label, command, log, cwd, run_env):
        print(label, flush=True)
        with log.open('x', encoding='utf-8') as stream:
            subprocess.run(command, cwd=cwd, env=run_env, stdout=stream, stderr=subprocess.STDOUT, check=True)

    for label, script, log in (('Preparing model', 'render_study.py', 'model.log'),
                               ('Creating animations', 'create_movement.py', 'movement.log')):
        run(label, [str(blender), '--factory-startup', '-b', '-t', '2', '--python-exit-code', '1', '--python', str(ROOT / script)],
            output / log, work, env)
    model = work / 'mods/pikachu/study/pikachu-movement.fbx'
    if not model.is_file(): raise RuntimeError('Blender did not produce the animated FBX')
    project = output / 'unity-project'; assets = project / 'Assets/OpenBonkModAuthoring/Pikachu'
    assets.mkdir(parents=True); editor = project / 'Assets/Editor'; editor.mkdir()
    (project / 'Packages').mkdir(); (project / 'ProjectSettings').mkdir()
    dependencies = {'com.unity.modules.animation': '1.0.0', 'com.unity.modules.assetbundle': '1.0.0',
                    'com.unity.modules.audio': '1.0.0'}
    (project / 'Packages/manifest.json').write_text(json.dumps({'dependencies': dependencies}) + '\n', encoding='utf-8')
    shutil.copy2(REPO / 'unity/OpenBonk/ProjectSettings/ProjectVersion.txt', project / 'ProjectSettings/ProjectVersion.txt')
    shutil.copy2(model, assets / 'pikachu-study.fbx')
    shutil.copy2(ROOT / 'content/thunderbolt-zap.wav', assets / 'thunderbolt-zap.wav')
    shutil.copy2(REPO / 'unity/OpenBonk/Assets/Editor/OpenBonkPikachuBundleBuild.cs', editor / 'OpenBonkPikachuBundleBuild.cs')
    env['OPENBONK_PIKACHU_BUNDLE_OUTPUT'] = str(output / 'android-bundle')
    command = ([args.runner] if args.runner else []) + [str(unity), '-batchmode', '-nographics', '-quit',
        '-projectPath', str(project), '-buildTarget', 'Android', '-executeMethod', 'OpenBonkPikachuBundleBuild.Build',
        '-logFile', str(output / 'unity.log')]
    run('Building Android bundle', command, output / 'unity-console.log', work, env)
    unity_log = (output / 'unity.log').read_text(encoding='utf-8', errors='replace')
    if 'is not supported because the module' in unity_log:
        raise RuntimeError('Unity omitted a required module; inspect unity.log')
    bundle = output / 'android-bundle/pikachu-model'
    if not bundle.is_file(): raise RuntimeError('Unity did not produce the Android bundle; inspect unity.log')
    run('Packaging mod', [sys.executable, str(ROOT / 'build_test_package.py'), '--bundle', str(bundle),
                          '--output', str(output / 'pikachu-mod.zip')], output / 'package.log', work, env)
    print('Local mod package:', output / 'pikachu-mod.zip', flush=True)


if __name__ == '__main__':
    main()
