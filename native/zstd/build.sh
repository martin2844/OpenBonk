#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
echo 'eb33e51f49a15e023950cd7825ca74a4a2b43db8354825ac24fc1b7ee09e6fa3  zstd-1.5.7.tar.gz' | sha256sum -c -
ndk=${ANDROID_NDK_HOME:-${ANDROID_NDK_ROOT:-}}
if [[ -z "$ndk" ]]; then echo 'Set ANDROID_NDK_HOME or ANDROID_NDK_ROOT.' >&2; exit 1; fi
compiler="$ndk/toolchains/llvm/prebuilt/linux-x86_64/bin/aarch64-linux-android26-clang"
if [[ ! -x "$compiler" ]]; then echo 'Android NDK compiler unavailable.' >&2; exit 1; fi
if [[ $# -ne 1 || -e "$1" || -L "$1" || ! -d "$(dirname "$1")" ]]; then
    echo 'Usage: build.sh NEW_OUTPUT_DIRECTORY' >&2; exit 2
fi
mkdir -m 700 "$1"
out=$(cd "$1" && pwd)
tar -xzf zstd-1.5.7.tar.gz -C "$out"
src="$out/zstd-1.5.7/lib"
sources=("$src"/common/*.c "$src"/decompress/*.c)
"$compiler" -O2 -fPIC -fvisibility=hidden -DZSTD_DISABLE_ASM -shared -I"$src" \
    obb_migration.c "${sources[@]}" -o "$out/libopenbonk_migration.so"
printf 'Built %s\n' "$out/libopenbonk_migration.so"
