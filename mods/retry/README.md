# Retry

When you take lethal damage, **Retry** revives you in place at full health
and shield. You keep your inventory and the run continues from the same time.
Choose **End run** to see the usual death screen. Forced challenge failures
still end the run.

Build the package from the repository root:

```sh
python3 mods/tools/package_mod.py mods/retry build/retry.zip
```

Import the ZIP in the OpenBonk launcher and enable it in **Manage Mods** before
launching the game. The mod requires a build with the API 1
`retry_on_death` hook. This hook is introduced after beta 0.1.10.
