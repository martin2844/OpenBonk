#!/usr/bin/env bash
# Local research only. The inputs and resulting APK contain private data and
# have not been cleared for distribution.
set -euo pipefail
if [ "$#" -ne 2 ]; then echo "Usage: $0 PLAN_DIRECTORY NEW_OUTPUT_DIRECTORY" >&2; exit 2; fi
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
plan_dir="$(realpath "$1")"
out_dir="$(realpath -m "$2")"
if [ ! -d "$plan_dir" ] || [ -e "$out_dir" ] || [ ! -d "$(dirname "$out_dir")" ]; then echo 'Invalid input or output directory' >&2; exit 2; fi
case "$out_dir/" in "$plan_dir/"*) echo 'Output must not be inside plans' >&2; exit 2;; esac
unity_editor="${OPENBONK_UNITY_EDITOR:-$HOME/Unity/Hub/Editor/2023.2.22f1/Editor}"
unity_android="$unity_editor/Data/PlaybackEngines/AndroidPlayer"
launcher_src="$repo_root/unity/OpenBonk/Assets/AndroidSplitLauncher"
unity_project="${OPENBONK_UNITY_PROJECT:-$repo_root/unity/OpenBonk}"
template="${OPENBONK_GRADLE_TEMPLATE:-$repo_root/build/android/launcher-cleanup-gradle}"
if [ ! -d "$unity_project" ] || [ ! -d "$template" ]; then echo 'Missing Unity project or Gradle template' >&2; exit 1; fi
unity_project="$(realpath "$unity_project")"
template="$(realpath "$template")"
case "$out_dir/" in "$template/"*|"$unity_project/"*) echo 'Output must be outside the Unity project and Gradle template' >&2; exit 2;; esac
base="$repo_root/build/android/launcher-steam-picker-integrated/OpenBonk-diagnostic.apk"
runtime_mode="${OPENBONK_RUNTIME_RESEARCH:-0}"
if [ "$runtime_mode" != 0 ] && [ "$runtime_mode" != 1 ] && [ "$runtime_mode" != 2 ]; then echo 'Invalid OPENBONK_RUNTIME_RESEARCH' >&2; exit 2; fi
no_water_probes="${OPENBONK_NO_WATER_PROBES:-0}"
if [ "$no_water_probes" != 0 ] && [ "$no_water_probes" != 1 ]; then echo 'Invalid OPENBONK_NO_WATER_PROBES' >&2; exit 2; fi
if [ "$no_water_probes" = 1 ] && [ "$runtime_mode" != 2 ]; then echo 'Reduced base requires runtime research mode 2' >&2; exit 2; fi
scrub_chests="${OPENBONK_SCRUB_CHEST_CLIPS:-$no_water_probes}"
if [ "$scrub_chests" != 0 ] && [ "$scrub_chests" != 1 ]; then echo 'Invalid OPENBONK_SCRUB_CHEST_CLIPS' >&2; exit 2; fi
if [ "$scrub_chests" = 1 ] && [ "$no_water_probes" != 1 ]; then echo 'Chest scrub requires reduced base' >&2; exit 2; fi
scrub_shared2="${OPENBONK_SCRUB_SHARED2_CLIPS:-$scrub_chests}"
if [ "$scrub_shared2" != 0 ] && [ "$scrub_shared2" != 1 ]; then echo 'Invalid OPENBONK_SCRUB_SHARED2_CLIPS' >&2; exit 2; fi
if [ "$scrub_shared2" = 1 ] && [ "$scrub_chests" != 1 ]; then echo 'Shared2 scrub requires chest scrub' >&2; exit 2; fi
scrub_particles="${OPENBONK_SCRUB_PARTICLES:-$scrub_shared2}"
if [ "$scrub_particles" != 0 ] && [ "$scrub_particles" != 1 ]; then echo 'Invalid OPENBONK_SCRUB_PARTICLES' >&2; exit 2; fi
if [ "$scrub_particles" = 1 ] && [ "$scrub_shared2" != 1 ]; then echo 'Particle scrub requires reduced shared2 base' >&2; exit 2; fi
scrub_shared1_particles="${OPENBONK_SCRUB_SHARED1_PARTICLES:-$scrub_particles}"
if [ "$scrub_shared1_particles" != 0 ] && [ "$scrub_shared1_particles" != 1 ]; then echo 'Invalid OPENBONK_SCRUB_SHARED1_PARTICLES' >&2; exit 2; fi
if [ "$scrub_shared1_particles" = 1 ] && [ "$scrub_particles" != 1 ]; then echo 'Shared1 particle scrub requires shared2 particle scrub' >&2; exit 2; fi
scrub_scene_components="${OPENBONK_SCRUB_SCENE_COMPONENTS:-$scrub_shared1_particles}"
if [ "$scrub_scene_components" != 0 ] && [ "$scrub_scene_components" != 1 ]; then echo 'Invalid OPENBONK_SCRUB_SCENE_COMPONENTS' >&2; exit 2; fi
if [ "$scrub_scene_components" = 1 ] && [ "$scrub_shared1_particles" != 1 ]; then echo 'Scene scrub requires shared1 particle scrub' >&2; exit 2; fi
scrub_shared_components="${OPENBONK_SCRUB_SHARED_COMPONENTS:-$scrub_scene_components}"
if [ "$scrub_shared_components" != 0 ] && [ "$scrub_shared_components" != 1 ]; then echo 'Invalid OPENBONK_SCRUB_SHARED_COMPONENTS' >&2; exit 2; fi
if [ "$scrub_shared_components" = 1 ] && [ "$scrub_scene_components" != 1 ]; then echo 'Shared component scrub requires scene scrub' >&2; exit 2; fi
scrub_materials="${OPENBONK_SCRUB_MATERIALS:-0}"
if [ "$scrub_materials" != 0 ] && [ "$scrub_materials" != 1 ]; then echo 'Invalid OPENBONK_SCRUB_MATERIALS' >&2; exit 2; fi
if [ "$scrub_materials" = 1 ] && [ "$scrub_shared_components" != 1 ]; then echo 'Material scrub requires shared component scrub' >&2; exit 2; fi
runtime_apk="${OPENBONK_RUNTIME_APK:-$repo_root/build/android/Megabonk-arm64-runtime-research.apk}"
redact_paths="${OPENBONK_REDACT_LOCAL_PATHS:-0}"
if [ "$redact_paths" != 0 ] && [ "$redact_paths" != 1 ]; then echo 'Invalid OPENBONK_REDACT_LOCAL_PATHS' >&2; exit 2; fi
if [ "$redact_paths" = 1 ] && [ "$runtime_mode" != 2 ]; then echo 'Path redaction requires runtime research mode 2' >&2; exit 2; fi
runtime_obb="$repo_root/build/android/stored-video-probe/android-rebased-video.obb"
runtime_plan="$repo_root/build/android/runtime-steam-compatibility/private-runtime-rebase-plan-v2.zip"
if [ "$runtime_mode" = 2 ]; then runtime_obb="$repo_root/build/android/no-rewired-current-rebase/android-video.obb"; fi
if [ "$no_water_probes" = 1 ]; then runtime_obb="$repo_root/build/android/water-probe-template/new-video.obb"; fi
texture_lib="${OPENBONK_TEXTURE_LIBRARY:-$repo_root/build/android/shared1-cubemap-plan/libtexture-arm64.so}"
python_cmd="${OPENBONK_PYTHON:-python3}"
# The pinned runtimes below read the legacy cache profile path. Never assemble
# them with a launcher that writes the new persistent queue: that combination
# would report a queued profile which the game cannot consume.
if grep -Fq 'string dir = files.Call<string>("getAbsolutePath")' "$unity_project/Assets/Scripts/Assembly-CSharp/AndroidProfileImport.cs"; then
 if [ "$runtime_mode" = 1 ]; then expected_runtime=f30a535b8f36151d0a80714e869d45ed7270aee17c55e415ad3d65240ce56ca4
 elif [ "$runtime_mode" = 2 ]; then expected_runtime=435618a799dc8821f713d28f3005d2338315916cc3fd604ad3ac521e9e7691be
 else expected_runtime=none; fi
 actual_runtime=$(sha256sum "$runtime_apk" 2>/dev/null | cut -d' ' -f1 || true)
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = 8eca98041e4d38ead18fe49a50d352936f6fae0344c71b4ada4bece1b5387735 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = 590a68f00c9459e1c709a8abe89061a25a818ebe7f4c6f1305f9f17c681f9d58 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = f6f6c256fc274b34957c67a10f2c70deed22231e42b0a4908853bb90e7677de4 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = 29d8b0a9b0ea951c0051a2ec7c48f0b7afccd3f55ec80df80533e59274f64410 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = dabfdee55c8bd2c56d1f2213a70aa5764465b99366e8143350df073ea74be301 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = dee2b0c36667e845f42acc1793a395b0fa0030025d4a212c467f03829dc86672 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = 31a4913f3004b13c6c91acaf312f55f53a87c89ed3a49c056053eb99a78cfb60 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = 9b39fabb6995b5f36d65875e4762df5b3cd08a554545463be17bee20545e2a92 ]; then expected_runtime="$actual_runtime"; fi
 if [ "$runtime_mode" = 2 ] && [ "$actual_runtime" = cfc632ec59451bbc1e3db4b25a4f451912dfa3d059da92b591484bc08897e574 ]; then expected_runtime="$actual_runtime"; fi
 if [ ! -f "$runtime_apk" ] || [ "$actual_runtime" != "$expected_runtime" ]; then
  echo 'Persistent profile queue requires the newly rebuilt research runtime; refusing incompatible assembly.' >&2
  exit 1
 fi
