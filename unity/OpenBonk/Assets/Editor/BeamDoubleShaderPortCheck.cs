using System;
using UnityEditor;
using UnityEngine;

public static class BeamDoubleShaderPortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/BeamLightning.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("BeamDouble shader unavailable or has errors");
        var material = new Material(source); var first = new Texture2D(1, 1, TextureFormat.RGBA32, false, true); var second = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        material.SetTexture("_TextureSample", first); material.SetTexture("_TextureSample2", second);
        material.SetTextureScale("_TextureSample", Vector2.one); material.SetTextureScale("_TextureSample2", Vector2.one);
        material.SetVector("_ScrollSpeed", Vector4.zero); material.SetVector("_ScrollSpeed2", Vector4.zero); material.SetColor("_Tint", Color.white); material.SetFloat("_ExtraGlow", 1f);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh = UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
        mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white }; quad.GetComponent<MeshFilter>().sharedMesh = mesh;
        quad.transform.localScale = Vector3.one * 2f; quad.GetComponent<Renderer>().sharedMaterial = material;
        var cameraObject = new GameObject("double beam shader check camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(64, 64, TextureFormat.RGBA32, false); camera.targetTexture = target;
        try {
            Color Render(Color a, Color b) { first.SetPixel(0, 0, a); second.SetPixel(0, 0, b); first.Apply(); second.Apply(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
            var clearFirst = Render(new Color(1,0,0,0), Color.white);
            var clearSecond = Render(Color.red, new Color(1,1,1,0));
            var opaque = Render(Color.red, Color.white);
            if (clearFirst.r > .05f || clearSecond.r > .05f) throw new Exception("either texture's alpha should suppress the beam: " + clearFirst + ", " + clearSecond);
            if (opaque.r < .8f || opaque.g > .05f || opaque.b > .05f) throw new Exception("combined texture color not applied: " + opaque);
            Debug.Log("BeamDoubleShaderPortCheck passed: both texture alpha channels and color with original material");
        } finally { RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); }
    }
}
