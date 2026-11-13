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
out=${OPENBONK_LUA_OUTPUT:-../../build/lua}
if [[ -n "${OPENBONK_LUA_OUTPUT:-}" ]]; then
    if [[ -e "$out" || -L "$out" || ! -d "$(dirname "$out")" ]]; then
        echo 'OPENBONK_LUA_OUTPUT must be a new directory in an existing parent.' >&2
        exit 1
    fi
    mkdir -m 700 "$out"
else
    mkdir -p "$out"
fi
tar -xzf lua-5.4.9.tar.gz -C "$out"
src="$out/lua-5.4.9/src"
sources=()
for file in "$src"/*.c; do
    case "${file##*/}" in lua.c|luac.c) continue ;; esac
    sources+=("$file")
done
"$compiler" -O2 -fPIC -fvisibility=hidden -shared -I"$src" megabonk_lua.c "${sources[@]}" -lm -o "$out/libmegabonk_lua.so"
"$compiler" -O2 -I"$src" megabonk_lua.c smoke.c "${sources[@]}" -lm -o "$out/megabonk-lua-smoke"
printf 'Built %s\n' "$out/libmegabonk_lua.so"
