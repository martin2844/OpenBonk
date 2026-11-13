#!/usr/bin/env bash
# Builds the private research adapter; zlib's upstream license is in zlib.h.
set -euo pipefail
if [ "$#" -ne 1 ] || [ -e "$1" ] || [ -L "$1" ] || [ ! -d "$(dirname "$1")" ]; then
 echo 'Usage: build-stable-zlib.sh NEW_OUTPUT_SO' >&2; exit 2
fi
root="$(cd "$(dirname "$0")/.." && pwd)"
editor="${OPENBONK_UNITY_EDITOR:-$HOME/Unity/Hub/Editor/2023.2.22f1/Editor}"
case "${OPENBONK_ZLIB_ARCH:-arm64}" in
 arm64) triple=aarch64-linux-android26 ;;
 x86_64) triple=x86_64-linux-android26 ;;
 *) echo 'Unsupported zlib target' >&2; exit 2 ;;
esac
clang="$editor/Data/PlaybackEngines/AndroidPlayer/NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/${triple}-clang"
runner=()
if [ "${OPENBONK_TOOL_RUNNER:-}" = none ]; then :
elif [ -n "${OPENBONK_TOOL_RUNNER:-}" ]; then runner=("$OPENBONK_TOOL_RUNNER")
elif command -v steam-run >/dev/null 2>&1; then runner=(steam-run); fi
if [ ! -f "$clang" ]; then echo 'Android NDK compiler not found' >&2; exit 1; fi
src="$root/third_party/zlib-1.3.2"
temp="$(mktemp "$(dirname "$1")/.openbonk-zlib-XXXXXXXX.so")"
trap 'rm -f "$temp"' EXIT
"${runner[@]}" "$clang" -shared -fPIC -fvisibility=hidden -DZ_PREFIX -Wl,-Bsymbolic -I"$src" \
 "$root/native/stable-zlib/openbonk_zlib.c" "$src/adler32.c" "$src/crc32.c" "$src/deflate.c" "$src/trees.c" "$src/zutil.c" -o "$temp"
mv -n "$temp" "$1"
if [ -e "$temp" ]; then echo 'Output appeared during build' >&2; exit 1; fi
echo "Built ${OPENBONK_ZLIB_ARCH:-arm64} stable zlib research adapter."
