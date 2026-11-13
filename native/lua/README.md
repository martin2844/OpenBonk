# Experimental Android Lua runtime

This directory contains the Lua 5.4.9 release archive from lua.org and a
small ARM64 bridge. API 1/2 evaluate stat hooks in a fresh Lua state. API 3
lightning weapons and items can use persistent states for their bounded numeric
callbacks, allowing state to survive between calls. The bridge exposes `math`,
`table`, `string`, `utf8` and selected base helpers including `pairs`, `ipairs`,
`type`, `tonumber` and `tostring`. It accepts text chunks and applies per-state
memory and Lua instruction limits. File, OS, package and debug libraries,
dynamic loading, protected calls and metatable helpers are not exposed. Lua
instruction limits do not bound the execution time of every native library call.

On this NixOS host, run `steam-run bash native/lua/build.sh` from the project
root. The script verifies the archive digest and writes an Android shared
library and a smoke-test executable to `build/lua`. Copy the resulting
`libmegabonk_lua.so` into Unity's `Assets/Plugins/Android/arm64-v8a/` when
updating the checked-in binary. Unity's `megabonk_lua_probe` intent extra
activates the separate diagnostic. The included Lua notice must accompany
the binary; a copy is in Unity's StreamingAssets/ThirdPartyNotices.

On Linux hosts with a different NDK location, set `ANDROID_NDK_HOME` (or
`ANDROID_NDK_ROOT`) to the NDK directory before running the script. The
current script targets Linux x86-64 NDK hosts and Android ARM64; it is not yet
a cross-platform release build.

An experimental package is a folder under the app's external files `Mods`
directory. Its `manifest.json` must identify the folder, set `api` to 1,
set `hook` to a supported name, and set `enabled` to true. Version 1 supports
`move_speed_multiplier`, `damage_multiplier`, `attack_speed_multiplier`,
`xp_multiplier`, `gold_multiplier`, and `pickup_range_multiplier`. Each
package affects one stat. `main.lua` returns the multiplier, between 0.1 and 4;
see `example-speed-mod` and `example-damage-mod`, which are disabled.
Enabled packages are read when a player is initialized, in folder
name order. Invalid packages are skipped. The launcher Manage Mods screen can
override each package's initial enabled value; choices persist in app settings.
Files are re-read at player initialization; the effect is a run-time modifier
and is not written into the profile. The launcher can import a ZIP via the
Android document picker: include exactly `manifest.json` and `main.lua` at the
ZIP root or in one directory matching the manifest ID. ZIPs are limited to
1 MiB and each file to 64 KiB. IDs start with an ASCII letter or digit and may
also include ASCII letters, digits, `_`, `-` and `.` (maximum 64 characters).
In the launcher, imports and updates start disabled; an existing package with
the same ID is replaced only after the new archive is validated and staged.
Enable the package again in Manage Mods after an update.

The sandbox and API are preliminary and are not a security boundary for
untrusted mods. Full mod support still needs broader hooks, stronger resource
limits and device tests.

API 2 permits a single package to affect multiple supported stat
hooks. Set `api` to 2 and put the hook names in the ordered `hooks` array;
`main.lua` must return a Lua array with the same number of numeric multipliers
in the same order. Each multiplier still has the 0.1–4 range and is validated
before any modifier from that package is applied. Duplicate, unknown and empty
hook lists are rejected. See `example-balanced-mod`; it starts disabled.
API 1 packages remain supported. API 2 still executes once at player
initialization; it is not yet a stateful or event-driven mod API.

`example-table-mod` demonstrates iterating a Lua table to compute an API 1
pickup-range multiplier. ZIP `manifest.json` and `main.lua` at the archive root,
import it from the launcher, and enable it in Manage Mods. It requires a build
with the expanded Lua libraries; older installed APKs do not include them.

## Target: extensible OpenBonk modding (in progress)

API 1/2 stat multipliers and API 3 characters/lightning weapons are early
building blocks. The intended
modding platform must support adding characters, weapons, enemies, maps and
terrain, changing existing definitions and values, and implementing new
behaviors. The following is a direction for implementation, **not a description
of APIs available today**:

1. Versioned packages: a manifest identifies the mod and its dependencies;
   Lua and referenced assets have explicit, validated paths. Imports, enable
   state, load order, error reporting and update/removal belong in the launcher.
