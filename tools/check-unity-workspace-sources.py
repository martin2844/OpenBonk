#!/usr/bin/env python3
"""Compare checkout source files with an assembled Unity workspace, read-only.

This checks file contents and expected diagnostic exclusions, not whether every
source is compiled, asset references resolve, or gameplay matches another APK.
"""
import argparse
import hashlib
import json
from pathlib import Path


ROOT = Path(__file__).resolve().parent.parent
MARKER = {'schema': 1, 'kind': 'openbonk-local-unity-workspace'}
GROUPS = {
    'C#': {'.cs'},
    'Java': {'.java'},
    'shaders': {'.shader', '.cginc', '.hlsl', '.compute'},
}
OPTIONAL_DIAGNOSTIC_SHADERS = {
    Path('Resources/OpenBonkWaterBrdf1Probe.shader'),
    Path('Resources/OpenBonkWaterClampedProbe.shader'),
    Path('Resources/OpenBonkWaterRawProbe.shader'),
}


def digest(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.digest()


def compare(source, workspace):
    result = {}
    for label, extensions in GROUPS.items():
        paths = sorted(p for p in source.rglob('*') if p.is_file() and p.suffix in extensions)
        if not paths:
            raise ValueError('no ' + label + ' source files found')
        missing, changed, excluded = [], [], []
        for path in paths:
            relative = path.relative_to(source)
            target = workspace / relative
            if path.is_symlink() or target.is_symlink():
                raise ValueError('symbolic link in compared source: ' + relative.as_posix())
            if not target.is_file():
                (excluded if label == 'shaders' and relative in OPTIONAL_DIAGNOSTIC_SHADERS else missing).append(relative.as_posix())
            elif digest(path) != digest(target):
                changed.append(relative.as_posix())
        unexpected = []
        for target in sorted(p for p in workspace.rglob('*') if p.is_file() and p.suffix in extensions):
            relative = target.relative_to(workspace)
            original = source / relative
            if target.is_symlink() or original.is_symlink():
                raise ValueError('symbolic link in compared source: ' + relative.as_posix())
            if not original.is_file():
                unexpected.append(relative.as_posix())
        result[label] = {'total': len(paths), 'missing': missing, 'changed': changed,
                         'unexpected': unexpected, 'excluded_diagnostics': excluded}
    return result


def compare_launcher_sources(root, gradle):
    package = gradle / 'unityLibrary/src/main/java/com'
    groups = (
        (root / 'unity/OpenBonk/Assets/AndroidSplitLauncher', '*.java', package / 'megabonk/launcher'),
        (root / 'unity/OpenBonk/Assets/AndroidModPicker', '*.java', package / 'megabonk/modpicker'),
        (root / 'analysis/data-conversion/android', 'Steam*.java', package / 'openbonk/dataprep'),
        (root / 'analysis/data-conversion/android', 'LauncherDiagnosticGenerator.java', package / 'megabonk/launcher'),
    )
    missing, changed, generated = [], [], []
    total = 0
    for source, pattern, target in groups:
        paths = sorted(source.glob(pattern))
        if not paths:
            raise ValueError('no launcher sources found in ' + str(source))
        for path in paths:
            relative = path.relative_to(root).as_posix()
            if path.name == 'SplitExpectedData.java':
                generated.append(relative)  # Build-specific checksums replace this placeholder.
                continue
            total += 1
            destination = target / path.name
            if path.is_symlink() or destination.is_symlink():
                raise ValueError('symbolic link in launcher source: ' + relative)
            if not destination.is_file():
                missing.append(relative)
            elif digest(path) != digest(destination):
                changed.append(relative)
    return {'total': total, 'missing': missing, 'changed': changed, 'generated_placeholders': generated}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project', required=True, type=Path, help='assembled local Unity workspace')
    parser.add_argument('--launcher-gradle', type=Path, help='optional final research launcher Gradle directory')
    args = parser.parse_args()
    try:
        project = args.project.expanduser().resolve(strict=True)
        marker = project / '.openbonk-workspace.json'
        if json.loads(marker.read_text(encoding='utf-8')) != MARKER:
            raise ValueError('invalid workspace marker')
        result = compare(ROOT / 'unity/OpenBonk/Assets', project / 'Assets')
        if args.launcher_gradle:
            result['launcher Java'] = compare_launcher_sources(ROOT, args.launcher_gradle.expanduser().resolve(strict=True))
    except (OSError, ValueError, json.JSONDecodeError) as error:
        parser.error(str(error))
    print(json.dumps(result, indent=2, sort_keys=True))
    if any(group['missing'] or group['changed'] or group.get('unexpected') for group in result.values()):
        raise SystemExit(1)
    print('PASS: checkout sources match the assembled workspace; compilation and asset parity are not established.')


if __name__ == '__main__':
    main()
