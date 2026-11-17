#!/usr/bin/env bash
set -euo pipefail
repo=$(cd "$(dirname "$0")/../.." && pwd)
check_dir=$(mktemp -d)
trap 'rm -rf -- "$check_dir"' EXIT
"${JAVAC:-javac}" --release 8 -d "$check_dir" \
  "$repo/analysis/data-conversion/android/SteamUnityFs.java" \
  "$repo/tools/unityfs-check/com/openbonk/dataprep/UnityFsCheck.java"
"${JAVA:-java}" -cp "$check_dir" com.openbonk.dataprep.UnityFsCheck "$@"
