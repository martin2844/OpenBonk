using System;
using UnityEditor;
using UnityEngine;

public static class DisintegrateShaderPortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Skull.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Disintegrate shader unavailable");
        var material = new Material(source); var texture = new Texture2D(1,1,TextureFormat.RGBA32,false,true); var ramp = new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        material.SetTexture("_TextureSample",texture); material.SetTexture("_Ramp",ramp); material.SetColor("_Tint",Color.white); material.SetFloat("_ExtraGlow",1);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh = UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh); quad.GetComponent<MeshFilter>().sharedMesh = mesh; quad.GetComponent<Renderer>().sharedMaterial = material; quad.transform.localScale = Vector3.one * 2;
        var cameraObject = new GameObject("disintegrate shader test camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image = new Texture2D(64,64,TextureFormat.RGBA32,false); camera.targetTexture = target;
        try {
            Color Render(float textureAlpha, float rampValue, float particleAlpha) { texture.SetPixel(0,0,new Color(1,0,0,textureAlpha)); ramp.SetPixel(0,0,new Color(rampValue,rampValue,rampValue,1)); texture.Apply(); ramp.Apply(); var c = new Color(1,1,1,particleAlpha); mesh.colors = new[] { c,c,c,c }; camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
            Color hidden = Render(1,.5f,0); Color opaque = Render(1,.5f,1); Color noTexture = Render(0,1,1);
            if (hidden.r > .05f || noTexture.r > .05f) throw new Exception("transparent cases rendered: " + hidden + ", " + noTexture);
            if (opaque.r < .8f || opaque.g > .05f || opaque.b > .05f) throw new Exception("opaque color not rendered: " + opaque);
            Debug.Log("DisintegrateShaderPortCheck passed: ramp, particle alpha and texture alpha using original material");
        } finally { RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(ramp); }
    }
}
