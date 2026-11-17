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
  these values. The development runtime now has a character-specific base
  configuration under test; it is not present in 0.1.2. A global stat mod
  would affect other characters too, so it is not a faithful substitute.
- The metadata requests a collider height of 3.75 and width of 0.7. The
  current external-character loader copies Fox's collider. In-game collision,
  aiming, hats and camera placement must be checked on Android with the new
  character-specific configuration.
- The passive grants 1% Luck per level. The existing content format can
  declare per-level stat modifiers. A short device check showed Luck increasing
  from 15% to 16% at level 1; more progression checks remain.
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
second Android bundle with the five selected clips and five textures. This
particular bundle was subsequently staged in beta 0.1.2 on the Xiaomi. The
character appeared with textures, entered Forest, defeated 16 enemies and
reached level 1; the Luck statistic showed 16%, versus 15% initially. This is
consistent with the requested per-level Luck passive in that brief test. The
weapon and base-stat limitations above remained, and animation fidelity was
not established. The temporary test installation was removed.

An experimental Android runtime with character-specific base configuration
subsequently loaded the study package on the same Xiaomi. In Forest it showed
65/65 HP, 1 extra jump, 1.10x movement speed, 25 HP regeneration and pickup
range 9, and reached level 1. The package used the eight upstream base modifiers
and collider 3.75 by 0.7. This confirms loading and several displayed stats in
IL2CPP; the physical collider was not independently measured on the device.
The experimental installation was removed. The custom weapon and visual
fidelity remain incomplete, so this is not a releasable port.

Before publishing, confirm rights and attribution for the source model and
assets, construct a package matching the installed host's actual capabilities,
and test catalog download, enable/disable, presentation, combat, upgrades,
scene transitions and interactions with other mods. Do not add a catalog entry
for the current prototype.
