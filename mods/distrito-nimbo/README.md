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

### Locally supplied Pokémon rigs

For the optional Pokémon package, place the decoded skinned GLBs in
`PRIVATE_ROOT/decoded/{15,16,19,20,22,24,34,6,112,130}.glb`, then run:

```sh
blender -b --python authoring/bake_private_creatures.py -- /path/to/PRIVATE_ROOT
```

This writes `PRIVATE_ROOT/creatures/*.json` for `OPENBONK_NIMBO_CREATURE`.
Optional model IDs after the root restrict the bake. The baker poses the
weighted skeleton for every frame: birds flap during flight and hovering,
land creatures move their legs, arms, heads and tails, and serpents bend
through their spine chains. Mesh simplification happens once before skinning
so all frames preserve topology and vertex colors.

Every baked clip must change distances between mesh vertices; a translated or
rotated static mesh fails this check. Pidgey and Fearow must also have at least
0.4 model units of wingtip travel relative to the torso, while never dipping
more than 0.1 below the initial frame's floor. The baker writes
`animation-validation.json` with the measured deformation for all clips.
Rebuild the Android bundle after rebaking. For the published Pokémon variant,
replace the existing ZIP's `assets/nimbo-props` bundle, preserve its Pokémon
manifest and gameplay settings, and advance the package version.

### Building visibility regression check

`BuildDistrictBundle` matches triangle winding to the supplied outward normals;
it must not reverse every triangle. The prop exporter uses a rotation, not a
reflection, so unconditional reversal hid the near walls of small houses.

Copy `CheckDistrictMeshWinding.cs` into the authoring project's `Assets/Editor`.
With graphics enabled, set `OPENBONK_NIMBO_PROPS` to the prop JSON and
`OPENBONK_NIMBO_WINDING_CHECK` to an output directory, then execute
`CheckDistrictMeshWinding.Run`. It renders both sides of the actual access
houses and checks that a near wall occludes a far wall, including reflected
source coordinates.
