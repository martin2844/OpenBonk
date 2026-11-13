using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ToonShaderPortCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception("ToonShaderPortCheck: " + message); }
    static int Count(Texture2D image, Func<Color, bool> f) { int n=0; foreach (var p in image.GetPixels()) if (f(p)) n++; return n; }
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_ent3.mat"); Check(source && source.shader.isSupported, "original material and supported shader");
        var errors = ShaderUtil.GetShaderMessages(source.shader); foreach (var e in errors) Check(e.severity != UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error, e.message);
        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere); var material = new Material(source); root.GetComponent<Renderer>().sharedMaterial = material;
        var camObject = new GameObject("toon test camera"); var camera = camObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var oldAmbientMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.1f,.1f,.1f);
        var lightObject = new GameObject("toon test light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1f; light.transform.rotation = Quaternion.identity;
        var rt = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); camera.targetTexture = rt; var image = new Texture2D(128,128,TextureFormat.RGBA32,false);
        try {
            material.SetTexture("_AlbedoMap", Texture2D.whiteTexture); material.SetColor("_AlbedoColor", Color.red); material.SetFloat("_Specular", 0); material.SetFloat("_Rim", 1); material.SetFloat("_RimSize", 1.5f); material.SetFloat("_RimSmoothness", .5f); material.SetColor("_RimColor", new Color(0,0,1,0));
            camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            int red = Count(image, c => c.r > .8f && c.g < .1f && c.b < .1f); int blue = Count(image, c => c.b > .4f && c.b > c.r); Check(red > 1000 && blue == 0, "red albedo with disabled rim, red=" + red + " blue=" + blue);
            float lit = image.GetPixel(64,64).r; light.intensity = .05f; camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); Check(image.GetPixel(64,64).r < lit * .7f, "directional light changes surface brightness"); light.intensity = 1f;
            material.SetColor("_RimColor", new Color(0,0,1,1)); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            blue = Count(image, c => c.b > .4f && c.b > c.r); Check(blue > 100, "visible rim, blue=" + blue);
            File.WriteAllBytes("/home/your-user/megabonk/build/android/toon-shader-check.png", image.EncodeToPNG());
            material.SetFloat("_Rim", 0); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            Check(Count(image, c => c.b > .4f && c.b > c.r) == 0, "rim disabled by material setting");
            material.SetColor("_OutlineColor", Color.green); material.SetFloat("_OutlineSize", 40); material.SetFloat("_Outline", 3);
            camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            int outline = Count(image, c => c.g > .8f && c.r < .1f && c.b < .1f); Check(outline > 50, "visible outline, pixels=" + outline);
            File.WriteAllBytes("/home/your-user/megabonk/build/android/toon-outline-check.png", image.EncodeToPNG());
            material.SetFloat("_Outline", 1); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            Check(Count(image, c => c.g > .8f && c.r < .1f && c.b < .1f) == 0, "outline disabled by material setting");
            Debug.Log("ToonShaderPortCheck passed: original material, lighting, configurable rim and outline");
        } finally { RenderTexture.active = null; RenderSettings.ambientMode = oldAmbientMode; RenderSettings.ambientLight = oldAmbient; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(camObject); UnityEngine.Object.DestroyImmediate(lightObject); }
    }
}
