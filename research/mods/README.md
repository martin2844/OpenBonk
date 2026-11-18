# Megabonk mod port research

Snapshot: 2026-11-17. The [Thunderstore Megabonk API](https://thunderstore.io/c/megabonk/api/v1/package/) returned 155 packages; the complete metadata snapshot is [here](thunderstore-2026-11-17.json). Downloads are summed across versions. This is a Thunderstore ranking, not a ranking across Nexus or an inventory of every mod.

The table excludes deprecated packages and the three platform-wide loaders/managers r2modman, BepInExPack IL2CPP and Gale. It includes the first 20 single-player candidates plus the excluded multiplayer entry for context. Other tools and libraries are included. Counts can include repeat downloads and are not unique players.

| Rank | Package | Downloads | Source |
| ---: | --- | ---: | --- |
| 1 | [MegabonkModders-CustomCharacterCreator](https://thunderstore.io/c/megabonk/p/MegabonkModders/CustomCharacterCreator/) | 49,176 | [Author site](https://github.com/PeterMoras/MEGABONK_CustomCharacterLoader) |
| 2 | [lukeod-UnlockEverything](https://thunderstore.io/c/megabonk/p/lukeod/UnlockEverything/) | 41,433 | [Author site](https://github.com/lukeod/megabonk_unlockEverything) |
| 3 | [BonkWithFriends-BonkWithFriends](https://thunderstore.io/c/megabonk/p/BonkWithFriends/BonkWithFriends/) | 39,570 | Not identified |
| 4 | [AutoBans-ItemSpawner](https://thunderstore.io/c/megabonk/p/AutoBans/ItemSpawner/) | 32,725 | Not identified |
| 5 | [Vegasx-Goth_Witch_Noelle](https://thunderstore.io/c/megabonk/p/Vegasx/Goth_Witch_Noelle/) | 26,669 | [Author site](https://github.com/Vegasx) |
| 6 | [kss-BonkersLib](https://thunderstore.io/c/megabonk/p/kss/BonkersLib/) | 22,620 | [Author site](https://github.com/kss306/MegaBonkPlus/tree/master/BonkersLib) |
| 7 | [Vegasx-Cownoichi](https://thunderstore.io/c/megabonk/p/Vegasx/Cownoichi/) | 21,166 | [Author site](https://github.com/Vegasx) |
| 8 | [ChrispyNi-GodOfDeath](https://thunderstore.io/c/megabonk/p/ChrispyNi/GodOfDeath/) | 20,279 | [Author site](https://www.twitch.tv/chrispyni) |
| 9 | [Intancote-ToggleEverythingForBepInEx](https://thunderstore.io/c/megabonk/p/Intancote/ToggleEverythingForBepInEx/) | 20,038 | [Author site](https://github.com/Intancote/ToggleEverything-BepInEx) |
| 10 | [wafuruns-MegabonkBetterMinimap](https://thunderstore.io/c/megabonk/p/wafuruns/MegabonkBetterMinimap/) | 18,783 | [Author site](https://github.com/WafuRuns/MegabonkBetterMinimap) |
| 11 | [anro-ItemGiver](https://thunderstore.io/c/megabonk/p/anro/ItemGiver/) | 18,651 | [Author site](https://github.com/anro772/ItemGiverMod) |
| 12 | [Antiparty-SpawnTracker](https://thunderstore.io/c/megabonk/p/Antiparty/SpawnTracker/) | 18,437 | Not identified |
| 13 | [kss-MegaBonkPlus](https://thunderstore.io/c/megabonk/p/kss/MegaBonkPlus/) | 18,184 | [Author site](https://github.com/kss306/MegaBonkPlus/tree/master/MegaBonkPlusMod) |
| 14 | [China1-InstantCollect](https://thunderstore.io/c/megabonk/p/China1/InstantCollect/) | 17,277 | [Author site](https://github.com/bluejaysFay420/InstantCollect) |
| 15 | [Semibox-LuckyMan](https://thunderstore.io/c/megabonk/p/Semibox/LuckyMan/) | 15,942 | [Author site](https://github.com/Semibox/MegabonkMod) |
| 16 | [Semibox-ToggleEverything](https://thunderstore.io/c/megabonk/p/Semibox/ToggleEverything/) | 15,522 | [Author site](https://github.com/Semibox/ToggleEverything) |
| 17 | [Celllnside-SimpleModMenu](https://thunderstore.io/c/megabonk/p/Celllnside/SimpleModMenu/) | 15,094 | [Author site](https://github.com/Celllnside/SimpleModMenu) |
| 18 | [Maskoliver-Potomatic](https://thunderstore.io/c/megabonk/p/Maskoliver/Potomatic/) | 14,167 | Not identified |
| 19 | [LethalM-Megabonk_Save_Editor_Unlock_All](https://thunderstore.io/c/megabonk/p/LethalM/Megabonk_Save_Editor_Unlock_All/) | 14,164 | Not identified |
| 20 | [timjobs-GothKittySiella](https://thunderstore.io/c/megabonk/p/timjobs/GothKittySiella/) | 13,980 | [Author site](https://twitch.tv/Timjobs) |
| 21 | [Helix128-StatHUD](https://thunderstore.io/c/megabonk/p/Helix128/StatHUD/) | 13,032 | [Author site](https://github.com/Helix128/StatHUD/) |

## Porting status

**Update for beta 0.1.8:** The inventory below records the earlier 0.1.4
feasibility review and is not the current catalog. The launcher now offers
Unlock Everything, Instant Collect, Lucky Tome, Auto Pots, Item Lab, Chest
Tracker, Minimap Zoom, Toggle Everything and Run Metrics alongside Pikachu.
Several are focused or partial independent adaptations, not full ports of the
original PC packages. See [the live catalog](../../mods/catalog/index.json) for
minimum versions and scope. Run Metrics counts effective weapon damage, not
all damage sources. Multiplayer remains excluded.

### Compatibility inventory for beta 0.1.4

The rank, download counts and package links in the table above identify these
20 single-player candidates and the excluded multiplayer entry. This is a feasibility review; only the explicitly described adaptations have been tested on a device.
“Unknown” means no license was established for the package; GitHub's detected
source-code license does not establish rights to third-party models or audio.
Multiplayer is excluded by request.

| Rank | Function | Source license evidence | Missing capability / result |
| ---: | --- | --- | --- |
| 1 | Load custom characters | [MIT source](https://github.com/PeterMoras/MEGABONK_CustomCharacterLoader/blob/main/LICENSE) | Existing API 3 supports a subset; PC bundles, weapons and arbitrary character behavior need conversion and more runtime APIs. |
| 2 | Unlock characters, skins and items | [MIT source](https://github.com/lukeod/megabonk_unlockEverything/blob/main/LICENSE) | [Experimental Lua port](../../mods/unlock-everything/) is available in the 0.1.4 catalog. Remote import, enable/disable, menu and a short run were tested on an isolated Android build. |
| 3 | Multiplayer | Unknown | Excluded. |
| 4 | Add selected items through a menu | Unknown | [Independent Item Lab adaptation](../../mods/item-lab/) provides bounded inventory actions and an in-run UI in a diagnostic build. Requires a future APK and integrated launcher test. |
| 5 | Goth Witch Noelle character | Unknown; model rights unverified | Windows bundle, custom character/weapon behavior and asset rights need review. |
| 6 | Shared BonkersLib framework | [MIT repository](https://github.com/kss306/MegaBonkPlus) | PC BepInEx library, not a self-contained Lua gameplay mod; each consumer needs equivalent APIs. |
| 7 | Cow-noichi character | Unknown; model rights unverified | See [study](COW_NOICHI.md): custom projectile, assets and published runtime support remain missing. |
| 8 | GodOfDeath character | Unknown; model rights unverified | PC bundle, base stats, collider and custom weapon; requires Android conversion and supported weapon behavior. |
| 9 | Toggle unlocked weapons, tomes and items | Unknown | Needs item-pool and inventory controls, plus configuration UI. |
| 10 | Minimap resize, zoom and map information | Unknown | Needs map/UI controls and map data unavailable to Lua. |
| 11 | Search and spawn items, including bulk actions | Unknown | [Independent Item Lab adaptation](../../mods/item-lab/) includes search, rarity filtering, rapid quantities and filtered bulk actions. Requires a future APK; parity and physical-controller testing remain incomplete. |
| 12 | Track chests, shrines and vendors in an overlay | Unknown | Needs world object enumeration/events and overlay UI. |
| 13 | Web dashboard for stats, inventory and game actions | [MIT repository](https://github.com/kss306/MegaBonkPlus) | Needs live state, inventory, teleport, spawn and UI/network APIs. |
| 14 | Instantly collect XP orbs when spawned | Unknown; linked source currently unavailable | [Independent experimental adaptation](../../mods/instant-collect/) is available in the 0.1.4 catalog. Remote import and enable/disable were tested on an isolated Android build; a separate probe covered combined orbs and single rewards. |
| 15 | Start with level 99 Luck Tome | Unknown; no license identified in [source repository](https://github.com/Semibox/MegabonkMod) | [Source](https://github.com/Semibox/MegabonkMod/blob/main/MegabonkMod.LuckyMan/Core.cs) inserts a tome with +10,000 Luck and 98 Legendary upgrades. The [independent experimental Lucky Tome implementation](../../mods/lucky-tome/) uses a new starting-tome hook tested in an isolated Android runtime; the published 0.1.4 APK does not support it. Repeated spawns on one inventory are intentionally guarded, unlike the observed original. |
| 16 | Toggle unlocked weapons, tomes and items | Unknown | Needs item-pool and inventory controls, plus configuration UI. |
| 17 | Mod menu for refreshes, resources and runtime patches | Unknown | Needs live resource mutation and menu controls beyond existing Lua hooks. |
| 18 | Automatically open nearby pots | Unknown | [Independent Auto Pots adaptation](../../mods/auto-pots/) uses a new startup policy and the normal pot interaction. An isolated Android probe covered range, pause and duplicate prevention. It is not in the 0.1.4 catalog; integrated launcher testing is pending. |
| 19 | Offline save editor and unlock-all tool | Unknown | Standalone save editing, not an in-game Lua hook; APK has no mod-facing save API. |
| 20 | Goth Kitty Siella character | Unknown; model rights unverified | Custom character assets and behavior require Android conversion and fidelity/rights review. |
| 21 | In-game DPS, XP and gold rate display | Unknown | Requires live rate data and an in-game overlay API; current Lua hooks cannot create the HUD. |

Lower-ranked [StatMod](https://thunderstore.io/c/megabonk/p/anro/StatMod/)
is also not equivalent to the current global stat hook: it offers an interactive
in-run editor and per-stat resets. [InstantCollect](https://thunderstore.io/c/megabonk/p/China1/InstantCollect/)
and [Potomatic](https://thunderstore.io/c/megabonk/p/Maskoliver/Potomatic/)
specify collection and interaction behavior respectively; inflating pickup
range is not equivalent. Unlock Everything and Instant Collect have been
published as experimental catalog adaptations; the other candidates remain
subject to the limitations listed above.

The smallest groups of missing host capabilities are:

1. **Inventory and unlocks:** inspect and add/remove/upgrade specific items,
   weapons and tomes; edit item pools; read/write progression unlocks. These
   are needed by ranks 2, 4, 9, 11, 15, 16 and 19. In particular, an API 3
   item definition does not give a Lua mod an arbitrary inventory command.
2. **World queries and actions:** enumerate nearby pots, chests, shrines,
   vendors and XP orbs; observe their creation and state; interact with or
   collect a specific object. Ranks 12, 14 and 18 need these operations. A
   kill callback supplies no orb handle and does not prevent duplicate rewards.
3. **Interactive UI and live game state:** display mod panels, controls and
   map overlays, inspect/change game state, and persist configuration. Ranks
   4, 9–13, 16, 17 and 21 need some combination. Fixed stat hooks do not implement
   an interactive editor or a dashboard.
4. **Characters and weapons:** load Android-compatible models, materials,
   animation and sounds with suitable attribution/permissions; define weapon
   projectiles, collision, damage and upgrades. Ranks 1, 5, 7, 8 and 20 need
   varying subsets. The base-stat/collider schema is present in 0.1.4 but
   does not solve custom projectiles or asset conversion.

These are broad requirements, not a promise that one hook would make every
mod compatible. In each case the original package's timing, numerical rules,
reset behavior and compatibility still require comparison and device tests.

The remote catalog contains experimental Unlock Everything and instant-XP
adaptations that require 0.1.4. The published runtime does not expose general
save/unlock mutation, spawn, UI, arbitrary pickup, network or Steamworks APIs.
A Lua script cannot by itself implement these without new host functionality.
Multiplayer additionally needs a networking and synchronization design. Models,
animations and audio need Android-compatible assets.

Multiplayer is excluded from the requested work. For the remaining candidates,
the installed beta's API must be checked before publishing a catalog entry: a remote
Lua package cannot add a host function that the installed APK does not expose.
The catalog can be updated without changing the APK when the package is already
compatible with that API.

On 2026-11-17 the published 0.1.2 APK was tested on a Xiaomi 22126RN91Y:
the launcher downloaded Pikachu from the GitHub-backed catalog, imported it
disabled, enabled it, and started a Forest run. Pikachu defeated enemies and
reached level 1; the stats screen showed 1.01x duration and power-up drop
chance. After disabling it and restarting the game, Pikachu was absent from
character selection. This is a short test of the existing Pikachu package, not
validation of the requested PC mods, every animation, or long-run stability.
The temporary installation was removed.

The downloaded Cow-noichi 1.1.1 and GodOfDeath 1.4.0 packages each contained
a Unity 2023.2.20f1 asset bundle targeting StandaloneWindows64, with meshes,
animation clips and textures. They are not Android bundles. Their JSON files
also specify character stats and custom weapons; matching those behaviors
requires more than adding their bundle to the current catalog. No files from
those packages were included in this repository.

For a local Cow-noichi experiment, AssetRipper exported the Windows bundle and
Unity 2023.2.22f1 rebuilt its prefab, mesh and five clips into an Android asset
bundle. A manually copied experimental API 3 package appeared in beta 0.1.2's
character-selection menu on the Xiaomi. The model rendered at an incorrect
scale and with white materials, its custom stats were not preserved, and its
weapon used the existing arrow host instead of the original Kunai behavior.
This was not a valid or complete port and was not published. The temporary test
installation and ZIP were removed from the device. A second local build used a
simple custom shader and adjusted the scale: textures appeared in character
selection and the prototype entered Forest and defeated eight enemies on the
same device. Its arrow-host projectile was visibly oversized. This remains an
incomplete port: the prototype does not preserve the original custom weapon or
base stats, and animations and long-run behavior have not been validated. The
second temporary installation was removed. Neither study establishes permission
to redistribute the original author's assets.

Cow-noichi 1.1.1's character metadata declares eight base stat modifiers,
custom collider dimensions, a per-level Luck passive and a Kunai weapon with
its own projectile prefab, sound and upgrade options. In 0.1.2, the API 3
character loader instead copies Fox's base stats and collider, and supports
only the host's lightning, fireball or arrow attack implementations. The
passive can express the per-level Luck change, but the complete character
cannot be reproduced by its Lua script alone. A faithful port needs validated
character-specific base stats/collider definitions and a suitable custom
projectile/weapon interface (including size, audio and upgrades). The original
model's material shader also needs an Android-compatible equivalent, not just
an arbitrary opaque fallback. These require changes to the installed host or
its supported data format; a catalog-only update cannot supply missing host
capabilities.

Check the original author, license and asset permissions separately for each port before copying or bundling source or artwork. A package link or source repository alone does not establish redistribution permission. Preserve attribution and applicable notices. Avoid claiming compatibility until package installation, enable/disable behavior and the feature itself are tested on Android.

Nexus also hosts Megabonk mods; its ranking is separate and overlap and download counts must be evaluated independently. The Thunderstore API snapshot above does not establish the top 20 across both services.
