using System;
using UnityEditor;
using UnityEngine;

public static class GridSpiralShaderPortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_deathScreen.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Spiral shader unavailable");
        var material = new Material(source); material.SetFloat("_BlockSize", .25f);
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.localScale = Vector3.one * 2; quad.GetComponent<Renderer>().sharedMaterial = material;
        var cameraObject = new GameObject("spiral test camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 1; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.allowHDR = false; camera.allowMSAA = false;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image = new Texture2D(64,64,TextureFormat.RGBA32,false); camera.targetTexture = target;
        try {
            int CountBlack(float progress) { material.SetFloat("_Progress",progress); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); int n = 0; for (int y=0;y<4;y++) for (int x=0;x<4;x++) { var c = image.GetPixel(x*16+8,y*16+8); if (c.r < .05f && c.g < .05f && c.b < .05f) n++; else if (c.b < .9f) throw new Exception("unexpected color " + c); } return n; }
            int start=CountBlack(0), middle=CountBlack(.5f), end=CountBlack(1);
            if (start != 0 || middle != 8 || end != 16) throw new Exception("wrong spiral coverage: " + start + ", " + middle + ", " + end);
            Debug.Log("GridSpiralShaderPortCheck passed: 0, 8 and 16 blocks at progress 0, .5 and 1");
        } finally { RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(material); }
    }
}
