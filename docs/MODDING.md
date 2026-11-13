# Lua mods

OpenBonk's mod API is experimental and specific to OpenBonk. Mods can use supported hooks and packaged content, but Lua does not have unrestricted access to Unity, game internals, files or the network. The sandbox is not a security boundary: only install mods from people you trust.

## Install a mod

1. Download or build a compatible mod ZIP and copy it to the Android device.
2. Open the OpenBonk **launcher** and choose **Import mod**. Select the ZIP using Android's file picker.
3. Open **Manage mods** and enable the imported mod. New imports and updates start disabled.
4. Press **Play** and start a new run. If you update a mod, enable it again in **Manage mods**.

The [Pikachu example](../mods/pikachu/README.md) adds an experimental character and weapon. Its ready-made ZIP is in that directory; it is not built into the APK. Additional examples are under [`mods/`](../mods/).

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

## Content packages

Experimental API 3 packages use a `manifest.json` with `api: 3`, `kind: "content"`, an `id`, and a `files` list with each payload's path, size and SHA-256. The package can declare supported character, weapon, item or map content. Exact fields and hooks depend on the content type; use the builders as working examples rather than assuming arbitrary game objects can be created.

- [Pikachu character and Thunderbolt weapon](../mods/pikachu/) — `attack(level)` returns a damage multiplier; the host handles its lightning effect.
- [Example Forest](../mods/example-forest/) — a map based on Forest with supported stage hooks.
- [Practice Arrow](../mods/example-arrow-weapon/) — a weapon based on the host's existing arrow effect.
- [XP item](../mods/example-xp-item/) — a periodic item callback.

For a procedural example, run `python3 mods/example-forest/build.py` from the repository root. It creates `mods/example-forest/example-forest.zip`; copy that ZIP to Android and import it as above. See each example's README for build-specific compatibility and its limits. API 3 asset bundles must target Android and match the host's expectations. Unsupported callbacks or assets cannot be made compatible merely by writing Lua.

For detailed API 1, 2 and 3 behavior, validation, callback limits and known gaps, read the [runtime reference](../native/lua/README.md). Some sections describe experimental builds; verify behavior against the APK version you use.