fi
tool_runner=()
if [ "${OPENBONK_TOOL_RUNNER:-}" = none ]; then
 :
elif [ -n "${OPENBONK_TOOL_RUNNER:-}" ]; then
 tool_runner=("$OPENBONK_TOOL_RUNNER")
elif command -v steam-run >/dev/null 2>&1; then
 tool_runner=(steam-run)
fi
for program in "$python_cmd" rsync sha256sum; do
 if ! command -v "$program" >/dev/null 2>&1; then echo "Missing required program: $program" >&2; exit 1; fi
done
if [ "${#tool_runner[@]}" -gt 0 ] && ! command -v "${tool_runner[0]}" >/dev/null 2>&1; then
 echo 'Configured tool runner not found' >&2; exit 1
fi
for input in "$texture_lib" "$plan_dir/animation-private.bin" \
 "$unity_android/OpenJDK/bin/java" "$unity_android/Tools/gradle/lib/gradle-launcher-7.6.jar" \
 "$unity_android/NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/llvm-strip" \
 "$unity_android/SDK/build-tools/34.0.0/zipalign" "$unity_android/SDK/build-tools/34.0.0/apksigner"; do
 if [ ! -f "$input" ]; then echo "Missing required input: $input" >&2; exit 1; fi
done
if [ "$runtime_mode" != 0 ]; then
 for input in "$runtime_apk" "$runtime_obb"; do
  if [ ! -f "$input" ]; then echo "Missing runtime research input: $input" >&2; exit 1; fi
 done
 if [ "$runtime_mode" = 1 ] && [ ! -f "$runtime_plan" ]; then echo 'Missing runtime rebase plan' >&2; exit 1; fi
