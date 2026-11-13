# Source and build status

This checkout exposes the current reconstruction and tools for inspection and development. **A clean checkout is not yet sufficient to rebuild the published APK.** The original local build depended on an exported Unity project, original game assets, third-party plugins, generated plans and binary intermediates that are not all included here. Some editor and research diagnostics still contain `/home/your-user` placeholders where local paths were removed; configure them for your environment before using them. Do not interpret a missing local input or an untested build as a working release pipeline.

## Layout

- `unity/OpenBonk/Assets/Scripts/` — reconstructed C# game and Android integration scripts.
- `unity/OpenBonk/Assets/AndroidSplitLauncher/` — Android launcher, Steam ZIP import, profiles and mods.
- `unity/OpenBonk/Assets/AndroidModPicker/` — Android mod picker integration.
- `unity/OpenBonk/Assets/Editor/` — editor tooling and diagnostic scripts; some require omitted assets or local inputs.
- `unity/OpenBonk/Assets/Shader/` — available reconstructed shader sources and metadata; provenance varies.
- `native/lua/` — Lua bridge, Lua 5.4.9 source archive, smoke test and example packages.
- `native/stable-zlib/`, `tools/`, `analysis/data-conversion/` — native support and experimental preparation, validation and reconstruction tools. Many research scripts depend on local generated intermediates and are not standalone build commands.
- `mods/` — example Lua mods and the experimental Pikachu package.

The local runtime work used Unity 2023.2.22f1 and Android ARM64. Reconstructing a working project also requires the appropriate Unity Android toolchain, compatible game data and unresolved third-party components. No personal Steam data, saves or signing keys are included. Reproducible clean setup, dependency provenance and licensing remain work to do.

The Steam data-preparation Java sources and three inputs used to generate position-mask constants are included. To check this component with Python 3, a JDK and an existing org.json 20180813 JAR:

```sh
python3 tools/check-data-prep-sources.py --json-jar /path/to/json-20180813.jar
```

This compiles the `Steam*.java` sources and checks that the three generated Java files match their pinned JSON inputs. It does not assemble an APK or supply missing Unity assets, plans, third-party components or build intermediates. The older `tools/isolated-workspace-inputs.json` records research inputs; it is not a manifest for a verified clean release build.

## Isolated Unity workspace

To include the Steam export in the experimental local build, use:

```sh
python3 tools/build-local-from-steam.py \
  --game /path/to/Steam/steamapps/common/Megabonk \
  --assetripper /path/to/AssetRipper.GUI.Free \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --output /path/to/new-local-build \
  --application-id com.example.openbonktest
```

This builds the Lua bridge with Unity's Android NDK, then runs the export and local Unity build described below. Alternatively, supply `--lua-library /path/to/libmegabonk_lua.so` to use a locally built bridge. Logs and intermediate files stay inside the new output directory, which contains game-derived data and must stay local. Add `--runner steam-run` if needed; `--check` only verifies initial inputs. This is not the data-separated release build.

For an experimental local, data-embedded APK, `tools/build-local-unity-apk.py` runs the preparation stages described below in sequence from an AssetRipper 2.0.0 `ExportedProject` and a locally built Lua bridge:

```sh
python3 tools/build-local-unity-apk.py \
  --base-project /path/to/ExportedProject \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --lua-library /path/to/libmegabonk_lua.so \
  --output /path/to/new-local-build \
  --application-id com.example.openbonktest
```

Add `--runner steam-run` if needed. `--check` validates the initial inputs only. The tool requires a new output directory, retains local intermediates and backups on failure, and does not install the APK. A run from a fresh local export completed and its APK reached a Forest run on an iPlay tablet. The example Forest mod was also imported and enabled from the launcher, appeared in map selection and started a run with its configured five-minute timer; its Lua multipliers were not measured. The output contains game-derived assets; it is not the data-separated release APK. The sequence remains experimental and the smoke tests do not establish full feature parity.

An experimental local export step uses AssetRipper.GUI.Web 2.0.0 and a game installation provided by the user:

```sh
python3 tools/export-steam-unity-project.py \
  --game /path/to/Steam/steamapps/common/Megabonk \
  --assetripper /path/to/AssetRipper.GUI.Free \
  --output /path/to/new-local-export
```

