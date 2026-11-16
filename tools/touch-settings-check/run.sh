#!/usr/bin/env bash
set -euo pipefail
repo=$(cd "$(dirname "$0")/../.." && pwd)
: "${OPENBONK_TEST_JSON_JAR:?Set OPENBONK_TEST_JSON_JAR to a local org.json jar}"
javac=${OPENBONK_JAVAC:-javac}
java=${OPENBONK_JAVA:-java}
runner=()
if [[ -n ${OPENBONK_TOOL_RUNNER:-} ]]; then runner=("$OPENBONK_TOOL_RUNNER"); fi
mkdir -p "$repo/build"
out=$(mktemp -d "$repo/build/touch-settings-check.XXXXXX")
trap 'rm -rf "$out"' EXIT
"${runner[@]}" "$javac" -source 8 -target 8 -cp "$OPENBONK_TEST_JSON_JAR" -d "$out" \
  "$repo/tools/touch-settings-check/android/content/Context.java" \
  "$repo/tools/touch-settings-check/android/content/SharedPreferences.java" \
  "$repo/unity/OpenBonk/Assets/AndroidSplitLauncher/LauncherTouchSettings.java" \
  "$repo/tools/touch-settings-check/com/megabonk/launcher/TouchSettingsCheck.java"
"${runner[@]}" "$java" -cp "$OPENBONK_TEST_JSON_JAR:$out" com.megabonk.launcher.TouchSettingsCheck