else
 if [ ! -f "$base" ]; then echo "Missing required input: $base" >&2; exit 1; fi
fi
if [ ! -d "$template" ] || [ ! -d "$launcher_src" ]; then echo 'Missing Gradle template or launcher source directory' >&2; exit 1; fi
shader_args=()
if [ -n "${OPENBONK_SHADER_PACK_DIR:-}" ]; then shader_args=(--shader-dir "$OPENBONK_SHADER_PACK_DIR"); fi
"$python_cmd" "$repo_root/tools/package-conversion-plans.py" "$plan_dir" --check "${shader_args[@]}"
minimal_shared0="$repo_root/build/android/shared0-no-sky-font-base/base.zip"
if [ ! -f "$minimal_shared0" ]; then "$python_cmd" "$repo_root/analysis/data-conversion/build-minimal-shared0-base.py"; fi
mkdir "$out_dir"
"$python_cmd" "$repo_root/analysis/data-conversion/generate-shared2-animation-masks.py" "$repo_root/analysis/data-conversion/shared2-animation-position-mask.json" "$out_dir/SteamShared2AnimationMasks.java"
cmp "$out_dir/SteamShared2AnimationMasks.java" "$repo_root/analysis/data-conversion/android/SteamShared2AnimationMasks.java"
"$python_cmd" "$repo_root/analysis/data-conversion/generate-shared2-particle-masks.py" "$repo_root/analysis/data-conversion/shared2-particle-position-mask.json" "$out_dir/SteamShared2ParticleMasks.java"
cmp "$out_dir/SteamShared2ParticleMasks.java" "$repo_root/analysis/data-conversion/android/SteamShared2ParticleMasks.java"
"$python_cmd" "$repo_root/analysis/data-conversion/generate-shared1-particle-masks.py" "$repo_root/analysis/data-conversion/shared1-particle-position-mask.json" "$out_dir/SteamShared1ParticleMasks.java"
cmp "$out_dir/SteamShared1ParticleMasks.java" "$repo_root/analysis/data-conversion/android/SteamShared1ParticleMasks.java"
mkdir "$out_dir/gradle"
rsync -a --exclude='build/' --exclude='.gradle/' --exclude='symbols/' "$template/" "$out_dir/gradle/"
work="$out_dir/gradle"
"$python_cmd" - "$work" "$unity_android" "$unity_project" <<'PY'
import pathlib, re, sys
work, unity_android, project = map(pathlib.Path, sys.argv[1:])

