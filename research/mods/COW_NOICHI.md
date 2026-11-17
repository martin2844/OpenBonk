# Cow-noichi port notes

Original: [Cownoichi 1.1.1 by Vegasx](https://thunderstore.io/c/megabonk/p/Vegasx/Cownoichi/).
These notes describe a local compatibility study; no original model, texture,
audio, bundle or code is included here, and a port has not been published.

The upstream package contains a Windows x64 Unity 2023.2.20f1 bundle, the
character's declarative JSON and an icon. The bundle contains a skinned mesh,
five animation clips and textures. An AssetRipper export was rebuilt locally
with Unity 2023.2.22f1 targeting Android. The `mods/tools/character_bundle_study.py`
script reproduces the authoring step from a separately obtained export.

## Required behavior and current gaps

- The upstream character specifies eight modifiers: flat ExtraJumps 1,
  Evasion 0.2, MoveSpeedMultiplier 0.1, JumpHeight 2, HealthRegen 15,
  PickupRange 4 and EliteDamageMultiplier 0.15, plus a MaxHealth multiplier
  of 0.65. The 0.1.2 character loader copies Fox's stats instead of accepting
  these values. A global stat mod would affect other characters too, so it is
  not a faithful substitute.
- The metadata requests a collider height of 3.75 and width of 0.7. The
  current external-character loader copies Fox's collider. In-game collision,
  aiming, hats and camera placement must be checked after adding a supported
  character-specific configuration.
- The passive grants 1% Luck per level. The existing content format can
  declare per-level stat modifiers, but the value and progression still need
  a device check against upstream behavior.
- Kunai is an upstream projectile with its own prefab, audio and upgrade
  options. The current host exposes only existing arrow, fireball and
  lightning effects. The local arrow-based experiment was visibly oversized,
  so it is not an acceptable replacement. A compatible custom projectile
  path or an explicitly equivalent, validated host effect is needed.
- The study's substitute unlit shader restored colors on one device but does
  not reproduce the upstream toon material, lighting or transparency. Idle,
  walking, running, jumping, surfing and other exposed animation transitions
  need inspection in gameplay, not only a clip-count check.

## Test evidence and release boundary

On a Xiaomi 22126RN91Y, a manually staged prototype appeared in character
selection and reached Forest, defeating eight enemies. Its base stats, collider
and weapon were substitutes; this was not a faithful port or a catalog import
test. The test installation was removed. The generic converter produced a
second Android bundle with the five selected clips and five textures; that
specific output has not yet been loaded on Android.

Before publishing, confirm rights and attribution for the source model and
assets, construct a package matching the installed host's actual capabilities,
and test catalog download, enable/disable, presentation, combat, upgrades,
scene transitions and interactions with other mods. Do not add a catalog entry
for the current prototype.
