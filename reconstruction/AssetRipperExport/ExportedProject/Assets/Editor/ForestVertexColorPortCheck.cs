using System;
using UnityEditor;
using UnityEngine;

public static class ForestVertexColorPortCheck
{
    public static void Run()
    {
        CheckMaterial("Assets/Material/m_fillMiddleEdge.mat", true);
    }
    public static void RunSimple()
    {
        CheckMaterial("Assets/Material/m_lockedCharacter.mat", false);
    }
    static void CheckMaterial(string path, bool checkWall)
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (source == null || source.IsKeywordEnabled("_MK_ALBEDO_MAP")) throw new Exception("Expected untextured material: " + path);
        var material = new Material(source);
        var model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh = UnityEngine.Object.Instantiate(model.GetComponent<MeshFilter>().sharedMesh);
        model.GetComponent<MeshFilter>().sharedMesh = mesh;
        model.GetComponent<Renderer>().sharedMaterial = material;
        var cameraObject = new GameObject("vertex color camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -4); camera.orthographic = true; camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var lightObject = new GameObject("vertex color light");
        var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1; light.transform.rotation = Quaternion.identity;
        var target = new RenderTexture(128, 128, 24);
        var image = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        var oldAmbientMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.white;
        try
        {
            if (material.HasProperty("_Outline")) material.SetFloat("_Outline", 0); material.SetFloat("_Specular", 0); material.SetFloat("_Rim", 0);
            material.SetColor("_AlbedoColor", Color.white); material.SetColor("_GoochDarkColor", Color.white); material.SetColor("_GoochBrightColor", Color.white);
            material.SetTexture("_AlbedoMap", Texture2D.whiteTexture);
            var colors = new Color[mesh.vertexCount];
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.red;
            mesh.colors = colors;
            camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
            var red = image.GetPixel(64, 64);
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.green;
            mesh.colors = colors;
            camera.Render(); image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
            var green = image.GetPixel(64, 64);
            if (red.r < .1f || red.r < red.g * 3f || green.g < .1f || green.g < green.r * 3f)
                throw new Exception("Untextured material did not use vertex colors: " + red + " -> " + green);
            material.EnableKeyword("_MK_ALBEDO_MAP");
            camera.Render(); image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
            var textured = image.GetPixel(64, 64);
            if (Mathf.Abs(textured.r - textured.g) > .05f) throw new Exception("Textured branch incorrectly retained vertex color: " + textured);
            material.DisableKeyword("_MK_ALBEDO_MAP");
            mesh.colors = Array.Empty<Color>();
            camera.Render(); image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
            var missing = image.GetPixel(64, 64);
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.white;
            mesh.colors = colors;
            camera.Render(); image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply();
            var explicitWhite = image.GetPixel(64, 64);
            if (Mathf.Abs(missing.r - explicitWhite.r) > .02f || Mathf.Abs(missing.g - explicitWhite.g) > .02f || Mathf.Abs(missing.b - explicitWhite.b) > .02f)
                throw new Exception("Missing vertex colors differ from white: " + missing + " versus " + explicitWhite);
            if (checkWall)
            {
                var wall = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Mesh/TileWall.asset");
                if (wall == null || wall.colors.Length != 0) throw new Exception("Forest wall color-channel assumption changed");
            }
            Debug.Log("ForestVertexColorPortCheck passed: " + path + ", red=" + red + ", green=" + green + ", textured=" + textured + ", missing=" + missing + ", white=" + explicitWhite);
        }
        finally
        {
            RenderTexture.active = null; RenderSettings.ambientMode = oldAmbientMode; RenderSettings.ambientLight = oldAmbient;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(model); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject);
        }
    }
}
