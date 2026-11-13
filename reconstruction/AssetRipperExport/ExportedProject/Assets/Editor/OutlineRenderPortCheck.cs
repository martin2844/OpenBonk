using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class OutlineRenderPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Call(object obj, string method) => obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(obj, null);
    static int Cyan(Texture2D image) { int count = 0; foreach (var pixel in image.GetPixels()) if (pixel.g > .7f && pixel.b > .7f && pixel.r < .25f) count++; return count; }
    public static void Run()
    {
        var cameraObject = new GameObject("outline camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -5); camera.orthographic = true; camera.orthographicSize = 1.5f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); var renderer = sphere.GetComponent<Renderer>(); var material = new Material(Shader.Find("Unlit/Color")); material.color = Color.white; renderer.sharedMaterial = material;
        var outline = sphere.AddComponent<Outline>(); Call(outline, "Awake"); outline.OutlineMode = Outline.Mode.OutlineVisible; outline.OutlineColor = Color.cyan; outline.OutlineWidth = 6f; Call(outline, "OnEnable"); Call(outline, "Update");
        Check(renderer.sharedMaterials.Length == 3, "outline materials must be appended");
        var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(128, 128, TextureFormat.RGBA32, false); camera.targetTexture = rt;
        camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply(); int outlined = Cyan(image);
        Check(outlined > 30, "visible cyan outline pixels: " + outlined); File.WriteAllBytes("/home/your-user/megabonk/build/android/outline-render-check.png", image.EncodeToPNG());
        Call(outline, "OnDisable"); Check(renderer.sharedMaterials.Length == 1, "disable removes outline materials");
        camera.Render(); image.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); image.Apply(); Check(Cyan(image) == 0, "disabled outline no longer renders");
        RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(sphere); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(material);
        Debug.Log("OutlineRenderPortCheck passed: stencil outline rendered " + outlined + " cyan pixels and disabled cleanly");
    }
}
