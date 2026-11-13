#!/usr/bin/env bash
# Sign a local candidate with an explicitly supplied key. This does not make
# any APK or its contents suitable for distribution.
set -euo pipefail
if [ "$#" -ne 4 ]; then
 echo 'Usage: sign-beta-candidate.sh INPUT_APK NEW_OUTPUT_APK KEYSTORE KEY_ALIAS' >&2
 exit 2
fi
input="$(realpath "$1")"
output="$(realpath -m "$2")"
keystore="$(realpath "$3")"
alias="$4"
if [ ! -f "$input" ] || [ ! -f "$keystore" ] || [ -e "$output" ] || [ -L "$output" ] || [ ! -d "$(dirname "$output")" ] || [ -z "$alias" ]; then
 echo 'Invalid input, output, keystore or alias' >&2; exit 2
fi
if [ -z "${OPENBONK_BETA_KEYSTORE_PASSWORD:-}" ] || [ -z "${OPENBONK_BETA_KEY_PASSWORD:-}" ]; then
 echo 'Set OPENBONK_BETA_KEYSTORE_PASSWORD and OPENBONK_BETA_KEY_PASSWORD in the environment' >&2; exit 2
fi
editor="${OPENBONK_UNITY_EDITOR:-$HOME/Unity/Hub/Editor/2023.2.22f1/Editor}"
android="$editor/Data/PlaybackEngines/AndroidPlayer"
export JAVA_HOME="$android/OpenJDK" PATH="$android/OpenJDK/bin:$PATH"
runner=()
if [ "${OPENBONK_TOOL_RUNNER:-}" = none ]; then :
elif [ -n "${OPENBONK_TOOL_RUNNER:-}" ]; then runner=("$OPENBONK_TOOL_RUNNER")
elif command -v steam-run >/dev/null 2>&1; then runner=(steam-run); fi
for bin in "$android/SDK/build-tools/34.0.0/zipalign" "$android/SDK/build-tools/34.0.0/apksigner"; do
 if [ ! -f "$bin" ]; then echo 'Android build tools not found' >&2; exit 2; fi
done
# Keep intermediates in the destination filesystem so the final link is atomic.
work="$(mktemp -d "$(dirname "$output")/.openbonk-sign-XXXXXXXX")"
trap 'rm -f -- "$work/aligned.apk" "$work/signed.apk" "$work/signed.apk.idsig"; rmdir -- "$work"' EXIT
"${runner[@]}" "$android/SDK/build-tools/34.0.0/zipalign" -f -p 4 "$input" "$work/aligned.apk"
"${runner[@]}" "$android/SDK/build-tools/34.0.0/apksigner" sign --ks "$keystore" --ks-key-alias "$alias" \
 --ks-pass env:OPENBONK_BETA_KEYSTORE_PASSWORD --key-pass env:OPENBONK_BETA_KEY_PASSWORD \
 --out "$work/signed.apk" "$work/aligned.apk"
"${runner[@]}" "$android/SDK/build-tools/34.0.0/apksigner" verify --print-certs "$work/signed.apk"
# A hard link fails if the destination appeared while signing.
ln "$work/signed.apk" "$output"
echo 'Signed local candidate; review contents and distribution rights separately.'
