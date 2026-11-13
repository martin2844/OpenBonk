using System;
using UnityEditor;
using UnityEngine;

public static class BeamShaderPortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Beam.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Beam shader unavailable or has errors");
        var material = new Material(source); var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        material.SetTexture("_TextureSample", texture); material.SetTextureScale("_TextureSample", Vector2.one); material.SetVector("_ScrollSpeed", Vector4.zero);
        material.SetColor("_Tint", Color.white); material.SetFloat("_ExtraGlow", 1f);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh = UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white }; quad.GetComponent<MeshFilter>().sharedMesh = mesh;
        quad.transform.localScale = Vector3.one * 2f; quad.GetComponent<Renderer>().sharedMaterial = material;
        var cameraObject = new GameObject("beam shader check camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        camera.targetTexture = target;
        try {
            texture.SetPixel(0, 0, new Color(1,0,0,0)); texture.Apply(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply();
            var transparent = image.GetPixel(32,32); if (transparent.r > .05f) throw new Exception("zero-alpha texture was opaque: " + transparent);
            texture.SetPixel(0, 0, new Color(1,0,0,1)); texture.Apply(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply();
            var opaque = image.GetPixel(32,32); if (opaque.r < .8f || opaque.g > .05f || opaque.b > .05f) throw new Exception("texture/tint not applied: " + opaque);
            Debug.Log("BeamShaderPortCheck passed: transparent and opaque texture sampling with original material");
        } finally { RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(texture); }
    }
}
