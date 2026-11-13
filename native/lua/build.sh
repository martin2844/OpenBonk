#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
echo '2335b6c582a52654f94612bf10d2f4672805d05329aa6568b1d8cd9e5c6fb8e6  lua-5.4.9.tar.gz' | sha256sum -c -
ndk=${ANDROID_NDK_HOME:-${ANDROID_NDK_ROOT:-}}
if [[ -z "$ndk" ]]; then echo "Set ANDROID_NDK_HOME or ANDROID_NDK_ROOT." >&2; exit 1; fi
compiler="$ndk/toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android23-clang"
if [[ ! -x "$compiler" ]]; then
    printf 'Android NDK compiler not found: %s\nSet ANDROID_NDK_HOME to a Linux Android NDK installation.\n' "$compiler" >&2
    exit 1
fi
mkdir -p ../../build/lua
tar -xzf lua-5.4.9.tar.gz -C ../../build/lua
src=../../build/lua/lua-5.4.9/src
sources=()
for file in "$src"/*.c; do
    case "${file##*/}" in lua.c|luac.c) continue ;; esac
    sources+=("$file")
done
"$compiler" -O2 -fPIC -fvisibility=hidden -shared -I"$src" megabonk_lua.c "${sources[@]}" -lm -o ../../build/lua/libmegabonk_lua.so
"$compiler" -O2 -I"$src" megabonk_lua.c smoke.c "${sources[@]}" -lm -o ../../build/lua/megabonk-lua-smoke
printf 'Built %s\n' ../../build/lua/libmegabonk_lua.so