def property_value(value):
    result = []
    for c in str(value):
        if c in '\\ =:#!': result.append('\\' + c)
        elif ord(c) < 32 or ord(c) > 126:
            for i in range(0, len(c.encode('utf-16-be')), 2):
                result.append('\\u%04x' % int.from_bytes(c.encode('utf-16-be')[i:i+2], 'big'))
        else: result.append(c)
    return ''.join(result)

def replace_once(path, pattern, replacement):
    text = path.read_text(encoding='utf-8')
    text, count = re.subn(pattern, lambda _: replacement, text, flags=re.MULTILINE)
    if count != 1: raise ValueError(f'unexpected Gradle template: {path.name}')
    path.write_text(text, encoding='utf-8')

(work / 'local.properties').write_text('sdk.dir=' + property_value(unity_android / 'SDK') + '\n', encoding='ascii')
replace_once(work / 'gradle.properties', r'^unityProjectPath=.*$', 'unityProjectPath=' + property_value(project))
ndk = str(unity_android / 'NDK')
if any(ord(c) < 32 for c in ndk): raise ValueError('control character in NDK path')
quoted_ndk = "'" + ndk.replace('\\', '\\\\').replace("'", "\\'") + "'"
for module in ('launcher', 'unityLibrary'):
    replace_once(work / module / 'build.gradle', r'^[ \t]*ndkPath[ \t]+.*$', '    ndkPath ' + quoted_ndk)
PY
java_dir="$work/unityLibrary/src/main/java"
assets="$work/unityLibrary/src/main/assets/openbonk-diagnostic"
mkdir -p "$java_dir/com/openbonk/dataprep" "$assets"
for file in "$repo_root"/analysis/data-conversion/android/Steam*.java; do
 cp "$file" "$java_dir/com/openbonk/dataprep/"
