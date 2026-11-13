#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname "$0")/../.." && pwd)
jdk=${OPENBONK_JDK_BIN:-/nix/store/2cv1b5y6h029cvg07c944pq89fszwbya-openjdk-21.0.12.1+1/bin}
json=${OPENBONK_JSON_JAR:-/home/your-user/.gradle/caches/modules-2/files-2.1/org.json/json/20180813/8566b2b0391d9d4479ea225645c6ed47ef17fe41/json-20180813.jar}
source="$repo/unity/OpenBonk/Assets/AndroidSplitLauncher"
out=$(mktemp -d)
trap 'rm -rf "$out"' EXIT
"$jdk/javac" -cp "$json" -d "$out" "$repo/tools/mod-recovery-check/android/content/SharedPreferences.java" "$source/LauncherContentImporter.java" "$source/LauncherModRecovery.java" "$repo/tools/mod-recovery-check/com/megabonk/launcher/RecoveryCheck.java" "$repo/tools/passive-validation/com/megabonk/launcher/PassiveCheck.java"
"$jdk/java" -cp "$out:$json" com.megabonk.launcher.PassiveCheck "$repo/mods/pikachu/study/pikachu-content-local-test.zip"
