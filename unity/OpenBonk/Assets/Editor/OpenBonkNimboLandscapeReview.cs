#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Graphics-enabled review of the actual packaged map, using Forest's shipped
// tile meshes, the production generator, profiles, textures and frontage kit.
public static class OpenBonkNimboLandscapeReview
{
    [Serializable] private sealed class Manifest { public Map map; }
    [Serializable] private sealed class Map
    {
        public string name, description, icon, bundle;
        public int tileGridSize;
        public float volatility, slopeStrength, flatMapBias, stageDuration;
        public float[] stageDurations;
        public Environment[] stageEnvironments;
    }
    [Serializable] private sealed class Environment
    {
        public int stage;
        public string groundTexture, wallTexture;
        public OpenBonkStreetLayout.Spec streetscape;
    }
    [Serializable] private sealed class Receipt
    {
        public string package, gpu;
        public List<StageRecord> stages = new List<StageRecord>();
    }
    [Serializable] private sealed class StageRecord
    {
        public int stage, seed, tileWidth, stepHeight, flatTiles, ramps, frontages, tallBuildings, grassPatches;
        public Vector3 streetAnchor, parkAnchor;
        public List<string> images = new List<string>();
    }

    public static void Run()
    {
        string packagePath = System.Environment.GetEnvironmentVariable("OPENBONK_NIMBO_LANDSCAPE_PACKAGE");
        string output = System.Environment.GetEnvironmentVariable("OPENBONK_NIMBO_LANDSCAPE_REVIEW");
        Require(File.Exists(packagePath) && !string.IsNullOrEmpty(output),
            "Set OPENBONK_NIMBO_LANDSCAPE_PACKAGE and OPENBONK_NIMBO_LANDSCAPE_REVIEW");
        Directory.CreateDirectory(output);
        // Changing scenes unloads unused assets, including objects held only
        // in managed local variables. Load the Forest fixture after that pass.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var forest = AssetDatabase.LoadAssetAtPath<MapData>("Assets/MonoBehaviour/MapForest.asset");
        Require(forest != null, "Forest map fixture");
        var terrainShader = Shader.Find("OpenBonk/NimboTerrain");
        var propShader = Shader.Find("OpenBonk/NimboProp");
        var skyShader = Shader.Find("OpenBonk/NimboDaySky");
        Require(terrainShader != null && propShader != null && skyShader != null,
            "Import source NimboTerrain/NimboProp/NimboSky shaders into the runtime workspace");
        using var archive = new ZipArchive(File.OpenRead(packagePath), ZipArchiveMode.Read);
        var manifest = JsonUtility.FromJson<Manifest>(System.Text.Encoding.UTF8.GetString(Read(archive, "manifest.json")));
        Require(manifest?.map?.stageEnvironments?.Length == 3, "Three packaged stages");
        var map = manifest.map;
        var bundle = AssetBundle.LoadFromMemory(Read(archive, map.bundle));
        Require(bundle != null, "Packaged Android bundle");
        var owned = new List<Object>();
        OpenBonkProceduralMapBuild build = null;
        ProceduralTileGeneration generator = null;
        try
        {
            var architecture = new Material(terrainShader); owned.Add(architecture);
            var sky = new Material(skyShader); owned.Add(sky);
            var environments = new OpenBonkProceduralMapRecipe.StageEnvironment[3];
            foreach (var environment in map.stageEnvironments)
            {
                Require(environment.streetscape != null && environment.streetscape.IsValid(), "Packaged street profile");
                var frontages = new List<GameObject>();
                foreach (string name in environment.streetscape.frontages)
                {
                    var prefab = bundle.LoadAsset<GameObject>(name);
                    Require(prefab != null, "Packaged frontage " + name);
                    // Reopened Android programs cannot render on the Editor GPU.
                    // Rebind the same authored source shader; serialized colors
                    // and meshes remain the bytes from the tested package.
                    prefab.GetComponent<MeshRenderer>().sharedMaterial.shader = propShader;
                    frontages.Add(prefab);
                }
                environments[environment.stage-1] = new OpenBonkProceduralMapRecipe.StageEnvironment
                {
                    stage = environment.stage, architectureMaterial = architecture, skyMaterial = sky,
                    groundTexture = Texture(archive, environment.groundTexture, owned),
                    wallTexture = Texture(archive, environment.wallTexture, owned), replaceDecorations = true,
                    streetscape = new OpenBonkStreetLayout.Profile((int)environment.streetscape.tileWidth,
                        (int)environment.streetscape.stepHeight, environment.streetscape.coverage, frontages.ToArray()),
                };
            }
            var recipe = new OpenBonkProceduralMapRecipe
            {
                name = map.name, description = map.description, tileGridSize = map.tileGridSize,
                volatility = map.volatility, slopeStrength = map.slopeStrength, flatMapBias = map.flatMapBias,
                stageDuration = map.stageDuration, stageDurations = map.stageDurations, stageEnvironments = environments,
            };
            var icon = Texture(archive, map.icon, owned);
            Debug.Log($"Nimbo landscape recipe input: forest={(forest != null ? forest.name : "<unloaded>")}, " +
                $"map={(forest != null ? forest.eMap.ToString() : "-")}, type={(forest != null ? forest.mapType.ToString() : "-")}, " +
                $"stages={(forest != null ? forest.stages?.Length : -1)}, name={recipe.name}, descriptionLength={recipe.description?.Length}, " +
                $"grid={recipe.tileGridSize}, volatility={recipe.volatility}, slopeStrength={recipe.slopeStrength}, " +
                $"flatMapBias={recipe.flatMapBias}, duration={recipe.stageDuration}, stageDurations={string.Join(",", recipe.stageDurations ?? Array.Empty<float>())}, " +
                $"icon={icon.width}x{icon.height}");
            build = OpenBonkProceduralMapBuild.Create(forest, recipe, icon);
            var owner = new GameObject("Nimbo landscape production generator"); owned.Add(owner);
            generator = owner.AddComponent<ProceduralTileGeneration>();
            generator.flatTile = Tile("TileGround"); generator.slopeTile = Tile("TileSlope");
            generator.ceilingTile = Tile("TileCeiling"); generator.stairsMesh = Tile("StairMesh");
            generator.wallFlat = Tile("TileWall"); generator.wallLeftUp = Tile("TileWallSlopeLeftUp");
            generator.wallLeftDown = Tile("TileWallSlopeLeftDown"); generator.wallLeftCross = Tile("TileWallSlopeLeftDownCross");
            var sun = new GameObject("Nimbo review sunlight").AddComponent<Light>(); owned.Add(sun.gameObject);
            sun.type = LightType.Directional; sun.color = new Color(1,.96f,.88f); sun.intensity = 1.1f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-.4f,-1,-.65f));
            sun.shadows = LightShadows.Soft; sun.shadowBias = .025f; sun.shadowNormalBias = .12f;
            RenderSettings.sun = sun; RenderSettings.skybox = sky; RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.66f,.72f,.82f);
            RenderSettings.ambientEquatorColor = new Color(.48f,.52f,.57f);
            RenderSettings.ambientGroundColor = new Color(.32f,.30f,.27f);
            var ambient = new SphericalHarmonicsL2(); ambient.AddAmbientLight(new Color(.57f,.61f,.68f));
            RenderSettings.ambientProbe = ambient;
            QualitySettings.shadows = UnityEngine.ShadowQuality.All; QualitySettings.shadowResolution = UnityEngine.ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 300; QualitySettings.shadowCascades = 4; QualitySettings.antiAliasing = 0;
            var camera = new GameObject("Nimbo review camera").AddComponent<Camera>(); owned.Add(camera.gameObject);
            camera.clearFlags = CameraClearFlags.Skybox; camera.allowHDR = false; camera.allowMSAA = false;
            camera.nearClipPlane = .2f; camera.farClipPlane = 1800; camera.fieldOfView = 68;
            camera.renderingPath = RenderingPath.Forward;
            var receipt = new Receipt { package = packagePath, gpu = SystemInfo.graphicsDeviceName };
            for (int stageIndex = 0; stageIndex < 2; stageIndex++)
            {
                var stage = build.Map.stages[stageIndex]; var profile = environments[stageIndex].streetscape;
                generator.debugSeed = 42;
                generator.Generate(out _, stage, stage.mapParameters, true, streetscape: profile);
                OpenBonkStreetLayout.Generate(generator, profile);
                Physics.SyncTransforms();
                var plan = OpenBonkStreetLayout.Plan(generator.flatTiles, profile, 42, generator.collider.GetComponent<MeshCollider>());
                var record = new StageRecord
                {
                    stage = stageIndex+1, seed = 42, tileWidth = profile.tileWidth, stepHeight = profile.stepHeight,
                    flatTiles = generator.flatTiles.Count, ramps = generator.tiles.Count-generator.flatTiles.Count, frontages = plan.Count,
                };
                OpenBonkStreetLayout.Placement? street = null;
                float streetScore = float.PositiveInfinity, parkScore = float.PositiveInfinity;
                foreach (var placement in plan)
                {
                    Vector3 center = generator.flatTiles[placement.tileIndex].transform.position;
                    if (placement.worldBounds.size.y > 8)
                    {
                        record.tallBuildings++;
                        float score = new Vector2(center.x,center.z).sqrMagnitude;
                        if (score < streetScore) { street = placement; streetScore = score; record.streetAnchor = center; }
                    }
                    if (placement.worldBounds.size.y <= 1.2f)
                    {
                        record.grassPatches++;
                        float green = OpenBonkStreetLayout.GreenDistrictWeight(center, profile.tileWidth);
                        float score = new Vector2(center.x,center.z).sqrMagnitude;
                        if (green > .8f && score < parkScore) { parkScore = score; record.parkAnchor = center; }
                    }
                }
                Require(street.HasValue && !float.IsPositiveInfinity(parkScore), "Actual generated buildings and park pockets");
                Vector3 heading = street.Value.worldBounds.center-record.streetAnchor; heading.y = 0; heading.Normalize();
                Capture(camera, output, record, "street", record.streetAnchor-heading*7+Vector3.up*2.6f,
                    street.Value.worldBounds.center-Vector3.up*(street.Value.worldBounds.size.y*.18f));
                Capture(camera, output, record, "reverse", record.streetAnchor+heading*6+Vector3.up*2.6f,
                    record.streetAnchor-heading*16+Vector3.up*5);
                Capture(camera, output, record, "park", record.parkAnchor+new Vector3(-10,5,-11), record.parkAnchor+Vector3.up*1.2f);
                float extent = profile.tileWidth*map.tileGridSize;
                Capture(camera, output, record, "elevated", new Vector3(extent*.57f,extent*.53f,-extent*.61f), Vector3.zero);
                camera.orthographic = true; camera.orthographicSize = extent*.55f;
                Capture(camera, output, record, "overhead", new Vector3(0,extent*1.25f,0), Vector3.zero);
                camera.orthographic = false;
                receipt.stages.Add(record);
                ReleaseCollision(generator);
            }
            File.WriteAllText(Path.Combine(output,"landscape-review.json"), JsonUtility.ToJson(receipt,true));
            Debug.Log("Nimbo landscape review PASS: actual packaged stage1/2, seed42, five views each including streets/parks/elevations");
        }
        finally
        {
            if (generator != null) { ReleaseCollision(generator); if (generator.tilesParent != null) Object.DestroyImmediate(generator.tilesParent); }
            build?.Dispose();
            foreach (var item in owned) if (item != null) Object.DestroyImmediate(item);
            bundle.Unload(true);
        }
    }

    private static void Capture(Camera camera, string output, StageRecord record, string view, Vector3 position, Vector3 target)
    {
        camera.transform.position = position;
        camera.transform.rotation = Quaternion.LookRotation(target-position, view == "overhead" ? Vector3.forward : Vector3.up);
        camera.aspect = 1.6f;
        var targetTexture = RenderTexture.GetTemporary(1280,800,24,RenderTextureFormat.ARGB32);
        var pixels = new Texture2D(1280,800,TextureFormat.RGB24,false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = targetTexture; camera.Render(); RenderTexture.active = targetTexture;
            pixels.ReadPixels(new Rect(0,0,1280,800),0,0); pixels.Apply();
            string file = $"stage{record.stage}-{view}.png";
            File.WriteAllBytes(Path.Combine(output,file),pixels.EncodeToPNG()); record.images.Add(file);
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(targetTexture); Object.DestroyImmediate(pixels); }
    }
    private static byte[] Read(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path); Require(entry != null,"Package entry "+path);
        using var source = entry.Open(); using var bytes = new MemoryStream(); source.CopyTo(bytes); return bytes.ToArray();
    }
    private static Texture2D Texture(ZipArchive archive, string path, List<Object> owned)
    {
        var texture = new Texture2D(2,2,TextureFormat.RGBA32,false); owned.Add(texture);
        Require(texture.LoadImage(Read(archive,path)),"Texture "+path); texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Repeat; return texture;
    }
    private static GameObject Tile(string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/"+name+".prefab"); Require(prefab != null,name); return prefab;
    }
    private static void ReleaseCollision(ProceduralTileGeneration generator)
    {
        if (generator.collider == null) return;
        var mesh = generator.collider.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(generator.collider); Object.DestroyImmediate(mesh);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
#endif
