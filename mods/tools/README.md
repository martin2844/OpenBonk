# Character bundle study

`character_bundle_study.py` rebuilds a prefab and selected animation clips from
a local AssetRipper Unity project into an Android AssetBundle. The script does
not download a mod, package an OpenBonk mod ZIP, copy files to Android, or
publish the author's assets. Supply your own export and review the original
creator's terms separately.

For example, on Linux with Unity 2023.2.22f1 and Android Build Support:

```sh
python3 mods/tools/character_bundle_study.py \
  --export /path/to/ExportedProject \
  --unity /path/to/Unity/2023.2.22f1/Editor/Unity \
  --prefab Assets/export/prefabs/Kunoichi.prefab \
  --clip Assets/AnimationClip/ShinobiIdle.anim \
  --clip Assets/AnimationClip/ShinobiRun.anim \
  --clip Assets/AnimationClip/ShinobiJump.anim \
  --clip Assets/AnimationClip/ShinobiGrind.anim \
  --clip Assets/AnimationClip/ShinobiFalling.anim \
  --output /path/to/new-output
```

Add `--runner /path/to/wrapper` where the editor needs a compatibility
environment. The output contains a generated Unity project, a log and
`bundle/character-android`. It includes material and model data from the input;
keep it separate from the source repository. Imported C# sources and native
plugins from the foreign export are excluded. AssetRipper exports can still
contain missing components or incomplete references; inspect the editor log.

This is a visual study: it substitutes a basic opaque, unlit shader for all
materials. It does not replicate original shaders, transparent surfaces,
weapon behavior, character stats, animations, audio or progression. It also
does not guarantee arbitrary exported assets work on Android. Validate each
result in the target APK before treating it as compatible.

In one local Cow-noichi study, Unity 2023.2.22f1 generated a bundle targeting
Android with one skinned renderer, five textures and the five selected clips.
An earlier manually generated bundle of the same study was loaded in beta
0.1.2 on a Xiaomi; that partial test does not prove this general converter or
the character's original gameplay works end to end.
