# Character bundle study

To inspect the metadata in a PC custom-character ZIP before conversion:

```sh
python3 mods/tools/audit_pc_character.py /path/to/character-mod.zip
```

The audit reads one bounded `.custom.json` entry without extracting its
contents and lists features for manual review. It does not establish whether
the bundle runs on Android or whether the character is compatible with the
current host. Local checks on Cow-noichi, GodOfDeath and Goth Kitty Siella
each identified character-specific stats and custom weapon definitions.

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
The log reports missing transform paths in ordinary animation curves; object
reference curves and runtime retargeting are not covered by that count.

This is a visual study: it substitutes a basic opaque, unlit shader for all
materials. It does not replicate original shaders, transparent surfaces,
weapon behavior, character stats, animations, audio or progression. It also
does not guarantee arbitrary exported assets work on Android. Validate each
result in the target APK before treating it as compatible.

In one local Cow-noichi study, Unity 2023.2.22f1 generated a bundle targeting
Android with one skinned renderer, five textures and the five selected clips.
An editor check found no missing transform paths across the five clips (but
does not assess how they look). That bundle was manually staged in beta 0.1.2
on a Xiaomi; the character appeared textured in the menu, entered Forest,
defeated 16 enemies and reached level 1. This checks one input, not this
converter for all characters or the original gameplay: the projectile was an
oversized built-in arrow and base stats and collider did not match upstream.
The temporary installation was removed.
