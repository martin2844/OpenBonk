using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SpriteGlowTransparencyCheck
{
    public static void Run()
    {
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.localScale = Vector3.one * 2f;
        var mesh = UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh); mesh.colors = new[] { Color.white, Color.white, Color.white, Color.white }; quad.GetComponent<MeshFilter>().sharedMesh = mesh;
        var go = new GameObject("sprite shader check camera"); var camera = go.AddComponent<Camera>(); camera.transform.position = new Vector3(0, 0, -4); camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGB32); camera.targetTexture = target;
        var image = new Texture2D(64, 64, TextureFormat.RGBA32, false); var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        var oldMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight; RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.white;
        try
        {
            foreach (string asset in new[] { "Assets/Material/ParticleSmooth.mat", "Assets/Material/Spark_UI.mat" })
            {
                var source = AssetDatabase.LoadAssetAtPath<Material>(asset);
                if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Shader unavailable: " + asset);
                var material = new Material(source); quad.GetComponent<Renderer>().sharedMaterial = material;
                if (asset.Contains("ParticleSmooth")) { material.SetTexture("_MainTex", texture); material.SetTexture("_EmissionTex", Texture2D.blackTexture); material.SetColor("_Color", Color.white); }
                else { material.SetTexture("_TextureSample", texture); material.SetColor("_Tint", Color.white); material.SetFloat("_ExtraGlow", 1f); }
                Color Render(Color color) { texture.SetPixel(0, 0, color); texture.Apply(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
                Color transparent = Render(new Color(1, 0, 0, 0));
                if (transparent.b < .9f || transparent.r > .05f) throw new Exception(asset + " transparent texel: " + transparent);
                Color opaque = Render(Color.red);
                if (opaque.r < .3f || opaque.b > .05f) throw new Exception(asset + " opaque texel: " + opaque);
                UnityEngine.Object.DestroyImmediate(material);
            }
            Debug.Log("SpriteGlowTransparencyCheck PASS");
        }
        finally { RenderSettings.ambientMode = oldMode; RenderSettings.ambientLight = oldAmbient; RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(mesh); }
    }
}
