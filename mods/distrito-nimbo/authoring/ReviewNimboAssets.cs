#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Graphics-enabled authoring-project QA. Uses saved meshes/materials directly;
// optionally set OPENBONK_NIMBO_REVIEW_BUNDLE to inspect a reopened bundle.
public static class ReviewNimboAssets
{
    private const string AssetRoot = "Assets/NimboAuthoring/";
    private const int Layer = 30, Tile = 384;
    private static readonly string[] Species = { "Beedrill", "Pidgey", "Rattata", "Raticate", "Fearow", "Arbok", "Nidoking", "Charizard", "Rhydon", "Gyarados" };
    private static readonly string[] Props = { "NimboVendingCorner", "NimboGachaCorner", "NimboRamenFront", "NimboLanternGate", "NimboPosterGarden", "NimboShrineCorner", "MewtwoStatue" };
    private static readonly int[] Poses = { 0, 3, 6, 9 };
    [Serializable] private sealed class Receipt { public string source, graphicsDevice; public List<Record> assets = new List<Record>(); }
    [Serializable] private sealed class Record
    {
        public string name, shader, atlas, contactSheet;
        public int vertices, triangles, atlasWidth, atlasHeight, colliderCount;
        public Vector3 minimum, maximum;
    }

    public static void Run()
    {
        string output = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_REVIEW");
        if (string.IsNullOrEmpty(output)) throw new ArgumentException("Set OPENBONK_NIMBO_REVIEW to the screenshot directory");
        Directory.CreateDirectory(output);
        string only = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_REVIEW_ONLY");
        string bundlePath = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_REVIEW_BUNDLE");
        AssetBundle bundle = null;
        if (!string.IsNullOrEmpty(bundlePath))
        {
            bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null) throw new IOException("Could not reopen " + bundlePath);
        }
        // This entry point runs in the dedicated batch authoring project.
        // Unity cannot add a scene beside its unsaved startup scene.
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var receipt = new Receipt { source = bundle == null ? AssetRoot : bundlePath, graphicsDevice = SystemInfo.graphicsDeviceName };
        var oldShadows = QualitySettings.shadows;
        var oldResolution = QualitySettings.shadowResolution;
        float oldDistance = QualitySettings.shadowDistance;
        int oldCascades = QualitySettings.shadowCascades, oldAA = QualitySettings.antiAliasing;
        Material groundMaterial = null;
        try
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
            QualitySettings.shadowDistance = 100;
            QualitySettings.shadowCascades = 4;
            QualitySettings.antiAliasing = 4;
            RenderSettings.fog = false;
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.66f, .72f, .82f);
            RenderSettings.ambientEquatorColor = new Color(.48f, .52f, .57f);
            RenderSettings.ambientGroundColor = new Color(.32f, .30f, .27f);
            RenderSettings.ambientIntensity = 1;
            var ambient = new SphericalHarmonicsL2();
            ambient.AddAmbientLight(new Color(.57f, .61f, .68f));
            RenderSettings.ambientProbe = ambient;
            var light = new GameObject("Review daylight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1, .96f, .88f);
            light.intensity = 1.15f;
            light.transform.rotation = Quaternion.LookRotation(new Vector3(-.4f, -1, -.65f));
            light.cullingMask = 1 << Layer;
            light.shadows = LightShadows.Soft;
            light.shadowBias = .025f;
            light.shadowNormalBias = .12f;
            RenderSettings.sun = light;
            var camera = new GameObject("Review camera").AddComponent<Camera>();
            camera.cullingMask = 1 << Layer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.70f, .80f, .88f);
            camera.orthographic = true;
            camera.aspect = 1;
            camera.renderingPath = RenderingPath.Forward;
            camera.allowHDR = false;
            camera.nearClipPlane = .01f;
            camera.farClipPlane = 1000;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Review ground";
            ground.layer = Layer;
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            groundMaterial = new Material(Shader.Find("Standard"));
            groundMaterial.color = new Color(.55f, .59f, .50f);
            groundMaterial.SetFloat("_Glossiness", .05f);
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            foreach (string species in Species)
            {
                if (!string.IsNullOrEmpty(only) && only != species) continue;
                var material = Load<Material>(bundle, "Nimbo" + species, ".mat");
                CheckMaterial(material);
                if (material.mainTexture == null) throw new InvalidDataException(species + " has no atlas");
                var meshes = new List<Mesh>();
                Bounds bounds = default;
                foreach (int pose in Poses)
                {
                    var mesh = Load<Mesh>(bundle, species + "Walk" + pose.ToString("00"), ".asset");
                    var regions = new List<Vector4>(); mesh.GetUVs(1, regions);
                    if (mesh.uv.Length != mesh.vertexCount || regions.Count != mesh.vertexCount)
                        throw new InvalidDataException(mesh.name + " lost UV or atlas-region data");
                    if (meshes.Count == 0) bounds = mesh.bounds; else bounds.Encapsulate(mesh.bounds);
                    meshes.Add(mesh);
                }
                var model = new GameObject(species) { layer = Layer };
                var filter = model.AddComponent<MeshFilter>();
                var renderer = model.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.On; renderer.receiveShadows = true;
                var sheet = new Texture2D(Tile * 4, Tile * 2, TextureFormat.RGB24, false);
                try
                {
                    for (int view = 0; view < 2; view++)
                    {
                        string viewName = view == 0 ? "front" : "side";
                        Frame(camera, ground, bounds, view == 0 ? new Vector3(.18f, .30f, 1) : new Vector3(1, .30f, 0));
                        for (int p = 0; p < meshes.Count; p++)
                        {
                            filter.sharedMesh = meshes[p];
                            var tile = Capture(camera, Path.Combine(output, meshes[p].name + "-" + viewName + ".png"));
                            sheet.SetPixels(p * Tile, (1 - view) * Tile, Tile, Tile, tile.GetPixels());
                            Object.DestroyImmediate(tile);
                        }
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(output, species + "-poses.png"), sheet.EncodeToPNG());
                    receipt.assets.Add(new Record { name = species, shader = material.shader.name,
                        atlas = material.mainTexture.name, atlasWidth = material.mainTexture.width, atlasHeight = material.mainTexture.height,
                        vertices = meshes[0].vertexCount, triangles = meshes[0].triangles.Length / 3,
                        minimum = bounds.min, maximum = bounds.max, contactSheet = species + "-poses.png" });
                }
                finally { Object.DestroyImmediate(model); Object.DestroyImmediate(sheet); }
            }

            foreach (string name in Props)
            {
                if (!string.IsNullOrEmpty(only) && only != name) continue;
                var model = Object.Instantiate(Load<GameObject>(bundle, name, ".prefab"));
                var renderers = model.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0) throw new InvalidDataException(name + " has no renderers");
                Bounds bounds = renderers[0].bounds;
                foreach (var transform in model.GetComponentsInChildren<Transform>()) transform.gameObject.layer = Layer;
                foreach (var renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                    foreach (var material in renderer.sharedMaterials) CheckMaterial(material);
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }
                var sheet = new Texture2D(Tile * 2, Tile * 2, TextureFormat.RGB24, false);
                try
                {
                    var directions = new[] { new Vector3(.18f,.35f,1), new Vector3(1,.40f,.55f), new Vector3(1,.35f,0), new Vector3(-.55f,.4f,-1) };
                    for (int view = 0; view < directions.Length; view++)
                    {
                        Frame(camera, ground, bounds, directions[view]);
                        var tile = Capture(camera, Path.Combine(output, name + "-view" + view + ".png"));
                        sheet.SetPixels((view % 2) * Tile, (1 - view / 2) * Tile, Tile, Tile, tile.GetPixels());
                        Object.DestroyImmediate(tile);
                    }
                    sheet.Apply();
                    File.WriteAllBytes(Path.Combine(output, name + "-views.png"), sheet.EncodeToPNG());
                    int vertices = 0, triangles = 0;
                    foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                    { vertices += filter.sharedMesh.vertexCount; triangles += filter.sharedMesh.triangles.Length / 3; }
                    receipt.assets.Add(new Record { name = name, shader = renderers[0].sharedMaterial.shader.name,
                        vertices = vertices, triangles = triangles, colliderCount = model.GetComponentsInChildren<Collider>().Length,
                        minimum = bounds.min, maximum = bounds.max, contactSheet = name + "-views.png" });
                }
                finally { Object.DestroyImmediate(model); Object.DestroyImmediate(sheet); }
            }
            File.WriteAllText(Path.Combine(output, "review.json"), JsonUtility.ToJson(receipt, true));
            var html = new StringBuilder("<!doctype html><meta charset=utf-8><title>Nimbo asset review</title><style>body{font:16px system-ui;background:#edf0ef;color:#172522;margin:32px}img{max-width:100%;height:auto}figure{max-width:1536px;margin:32px 0}figcaption{margin:10px 0}</style><h1>Nimbo: actual Unity assets</h1><p>Species columns: walk 00,03,06,09. Top row: front; bottom: side. Each species keeps a fixed camera across poses. Prop views: front, quarter, side, rear. Daylight/shadows use the saved materials. This is presentation QA, not a device performance test.</p>");
            foreach (var record in receipt.assets)
                html.Append("<figure><figcaption>").Append(record.name).Append(" — ").Append(record.triangles).Append(" triangles</figcaption><img src=\"").Append(record.contactSheet).Append("\"></figure>");
            File.WriteAllText(Path.Combine(output, "index.html"), html.ToString());
            Debug.Log($"Nimbo asset review PASS: {receipt.assets.Count} reviewed assets; {output}");
        }
        finally
        {
            QualitySettings.shadows = oldShadows; QualitySettings.shadowResolution = oldResolution;
            QualitySettings.shadowDistance = oldDistance; QualitySettings.shadowCascades = oldCascades; QualitySettings.antiAliasing = oldAA;
            if (groundMaterial != null) Object.DestroyImmediate(groundMaterial);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (bundle != null) bundle.Unload(true);
        }
    }

    private static T Load<T>(AssetBundle bundle, string name, string extension) where T : Object
    {
        var asset = bundle == null ? AssetDatabase.LoadAssetAtPath<T>(AssetRoot + name + extension) : bundle.LoadAsset<T>(name);
        if (asset == null) throw new InvalidDataException("Missing review asset " + name);
        return asset;
    }

    private static void CheckMaterial(Material material)
    {
        if (material == null || material.shader == null || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
            throw new InvalidDataException("Missing or unsupported review shader for " + (material == null ? "null material" : material.name));
    }

    private static void Frame(Camera camera, GameObject ground, Bounds bounds, Vector3 direction)
    {
        camera.transform.position = bounds.center + direction.normalized * (bounds.size.magnitude * 4 + 10);
        camera.transform.LookAt(bounds.center);
        float halfWidth = 0, halfHeight = 0;
        for (int x = -1; x <= 1; x += 2)
        for (int y = -1; y <= 1; y += 2)
        for (int z = -1; z <= 1; z += 2)
        {
            var corner = Vector3.Scale(bounds.extents, new Vector3(x,y,z));
            halfWidth = Mathf.Max(halfWidth, Mathf.Abs(Vector3.Dot(camera.transform.right, corner)));
            halfHeight = Mathf.Max(halfHeight, Mathf.Abs(Vector3.Dot(camera.transform.up, corner)));
        }
        camera.orthographicSize = Mathf.Max(.25f, Mathf.Max(halfWidth, halfHeight) * 1.18f);
        ground.transform.position = new Vector3(bounds.center.x, Mathf.Min(0, bounds.min.y) - .015f, bounds.center.z);
        ground.transform.localScale = Vector3.one * Mathf.Max(10, bounds.size.magnitude * 3);
    }

    private static Texture2D Capture(Camera camera, string path)
    {
        var target = RenderTexture.GetTemporary(Tile, Tile, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default, 4);
        var previous = RenderTexture.active;
        var image = new Texture2D(Tile, Tile, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0,0,Tile,Tile),0,0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            return image;
        }
        catch { Object.DestroyImmediate(image); throw; }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
#endif
