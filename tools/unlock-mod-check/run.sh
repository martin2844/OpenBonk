#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname "$0")/../.." && pwd)
jdk=${OPENBONK_JDK_BIN:-/nix/store/2cv1b5y6h029cvg07c944pq89fszwbya-openjdk-21.0.12.1+1/bin}
json=${OPENBONK_JSON_JAR:-/home/martin/megabonk/build/json-20180813-maven.jar}
src="$repo/unity/OpenBonk/Assets/AndroidSplitLauncher"
out=$(mktemp -d)
trap 'rm -rf "$out"' EXIT
"$jdk/javac" -cp "$json" -d "$out" "$repo/tools/mod-recovery-check/android/content/SharedPreferences.java" "$repo/tools/mod-recovery-check/com/megabonk/launcher/RecoveryCheck.java" "$repo/tools/unlock-mod-check/SplitDataLauncher.java" "$repo/tools/unlock-mod-check/UnlockImportCheck.java" "$src/LauncherModHookValidation.java" "$src/LauncherModImporter.java" "$src/LauncherModIntegrity.java" "$src/LauncherModRecovery.java" "$src/LauncherContentImporter.java"
"$jdk/java" -cp "$out:$json" com.megabonk.launcher.UnlockImportCheck "$repo/mods/unlock-everything" "$repo/mods/instant-collect" "$repo/mods/lucky-tome" "$repo/mods/auto-pots" "$repo/mods/item-lab" "$repo/mods/run-metrics" "$repo/mods/offer-tools"