done
for file in "$launcher_src"/*.java; do
 if [ "$(basename "$file")" != SplitExpectedData.java ]; then cp "$file" "$java_dir/com/megabonk/launcher/"; fi
done
cp "$repo_root/analysis/data-conversion/android/LauncherDiagnosticGenerator.java" "$java_dir/com/megabonk/launcher/"
animation_plan_sha="$(sha256sum "$plan_dir/animation-private.bin" | cut -d' ' -f1)"
second_legacy_sha=
third_legacy_sha=
fourth_legacy_sha=
fifth_legacy_sha=
sixth_legacy_sha=
case "$animation_plan_sha" in
 c6344331782f79bcc2c26dfb10ecfc942ef435c6e31bd237a5bca49c9b8aeaec) obb_sha=ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9; legacy_sha=;;
 4c7f8b9ac3561896589ceae08e074a6e6a0f0216e9347fcfc8e05ef52d62da0a) obb_sha=576d0f7edf17c550d6ea3af4c623604c54984fbdc1c7a9c8d71839dc6aa6568a; legacy_sha=ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9;;
 5234be766f35adcef52b6b8114d0273f0be1f5a60d3e89d78e9e90a14e12ba89) obb_sha=30c2ee4f9c979a6dd0a963e0fa83967225f7b8eb68c3425e0bf739f7455db99d; legacy_sha=576d0f7edf17c550d6ea3af4c623604c54984fbdc1c7a9c8d71839dc6aa6568a; second_legacy_sha=ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9;;
 4c31e0fa6c7c9b95bfbbccac7cc2786e6ed5e5d4d6faa59ddaf4b0a4f6528cfb) obb_sha=d81d59963ef4f7664638f59b96fb5c6735b12a4b4ae340d0edbc8cbd5731e564; legacy_sha=30c2ee4f9c979a6dd0a963e0fa83967225f7b8eb68c3425e0bf739f7455db99d; second_legacy_sha=576d0f7edf17c550d6ea3af4c623604c54984fbdc1c7a9c8d71839dc6aa6568a; third_legacy_sha=ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9;;
 c744c59fa5d60e6f73285e8277c7615db8fd38201298d6466ca7d4bde47a04a7) obb_sha=334ca54ad010cb754b11e548bd5401d83dc7f4f7853aa9c6a294692e248d1fc1; legacy_sha=d81d59963ef4f7664638f59b96fb5c6735b12a4b4ae340d0edbc8cbd5731e564; second_legacy_sha=30c2ee4f9c979a6dd0a963e0fa83967225f7b8eb68c3425e0bf739f7455db99d; third_legacy_sha=576d0f7edf17c550d6ea3af4c623604c54984fbdc1c7a9c8d71839dc6aa6568a; fourth_legacy_sha=ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9;;
 857857af2a38b4452112d58c8e157f2448a5e8b58838159ef93b762676d64a33) obb_sha=a2c93164b591dc75c67b4cd2bd1f67bc3ede3c68a869559a4c68b9959fb4d65f; legacy_sha=334ca54ad010cb754b11e548bd5401d83dc7f4f7853aa9c6a294692e248d1fc1; second_legacy_sha=d81d59963ef4f7664638f59b96fb5c6735b12a4b4ae340d0edbc8cbd5731e564; third_legacy_sha=30c2ee4f9c979a6dd0a963e0fa83967225f7b8eb68c3425e0bf739f7455db99d; fourth_legacy_sha=576d0f7edf17c550d6ea3af4c623604c54984fbdc1c7a9c8d71839dc6aa6568a; fifth_legacy_sha=ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9;;
 72b2f5bbff0e255c2fa14af35d73b22a2c81f65bb5725cc92c00e09cffd3a975) obb_sha=632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29; legacy_sha=a2c93164b591dc75c67b4cd2bd1f67bc3ede3c68a869559a4c68b9959fb4d65f; second_legacy_sha=334ca54ad010cb754b11e548bd5401d83dc7f4f7853aa9c6a294692e248d1fc1; third_legacy_sha=d81d59963ef4f7664638f59b96fb5c6735b12a4b4ae340d0edbc8cbd5731e564; fourth_legacy_sha=30c2ee4f9c979a6dd0a963e0fa83967225f7b8eb68c3425e0bf739f7455db99d; fifth_legacy_sha=576d0f7edf17c550d6ea3af4c623604c54984fbdc1c7a9c8d71839dc6aa6568a; sixth_legacy_sha=ceb0bc511c8ce043761733ab7486448e8dcefddace0183ad8fb28e1d420091b9;;
 *) echo 'Unsupported animation plan' >&2; exit 1;;
esac
if [ "$runtime_mode" != 0 ]; then
 if [ "$animation_plan_sha" != 72b2f5bbff0e255c2fa14af35d73b22a2c81f65bb5725cc92c00e09cffd3a975 ]; then echo 'Runtime research requires the current animation plan' >&2; exit 1; fi
 if [ "$runtime_mode" = 1 ]; then obb_sha=18280c2798fa40248a312575fe1eeabd167951fbd05282a9f9d7e2b0779e6499
 else obb_sha=5acfe4f4b62c97e972e46525977375378cabdd09e33d5311f89bb637e2abb931; fi
 if [ "$no_water_probes" = 1 ]; then obb_sha=c367ae38db2ba09fc3ddb1de428bb1782664916905565bb68b1e850ac4ee09f5; fi
 legacy_sha=; second_legacy_sha=; third_legacy_sha=; fourth_legacy_sha=; fifth_legacy_sha=; sixth_legacy_sha=
fi
cat > "$java_dir/com/megabonk/launcher/SplitExpectedData.java" <<JAVA
package com.megabonk.launcher;
final class SplitExpectedData { static final String SHA256 = "$obb_sha", LEGACY_SHA256 = "$legacy_sha", SECOND_LEGACY_SHA256 = "$second_legacy_sha", THIRD_LEGACY_SHA256 = "$third_legacy_sha", FOURTH_LEGACY_SHA256 = "$fourth_legacy_sha", FIFTH_LEGACY_SHA256 = "$fifth_legacy_sha", SIXTH_LEGACY_SHA256 = "$sixth_legacy_sha"; }
JAVA
cp "$minimal_shared0" "$assets/shared0.zip"
cp "$repo_root/unity/OpenBonk/Assets/Plugins/Android/OpenBonkArt.androidlib/res/drawable-nodpi/openbonk_mascot.png" "$assets/openbonk_mascot.png"
standalone_base="${OPENBONK_STANDALONE_SCRUBBED_BASE:-}"
standalone_pack="${OPENBONK_STANDALONE_SHADER_PACK:-}"
particle_base="${OPENBONK_PARTICLE_SCRUBBED_BASE:-}"
shared_component_input="$assets/base.obb"
if [ "$scrub_shared_components" = 1 ]; then shared_component_input="$out_dir/before-shared-components.obb"; fi
scene_input="$shared_component_input"
if [ "$scrub_scene_components" = 1 ]; then scene_input="$out_dir/before-scene-components.obb"; fi
shared1_input="$scene_input"
if [ "$scrub_shared1_particles" = 1 ]; then shared1_input="$out_dir/before-shared1-particles.obb"; fi
base_output="$assets/base.obb"
if [ "$scrub_particles" = 1 ] && [ -z "$particle_base" ]; then base_output="$out_dir/before-particles.obb"; fi
if [ -n "$particle_base" ]; then
 if [ -n "$standalone_base" ] || [ -n "$standalone_pack" ] || [ "$scrub_particles" != 1 ] || [ ! -f "$particle_base" ] || [ "$(sha256sum "$particle_base" | cut -d' ' -f1)" != 2b4d71ddf84456b17ae1624847def47acfc3d896c2cd2f67ff096016e9c99650 ]; then
  echo 'Invalid particle base or incompatible template options' >&2; exit 1
 fi
 cp "$particle_base" "$shared1_input"
elif [ -n "$standalone_base" ] || [ -n "$standalone_pack" ]; then
 if [ -z "$standalone_base" ] || [ -z "$standalone_pack" ] || [ "$scrub_shared2" != 1 ]; then
  echo 'Standalone shader rebuild requires both inputs and the reduced shared2 base' >&2; exit 1
 fi
 "$python_cmd" "$repo_root/analysis/data-conversion/standalone-shader-template.py" restore "$standalone_base" "$base_output" --pack "$standalone_pack"
else
 scrubbed_obb="$repo_root/build/android/obb-no-liberation-base/base.obb"
 if [ ! -f "$scrubbed_obb" ]; then "$python_cmd" "$repo_root/analysis/data-conversion/build-scrubbed-font-obb.py"; fi
 if [ "$no_water_probes" = 1 ]; then
 if [ "$scrub_chests" = 1 ]; then
  "$python_cmd" "$repo_root/analysis/data-conversion/scrub-water-probe-template.py" "$scrubbed_obb" "$out_dir/reduced-base.obb"
  if [ "$scrub_shared2" = 1 ]; then
   "$python_cmd" "$repo_root/analysis/data-conversion/scrub-chest-animation-base.py" scrub "$out_dir/reduced-base.obb" "$out_dir/chest-base.obb"
   "$python_cmd" "$repo_root/analysis/data-conversion/apply-shared2-animation-mask.py" "$out_dir/chest-base.obb" "$repo_root/analysis/data-conversion/shared2-animation-position-mask.json" "$base_output"
  else
   "$python_cmd" "$repo_root/analysis/data-conversion/scrub-chest-animation-base.py" scrub "$out_dir/reduced-base.obb" "$base_output"
  fi
 else
  "$python_cmd" "$repo_root/analysis/data-conversion/scrub-water-probe-template.py" "$scrubbed_obb" "$base_output"
 fi
 else
  cp "$scrubbed_obb" "$base_output"
 fi
fi
if [ "$scrub_particles" = 1 ] && [ -z "$particle_base" ]; then
 "$python_cmd" "$repo_root/analysis/data-conversion/apply-shared2-particle-mask.py" "$base_output" "$repo_root/analysis/data-conversion/shared2-particle-position-mask.json" "$shared1_input"
fi
if [ "$scrub_shared1_particles" = 1 ]; then
 "$python_cmd" "$repo_root/analysis/data-conversion/apply-shared1-particle-mask.py" "$shared1_input" "$repo_root/analysis/data-conversion/shared1-particle-position-mask.json" "$scene_input"
fi
if [ "$scrub_scene_components" = 1 ]; then
 "$python_cmd" "$repo_root/analysis/data-conversion/apply-scene-component-plan.py" "$scene_input" "$repo_root/analysis/data-conversion/scene-component-position-plan.bin" "$shared_component_input"
 cp "$repo_root/analysis/data-conversion/scene-component-position-plan.bin" "$assets/scene-component-plan.bin"
fi
if [ "$scrub_shared_components" = 1 ]; then
 shared_component_output="$assets/base.obb"
 if [ "$scrub_materials" = 1 ]; then shared_component_output="$out_dir/before-materials.obb"; fi
 "$python_cmd" "$repo_root/analysis/data-conversion/apply-shared-component-plan.py" "$shared_component_input" "$repo_root/analysis/data-conversion/shared-component-position-plan.bin" "$shared_component_output"
 cp "$repo_root/analysis/data-conversion/shared-component-position-plan.bin" "$assets/shared-component-plan.bin"
fi
if [ "$scrub_materials" = 1 ]; then
 "$python_cmd" "$repo_root/analysis/data-conversion/apply-material-plan.py" "$shared_component_output" "$repo_root/analysis/data-conversion/material-position-plan.bin" "$assets/base.obb"
 cp "$repo_root/analysis/data-conversion/material-position-plan.bin" "$assets/material-plan.bin"
fi
if [ "$runtime_mode" = 1 ]; then cp "$runtime_plan" "$assets/runtime-rebase.zip"; fi
if [ "$runtime_mode" = 2 ]; then printf %s OPENBONK_VIDEO_ONLY_V1 > "$assets/runtime-video-only"; fi
printf %s OPENBONK_PRIVATE_DIAGNOSTIC_V1 > "$assets/marker"
mkdir "$assets/notices"
cp "$repo_root"/third_party/openbonk-texture-notices/* "$assets/notices/"
lua_notice="$repo_root/unity/OpenBonk/Assets/StreamingAssets/ThirdPartyNotices/Lua.txt"
if [ ! -f "$lua_notice" ] || [ "$(sha256sum "$lua_notice" | cut -d' ' -f1)" != d14fe4a91721fd1d0f6650137594ee83c692f422864a00e69bd6a9b42cdda005 ]; then
 echo 'Missing or unexpected Lua notice' >&2; exit 1
fi
cp "$lua_notice" "$assets/notices/Lua-5.4.9.txt"
cp "$texture_lib" "$work/unityLibrary/src/main/jniLibs/arm64-v8a/libopenbonk_texture.so"
OPENBONK_ZLIB_ARCH=arm64 OPENBONK_UNITY_EDITOR="$unity_editor" OPENBONK_TOOL_RUNNER="${OPENBONK_TOOL_RUNNER:-}" "$repo_root/tools/build-stable-zlib.sh" "$work/unityLibrary/src/main/jniLibs/arm64-v8a/libopenbonk_zlib.so"
"$python_cmd" "$repo_root/tools/package-conversion-plans.py" "$plan_dir" "$assets/plans.zip" "${shader_args[@]}"
(
 cd "$work"
 export JAVA_HOME="$unity_android/OpenJDK" PATH="$unity_android/OpenJDK/bin:$PATH"
 "${tool_runner[@]}" java -classpath "$unity_android/Tools/gradle/lib/gradle-launcher-7.6.jar" org.gradle.launcher.GradleMain --offline --no-daemon :launcher:assembleRelease
) > "$out_dir/gradle.log" 2>&1
if [ "$runtime_mode" != 0 ]; then
 if [ "$redact_paths" = 1 ]; then
  mkdir "$out_dir/native-original" "$out_dir/native-redacted"
  "$python_cmd" - "$runtime_apk" "$out_dir/native-original" <<'PY'
import hashlib, pathlib, sys, zipfile
source, target = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
if hashlib.file_digest(source.open('rb'), 'sha256').hexdigest() not in {'f6f6c256fc274b34957c67a10f2c70deed22231e42b0a4908853bb90e7677de4', '29d8b0a9b0ea951c0051a2ec7c48f0b7afccd3f55ec80df80533e59274f64410', 'dabfdee55c8bd2c56d1f2213a70aa5764465b99366e8143350df073ea74be301', 'dee2b0c36667e845f42acc1793a395b0fa0030025d4a212c467f03829dc86672', '31a4913f3004b13c6c91acaf312f55f53a87c89ed3a49c056053eb99a78cfb60', '9b39fabb6995b5f36d65875e4762df5b3cd08a554545463be17bee20545e2a92', 'cfc632ec59451bbc1e3db4b25a4f451912dfa3d059da92b591484bc08897e574'}:
 raise ValueError('unexpected runtime for path redaction')
with zipfile.ZipFile(source) as z:
 for name in ('libgame.so', 'libil2cpp.so', 'libswappywrapper.so'):
  (target/name).write_bytes(z.read('lib/arm64-v8a/'+name))
PY
  for name in libgame.so libswappywrapper.so; do
   "${tool_runner[@]}" "$unity_android/NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/llvm-strip" --strip-debug -o "$out_dir/native-redacted/$name" "$out_dir/native-original/$name"
  done
  "$python_cmd" "$repo_root/analysis/data-conversion/redact-il2cpp-paths.py" "$out_dir/native-original/libil2cpp.so" "$out_dir/native-redacted/libil2cpp.so"
  "$python_cmd" "$repo_root/analysis/data-conversion/build-private-runtime-steam-apk.py" "$runtime_apk" "$work/launcher/build/outputs/apk/release/launcher-release.apk" "$runtime_obb" "$out_dir/unsigned.apk" "$out_dir/native-redacted"
 else
  "$python_cmd" "$repo_root/analysis/data-conversion/build-private-runtime-steam-apk.py" "$runtime_apk" "$work/launcher/build/outputs/apk/release/launcher-release.apk" "$runtime_obb" "$out_dir/unsigned.apk"
 fi
else
mkdir "$out_dir/native-original" "$out_dir/native-stripped"
"$python_cmd" - "$base" "$out_dir/native-original" <<'PY'
import sys,zipfile
from pathlib import Path
base,output=map(Path,sys.argv[1:])
with zipfile.ZipFile(base) as z:
 for name in ('libgame.so','libswappywrapper.so','libil2cpp.so'):
  with (output/name).open('xb') as f:f.write(z.read('lib/arm64-v8a/'+name))
PY
strip_tool="$unity_android/NDK/toolchains/llvm/prebuilt/linux-x86_64/bin/llvm-strip"
for name in libgame.so libswappywrapper.so; do
 "${tool_runner[@]}" "$strip_tool" --strip-debug -o "$out_dir/native-stripped/$name" "$out_dir/native-original/$name"
done
"$python_cmd" "$repo_root/analysis/data-conversion/redact-il2cpp-paths.py" "$out_dir/native-original/libil2cpp.so" "$out_dir/native-stripped/libil2cpp.so"
"$python_cmd" "$repo_root/analysis/data-conversion/build-private-launcher-apk.py" "$base" "$work/launcher/build/outputs/apk/release/launcher-release.apk" "$out_dir/unsigned.apk" --stripped-libs "$out_dir/native-stripped"
fi
"${tool_runner[@]}" "$unity_android/SDK/build-tools/34.0.0/zipalign" -f -p 4 "$out_dir/unsigned.apk" "$out_dir/aligned.apk"
export JAVA_HOME="$unity_android/OpenJDK" PATH="$unity_android/OpenJDK/bin:$PATH"
"${tool_runner[@]}" "$unity_android/SDK/build-tools/34.0.0/apksigner" sign --ks "${OPENBONK_DEBUG_KEYSTORE:-$HOME/.android/debug.keystore}" --ks-pass pass:android --key-pass pass:android --out "$out_dir/OpenBonk-private-test.apk" "$out_dir/aligned.apk"
"${tool_runner[@]}" "$unity_android/SDK/build-tools/34.0.0/apksigner" verify "$out_dir/OpenBonk-private-test.apk"
echo 'Built local research APK. It contains private inputs and is not distributable.'