2. Registries: mods register or patch named game definitions before menus and
   pools are built. Character definitions need appearance, animations, starting
   weapon, stats, unlocks and passive behavior. Weapon and enemy definitions need
   visuals, timing, targeting, damage and behavior. Map definitions need scenes
   or terrain recipes, spawns, objectives, transitions and bosses.
3. Runtime events and commands: subscriptions for run, scene, entity, combat,
   inventory, UI, spawn and save lifecycle events; commands to inspect and change
   live state and spawn, modify or remove supported objects. Define callback
   order, cancellation, error handling and stable entity handles explicitly.
4. Content assets: import a documented Android-compatible asset format with
   version checks, dependency management and safe lifetime/unloading. Lua code
   alone cannot provide new meshes, animations, textures, audio or scenes.
5. Persistence and compatibility: per-mod saved state, migrations, missing-mod
   behavior, deterministic ordering and diagnostics. Existing API 1/2 packages
   continue to work or receive a documented migration path.
6. Coverage: compare hooks to game systems and grow the API until mod authors
   can express the intended changes; test full runs, controller/touch UI,
   malformed packages and performance on the physical handheld.

A general-purpose Lua language does not by itself expose Unity/IL2CPP internals:
each supported interaction requires a bridge, or a separately designed native
extension mechanism. The objective is broad capability, including new systems,
but arbitrary existing mods from other recomp projects are not compatible
without implementing their particular APIs and asset formats.

### Experimental API 3 content packages

The Android launcher imports API 3 ZIPs and enables them from its Mods dialog.
A `manifest.json` at the ZIP root or inside one top-level folder uses `api: 3`, `kind: "content"`, a lowercase ASCII
package `id`, and `files` entries carrying exact relative `path`, byte `size`,
and SHA-256. All files, including `main.lua`, must be declared. Extraction
checks bounds, paths and hashes before moving a staged directory into place;
the launcher can replace an existing mod ID after validation. Imports and updates
start disabled. The character format defines a model bundle, portrait, walk clip, lightning weapon,
and optional per-level stat passive. See [the Pikachu package builder](../../mods/pikachu/build_test_package.py)
for its exact fields. Its `main.lua` returns a table whose `attack(level)`
callback returns a damage multiplier between 0 and 10. The engine provides the
lightning targeting and chain effect; arbitrary Lua commands and compatibility
with other games' mod APIs are not yet implemented. A physical Retroid loaded
the character selection; its full combat and long-run behavior remain to be
tested. `OpenBonkContentArchive` remains a separate transport test harness.

A character may also specify `run`, `jump`, and `surf` clip names and a
`surfboard` transform name inside `character`, as shown in the Pikachu package
builder. These names are case-sensitive and must exist in the bundle when
declared. The board is shown during the surf animation. The walk clip remains
the fallback when optional clips are absent. This animation selection changes
presentation only; it does not change player physics. The board and these
clips have been exercised in an emulator, while physical-controller animation
quality still needs a Retroid play test.

The current build can attach existing hats to an external character's head.
By default it looks for a `Head` or `Head_Armature` transform and estimates the
placement from the model bounds. A mod can instead add the following object
inside `character` to specify a case-sensitive transform name and placement:

```json
"hat": {"bone": "Head_Armature", "offsetX": 0, "offsetY": 0.5, "offsetZ": 0.15, "scale": 1}
```

Offsets are measured in player-renderer local units from the bone; each must
be between -20 and 20, and scale must be between 0.01 and 10. All fields are
required when `hat` is present. The runtime rejects a declared bone that does
not exist. Hats reuse the existing Fox hat orientations, so placement is a
starting point and should be checked for each model and hat. The automatic
estimate was visually checked on Pikachu with three hats; it is not a general
guarantee for other models. The Retroid build installed on 2026-11-10 includes
this support; its behavior with other models and hats remains unverified.

The development build also supports an item-only API 3 package: use an `item`
object instead of `character`, `weapon`, and `passive`. It specifies `id`,
`name`, `description`, `icon` (a declared PNG), `rarity` (`Common`, `Rare`,
`Epic`, `Legendary`, `Corrupted`, or `Quest`), `inPool`, optional `maxAmount`
and `maxAmountPerRun` (zero means no limit), and optionally a `bonuses` array. Each bonus
has an exact `EStat` name such as `PowerupChance`, `mode` of `Flat` or
`Addition`, and numeric `amount` per stack. For example:

