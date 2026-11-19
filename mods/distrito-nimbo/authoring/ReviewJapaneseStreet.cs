#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

// Imports the exact baked study geometry and reviews it with the shipped shader.
// Separate Assets/NimboJapanStudy namespace: does not overwrite production assets.
public static class ReviewJapaneseStreet
{
    [Serializable] private class Package { public Prop[] props; }
    [Serializable] private class Prop { public string name; public Vector3[] points, normals; public Color[] colors; }
    private const string Root = "Assets/NimboJapanStudy";
    public static void Run()
    {
        var input = Environment.GetEnvironmentVariable("OPENBONK_JAPAN_STUDY");
        if (string.IsNullOrEmpty(input)) throw new ArgumentException("Set OPENBONK_JAPAN_STUDY");
        string output = Path.Combine(input, "unity-review"); Directory.CreateDirectory(output);
        if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets", "NimboJapanStudy");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var shader = Shader.Find("OpenBonk/NimboProp");
        if (shader == null) throw new Exception("NimboProp shader missing");
        var material = new Material(shader);
        string materialPath = Root + "/Study.mat";
        AssetDatabase.DeleteAsset(materialPath); AssetDatabase.CreateAsset(material, materialPath);
        var package = JsonUtility.FromJson<Package>(File.ReadAllText(Path.Combine(input, "japanese-block.json")));
        var parent = new GameObject("Japanese street study");
        int triangles = 0;
        foreach (var prop in package.props)
        {
            if (prop.points.Length == 0 || prop.points.Length % 3 != 0 || prop.normals.Length != prop.points.Length || prop.colors.Length != prop.points.Length)
                throw new Exception("Invalid geometry " + prop.name);
            foreach (var p in prop.points) if (!Finite(p.x) || !Finite(p.y) || !Finite(p.z)) throw new Exception("Nonfinite vertex");
            var mesh = new Mesh { name = prop.name, indexFormat = IndexFormat.UInt32, vertices = prop.points, normals = prop.normals, colors = prop.colors };
            mesh.triangles = BuildDistrictBundle.OutwardTriangles(prop.points, prop.normals); mesh.RecalculateBounds();
            string path = Root + "/" + prop.name + ".asset";
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
            var obj = new GameObject(prop.name); obj.transform.SetParent(parent.transform, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            triangles += prop.points.Length / 3;
        }
        string scenePath = Root + "/JapaneseStreet.unity";
        EditorSceneManager.SaveScene(parent.scene, scenePath); AssetDatabase.SaveAssets();
        // Fresh scene instance prevents stale renderer buffers from masking edits.
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        var bounds = renderers[0].bounds; foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        var cameraObject = new GameObject("Review camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.66f,.80f,.85f);
        camera.nearClipPlane = .08f; camera.farClipPlane = 300; camera.fieldOfView = 52;
        var texture = new RenderTexture(1440, 900, 24); camera.targetTexture = texture;
        float size = Mathf.Max(bounds.size.x, bounds.size.z);
        Capture(camera, texture, bounds.center + new Vector3(size*.65f,size*.50f,size*.65f), bounds.center, output + "/elevated.png");
        Capture(camera, texture, bounds.center + new Vector3(-size*.65f,size*.50f,-size*.65f), bounds.center, output + "/reverse-elevated.png");
        Capture(camera, texture, new Vector3(bounds.max.x+1,3.2f,0), new Vector3(0,2.2f,0), output + "/street.png");
        Capture(camera, texture, new Vector3(bounds.min.x-1,3.2f,0), new Vector3(0,2.2f,0), output + "/reverse-street.png");
        Capture(camera, texture, bounds.center + new Vector3(0,size*1.1f,.01f), bounds.center, output + "/plan.png");
        File.WriteAllText(output + "/verification.txt", $"Exact baked study geometry; shipped NimboProp shader.\n{triangles} triangles; {renderers.Length} meshes; bounds {bounds}.\nSaved and reopened scene: {scenePath}\nPresentation review only; no live movement, placement or performance claims.\n");
        Debug.Log("Japanese street Unity review PASS: " + triangles + " triangles; saved/reopened scene; five views");
        camera.targetTexture = null; RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(cameraObject);
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static void Capture(Camera camera, RenderTexture target, Vector3 eye, Vector3 look, string path)
    {
        camera.transform.position = eye; camera.transform.LookAt(look); camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
        File.WriteAllBytes(path,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
    }
}
#endif
