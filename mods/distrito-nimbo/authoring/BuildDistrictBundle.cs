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
    [Serializable] private sealed class Creature { public Clip[] clips; }
    [Serializable] private sealed class Clip { public string name; public Prop[] frames; }
    [Serializable] private sealed class Prop
    {
        public string name;
        public Vector3[] points, normals;
        public Color[] colors;
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
        if (!string.IsNullOrEmpty(creaturePath))
        {
            var inputs = Directory.Exists(creaturePath) ? Directory.GetFiles(creaturePath, "*.json") : new[] { creaturePath };
            Array.Sort(inputs, StringComparer.Ordinal);
            foreach (string path in inputs)
            {
                var creature = JsonUtility.FromJson<Creature>(File.ReadAllText(path));
                if (creature?.clips == null || creature.clips.Length < 2 || creature.clips.Length > 3)
                    throw new InvalidDataException($"Missing creature clips in {path}");
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
            foreach (string materialName in new[] { "NimboProp", "NimboArchitecture", "NimboTerrain", "NimboDaySky" })
                if (bundle.LoadAsset<Material>(materialName) == null)
                    throw new InvalidDataException($"Missing {materialName} material");
            foreach (var prop in package.props)
            {
                var loaded = bundle.LoadAsset<GameObject>(prop.name);
                if (loaded == null || loaded.GetComponent<MeshFilter>()?.sharedMesh == null ||
                    (loaded.GetComponent<BoxCollider>() != null) != prop.collide)
                    throw new InvalidDataException($"Incomplete packed prop {prop.name}");
            }
            foreach (string frame in frames)
                if (bundle.LoadAsset<Mesh>(Path.GetFileNameWithoutExtension(frame)) == null)
                    throw new InvalidDataException($"Missing creature frame {frame}");
            if (bundle.LoadAsset<GameObject>("NimboCapsuleBurst")?.GetComponent<ParticleSystem>() == null)
                throw new InvalidDataException("Missing capsule burst");
        }
        finally { bundle.Unload(true); }
        Debug.Log("Distrito Nimbo Android prop bundle PASS");
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
        var material = new Material(shader);
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(material, path);
        assets.Add(path);
        return material;
    }

    private static string SaveMesh(Prop prop)
    {
        int count = prop.points.Length;
        if (count == 0 || count > 65535 || count % 3 != 0 ||
            prop.normals.Length != count || prop.colors.Length != count)
            throw new InvalidDataException($"Invalid source mesh for {prop.name}");
        var mesh = new Mesh { name = prop.name, vertices = prop.points, normals = prop.normals, colors = prop.colors };
        mesh.triangles = OutwardTriangles(prop.points, prop.normals);
        mesh.RecalculateBounds();
        string path = $"{AssetRoot}/{prop.name}.asset";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mesh, path);
        return path;
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

    private static string SaveProp(Prop prop, Material material)
    {
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(SaveMesh(prop));
        var instance = new GameObject(prop.name);
        try
        {
            instance.isStatic = !prop.name.EndsWith("Capsule", StringComparison.Ordinal);
            instance.AddComponent<MeshFilter>().sharedMesh = mesh;
            instance.AddComponent<MeshRenderer>().sharedMaterial = material;
            if (prop.collide)
            {
                int layer = LayerMask.NameToLayer("Object");
                if (layer < 0) throw new InvalidDataException("OpenBonk Object layer is missing");
                instance.layer = layer;
                var collider = instance.AddComponent<BoxCollider>();
                collider.center = mesh.bounds.center;
                collider.size = mesh.bounds.size;
            }
            string path = $"{AssetRoot}/{prop.name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            return path;
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
    }
}
#endif
