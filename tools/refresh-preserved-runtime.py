#!/usr/bin/env python3
"""Refresh runtime code in an existing launcher Gradle project, preserving its data.

The preserved launcher must already have a generated SplitExpectedData SHA256.
This does not build an APK, regenerate import plans, or update version numbers.
"""
import argparse
from pathlib import Path
import re
import shutil
import subprocess


METADATA = (
    "Managed/Metadata/global-metadata.dat",
    "RuntimeInitializeOnLoads.json",
    "ScriptingAssemblies.json",
)
LAUNCHER_SOURCES = (
    "LauncherContentImporter.java",
    "LauncherModImporter.java",
    "LauncherModIntegrity.java",
    "LauncherOnlineUi.java",
    "LauncherTransferState.java",
    "SplitDataLauncher.java",
)
NEW_LAUNCHER_SOURCES = {"LauncherTransferState.java"}
MAIN = Path("unityLibrary/src/main")
JAVA = Path("java/com/megabonk/launcher")


def require_file(path):
    if path.is_symlink() or not path.is_file():
        raise ValueError(f"Missing regular file: {path}")


def refresh(exported_project, preserved_project, assets_root, *, dry_run=False):
    exported_project = exported_project.resolve(strict=True)
    preserved_project = preserved_project.resolve(strict=True)
    assets_root = assets_root.resolve(strict=True)
    if exported_project.is_relative_to(preserved_project) or preserved_project.is_relative_to(exported_project):
        raise ValueError("Exported and preserved projects must be separate directories")
    source = exported_project / MAIN
    target = preserved_project / MAIN
    generated = target / JAVA / "SplitExpectedData.java"
    require_file(generated)
    hashes = re.findall(r'\bSHA256\s*=\s*"([^"\r\n]*)"', generated.read_text(encoding="utf-8"))
    if len(hashes) != 1 or not re.fullmatch(r"[0-9a-fA-F]{64}", hashes[0]) or int(hashes[0], 16) == 0:
        raise ValueError("SplitExpectedData.java needs a generated SHA256; EMBEDDED/unresolved values are not allowed")

    il2cpp_source, il2cpp_target = (p / "Il2CppOutputProject" for p in (source, target))
    for directory in (il2cpp_source, il2cpp_target):
        if directory.is_symlink() or not directory.is_dir():
            raise ValueError(f"Missing regular IL2CPP directory: {directory}")
    # Refuse an empty/wrong export before rsync --delete can remove old code.
    require_file(il2cpp_source / "Source/il2cppOutput/Il2CppCodeRegistration.cpp")
    copies = [(source / "assets/bin/Data" / name, target / "assets/bin/Data" / name) for name in METADATA]
    copies += [(assets_root / "AndroidSplitLauncher" / name, target / JAVA / name) for name in LAUNCHER_SOURCES]
    for origin, destination in copies:
        require_file(origin)
        # This explicitly added helper has no counterpart in older preserved
        # launchers. All existing runtime files must still be present.
        if destination.name not in NEW_LAUNCHER_SOURCES or destination.exists() or destination.is_symlink():
            require_file(destination)
        # Copying through a redirected ancestor could replace generated data.
        for path in (origin, destination):
            if any(parent.is_symlink() for parent in path.parents):
                raise ValueError(f"Symbolic link in refresh path: {path}")
    rsync = shutil.which("rsync")
    if rsync is None:
        raise ValueError("rsync is required")
    command = [rsync, "-a", "--delete", "--", str(il2cpp_source) + "/", str(il2cpp_target) + "/"]
    print(f"{'Would refresh' if dry_run else 'Refreshing'} IL2CPP: {il2cpp_source} -> {il2cpp_target}")
    for origin, destination in copies:
        print(f"{'Would copy' if dry_run else 'Copy'} {origin} -> {destination}")
    if dry_run:
        return
    subprocess.run(command, check=True)
    for origin, destination in copies:
        shutil.copy2(origin, destination)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("exported_project", type=Path, help="fresh exported Unity Gradle project root")
    parser.add_argument("preserved_project", type=Path, help="existing packaged launcher Gradle project root")
    parser.add_argument("assets_root", type=Path, help="checkout unity/OpenBonk/Assets directory")
    parser.add_argument("--dry-run", action="store_true", help="validate inputs and print the refresh without changing files")
    args = parser.parse_args()
    try:
        refresh(args.exported_project, args.preserved_project, args.assets_root, dry_run=args.dry_run)
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        parser.exit(1, f"Runtime refresh failed: {error}\n")


if __name__ == "__main__":
    main()