```json
"item": {
  "id": "training-cell", "name": "Training Cell",
  "description": "Improves power-up chance and duration.",
  "icon": "assets/icon.png", "rarity": "Common", "inPool": true,
  "bonuses": [
    {"stat": "PowerupChance", "mode": "Flat", "amount": 0.05},
    {"stat": "DurationMultiplier", "mode": "Flat", "amount": 0.02}
  ]
}
```

These fields belong inside the full manifest; the icon and `main.lua` still
need entries in `files`. Optionally add `"hooks": ["stack_changed", "pre_attack"]`
to `item` and return a callback table from `main.lua`:

```lua
local changes = 0
return {
  stack_changed = function(stacks)
    changes = changes + 1
    return stacks == 2 and changes == 2 and 1.5 or 1.0
  end,
  pre_attack = function(stacks, context)
    local bonus = context.boss and 0.5 or 0.25
    return 1.0 + bonus * stacks
  end
}
```

Each acquired item type has one bounded Lua session. `stack_changed` runs on
each gain or loss, including the transition to zero, and its numeric return
scales the item's declared per-stack stat bonuses. `pre_attack` runs through
the inventory's pre-attack path and returns a damage multiplier. Either hook
may be omitted from the manifest. Returns must be finite numbers from 0 to 10.
When the last stack is removed, the session is destroyed; acquiring it later
starts a fresh one. An invalid callback or instruction-limit error closes that
session; a failed stack callback applies no declared stat bonus, and a failed
pre-attack callback adds no damage modifier. `pre_attack` receives a second
argument: a copied context table containing `has_target`, `boss`, `elite`,
`target_health`, numeric `element`, `critical`, and `proc_coefficient`.
Health is sampled before the hit; `element` is 0 neutral, 1 lightning,
2 ice, 3 fire, or 4 bleed. Without a target, the booleans are false and
health is zero.
There is no target handle and changing the table does not change game state.
Older callbacks that accept only `stacks` continue to work. Scripts cannot yet
issue arbitrary commands or inspect other enemy properties.
An item may omit `bonuses` or supply an empty array if it declares a combat
hook (`pre_attack`, `on_hit` or `before_player_damage`). `stack_changed` can also maintain Lua state.
An item with neither bonuses nor a combat hook is rejected.

An optional `"on_hit"` item hook receives the same copied context as
`pre_attack`, plus `damage`, and returns a number from 0 to 10. The host applies
that fraction of the triggering hit as a separate hit to the same living
target; zero adds no hit. Its context health is sampled after the triggering
hit and damage is the triggering damage at the post-hit stage. The extra hit
uses the normal enemy damage path, so target modifiers and mitigation can apply
again. It uses proc coefficient zero, so it cannot trigger other on-hit callbacks. For
example, `on_hit = function(stacks, c) return c.critical and 0.2 * stacks or 0 end`
adds 20% of a critical hit per stack. It is skipped for a target already dead
or dying, invulnerable or teleporting, and for hits whose proc coefficient is zero. It does not let Lua
select or mutate targets or issue arbitrary engine commands.

Alternatively, `on_hit` may return an action table such as
`{damage = 0.25, freeze = 3, poison = 4}`. `damage` is the same extra-hit
fraction as the numeric form (0–10); `freeze`, `stun`, `burn` and `poison`
are durations in seconds (0–30), with one stack per declared effect. Omitted
fields default to zero. The host validates the entire table before doing
anything, then uses the target's ordinary debuff queue and damage paths. A
bad type or out-of-range value stops the item session and applies none of that
callback's actions. This currently affects only the hit target; it is not a
general-purpose world command API.
One package provides one item, one weapon, or one character/weapon pair. These
item hooks have been tested in the emulator; gameplay with them on the Retroid
remains unverified.


### Experimental item level-up callback

