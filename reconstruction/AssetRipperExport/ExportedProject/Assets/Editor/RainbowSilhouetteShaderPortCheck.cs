using System;
using UnityEditor;
using UnityEngine;

public static class RainbowSilhouetteShaderPortCheck
{
    public static void Run()
    {
        var whiteSource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/WhiteUiOverlay.mat");
        var rainbowSource = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/rainbow.mat");
        if (!whiteSource || !rainbowSource || !whiteSource.shader.isSupported || !rainbowSource.shader.isSupported || ShaderUtil.ShaderHasError(whiteSource.shader) || ShaderUtil.ShaderHasError(rainbowSource.shader)) throw new Exception("UI shader unavailable");
        var white = new Material(whiteSource); var rainbow = new Material(rainbowSource); var texture = new Texture2D(1,1,TextureFormat.RGBA32,false,true); white.mainTexture = texture; rainbow.SetFloat("_Speed",0);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.localScale = Vector3.one * 2; var renderer = quad.GetComponent<Renderer>(); renderer.sharedMaterial = white;
        var cameraObject = new GameObject("UI shader test camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(120,60,24,RenderTextureFormat.ARGB32); var image = new Texture2D(120,60,TextureFormat.RGBA32,false); camera.targetTexture = target; camera.aspect = 1f;
        try {
            void Render() { camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,120,60),0,0); image.Apply(); }
            texture.SetPixel(0,0,new Color(1,0,0,0)); texture.Apply(); Render(); var clear=image.GetPixel(60,30);
            texture.SetPixel(0,0,Color.red); texture.Apply(); Render(); var opaque=image.GetPixel(60,30);
            if (clear.b < .9f || clear.r > .05f || opaque.r < .9f || opaque.g < .9f || opaque.b < .9f) throw new Exception("silhouette alpha or white color incorrect: " + clear + ", " + opaque);
            renderer.sharedMaterial = rainbow; Render();
            int distinct=0; var colors = new System.Collections.Generic.HashSet<Color32>();
            for (int x=10;x<120;x+=20) colors.Add(image.GetPixel(x,30));
            distinct=colors.Count; if (distinct != 6) throw new Exception("rainbow expected six color bands, got " + distinct);
            Debug.Log("RainbowSilhouetteShaderPortCheck passed: silhouette alpha/white and six rainbow bands");
        } finally { RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(white); UnityEngine.Object.DestroyImmediate(rainbow); UnityEngine.Object.DestroyImmediate(texture); }
    }
}
