#!/bin/sh
set -eu
repo=$(CDPATH= cd -- "$(dirname "$0")/../.." && pwd)
out=$(mktemp -d)
trap 'rm -rf "$out"' EXIT
javac -d "$out" "$repo/unity/OpenBonk/Assets/AndroidSplitLauncher/LauncherTransferState.java" "$repo/tools/transfer-state-check/com/megabonk/launcher/TransferStateCheck.java"
java -cp "$out" com.megabonk.launcher.TransferStateCheck