On systems that need it, add `--runner steam-run`. The tool starts AssetRipper on a loopback-only server, retains a local log, and requires a new output directory. `--check` validates prerequisites. The exported game assets and log stay local; this does not by itself produce an APK.

The checkout can overlay its code into a separate local copy of a compatible exported Unity project with `Assets`, `Packages` and `ProjectSettings`:

```sh
python3 tools/assemble-unity-workspace.py \
  --base-project /path/to/reconstructed-unity-project \
  --output /path/to/new-local-workspace
```

Use `--check` to validate without writing. Both projects must specify Unity 2023.2.22f1; the tool checks GUIDs for metadata present in both, rejects symbolic links and existing output paths, and leaves the base untouched. The workspace includes the base project's data, which should remain local. This tool does not generate the base project, reconstruct all missing build intermediates or build an APK.

A fresh local Steam export with AssetRipper 1.3.14 was also tried: 69 GUIDs differed. The optional `--allow-guid-remap` reconciled those GUID references in 623 local text files while preserving the original export. This is experimental: it does not prove that every asset or binary reference is correct, and the resulting fresh-export project still lacks configuration and build inputs. Without the flag the tool rejects mismatched GUIDs.

AssetRipper 2.0.0 was also tested with a fresh local export. In a comparison of 1,093 `MonoBehaviour` assets, none of the 741 assets that were empty in the 1.3.14 export remained empty while populated in the reference project. Its export still required the same 69 GUID remappings; a complete APK built from it has not yet been validated.

To generate the managed Android build inputs locally from the exported project rather than copying another developer's DLLs:

```sh
python3 tools/prepare-managed-plugins.py \
  --project /path/to/exported-project \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --output /path/to/new-managed-plugins
```

The tool uses Unity's Mono and Mono.Cecil to normalize two assemblies and create the Android Steamworks stub; `--runner steam-run` is available where needed. The generated inputs contain derived third-party code and should remain local. Outputs generated from a fresh Steam export are not byte-identical to the previous local reference and have not yet been validated in an end-to-end build.

Build the Lua bridge as described below, then prepare the isolated workspace before opening it in Unity:

```sh
python3 tools/prepare-unity-workspace.py \
  --project /path/to/new-local-workspace \
  --plugins /path/to/new-managed-plugins \
  --lua-library /path/to/libmegabonk_lua.so \
  --remove-duplicate-package-dlls
```

This installs locally generated plugins, restores their checked-in metadata and backs up replaced files in `.openbonk-original-plugins` inside the workspace. The optional removal flag is intended for the observed AssetRipper 1.3.14 export: it backs up and removes specific DLLs that duplicate Unity packages. Use `--check` first and keep the original export. Preparing these files does not reconstruct the missing project configuration and assets.

The observed 2.0.0 export contains three pairs of textures whose paths differ only in letter case; Unity can fail to import both. Before opening the workspace in Unity, rename the conflicting texture copies and their metadata together:

```sh
python3 tools/resolve-unity-texture-case-collisions.py \
  --project /path/to/new-local-workspace \
  --manifest /path/to/new-local-texture-renames.json
```

The tool preserves asset GUIDs, records the local renames and hashes, rejects unsupported collision types, and supports `--check`. The original export is unchanged. Paths should be reviewed if a different game version adds case collisions.

Import the prepared workspace in Unity with the required build defines:

```sh
python3 tools/import-unity-workspace.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --log /path/to/new-local-import-log
```

The import keeps game data local, offers `--check` and `--runner steam-run`, and retains the log if Unity fails. After importing, remap references from the exported package DLLs to scripts in the imported Unity packages:

```sh
python3 tools/remap-unity-package-scripts.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-script-backup
```

The 43 mappings were compared against corresponding component references in the existing working project. On the 1.3.14 export, 41 mappings covered 13,738 references in 86 files; the 2.0.0 export also needed two localization mappings. The tool checks the installed package GUIDs, rejects unknown references to removed package DLLs, backs up every changed file, and supports `--check`. This is a version-specific reconstruction step, not proof that every scene or component behaves correctly.