An API 3 item with declared stat bonuses can add `level_up` to its `hooks` array and return `level_up = function(level) ... end` from `main.lua`. It runs after the game emits its level-up event while the item is held. Its numeric return, between 0 and 10, scales the declared per-stack stat bonuses. Before the first level-up the scale is 1. When `stack_changed` is also declared, the two returned scales multiply, capped at 10; updating stacks does not invoke `level_up`. Removing the final stack unsubscribes the callback and destroys that item's Lua session. The callback receives only the new level, not a general game context or arbitrary commands. Level-only hooks still require at least one declared stat bonus or another supported combat hook.

### Experimental item enemy-killed callback

An API 3 item with declared stat bonuses can add `enemy_killed` to its `hooks`
array and implement `enemy_killed = function(stacks, context) ... end`. It runs
for each enemy death while the item is held and returns a multiplier between 0
and 10 for that item's declared stat bonuses. The initial multiplier is 1;
it composes with the stack and level multipliers and the combined multiplier is
capped at 10. Lua state can count deaths across callbacks while the item is
held. The callback receives a copied hit context with the same fields as
`on_hit`; `target_health` is sampled after death and is normally zero. Deaths
without a damage container receive zero/default hit fields. This event reports
enemy deaths, not necessarily kills credited to the player. Removing the last
stack unsubscribes the callback and discards its Lua state. It cannot issue
arbitrary commands or select targets. A callback failure stops the item session
and sets this multiplier to zero.

The experimental source also permits `enemy_killed` to return an action table:
`{scale = 1, heal = 1, xp = 1, gold = 2}`. All fields are optional; `scale` defaults to
1 and is bounded from 0 to 10, `heal` from 0 to 100, and `xp` and `gold` must be
integers from 0 to 100. The host validates the complete result before applying
any action. Healing, XP and gold use normal game rules and only apply while the
player is alive. In particular, gold uses the usual multiplier and per-frame cap;
the awarded amount may differ from the requested amount. A numeric result keeps
the earlier multiplier-only behavior. An item may declare only this hook without
stat bonuses. `gold` requires a build with the newer action ABI; older builds
ignore it. See `mods/example-kill-reward` and `mods/example-gold-reward`; this is
not a general world-command API.

### Experimental item player-damaged callback

An API 3 item with declared stat bonuses can declare `player_damaged` and
implement `player_damaged = function(damage) ... end`. After the player's real
health damage path accepts a positive hit, including a shield hit, the callback
receives the damage value from the game's damage container and returns a number
between 0 and 10 to scale its declared bonuses. Blocked hits do not call it;
the value need not equal the exact amount of HP or shield lost. Its initial
scale is 1, it multiplies the other item scales with a combined cap of 10, and
removing the final stack unsubscribes it and discards its Lua state. It cannot
prevent or modify the hit that triggered it or issue arbitrary game commands.
An optional second argument is a snapshot table: `stacks`, `hp`, `max_hp`,
`shield`, `max_shield`, `shield_hit`, `has_enemy`, `boss`, `elite`, `element`
and `critical`. Health and shield are sampled after damage; `shield_hit` is a
boolean. Enemy flags are false if the hit has no enemy. Element uses the same
numeric values as the attack context above. Changing the table does not alter
the game. Older one-argument callbacks continue to work. Callback failure ends
the session and sets this scale to zero.

### Experimental item incoming-damage modifier

An API 3 item can declare `before_player_damage` and implement
`before_player_damage = function(damage) ... end`. It receives the current
positive incoming damage after the game's stop-damage check and returns a
multiplier from 0 to 10. The host applies that multiplier before retaliation,
shield and health subtraction; 0 prevents health or shield loss from that hit,
but does not undo stop-damage checks or other earlier effects. Multiple item
hooks run in inventory tick-priority order, and each sees the damage left by
previous hooks. A failed or unavailable callback leaves that hit unchanged and
stops its Lua session. This hook is allowed without stat bonuses. Its optional
second argument has the same snapshot fields as `player_damaged`, except that
health and shield are sampled before subtraction and `shield_hit` is absent:
whether the hit will use the shield has not yet been determined. It does not
expose enemy objects or arbitrary game commands.

### Experimental standalone lightning weapon

