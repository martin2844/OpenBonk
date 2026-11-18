# Distrito Nimbo

Development package for an original rooftop district in OpenBonk. It supplies
three stage palettes, facade and roof textures, an antenna, a heat pump, a
basalt-head sculpture, and a map selection icon. The maze and run progression
use the tiled map system.

## Build the package

1. Open `authoring/distrito-nimbo.blend` in Blender and bake its props with
   `blender -b authoring/distrito-nimbo.blend --python authoring/export_props.py -- /tmp/nimbo-props.json`
   from this directory.
2. In an Android-capable Unity project using the same Unity version and layer
   settings as OpenBonk, put `BuildDistrictBundle.cs` in `Assets/Editor` and
   import `NimboProp.shader`. Use a project dedicated to authoring. The builder
   regenerates assets with the same names in `Assets/NimboAuthoring`.
3. Set `OPENBONK_NIMBO_PROPS` to the JSON path and `OPENBONK_NIMBO_BUNDLE` to an
   output directory. Run Unity in batch mode with `-buildTarget Android` and
   `-executeMethod BuildDistrictBundle.Run`.
4. Run `python3 build.py /tmp/distrito-nimbo.zip --props /path/to/nimbo-props`.

Import the ZIP through **Mods** in a development APK with the per-stage map
environment feature, enable the package, and select **Distrito Nimbo** in
the game's map menu. The 0.1.13 release does not apply the new stage settings.

This package is for visual and map-loader checks. Dedicated rooftop geometry,
creatures, bosses and breakable capsule drops are pending integration.
