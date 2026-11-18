# Distrito Nimbo

Development package for a terraced city in OpenBonk. Three districts have
distinct palettes, arena landmarks, local encounters, and boss rewards.
Each seed uses the Forest tile mesh and its full elevation changes. Slopes
are avenues with sidewalks and crosswalks. Flat tiles are city plazas and
terraces, and cliffs use contrasting architectural facades.

## Build the package

1. Open `authoring/distrito-nimbo.blend` in Blender and bake its props with
   `blender -b authoring/distrito-nimbo.blend --python authoring/export_props.py -- /tmp/nimbo-props.json`
   from this directory.
2. In an Android-capable Unity project using the same Unity version and layer
   settings as OpenBonk, put `BuildDistrictBundle.cs` in `Assets/Editor` and
   import `NimboProp.shader`, `NimboArchitecture.shader`, `NimboTerrain.shader`,
   `NimboSky.shader`, and `NimboWarning.shader`. Enable Unity's built-in
   Particle System module for the capsule break effect.
   Use a project dedicated to authoring. The builder
   regenerates assets with the same names in `Assets/NimboAuthoring`.
3. Bake the enemy rigs using the commands below. Set `OPENBONK_NIMBO_PROPS` to
   the prop JSON, `OPENBONK_NIMBO_CREATURE` to the directory containing the
   creature JSON files, and `OPENBONK_NIMBO_BUNDLE` to an output
   directory. Run Unity in batch mode with `-buildTarget Android` and
   `-executeMethod BuildDistrictBundle.Run`.
4. Run `python3 build.py /tmp/distrito-nimbo.zip --props /path/to/nimbo-props`.

Import the ZIP through **Mods** in a development APK with tiled architecture and
map encounter support, enable the package, and select **Distrito Nimbo** in the
game's map menu.

The current roster uses original models. Terminal features the Coil
Mastiff's charge, Interchange features the River Manta's sweep, and Relay
features the Relay Keeper's pulse. A defeated boss grants a district item
and opens an onward portal. The final portal completes and saves the run.
Red-and-white capsules have a custom shatter effect and use the base drop
rules.

## Creature art

The Blender files contain editable rigs and named timelines for the
current roster. Bake their mesh frames from this directory.

```sh
mkdir -p /tmp/nimbo-creatures
blender -b authoring/beacon-beetle.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/beetle.json
blender -b authoring/rooftop-firefly.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/firefly.json "Rooftop firefly" RooftopFirefly
blender -b authoring/coil-mastiff.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/mastiff.json "Coil mastiff" CoilMastiff
blender -b authoring/reed-runner.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/runner.json "Reed runner" ReedRunner
blender -b authoring/river-manta.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/manta.json "River manta" RiverManta
blender -b authoring/glass-spark.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/spark.json "Glass spark" GlassSpark
blender -b authoring/relay-keeper.blend --python authoring/export_beetle.py -- /tmp/nimbo-creatures/keeper.json "Relay keeper" RelayKeeper
```