On the populated 2.0.0 export, generate the local character Resources after importing and remapping the workspace:

```sh
python3 tools/prepare-unity-characters.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --log /path/to/new-local-character-log \
  --backup /path/to/new-local-character-backup
```

This verifies the 21 character assets and their model, weapon, passive and icon references, backs up Bandit and Clank before correcting their default meshes to match their animated prefabs, then creates Resources copies and a fallback prefab locally. `--check` validates prerequisites without writing, and `--runner steam-run` is available where needed. One character was exercised in a short Android smoke test; the full roster and gameplay remain unverified.

Restore post-processing shader references to the imported Unity package, preserving the original local asset:

```sh
python3 tools/prepare-unity-postprocessing.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --log /path/to/new-local-postprocessing-log \
  --backup /path/to/new-local-postprocessing-backup
```

A local test restored the same 24 package shaders and 12 compute shaders as the reference project. `--check` and `--runner steam-run` are supported; this check does not establish rendering equivalence.

Restore the eight exported skybox materials to OpenBonk's reconstructed skybox shader:

```sh
python3 tools/prepare-unity-skyboxes.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-skybox-backup
```

The step backs up the materials and metadata, checks their existing shader references, and changes only those shader references. It does not redistribute cubemaps; `--check` makes no changes.

An experimental localization stage reconstructs Android localization from the 19 supported local Linux Steam bundles, without adding game text to this checkout:

```sh
python3 tools/rebuild-unity-localization.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --build-root /path/to/new-localization-output
```

The project must already be prepared, and must contain the supported bundles under `Assets/StreamingAssets/aa/StandaloneLinux64`. The tool verifies their hashes, requires a new output directory, and offers `--check` and `--runner steam-run`. It modifies the isolated workspace and stores derived game data and logs locally. A manual run on a fresh export restored 17 locales and 867 tables and built Android Addressables; the complete APK and gameplay still need separate validation.

The observed Steam export also has Addressables files that conflict with Unity's newly generated Android files at player build time. After successfully rebuilding Android Addressables, back up and remove conflicting exported files from the isolated workspace:

```sh
python3 tools/prepare-android-addressables.py \
  --project /path/to/new-local-workspace \
  --backup /path/to/new-local-catalog-backup
```

This checks for generated Android catalogs, compares their relative paths against exported files, and records hashes of conflicting files and their metadata in the local backup; `--check` makes no changes. The original Steam export remains untouched. Retain the backup and the Steam-derived bundles locally.

The experimental runtime build stage can use that isolated workspace and a local directory containing the three managed DLLs named by the tool:

```sh
python3 tools/build-unity-runtime.py \
  --project /path/to/new-local-workspace \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --plugins /path/to/local/managed-plugins \
  --build-root /path/to/new-runtime-output
```

On a system that needs a compatibility wrapper, add e.g. `--runner steam-run`; any required system libraries must also be configured locally. The tool has a `--check` option, refuses an existing output or response file, and retains its output and log on build failure. It invokes Unity's ARM64 runtime entry point with Android target SDK 35 and explicit no-Rewired and no-probe defines. Its inputs and outputs can contain game-derived and third-party material. A runtime APK plus OBB is an intermediate stage, not the final OpenBonk launcher APK; the clean end-to-end build and independent reconstruction of the inputs remain unverified.

An optional `--application-id com.example.openbonktest` builds an isolated local test package without changing the default runtime ID. This does not install anything and is not a release signing configuration.

For local experiments, `--embedded` instead requests a single APK containing the exported game data. A locally generated APK installed under an isolated package ID on an iPlay tablet and reached a Forest run with touch controls without a separate OBB. This output contains game-derived material and is not the data-separated APK intended for distribution. The short smoke test does not establish feature parity, and this mode does not resolve the conversion-plan or release-signing gaps.

To generate a local Android Gradle project instead, use the same command with `--gradle-project`. A local Unity export created the expected Gradle modules and Unity library; integrating it with the launcher, generated conversion plans and signing has not yet been validated. The exported project contains game-derived data and should remain local.