An API 3 content ZIP may contain a `weapon` without a `character` or `item`.
It declares the same lightning fields as the character weapon (`id`, `name`,
`description`, `effect: "lightning"`, `damage`, `cooldown`, `range`,
`chainRange`, `chainProc` and integer `bounces`) plus `icon`, the path of a
PNG listed in `files`. Its `main.lua` returns an `attack(level)` numeric
multiplier. The weapon is registered as available for normal weapon offers;
it respects weapon-slot limits and does not require a custom character. Each enabled weapon still uses the
built-in lightning effect and the current Lua attack callback, not arbitrary
projectile types, weapon visuals or engine commands. A package contains one
standalone weapon, one item, or one character/weapon pair.

Experimental projectile weapons: an API 3 `weapon` may declare `effect` as
`fireball` instead of `lightning`, either standalone or on a character. This
reuses the built-in Fire Staff projectile and its timing and upgrade template;
it does not provide arbitrary projectile models or behavior. The same required
weapon fields are currently validated for both effects, including `chainRange`,
`chainProc`, and `bounces`; the chain fields have no effect on fireballs. The
Lua `attack(level)` multiplier is captured separately for each attack and copied
to its projectiles. An emulator test imported a standalone fireball package and
observed two noncritical hits of 10 and 20 damage from an alternating callback.
A controlled emulator follow-up observed differently scaled projectiles in
flight at the same time and confirmed the original Fire Staff could still hit.
Character-owned fireballs, natural acquisition and physical device behavior
have not yet been verified.

The experimental `arrow` host uses the built-in Bow attack and projectile for
standalone or character weapons. It accepts the same weapon fields as the
fireball host; `chainRange` and `chainProc` do not change arrow behavior. The
Lua multiplier is captured per attack. A standalone diagnostic on Retroid
Pocket 5 observed alternating 10 and 20 damage, differently scaled arrows in
flight simultaneously, and a separate original Bow projectile and hit. It did
not validate character-owned arrows or natural offers. The host does not support
custom projectile models, sounds, or arbitrary engine actions. See
[`mods/example-arrow-weapon`](../../mods/example-arrow-weapon/README.md).

External characters may optionally declare an `idle` clip name in the
`character` object. It must be present in the same model bundle; it plays while
grounded and not moving, and the walk clip remains the fallback when omitted.
This affects presentation only. Selection-screen and physical-controller visual
quality should be checked separately for each model.

When an installed mod is updated, the launcher retains one previous version.
Use Manage mods → Import / restore → Restore previous version and choose the
mod to swap it with that saved version. The restored mod is disabled until
explicitly enabled; the version it replaces becomes the saved previous version.
Restoration is manual: the launcher cannot inspect animation clips inside a
Unity AssetBundle before the game loads it.

### Experimental global stat modifiers

API 1 and 2 also accept `stat_add:<EStat>` and `stat_mul:<EStat>` hooks,
where `<EStat>` is an exact, case-sensitive name in the game's `EStat` enum
(except `Unused0`). `stat_add` returns a flat game-unit change from -1000 to
1000; `stat_mul` returns a multiplier from 0.1 to 4. A multiplier on a stat
whose base and other contributions are zero remains zero. The game's normal
stat calculation, caps and rounding still apply. API 2 accepts up to 112 hooks: at most one flat addition and one
multiplier per stat, including when mixing old aliases and new names. The
normal game calculation applies flat additions before multipliers. Mods run once when the
player inventory is initialized and their Lua state is not persistent. They
cannot register new stats or directly control arbitrary game behavior. See
`example-stat-mod` for health, duration and extra-jump examples.

### Experimental level-dependent global stats

An API 2 package can set `"refresh": "level_up"`. The host evaluates its
`main.lua` with a local numeric `level` set to the player's current level at
initialization and reevaluates it after each level-up event. The result remains
an ordered array matching `hooks`, and replaces that package's previous stat
modifiers. Each evaluation starts a fresh Lua state; the script cannot retain
Lua variables or invoke arbitrary game commands. If a refresh fails or any
value is invalid, none of its new values are applied and the last valid values
remain; a later level-up tries again. If the initial
evaluation fails, the package is skipped until the next player initialization.
A stage initialization rereads the package and current level. If the same
enabled package still declares the same hooks but evaluation fails during a
stage transition with the same inventory, its previous values are retained.
See `example-level-stats-mod`.

### Experimental external maps

