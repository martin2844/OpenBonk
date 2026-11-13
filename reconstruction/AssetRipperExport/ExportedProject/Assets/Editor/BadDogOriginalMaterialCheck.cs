using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BadDogOriginalMaterialCheck
{
    public static void Run()
    {
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/DomeField.mat");
        var pattern=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Texture2D/HexagonGrid.png");
        if (!source || !pattern || source.GetTexture("_PatternTex") != pattern) throw new Exception("original material or texture binding missing");
        if (source.GetColor("_EmissionColor").maxColorComponent != 0 || Math.Abs(source.GetFloat("_Metallic")-1f)>.001f) throw new Exception("unexpected original material parameters");
        var previous=RenderSettings.customReflectionTexture; var previousMode=RenderSettings.defaultReflectionMode;
        var cubemap=new Cubemap(16,TextureFormat.RGBA32,false); var pixels=new Color[256]; for(int i=0;i<pixels.Length;i++) pixels[i]=new Color(.8f,.8f,.8f,1);
        foreach(CubemapFace face in new[] { CubemapFace.PositiveX,CubemapFace.NegativeX,CubemapFace.PositiveY,CubemapFace.NegativeY,CubemapFace.PositiveZ,CubemapFace.NegativeZ }) cubemap.SetPixels(pixels,face);
        cubemap.Apply(); RenderSettings.defaultReflectionMode=DefaultReflectionMode.Custom; RenderSettings.customReflectionTexture=cubemap;
        var material=new Material(source); var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.GetComponent<Renderer>().sharedMaterial=material; sphere.transform.localScale=Vector3.one*2;
        var cameraObject=new GameObject("original dome material check"); var cam=cameraObject.AddComponent<Camera>(); cam.transform.position=new Vector3(0,0,-4); cam.transform.LookAt(Vector3.zero);
        cam.fieldOfView=42; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=new Color(.1f,.2f,.3f,1); cam.allowHDR=false; cam.allowMSAA=false; cam.depthTextureMode=DepthTextureMode.Depth;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image=new Texture2D(128,128,TextureFormat.RGBA32,false); cam.targetTexture=target;
        try {
            Color[] Render(float strength) { material.SetFloat("_AlphaStrength",strength); cam.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); return image.GetPixels(); }
            var disabled=Render(0); var enabled=Render(source.GetFloat("_AlphaStrength"));
            float sum=0,peak=0; int changed=0;
            for(int i=0;i<enabled.Length;i++) { float d=Mathf.Max(Mathf.Abs(enabled[i].r-disabled[i].r),Mathf.Abs(enabled[i].g-disabled[i].g),Mathf.Abs(enabled[i].b-disabled[i].b)); sum+=d; peak=Mathf.Max(peak,d); if(d>.02f) changed++; }
            if(changed<30 || peak<.05f) throw new Exception($"real texture with zero emission did not visibly affect image: changed={changed}, peak={peak}");
            System.IO.File.WriteAllBytes("/home/your-user/megabonk/build/android/baddog-original-material.png",image.EncodeToPNG());
            Debug.Log($"BadDogOriginalMaterialCheck passed: original HexagonGrid, zero emission, metal=1, controlled cubemap; changed={changed}, peak={peak:F3}, mean={sum/enabled.Length:F4}");
        } finally { RenderSettings.defaultReflectionMode=previousMode; RenderSettings.customReflectionTexture=previous; cam.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(sphere); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(cubemap); }
    }
}
