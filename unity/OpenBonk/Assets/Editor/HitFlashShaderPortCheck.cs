using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HitFlashShaderPortCheck
{
    static Color Capture(Camera camera, RenderTexture rt, Material mat, float amount, string path)
    {
        mat.SetFloat("_FlashAmount", amount); camera.Render(); RenderTexture.active = rt;
        var image = new Texture2D(128,128,TextureFormat.RGBA32,false); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
        Color value = image.GetPixel(64,64); if (path != null) File.WriteAllBytes(path, image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image); return value;
    }
    public static void Run()
    {
        var cameraObject = new GameObject("hit flash camera"); var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.orthographic = true; camera.orthographicSize = 1f; camera.transform.position = new Vector3(0,0,-5);
        var rt = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); camera.targetTexture = rt; var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/DamageFlash.mat"); if (source == null) throw new Exception("original flash material missing");
        if (source.GetFloat("_FlashAmount") != 1f || source.GetColor("_FlashColor") != Color.white) throw new Exception("original full-white material settings changed");
        var material = new Material(source); material.SetColor("_Color", Color.red); material.SetColor("_FlashColor", Color.green); quad.GetComponent<Renderer>().sharedMaterial = material;
        var normal = Capture(camera,rt,material,0,null); var partial = Capture(camera,rt,material,.5f,null); var flash = Capture(camera,rt,material,1,"/home/your-user/megabonk/build/android/hit-flash-shader-check.png");
        if (!(normal.r>.8f && normal.g<.1f && partial.r>.25f && partial.g>.25f && flash.g>.8f && flash.r<.1f)) throw new Exception("flash interpolation failed: " + normal + " / " + partial + " / " + flash);
        Debug.Log("HitFlashShaderPortCheck passed: original material base, half flash and full flash colors="+normal+" / "+partial+" / "+flash);
        RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(cameraObject);
    }
}
