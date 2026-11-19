#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

// Run in the bundle authoring project with graphics enabled. Uses the same
// backface-culling shader and triangle builder as the shipped props.
public static class CheckDistrictMeshWinding
{
    [Serializable] private sealed class Package { public Prop[] props; }
    [Serializable] private sealed class Prop
    {
        public string name;
        public Vector3[] points, normals;
        public Color[] colors;
    }

    public static void Run()
    {
        string input = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_PROPS");
        string output = Environment.GetEnvironmentVariable("OPENBONK_NIMBO_WINDING_CHECK");
        Directory.CreateDirectory(output);
        var package = JsonUtility.FromJson<Package>(File.ReadAllText(input));
        int triangles = 0;
        foreach (var prop in package.props)
        {
            var indices = BuildDistrictBundle.OutwardTriangles(prop.points, prop.normals);
            for (int i = 0; i < indices.Length; i += 3)
            {
                var a = prop.points[indices[i]];
                var face = Vector3.Cross(prop.points[indices[i + 1]] - a, prop.points[indices[i + 2]] - a);
                Require(Vector3.Dot(face, prop.normals[i] + prop.normals[i + 1] + prop.normals[i + 2]) >= 0,
                    "inward face in " + prop.name);
                triangles++;
            }
            if (prop.name == "SageAccessHouse" || prop.name == "LilacAccessHouse")
            {
                foreach (bool fixedWinding in new[] { false, true })
                {
                    string suffix = fixedWinding ? "after" : "before";
                    Render(prop, fixedWinding, new Vector3(7, 4.8f, -9), new Vector3(0, 1.35f, 0),
                        Path.Combine(output, prop.name + "-" + suffix + ".png"));
                    Render(prop, fixedWinding, new Vector3(-7, 4.8f, 9), new Vector3(0, 1.35f, 0),
                        Path.Combine(output, prop.name + "-rear-" + suffix + ".png"));
                }
            }
        }
        // Near red wall must hide the far blue wall. The old unconditional
        // reversal culls the near wall and exposes the blue interior face.
        var box = new Prop { name = "Occlusion fixture", points = new[] {
            new Vector3(-1,-1,-1), new Vector3(-1,1,-1), new Vector3(1,1,-1),
            new Vector3(-1,-1,-1), new Vector3(1,1,-1), new Vector3(1,-1,-1),
            new Vector3(1,-1,1), new Vector3(1,1,1), new Vector3(-1,1,1),
            new Vector3(1,-1,1), new Vector3(-1,1,1), new Vector3(-1,-1,1),
        }, normals = new Vector3[12], colors = new Color[12] };
        for (int i = 0; i < 12; i++)
        {
            box.normals[i] = i < 6 ? Vector3.back : Vector3.forward;
            box.colors[i] = i < 6 ? Color.red : Color.blue;
        }
        Color before = Render(box, false, new Vector3(0, 0, -6), Vector3.zero, Path.Combine(output, "occlusion-before.png"));
        Color after = Render(box, true, new Vector3(0, 0, -6), Vector3.zero, Path.Combine(output, "occlusion-after.png"));
        Require(before.b > before.r * 2, "fixture did not reproduce the exposed far wall");
        Require(after.r > after.b * 2, "near wall failed to occlude the far wall");
        // A source that already reflects its coordinate system must also work.
        for (int i = 0; i < box.points.Length; i++) box.points[i].x = -box.points[i].x;
        Color reflected = Render(box, true, new Vector3(0, 0, -6), Vector3.zero,
            Path.Combine(output, "occlusion-reflected.png"));
        Require(reflected.r > reflected.b * 2, "reflected source lost its near wall");
        Debug.Log($"District winding PASS: {package.props.Length} props, {triangles} outward triangles, near-wall occlusion and reflected-source render verified");
    }

    private static Color Render(Prop prop, bool fixedWinding, Vector3 eye, Vector3 target, string path)
    {
        var mesh = new Mesh { vertices = prop.points, normals = prop.normals, colors = prop.colors };
        var indices = BuildDistrictBundle.OutwardTriangles(prop.points, prop.normals);
        if (!fixedWinding)
            for (int i = 0; i < indices.Length; i += 3)
            { indices[i] = i; indices[i + 1] = i + 2; indices[i + 2] = i + 1; }
        mesh.triangles = indices;
        mesh.RecalculateBounds();
        var material = new Material(Shader.Find("OpenBonk/NimboProp"));
        var model = new GameObject(prop.name) { layer = 30 };
        model.AddComponent<MeshFilter>().sharedMesh = mesh;
        model.AddComponent<MeshRenderer>().sharedMaterial = material;
        var cameraObject = new GameObject("Winding check camera");
        var camera = cameraObject.AddComponent<Camera>();
        var render = new RenderTexture(720, 600, 24);
        var texture = new Texture2D(720, 600, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        bool fog = RenderSettings.fog;
        try
        {
            RenderSettings.fog = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.18f, .27f, .36f);
            camera.cullingMask = 1 << 30;
            camera.fieldOfView = 32;
            camera.transform.position = eye;
            camera.transform.LookAt(target);
            camera.targetTexture = render;
            camera.Render();
            RenderTexture.active = render;
            texture.ReadPixels(new Rect(0, 0, 720, 600), 0, 0);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            return texture.GetPixel(360, 300);
        }
        finally
        {
            RenderTexture.active = previous;
            RenderSettings.fog = fog;
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(model);
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(render);
            Object.DestroyImmediate(texture);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
#endif
