#!/usr/bin/env python3
"""Rebuild private Android shader bundles from an isolated local Unity workspace.

The workspace and output can contain game-derived data. This builds shader
inputs, not an APK, and does not establish distribution rights.
"""
import argparse
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT/'analysis/port-inventory/OpenBonkComputeStudy.cs'
MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
SUCCESS = ('OPENBONK_COMPUTE_STUDY_OK 12', 'OPENBONK_RENDER_STUDY_OK 24', 'OPENBONK_LOCAL_STUDY_OK 17',
           'OPENBONK_INSTANCE_STUDY_OK 17', 'OPENBONK_BUILTIN_PROBE_OK', 'OPENBONK_SHADER_FEATURE_PROBE_OK')


def safe_copy(src, dst):
    """Use copy-on-write when supported; fall back to a normal copy."""
    if Path(dst).exists() or Path(dst).is_symlink(): raise FileExistsError(dst)
    created = False
    try:
        import fcntl
        with open(src, 'rb') as source, open(dst, 'xb') as target:
            created = True
            fcntl.ioctl(target.fileno(), 0x40049409, source.fileno())  # Linux FICLONE
        shutil.copystat(src, dst)
    except FileExistsError:
        raise
    except (ImportError, OSError):
        if created: Path(dst).unlink(missing_ok=True)
        with open(src, 'rb') as source, open(dst, 'xb') as target:
            shutil.copyfileobj(source, target)
        shutil.copystat(src, dst)
    return dst


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--project', type=Path, required=True, help='prepared local Steam-derived Unity workspace')
    p.add_argument('--unity', type=Path, required=True, help='Unity 2023.2.22f1 executable')
    p.add_argument('--output', type=Path, required=True, help='new private output directory')
    p.add_argument('--runner', help='optional Unity executable wrapper')
    p.add_argument('--check', action='store_true')
    args = p.parse_args()
    project = args.project.expanduser().resolve(strict=True); unity = args.unity.expanduser().resolve(strict=True)
    output = args.output.expanduser().absolute()
    if output.exists() or output.is_symlink() or not output.parent.is_dir(): p.error('output must be new')
    output = output.parent.resolve()/output.name
    if any(output.is_relative_to(x) or x.is_relative_to(output) for x in (ROOT, project, unity)):
        p.error('output must be separate from project and inputs')
    marker = project/'.openbonk-workspace.json'
    if marker.is_symlink() or not marker.is_file() or json.loads(marker.read_text()) != MARKER: p.error('project must be an isolated workspace')
    if not unity.is_file() or not os.access(unity, os.X_OK): p.error('Unity executable unavailable')
    if not (project/'ProjectSettings/ProjectVersion.txt').is_file() or 'm_EditorVersion: 2023.2.22f1' not in (project/'ProjectSettings/ProjectVersion.txt').read_text().splitlines():
        p.error('workspace requires Unity 2023.2.22f1')
    if (project/'Temp/UnityLockfile').exists(): p.error('workspace is in use')
    for name in ('Assets', 'Packages', 'ProjectSettings', 'Library'):
        folder = project/name
        if folder.is_symlink() or not folder.is_dir(): p.error('workspace is missing '+name)
        for base, dirs, files in os.walk(folder, followlinks=False):
            if any((Path(base)/entry).is_symlink() for entry in dirs + files): p.error('workspace contains a symbolic link')
    for name in ('Assets/Editor/OpenBonkComputeStudy.cs', 'Assets/Editor/OpenBonkComputeStudy.cs.meta',
                 'Assets/OpenBonkShaderFeatureProbeTemp', 'Assets/OpenBonkShaderFeatureProbeTemp.meta',
                 'Assets/OpenBonkBuiltinProbeTemp', 'Assets/OpenBonkBuiltinProbeTemp.meta'):
        path = project/name
        if path.exists() or path.is_symlink(): p.error('workspace already contains temporary shader inputs')
    runner = shutil.which(args.runner) if args.runner else None
    if args.runner and not runner: p.error('runner unavailable')
    if args.check:
        print('Initial shader workspace inputs checked; Unity has not run.')
        return
    spec = importlib.util.spec_from_file_location('pinned_shader_bundles', ROOT/'tools/pinned-shader-bundles.py')
    pins = importlib.util.module_from_spec(spec); spec.loader.exec_module(pins)
    with tempfile.TemporaryDirectory(prefix='.openbonk-shader-build-', dir=output.parent) as directory:
        temp = Path(directory); clone = temp/'project'; clone.mkdir()
        for name in ('Assets', 'Packages', 'ProjectSettings', 'Library'):
            shutil.copytree(project/name, clone/name, copy_function=safe_copy)
        script = clone/'Assets/Editor/OpenBonkComputeStudy.cs'; script.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(SOURCE, script)
        bundles = temp/'bundles'; bundles.mkdir()
        log = temp/'unity.log'; env = os.environ.copy(); env['OPENBONK_COMPUTE_STUDY_OUT'] = str(bundles)
        command = ([runner] if runner else []) + [str(unity), '-batchmode', '-nographics', '-quit',
                   '-projectPath', str(clone), '-executeMethod', 'OpenBonkComputeStudy.Build', '-logFile', str(log)]
        status = subprocess.run(command, env=env).returncode
        text = log.read_text(errors='replace') if log.is_file() else ''
        if status or any(marker not in text for marker in SUCCESS):
            raise SystemExit('Unity shader build failed or did not complete; '+text[-3000:])
        if not any(line.startswith('Unity Editor version:') and len(line.split()) >= 4 and line.split()[3] == '2023.2.22f1'
                   for line in text.splitlines()):
            raise SystemExit('Unexpected Unity editor version in shader build')
        pins.verify(bundles)
        pins.stage(bundles, output)
    print('Generated and verified 17 private local shader bundles; this is not an APK.')


if __name__ == '__main__': main()
