using System;
using UnityEditor;
using UnityEngine;

public static class PowerupTransparencyPortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_powerup.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Powerup shader unavailable");
        var material = new Material(source); var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        material.mainTexture = texture; material.color = Color.white;
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.localScale = Vector3.one * 2f; quad.GetComponent<Renderer>().sharedMaterial = material;
        var go = new GameObject("powerup transparency camera"); var camera = go.AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -4); camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32); var image = new Texture2D(64, 64, TextureFormat.RGBA32, false); camera.targetTexture = target;
        try
        {
            texture.SetPixel(0, 0, new Color(1, 0, 0, 0)); texture.Apply(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply();
            Color clear = image.GetPixel(32,32); if (clear.b < .9f || clear.r > .05f) throw new Exception("transparent texel did not reveal background: " + clear);
            texture.SetPixel(0, 0, Color.red); texture.Apply(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply();
            Color opaque = image.GetPixel(32,32); if (opaque.r < .9f || opaque.b > .05f) throw new Exception("opaque texel did not render: " + opaque);
            Debug.Log("PowerupTransparencyPortCheck passed: transparent and opaque texels with original powerup material");
        }
        finally { RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(texture); }
    }
}
