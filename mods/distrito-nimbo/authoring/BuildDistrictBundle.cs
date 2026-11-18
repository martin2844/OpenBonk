#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildDistrictBundle
{
    private const string AssetRoot = "Assets/NimboAuthoring";
    private const string BundleName = "nimbo-props";

    [Serializable] private sealed class Package { public Prop[] props; }
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
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(output))
            throw new ArgumentException("Set OPENBONK_NIMBO_PROPS and OPENBONK_NIMBO_BUNDLE");
        var package = JsonUtility.FromJson<Package>(File.ReadAllText(input));
        if (package?.props == null || package.props.Length != 5)
            throw new InvalidDataException("Expected three roof props and two capsule variants");

        if (!AssetDatabase.IsValidFolder(AssetRoot)) AssetDatabase.CreateFolder("Assets", "NimboAuthoring");
        foreach (Prop prop in package.props) AssetDatabase.DeleteAsset($"{AssetRoot}/{prop.name}.prefab");
        foreach (Prop prop in package.props) AssetDatabase.DeleteAsset($"{AssetRoot}/{prop.name}.asset");
        AssetDatabase.DeleteAsset($"{AssetRoot}/NimboProp.mat");

        var shader = Shader.Find("OpenBonk/NimboProp");
        if (shader == null) throw new InvalidDataException("Import NimboProp.shader before building");
        var material = new Material(shader);
        AssetDatabase.CreateAsset(material, $"{AssetRoot}/NimboProp.mat");

        var prefabs = new string[package.props.Length];
        for (int i = 0; i < package.props.Length; i++) prefabs[i] = SaveProp(package.props[i], material);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(output);
        var build = new AssetBundleBuild { assetBundleName = BundleName, assetNames = prefabs };
        var result = BuildPipeline.BuildAssetBundles(output, new[] { build },
            BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.Android);
        if (result == null || !File.Exists(Path.Combine(output, BundleName)))
            throw new IOException("Android prop bundle build failed");

        var bundle = AssetBundle.LoadFromFile(Path.Combine(output, BundleName));
        if (bundle == null) throw new IOException("Could not reopen Android prop bundle");
        try
        {
            foreach (var prop in package.props)
            {
                var loaded = bundle.LoadAsset<GameObject>(prop.name);
                if (loaded == null || loaded.GetComponent<MeshFilter>()?.sharedMesh == null ||
                    (loaded.GetComponent<BoxCollider>() != null) != prop.collide)
                    throw new InvalidDataException($"Incomplete packed prop {prop.name}");
            }
        }
        finally { bundle.Unload(true); }
        Debug.Log("Distrito Nimbo Android prop bundle PASS");
    }

    private static string SaveProp(Prop prop, Material material)
    {
        int count = prop.points.Length;
        if (count == 0 || count > 65535 || count % 3 != 0 ||
            prop.normals.Length != count || prop.colors.Length != count)
            throw new InvalidDataException($"Invalid source mesh for {prop.name}");
        var mesh = new Mesh { name = prop.name, vertices = prop.points, normals = prop.normals, colors = prop.colors };
        var indices = new int[count];
        // The Blender bake uses counterclockwise front faces. Unity uses clockwise.
        for (int i = 0; i < count; i += 3)
        {
            indices[i] = i;
            indices[i + 1] = i + 2;
            indices[i + 2] = i + 1;
        }
        mesh.triangles = indices;
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, $"{AssetRoot}/{prop.name}.asset");

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
