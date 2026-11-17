# Megabonk mod port research

Snapshot: 2026-11-17. The [Thunderstore Megabonk API](https://thunderstore.io/c/megabonk/api/v1/package/) returned 155 packages; the complete metadata snapshot is [here](thunderstore-2026-11-17.json). Downloads are summed across versions. This is a Thunderstore ranking, not a ranking across Nexus or an inventory of every mod.

The table excludes deprecated packages and the three platform-wide loaders/managers r2modman, BepInExPack IL2CPP and Gale. Other tools and libraries are included where they rank in the top 20. Counts can include repeat downloads and are not unique players.

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

## Porting status

None of these packages has been ported and bundled as part of this research. The existing experimental API does not expose general save/unlock, spawn, UI, per-kill pickup, network or Steamworks APIs. A Lua script cannot by itself implement these without new host functionality. Multiplayer additionally needs a networking and synchronization design. Models, animations and audio need Android-compatible assets.

Check the original author, license and asset permissions separately for each port before copying or bundling source or artwork. A package link or source repository alone does not establish redistribution permission. Preserve attribution and applicable notices. Avoid claiming compatibility until package installation, enable/disable behavior and the feature itself are tested on Android.

Nexus also hosts Megabonk mods; its ranking is separate and overlap and download counts must be evaluated independently. The Thunderstore API snapshot above does not establish the top 20 across both services.
