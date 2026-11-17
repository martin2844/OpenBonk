# Lua mods

OpenBonk's mod API is experimental and specific to OpenBonk. Mods can use supported hooks and packaged content, but Lua does not have unrestricted access to Unity, game internals, files or the network. The sandbox is not a security boundary: only install mods from people you trust.

## Install a mod

1. Download or build a compatible mod ZIP and copy it to the Android device.
2. Open the OpenBonk **launcher** and choose **Import mod**. Select the ZIP using Android's file picker.
3. Open **Manage mods** and enable the imported mod. New imports and updates start disabled.
4. Press **Play** and start a new run. If you update a mod, enable it again in **Manage mods**.

The [Pikachu example](../mods/pikachu/README.md) adds an experimental character and weapon. Its guide links to the downloadable ZIP and explains how to build it from the checked-in files; the mod is not built into the APK. Additional examples are under [`mods/`](../mods/).

## Online catalog

In beta 0.1.2, the launcher's **Mods → Browse mods** can search the curated [mod catalog](../mods/catalog/index.json), download a selected ZIP, check its size and SHA-256, and pass it through the normal mod importer. Imports start disabled: enable them under **Manage mods**. A checksum detects corrupt or unexpected downloads; it does not make a mod trustworthy.

To propose a catalog entry, open a pull request updating the index with the mod ID, name, description, version, GitHub release ZIP URL, exact byte size and SHA-256. The ZIP must use the documented mod format and its manifest ID must match the entry. Downloads currently support GitHub release assets only. Keep old release assets available for reproducibility.

## Your first stat mod

Create `manifest.json`:

```json
{
  "id": "my-speed-mod",
  "api": 1,
  "enabled": false,
  "hook": "move_speed_multiplier"
}
```

Create `main.lua`:

```lua
-- 1.25 means a 25% increase, applied when the player is initialized.
return 1.25
```

Put both files at the root of a ZIP and import it in the launcher. API 1 accepts one of `move_speed_multiplier`, `damage_multiplier`, `attack_speed_multiplier`, `xp_multiplier`, `gold_multiplier`, or `pickup_range_multiplier`; its result must be between 0.1 and 4. API 2 supports multiple stat hooks. See the [runtime reference](../native/lua/README.md) and [`native/lua/example-*`](../native/lua/) for examples and restrictions.

## Experimental starting tomes (source builds only)

A development build may support the API 1 hook `starting_tome:Luck` (or another
exact `ETome` name). Return a two-number Lua table containing a flat stat bonus
from -10,000 through 10,000 and an integer level count from 1 through 99:

```lua
return { 10000, 99 }
```

The host adds the tome once to each new player inventory, using a New grant and
then empty Legendary upgrades. A repeated spawn reusing the same inventory is
ignored. This affects future runs after enabling or disabling the package; it
does not remove a grant from a run already in progress. Special tomes may
perform their usual effects when upgraded. This hook requires a future build;
**beta 0.1.4 does not support it**. See the independent [Lucky Tome example](../mods/lucky-tome/).

## Content packages

Experimental API 3 packages use a `manifest.json` with `api: 3`, `kind: "content"`, an `id`, and a `files` list with each payload's path, size and SHA-256. The package can declare supported character, weapon, item or map content. Exact fields and hooks depend on the content type; use the builders as working examples rather than assuming arbitrary game objects can be created.

- [Pikachu character and Thunderbolt weapon](../mods/pikachu/) — `attack(level)` returns a damage multiplier; the host handles its lightning effect.
- [Example Forest](../mods/example-forest/) — a map based on Forest with supported stage hooks.
- [Practice Arrow](../mods/example-arrow-weapon/) — a weapon based on the host's existing arrow effect.
- [XP item](../mods/example-xp-item/) — a periodic item callback.

For a procedural example, run `python3 mods/example-forest/build.py` from the repository root. It creates `mods/example-forest/example-forest.zip`; copy that ZIP to Android and import it as above. See each example's README for build-specific compatibility and its limits. API 3 asset bundles must target Android and match the host's expectations. Unsupported callbacks or assets cannot be made compatible merely by writing Lua.

For detailed API 1, 2 and 3 behavior, validation, callback limits and known gaps, read the [runtime reference](../native/lua/README.md). Some sections describe experimental builds; verify behavior against the APK version you use.
