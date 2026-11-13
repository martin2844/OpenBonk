#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname "$0")/../.." && pwd)
out=$(mktemp -d)
trap 'rm -rf "$out"' EXIT
javac -d "$out" "$repo/tools/mod-recovery-check/android/content/SharedPreferences.java" "$repo/unity/OpenBonk/Assets/AndroidSplitLauncher/LauncherModRecovery.java" "$repo/tools/mod-recovery-check/com/megabonk/launcher/RecoveryCheck.java"
java -cp "$out" com.megabonk.launcher.RecoveryCheck
