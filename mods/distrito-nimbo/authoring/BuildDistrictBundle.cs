#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildDistrictBundle
{
    private const string AssetRoot = "Assets/NimboAuthoring";
    private const string BundleName = "nimbo-props";

    [Serializable] private sealed class Package { public Prop[] props; }
    [Serializable] private sealed class Creature { public string material, texture; public bool alphaClip, doubleSided; public Clip[] clips; }
    [Serializable] private sealed class Clip { public string name; public Prop[] frames; }
    [Serializable] private sealed class BoxSpec { public Vector3 center, size; }
    [Serializable] private sealed class Prop
    {
        public string name;
        public Vector3[] points, normals;
        public Color[] colors;
        public Vector2[] uv;
        public Vector4[] atlasRects;
        public int[] indices;
        public BoxSpec[] colliders;
        public bool collide;
    }

    public static void Run()
    {
        string input = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_PROPS");
        string output = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_BUNDLE");
        string creaturePath = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_CREATURE");
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            throw new ArgumentException("Set OPENBONK_NIMBO_PROPS and OPENBONK_NIMBO_BUNDLE");
        var package = JsonUtility.FromJson<Package>(File.ReadAllText(input));
        if (package?.props == null || package.props.Length == 0)
            throw new InvalidDataException("The district kit needs prop meshes");

        if (!AssetDatabase.IsValidFolder(AssetRoot)) AssetDatabase.CreateFolder("Assets", "NimboAuthoring");
        foreach (Prop prop in package.props) AssetDatabase.DeleteAsset($"{AssetRoot}/{prop.name}.prefab");
        var packed = new List<string>();
        var material = SaveMaterial("NimboProp", "OpenBonk/NimboProp", packed);
        var capsuleMaterial = SaveMaterial("NimboCapsule", "OpenBonk/NimboProp", packed);
        capsuleMaterial.SetFloat("_Glaze", .72f);
        EditorUtility.SetDirty(capsuleMaterial);
        SaveMaterial("NimboArchitecture", "OpenBonk/NimboArchitecture", packed);
        SaveMaterial("NimboTerrain", "OpenBonk/NimboTerrain", packed);
        SaveMaterial("NimboWarning", "OpenBonk/NimboWarning", packed);
        SaveMaterial("NimboDaySky", "OpenBonk/NimboDaySky", packed);
        foreach (var prop in package.props)
            packed.Add(SaveProp(prop, prop.name.EndsWith("Capsule", StringComparison.Ordinal) ? capsuleMaterial : material));
        packed.Add(SaveCapsuleEffect(material));
        var frames = new List<string>();
        var creatures = new List<Creature>();
        if (!string.IsNullOrEmpty(creaturePath))
        {
            var inputs = Directory.Exists(creaturePath) ? Directory.GetFiles(creaturePath, "*.json") : new[] { creaturePath };
            Array.Sort(inputs, StringComparer.Ordinal);
            foreach (string path in inputs)
            {
                var creature = JsonUtility.FromJson<Creature>(File.ReadAllText(path));
                if (creature?.clips == null || creature.clips.Length < 2 || creature.clips.Length > 3)
                    throw new InvalidDataException($"Missing creature clips in {path}");
                if (!string.IsNullOrEmpty(creature.material)) { SaveCreatureMaterial(creature, path, packed); creatures.Add(creature); }
                foreach (var clip in creature.clips)
                {
                    if (clip.frames == null || clip.frames.Length != 12) throw new InvalidDataException($"Missing {clip.name} frames");
                    foreach (var frame in clip.frames)
                    {
                        string asset = SaveMesh(frame);
                        frames.Add(asset);
                        packed.Add(asset);
                    }
                }
            }
        }
        AssetDatabase.SaveAssets();
        RequireDrawModes(name => AssetDatabase.LoadAssetAtPath<Material>($"{AssetRoot}/{name}.mat"), creatures);
        Directory.CreateDirectory(output);
        var build = new AssetBundleBuild { assetBundleName = BundleName, assetNames = packed.ToArray() };
        var result = BuildPipeline.BuildAssetBundles(output, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.Android);
        if (result == null || !File.Exists(Path.Combine(output, BundleName)))
            throw new IOException("Android prop bundle build failed");

        var bundle = AssetBundle.LoadFromFile(Path.Combine(output, BundleName));
        if (bundle == null) throw new IOException("Could not reopen Android prop bundle");
        try
        {
            // Check serialized flags before replacing unsupported Android shaders
            // with their Editor equivalents for the later property inspection.
            RequireDrawModes(name => bundle.LoadAsset<Material>(name), creatures);
            foreach (string materialName in new[] { "NimboProp", "NimboArchitecture", "NimboTerrain", "NimboDaySky" })
                if (bundle.LoadAsset<Material>(materialName) == null)
                    throw new InvalidDataException($"Missing {materialName} material");
            foreach (var prop in package.props)
            {
                var loaded = bundle.LoadAsset<GameObject>(prop.name);
                if (loaded == null || loaded.GetComponent<MeshFilter>()?.sharedMesh == null ||
                    loaded.GetComponents<BoxCollider>().Length != (prop.colliders != null ? prop.colliders.Length : prop.collide ? 1 : 0))
                    throw new InvalidDataException($"Incomplete packed prop {prop.name}");
                RequireInstancing(loaded.GetComponent<MeshRenderer>()?.sharedMaterial, false, prop.name);
            }
            foreach (var creature in creatures)
            {
                var loaded = bundle.LoadAsset<Material>(creature.material);
                // Android shader programs cannot run in the desktop Editor.
                // Rebind to the same source shader to inspect serialized material properties.
                if (loaded != null) loaded.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/NimboCreature.shader");
                if (loaded == null || !(loaded.mainTexture is Texture2D atlas) || atlas.width < 128 || atlas.height < 128 ||
                    loaded.IsKeywordEnabled("_NIMBO_ALPHA_CLIP") != creature.alphaClip ||
                    loaded.GetFloat("_Cull") != (creature.doubleSided ? 0 : 2))
                    throw new InvalidDataException("Incomplete creature material " + creature.material);
                Debug.Log($"Packed {creature.material}: {atlas.width}x{atlas.height} {atlas.format}, {atlas.mipmapCount} mips");
            }
            foreach (string frame in frames)
            {
                var loaded = bundle.LoadAsset<Mesh>(Path.GetFileNameWithoutExtension(frame));
                var regions = new List<Vector4>(); if (loaded != null) loaded.GetUVs(1, regions);
                if (loaded == null || (creatures.Count > 0 && (loaded.uv.Length != loaded.vertexCount || regions.Count != loaded.vertexCount)))
                    throw new InvalidDataException($"Missing creature frame/UVs {frame}");
            }
            if (bundle.LoadAsset<GameObject>("NimboCapsuleBurst")?.GetComponent<ParticleSystem>() == null)
                throw new InvalidDataException("Missing capsule burst");
        }
        finally { bundle.Unload(true); }
        foreach (var path in packed)
        {
            var checkedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (checkedMaterial != null && ShaderUtil.ShaderHasError(checkedMaterial.shader))
                throw new InvalidDataException("Shader compilation failed: " + checkedMaterial.shader.name);
        }
        Debug.Log("Distrito Nimbo Android prop bundle PASS");
    }

    private static void RequireDrawModes(Func<string, Material> load, List<Creature> creatures)
    {
        RequireInstancing(load("NimboProp"), false, "NimboProp");
        RequireInstancing(load("NimboCapsule"), false, "NimboCapsule");
        RequireInstancing(load("NimboTerrain"), true, "NimboTerrain");
        foreach (var creature in creatures)
            RequireInstancing(load(creature.material), false, creature.material);
    }

    private static void RequireInstancing(Material material, bool expected, string name)
    {
        if (material == null || material.enableInstancing != expected)
            throw new InvalidDataException($"Incorrect GPU instancing mode for {name}: expected {expected}");
    }

    private static string SaveCapsuleEffect(Material material)
    {
        const string name = "NimboCapsuleBurst";
        string meshPath = $"{AssetRoot}/{name}.asset";
        AssetDatabase.DeleteAsset(meshPath);
        var mesh = new Mesh { name = name };
        mesh.vertices = new[]
        {
            new Vector3(-.6f, -.34f, 0), new Vector3(.20f, .59f, 0), new Vector3(.56f, -.1f, 0),
            new Vector3(-.6f, -.34f, .01f), new Vector3(.56f, -.1f, .01f), new Vector3(.20f, .59f, .01f),
        };
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white, Color.white, Color.white };
        mesh.triangles = new[] { 0, 1, 2, 3, 4, 5 };
        mesh.RecalculateNormals();
        AssetDatabase.CreateAsset(mesh, meshPath);
        var root = new GameObject(name);
        try
        {
            var particles = root.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = .6f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.32f, .57f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.7f, 5.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.12f, .26f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(.87f, .04f, .06f), new Color(1, 1, .94f));
            main.gravityModifier = .85f;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = particles.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, 18) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .22f;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = mesh;
            renderer.sharedMaterial = material;
            string path = $"{AssetRoot}/{name}.prefab";
            AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return path;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static Material SaveMaterial(string name, string shaderName, List<string> assets)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) throw new InvalidDataException($"Import the shader {shaderName} before building");
        string path = $"{AssetRoot}/{name}.mat";
        var material = new Material(shader)
        {
            // Keep repeated prop/creature draws independent on GLES devices;
            // terrain retains its bounded instancing path.
            enableInstancing = shaderName != "OpenBonk/NimboProp" && shaderName != "OpenBonk/NimboCreature",
        };
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(material, path);
        assets.Add(path);
        return material;
    }

    private static string SaveMesh(Prop prop)
    {
        int count = prop.points.Length;
        bool indexed = prop.indices != null;
        if (count == 0 || count > 65535 || (!indexed && count % 3 != 0) ||
            prop.normals.Length != count || (prop.colors != null && prop.colors.Length != count) ||
            (prop.uv != null && prop.uv.Length != count) ||
            (prop.atlasRects != null && prop.atlasRects.Length != count) ||
            (indexed && (prop.uv == null || prop.indices.Length == 0 || prop.indices.Length % 3 != 0)))
            throw new InvalidDataException($"Invalid source mesh for {prop.name}");
        var mesh = new Mesh { name = prop.name, vertices = prop.points, normals = prop.normals };
        if (prop.colors != null) mesh.colors = prop.colors;
        if (prop.uv != null) mesh.uv = prop.uv;
        if (prop.atlasRects != null) mesh.SetUVs(1, new List<Vector4>(prop.atlasRects));
        mesh.triangles = indexed ? IndexedOutwardTriangles(prop.points, prop.normals, prop.indices) : OutwardTriangles(prop.points, prop.normals);
        mesh.RecalculateBounds();
        string path = $"{AssetRoot}/{prop.name}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return path;
    }

    private static int[] IndexedOutwardTriangles(Vector3[] points, Vector3[] normals, int[] input)
    {
        var indices = (int[])input.Clone();
        for (int i = 0; i < indices.Length; i += 3)
        {
            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
            if (a < 0 || b < 0 || c < 0 || a >= points.Length || b >= points.Length || c >= points.Length)
                throw new InvalidDataException("Creature triangle index out of range");
            if (Vector3.Dot(Vector3.Cross(points[b] - points[a], points[c] - points[a]), normals[a] + normals[b] + normals[c]) < 0)
            { indices[i + 1] = c; indices[i + 2] = b; }
        }
        return indices;
    }

    private static void SaveCreatureMaterial(Creature creature, string sourcePath, List<string> packed)
    {
        if (string.IsNullOrEmpty(creature.texture) || Path.GetFileName(creature.texture) != creature.texture ||
            Path.GetFileName(creature.material) != creature.material)
            throw new InvalidDataException("Invalid creature texture/material name");
        string texturePath = $"{AssetRoot}/{creature.texture}";
        File.Copy(Path.Combine(Path.GetDirectoryName(sourcePath), creature.texture), texturePath, true);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = creature.alphaClip ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        importer.alphaIsTransparency = creature.alphaClip;
        importer.mipmapEnabled = true;
        importer.mipMapsPreserveCoverage = creature.alphaClip;
        importer.alphaTestReferenceValue = .5f;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 1024;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
        {
            name = "Android", overridden = true, maxTextureSize = 1024,
            format = creature.alphaClip ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4,
            compressionQuality = 100,
        });
        importer.SaveAndReimport();
        var material = SaveMaterial(creature.material, "OpenBonk/NimboCreature", packed);
        material.SetFloat("_Cull", creature.doubleSided ? 0 : 2);
        if (creature.alphaClip)
        {
            material.SetFloat("_AlphaClip", 1);
            material.EnableKeyword("_NIMBO_ALPHA_CLIP");
        }
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (material.mainTexture == null) throw new InvalidDataException("Missing creature texture " + texturePath);
        material.enableInstancing = false;
        EditorUtility.SetDirty(material);
        packed.Add(texturePath);
    }

    internal static int[] OutwardTriangles(Vector3[] points, Vector3[] normals)
    {
        var indices = new int[points.Length];
        for (int i = 0; i < points.Length; i += 3)
        {
            // The prop exporter rotates (x, y, z) to (x, z, -y), while other
            // sources can reflect an axis. Match the baked outward normals
            // instead of reversing every source as if all changed handedness.
            var face = Vector3.Cross(points[i + 1] - points[i], points[i + 2] - points[i]);
            bool reverse = Vector3.Dot(face, normals[i] + normals[i + 1] + normals[i + 2]) < 0;
            indices[i] = i;
            indices[i + 1] = i + (reverse ? 2 : 1);
            indices[i + 2] = i + (reverse ? 1 : 2);
        }
        return indices;
    }

    private static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);

    private static string SaveProp(Prop prop, Material material)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SaveMesh(prop));
        var instance = new GameObject(prop.name);
        try
        {
            instance.isStatic = !prop.name.EndsWith("Capsule", StringComparison.Ordinal);
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            instance.AddComponent<MeshRenderer>().sharedMaterial = material;
            var boxes = prop.colliders ?? (prop.collide
                ? new[] { new BoxSpec { center = mesh.bounds.center, size = mesh.bounds.size } }
                : Array.Empty<BoxSpec>());
            if (boxes.Length > 16) throw new InvalidDataException("Too many solid boxes for " + prop.name);
            if (boxes.Length > 0)
            {
                int layer = LayerMask.NameToLayer("Object");
                if (layer < 0) throw new InvalidDataException("OpenBonk Object layer is missing");
                instance.layer = layer;
                var envelope = mesh.bounds; envelope.Expand(.25f);
                foreach (var box in boxes)
                {
                    if (box == null || !Finite(box.center) || !Finite(box.size) ||
                        box.size.x <= 0 || box.size.y <= 0 || box.size.z <= 0 ||
                        !envelope.Contains(box.center - box.size * .5f) || !envelope.Contains(box.center + box.size * .5f))
                        throw new InvalidDataException("Invalid solid box for " + prop.name);
                    var collider = instance.AddComponent<BoxCollider>();
                    collider.center = box.center;
                    collider.size = box.size;
                }
            }
            string path = $"{AssetRoot}/{prop.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            return path;
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
#endif
