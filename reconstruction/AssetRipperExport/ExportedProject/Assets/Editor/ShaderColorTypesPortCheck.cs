using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ShaderColorTypesPortCheck
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception("ShaderColorTypesPortCheck: " + message); }
    public static void Run()
    {
        var cases = new[] {
            new[] { "Assets/Shader/MK_Toon_Built-in_Standard_Simple + Outline.shader", "_AlbedoColor", "_RimColor", "_OutlineColor" },
            new[] { "Assets/Shader/Toon_Lit Tri Planar Normal.shader", "_Color", "_EdgeColor", "_OutlineColor" },
            new[] { "Assets/Shader/Custom_UnlitTransparentBlended.shader", "_Color" },
            new[] { "Assets/Shader/Custom_MinimapIconInstancedCutoutColor.shader", "_Color" },
            new[] { "Assets/Shader/Archanor VFX_Retro Arsenal_Beam.shader", "_Tint" }
        };
        foreach (var entry in cases) {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(entry[0]);
            Require(shader != null, entry[0] + " missing");
            for (int i = 1; i < entry.Length; i++) {
                int index = shader.FindPropertyIndex(entry[i]);
                Require(index >= 0 && shader.GetPropertyType(index) == ShaderPropertyType.Color, entry[0] + " " + entry[i]);
            }
        }
        Require(QualitySettings.activeColorSpace == ColorSpace.Linear, "expected linear color space");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("color test camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false; camera.transform.position = new Vector3(0, 0, -4); camera.orthographic = true; camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear); camera.targetTexture = target;
        var pixel = new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        var material = new Material(AssetDatabase.LoadAssetAtPath<Shader>(cases[2][0]));
        var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh = primitive.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.DestroyImmediate(primitive);
        var commands = new CommandBuffer();
        try {
            var tint = new Color(.2f,.3f,.4f,1f); material.SetColor("_Color",tint);
            commands.DrawMesh(mesh,Matrix4x4.identity,material,0,0); camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,commands);
            camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target; pixel.ReadPixels(new Rect(32,32,1,1),0,0); pixel.Apply(); RenderTexture.active = previous;
            var actual = pixel.GetPixel(0,0); var expected = tint.linear;
            Require(Mathf.Abs(actual.r-expected.r)<.015f && Mathf.Abs(actual.g-expected.g)<.015f && Mathf.Abs(actual.b-expected.b)<.015f, $"render {actual} expected {expected}");
            Debug.Log($"ShaderColorTypesPortCheck passed: original property types and linear tint rendering {actual} expected {expected}");
        } finally {
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,commands); commands.Release(); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(pixel); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