The game-side registry now has an internal `RegisterExternalMap` adapter and a
stable, namespaced map ID, with duplicate and collision rejection. External
map menu text can use explicit names and descriptions. An editor check covers
registry lookup and map-progress JSON serialization. An experimental source
branch also accepts bounded Forest- and Desert-based recipes and clones their
stage parameters. Both recipes can replace top-surface and wall textures with
declared `groundTexture` and `wallTexture` files. Desert recipes expose noise
scale, height and map scale and inherit Desert's environmental events;
the resulting Android build remains experimental. The Retroid build installed
on 2026-11-10 includes these map changes; they have not yet been verified in a
complete natural run on that device.
An emulator smoke test covered startup, one forced portal transition and a forced
victory across mod disable/enable. A map may opt into the bounded
`stage_spawn_multiplier` Lua hook by declaring it in `map.hooks`. Its function
receives the one-based stage number and returns a finite multiplier from 0 to 10.
The host evaluates it once when the ordinary stage summoner is created; the value
scales that summoner's credit rate and target enemy count, not bosses or every
other spawn source. Lua state is kept between stages for the run and reset on a
new run. A missing, invalid or failing callback falls back to 1. Maps without the
hook use 1 and do not execute the script for this purpose. This hook has only
been exercised across one forced portal transition in an emulator; custom stage
geometry, broader Lua map hooks, complete natural gameplay and arbitrary assets
remain unsupported or unverified. Do not treat this as a complete map API.

An experimental map may also declare `stage_enemy_health_multiplier` in
`map.hooks` and return a function by that name from `main.lua`. It receives the
one-based stage number and returns a finite multiplier from 0 to 10. The host
evaluates it on the first applicable enemy health calculation per stage and
reuses that result for the stage. It scales the ordinary calculated health,
including boss health, but the invincible ghost's special value is unchanged.
If the callback is absent, fails or returns an invalid value, health uses the
normal multiplier of 1. It shares the map's Lua session with the spawn hook;
an error stops that session, and callback ordering should not be assumed.
The hook has been tested with controlled Goblin and Ent enemies on an Android
emulator, including an invalid result, a forced transition with Lua state
retained and a forced run restart with state reset. The ghost's early return was
also checked by temporarily substituting its data in a health calculation.
Other enemies, natural runs and physical hardware are unverified.

Maps can also declare `stage_enemy_speed_multiplier` and
`stage_enemy_damage_multiplier`. Each receives the one-based stage and returns
a finite multiplier from 0 to 10; each result is cached for that stage and
reset for a new run. The speed multiplier applies before the game's normal
speed cap, so the final ratio can be smaller than the requested multiplier;
Ghost King's special speed return remains unchanged. Damage scales the game's
enemy damage calculation. They share the same session and failure behavior as
the health hook. A failing callback stops that session; subsequent hook reads,
including values cached earlier in the stage, use the neutral multiplier 1.
Effects already applied, such as an enemy's assigned maximum health, are not
retroactively undone. Controlled
emulator checks cover a Goblin, one forced transition and one forced restart,
not natural combat, all enemies or physical hardware.

The experimental source also accepts `enemy_health_multiplier`,
`enemy_speed_multiplier` and `enemy_damage_multiplier` in `map.hooks`. Each
receives a copied table such as `{stage = 1, enemy = "Goblin"}` and returns a
finite multiplier from 0 to 10. Results are cached per stage, enemy type and
stat, and multiply the corresponding stage-wide value. They therefore cannot
vary by individual enemy, elite status, or time within a stage. The invincible
ghost health and Ghost King's speed keep their existing special cases. Failure
stops the shared session and subsequent reads fall back to 1. These hooks are
experimental and do not provide arbitrary enemy behavior or new enemy types.

The experimental source branch also accepts decorative map prefabs from an
Android Unity AssetBundle. Declare the bundle as a checked package file and
set `map.bundle` to its package path. Add `map.objects`, an array of one to
 eight entries, each with `prefab` (asset name), `amount` (1–64), `scaleMin`
and `scaleMax` (0.1–10, maximum at least minimum), `checkRadius` (0.1–20),
`maxSlopeAngle` (0–60), and `upOffset` (-10–10). The prefab must contain a
renderer. The host appends these placements to each cloned stage and chooses
positions and Y rotation using the existing map generator. For example:

