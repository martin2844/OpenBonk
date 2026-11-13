using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class GrassColorPropertyCheck
{
    static void Require(bool value, string message) { if (!value) throw new Exception("GrassColorPropertyCheck: " + message); }
    public static void Run()
    {
        string[] paths = { "Assets/Shader/Custom_GrassShaderOptimized.shader", "Assets/Resources/GrassFallback.shader", "Assets/Resources/GrassBatched.shader" };
        foreach (string path in paths)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            Require(shader != null, path + " missing");
            foreach (string property in new[] { "_BottomColor", "_TopColor" })
            {
                int index = shader.FindPropertyIndex(property);
                Require(index >= 0 && shader.GetPropertyType(index) == ShaderPropertyType.Color, path + " " + property + " must be Color");
            }
        }
        Require(QualitySettings.activeColorSpace == ColorSpace.Linear, "project must be in linear color space");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("grass color camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false; camera.transform.position = new Vector3(0, 0, -4); camera.orthographic = true; camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.nearClipPlane = .1f; camera.farClipPlane = 20f;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear); camera.targetTexture = target;
        var pixel = new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        var white = new Texture2D(1,1,TextureFormat.RGBA32,false,true); white.SetPixel(0,0,Color.white); white.Apply();
        var material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(paths[2]));
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh = primitive.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.DestroyImmediate(primitive);
        bool oldFog = RenderSettings.fog;
        try
        {
            RenderSettings.fog = false;
            var tint = new Color(.2f,.3f,.4f,1f);
            material.SetTexture("_MainTex",white); material.SetColor("_BottomColor",tint); material.SetColor("_TopColor",tint);
            material.SetFloat("_WindStrength",0); material.SetFloat("_Cutoff",.5f); material.SetFloat("_RenderDistance",100); material.SetVector("_PlayerPos",Vector4.zero);
            var commands = new CommandBuffer(); commands.DrawMesh(mesh,Matrix4x4.identity,material,0,0); camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,commands);
            camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target; pixel.ReadPixels(new Rect(32,32,1,1),0,0); pixel.Apply(); RenderTexture.active = previous;
            var actual = pixel.GetPixel(0,0); var expected = tint.linear;
            Require(Mathf.Abs(actual.r-expected.r)<.015f && Mathf.Abs(actual.g-expected.g)<.015f && Mathf.Abs(actual.b-expected.b)<.015f, $"linear render {actual} expected {expected}");
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,commands); commands.Release();
            Debug.Log($"GrassColorPropertyCheck passed: three shader Color properties and linear tint render actual={actual}, expected={expected}");
        }
        finally
        {
            RenderSettings.fog = oldFog; UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(white); UnityEngine.Object.DestroyImmediate(pixel); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
