using System;
using UnityEditor;
using UnityEngine;

public static class AlwaysOnTopShaderPortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/ShrineProgress.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Always-on-top shader unavailable");
        var material = new Material(source); var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false, true); material.mainTexture = texture; material.SetColor("_Color", Color.white);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.localScale = Vector3.one * 2; quad.GetComponent<Renderer>().sharedMaterial = material;
        var blocker = GameObject.CreatePrimitive(PrimitiveType.Cube); blocker.transform.position = new Vector3(0,0,-1); blocker.transform.localScale = new Vector3(3,3,.1f);
        var green = new Material(Shader.Find("Unlit/Color")); green.color = Color.green; blocker.GetComponent<Renderer>().sharedMaterial = green;
        var cameraObject = new GameObject("always-on-top test camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 1; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image = new Texture2D(64,64,TextureFormat.RGBA32,false); camera.targetTexture = target;
        try {
            Color Render(Color c) { texture.SetPixel(0,0,c); texture.Apply(); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
            Color clear = Render(new Color(1,0,0,0)); Color red = Render(Color.red);
            if (clear.g < .9f || clear.r > .05f) throw new Exception("transparent pixel did not reveal foreground: " + clear);
            if (red.r < .9f || red.g > .05f) throw new Exception("opaque pixel did not render above foreground: " + red);
            Debug.Log("AlwaysOnTopShaderPortCheck passed: transparency and foreground occlusion with original shrine material");
        } finally { RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(blocker); UnityEngine.Object.DestroyImmediate(green); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(texture); }
    }
}
