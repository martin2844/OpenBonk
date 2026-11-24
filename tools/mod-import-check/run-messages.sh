#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname "$0")/../.." && pwd)
out=$(mktemp -d "$repo/build/.mod-message-check-XXXXXXXX")
trap 'rm -rf "$out"' EXIT
jdk=${OPENBONK_JAVA_HOME:-"$HOME/Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK"}
android=${OPENBONK_ANDROID_JAR:-"$HOME/Unity/Hub/Editor/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platforms/android-35/android.jar"}
src="$repo/unity/OpenBonk/Assets/AndroidSplitLauncher"
steam-run "$jdk/bin/javac" -cp "$android" -d "$out" \
  "$src/LauncherImportError.java" \
  "$repo/tools/mod-import-check/com/megabonk/launcher/ModImportMessageCheck.java"
steam-run "$jdk/bin/java" -cp "$out:$android" com.megabonk.launcher.ModImportMessageCheck