```json
"bundle": "assets/map-props",
"objects": [{"prefab": "ProbeBeacon", "amount": 25, "scaleMin": 1,
  "scaleMax": 1, "checkRadius": 1, "maxSlopeAngle": 45, "upOffset": 4}]
```

This has been checked with one simple visual prefab in an emulator. It is not
an arbitrary script or gameplay-component API, and general asset, shader and
physical-device compatibility remain unverified.

For a small, reproducible importable example with all four Lua hooks, see
[`mods/example-forest`](../../mods/example-forest/README.md). It requires an
build that includes external-map support, such as the experimental Retroid APK
installed on 2026-11-10.

An object may also include `"stages": [1, 3]` to appear only in those
one-based stages. The list must contain one to three distinct integers from 1
to 3. Omitting it keeps the default of placement in all three stages. Invalid
lists are rejected; they do not modify the cloned stages.

### Experimental weapon target context

On the experimental weapon-context build, an API 3 weapon's `attack(level, target)` callback receives a copied table for the first enemy selected by the host: `enemy` (the built-in enemy enum name), `boss` and `elite` booleans, `target_health`, and `distance` between the player and target transforms. For example:

```lua
return {
  attack = function(level, target)
    if target.boss then return 1.5 end
    return 1.0
  end
}
```

The return remains a multiplier from 0 to 10. The callback runs once per host attack after a target is found; multiple lightning strikes share that one result, and a fireball's later projectile target is not guaranteed to be the selected enemy. No callback is made when no enemy is found. The table contains values only, not a handle or a command to alter the enemy. Existing `attack(level)` functions ignore Lua's extra argument. This remains experimental and does not provide arbitrary targeting, new attack types or full mod compatibility.

### Experimental periodic item callback

On the tick-context build, `tick(stacks, context)` also receives a copy of
`health_available`, `hp`, `max_hp`, `shield`, `max_shield`, and game `time` in
seconds. `time` is the game's scaled, pause-aware clock (`MyTime.time`); it is
not reset at the start of a run and must not be treated as elapsed run time.
When the player's health component is unavailable the boolean is
false and the four health values are zero. The values are a snapshot before
the requested healing is applied; assigning to the table cannot change the
player. Older `tick(stacks)` callbacks ignore the extra argument. For example:

```lua
return {tick = function(stacks, context)
  if context.health_available and context.hp < context.max_hp * 0.5 then
    return {heal = math.min(2 * stacks, 100)}
  end
  return {heal = 0}
end}
```

The same bounds and pause behavior below still apply. This does not expose
arbitrary player mutation or an unbounded per-frame callback.

The extended tick-context build additionally provides `level_available` and
`level` (the current `PlayerXp.level`), plus `manager_time_available` and
`manager_time` (seconds from the current `GameManager.GetAliveTime()`). When
the corresponding object is unavailable, the flag is false and its numeric
value is zero. The manager clock advances only while its game manager is
playing and a player exists; it belongs to that manager instance and must not
be assumed to represent an entire multi-stage run. These values are copies and
do not give the script permission to change the player or the clock. Older
callbacks may ignore the added fields; the previous native tick entry point
also remains available.

An API 3 item may include `"tick"` in its `hooks` and implement `tick = function(stacks) ... end`. Starting one game second after acquisition, the callback runs at most once per game second while the item is held and the game is unpaused. It can return either a finite 0–10 multiplier for its declared stat bonuses, or a table such as `{scale = 1, heal = 7}`. The table's optional `scale` defaults to 1 and `heal` defaults to 0; healing must be finite and between 0 and 100. An item with `tick` may have an empty bonus list, for example to provide healing alone. Its multiplier combines with the existing stack, level, kill and damage scales, with the combined scale capped at 10.

Healing uses the game's normal player health function, including its existing healing rules, without overhealing or reviving a player at zero health. The entire table is validated before its requested actions are applied: an invalid return cannot partially heal or apply its requested scale. The Lua session and its local variables persist while the item is held and are discarded after its last stack is removed. Missed intervals are not replayed; a delayed callback schedules its next call from the current game time. A callback failure stops its Lua session and sets this scale to zero. This hook does not currently spawn entities, change arbitrary values or expose an unbounded per-frame update.