The experimental `tools/build-private-launcher-prototype.sh` accepts `OPENBONK_GRADLE_TEMPLATE` and `OPENBONK_UNITY_PROJECT` for these local paths, and `OPENBONK_TEXTURE_LIBRARY` for the locally built ARM64 texture adapter. It still requires its pinned runtime, OBB and conversion plans and intentionally rejects incompatible inputs; setting those paths does not make it a verified clean APK build.

Three version-specific position-only scrub plans are included under `analysis/data-conversion/`; `python3 tools/check-position-plans.py` validates their hashes and structure. They contain offsets, hashes and bit masks rather than the masked asset bytes. They are pinned to older intermediate builds and do not resolve the remaining plan generation and compatibility work for the newly built runtime.

The ARM64 texture adapter can be built locally from the pinned source archive and the Unity Android NDK:

```sh
python3 tools/build-android-texture-library.py \
  --ndk /path/to/Unity/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/NDK \
  --output /path/to/new/libopenbonk_texture.so \
  --runner steam-run
```

The build requires Python 3.12 or newer and validates source hashes before compiling. The resulting scalar ARM64 library matched 269 local game-texture fixtures, six Crunch cubemaps and 20 synthetic cases on Android; this does not cover every input or game version. See `third_party/` for source provenance and license notices.

To repeat the synthetic checks on an authorized ARM64 device without installing an app or transferring game data:

```sh
python3 tools/test-android-texture-library.py \
  --library /path/to/libopenbonk_texture.so \
  --device YOUR_ADB_SERIAL \
  --sdk /path/to/Unity/2023.2.22f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK \
  --java-home /path/to/JDK
```

The tool removes its temporary device files after the test. It offers `--check` and an optional `--runner` for the JDK.

A local runtime build from the 1.3.14 export succeeded after the package-script and Addressables preparation above. Its script registry matched the 2,349 script IDs and identities in the previous runtime, but three empty script components remained in the scene build and its OBB had 207 ZIP entries versus 946 in the reference. One concrete gap is that the fresh export lacks the 21 populated `Resources/AndroidCharacters` assets used by the gameplay bootstrap. A comparison of 1,093 corresponding `MonoBehaviour` assets also found 741 whose 1.3.14 exports contain only an empty script header while the reference assets have additional serialized fields. Many asset entries therefore remain different or absent; neither gameplay equivalence nor a distributable APK has been established. The local Steam bundles must not be published as part of this experiment.

A later local runtime build using the 2.0.0 export and regenerated character resources also succeeded. Its script registry matched all 2,349 IDs and identities; its OBB had 919 ZIP entries versus 946 in the reference. A subsequent build with corrected character meshes and post-processing references produced 921 entries. After package remapping, 1,091 of 1,093 `MonoBehaviour` files matched byte-for-byte when GUID values were normalized. Those checks do not establish that GUIDs resolve to equivalent assets or that gameplay matches, and the remaining differences need investigation.

An isolated Android test package built from the local export reached the launcher, character selection and a Forest run with touch input. The 3D view was initially black; restoring the package post-processing shaders made the world visible. Restoring the skybox shader references fixed the white sky in the menu and in a Forest run on an iPlay tablet. This is a limited device smoke test, not full gameplay validation or proof that the final APK can be assembled.

`tools/extract-steam-character-data.py` is an experimental investigation into this gap. With local `GameAssemblies` and Python dependencies `UnityPy==1.25.4` and `TypeTreeGeneratorAPI==0.0.10`, it decoded 21 character records from a local Steam installation. Their character IDs and collider values matched the 21 reference resources. The output contains game data and must stay local; its Unity pointers have not yet been mapped to project assets, so it cannot currently restore the characters or make the build playable.

## Lua bridge

On a Linux x86-64 host with an Android NDK, set `ANDROID_NDK_HOME` or `ANDROID_NDK_ROOT`, then run `bash native/lua/build.sh`. It checks the Lua archive hash and builds the ARM64 library and smoke executable in `build/lua`. This builds the bridge only; it does not build the OpenBonk APK. The APK and game integration require the missing Unity project inputs described above.

The project includes reconstructed game-derived material and third-party components of different provenance. No blanket source license is asserted here; check the applicable rights and component notices before redistribution or reuse.
