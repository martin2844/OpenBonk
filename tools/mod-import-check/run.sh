#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname "$0")/../.." && pwd)
out=$(mktemp -d "$repo/build/.mod-import-check-XXXXXXXX")
trap 'rm -rf "$out"' EXIT
jdk=${OPENBONK_JAVA_HOME:-"$HOME/Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK"}
json=${OPENBONK_TEST_JSON_JAR:-"$repo/build/android/gradle-isolated/caches/modules-2/files-2.1/org.json/json/20180813/8566b2b0391d9d4479ea225645c6ed47ef17fe41/json-20180813.jar"}
src="$repo/reconstruction/AssetRipperExport/ExportedProject/Assets/AndroidSplitLauncher"
steam-run "$jdk/bin/javac" -cp "$json" -d "$out" \
  "$repo/tools/mod-recovery-check/android/content/SharedPreferences.java" \
  "$repo/tools/mod-recovery-check/com/megabonk/launcher/RecoveryCheck.java" \
  "$repo/tools/mod-import-check/com/megabonk/launcher/SplitDataLauncher.java" \
  "$repo/tools/mod-import-check/com/megabonk/launcher/ContentFolderCheck.java" \
  "$src/LauncherModIntegrity.java" "$src/LauncherModRecovery.java" "$src/LauncherModImporter.java" "$src/LauncherContentImporter.java"
steam-run "$jdk/bin/java" -cp "$out:$json" com.megabonk.launcher.ContentFolderCheck "${1:-$repo/mods/example-healing-item/example-healing-item.zip}"
