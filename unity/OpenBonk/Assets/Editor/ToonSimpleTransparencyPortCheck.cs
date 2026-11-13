using System;
using UnityEditor;
using UnityEngine;

public static class ToonSimpleTransparencyPortCheck
{
    public static void Run()
    {
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_Arrow.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Toon simple shader unavailable");
        var material=new Material(source); var tex=new Texture2D(1,1,TextureFormat.RGBA32,false,true); material.SetTexture("_AlbedoMap",tex); material.SetColor("_AlbedoColor",Color.white);
        var fadingSource=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_boulder_fade.mat");
        if (!fadingSource) throw new Exception("Fading boulder material unavailable");
        var fading=new Material(fadingSource); fading.SetTexture("_AlbedoMap",tex); fading.SetFloat("_Rim",0); fading.SetFloat("_Specular",0); fading.SetFloat("_ReceiveShadows",0);
        material.SetFloat("_Rim",0); material.SetFloat("_Specular",0); material.SetFloat("_ReceiveShadows",0);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.localScale=Vector3.one*2; quad.GetComponent<Renderer>().sharedMaterial=material;
        var cameraObject=new GameObject("toon simple camera"); var camera=cameraObject.AddComponent<Camera>(); camera.transform.position=new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic=true; camera.orthographicSize=1; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.blue; camera.allowHDR=false; camera.allowMSAA=false;
        var lightObject=new GameObject("toon light"); var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.intensity=1f; light.transform.rotation=Quaternion.identity;
        var oldMode=RenderSettings.ambientMode; var oldLight=RenderSettings.ambientLight; RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight=Color.white;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image=new Texture2D(64,64,TextureFormat.RGBA32,false); camera.targetTexture=target;
        try {
            Color Render(float alpha) { tex.SetPixel(0,0,new Color(1,0,0,alpha)); tex.Apply(); camera.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
            Color clear=Render(0), opaque=Render(1);
            if (clear.b<.9f || clear.r>.05f) throw new Exception("zero alpha did not reveal background: "+clear);
            if (opaque.r<.3f || opaque.r<=opaque.b) throw new Exception("opaque red albedo missing: "+opaque);
            quad.GetComponent<Renderer>().sharedMaterial=fading;
            Color partial=Render(1);
            if (partial.r<.05f || partial.b<.5f) throw new Exception("boulder alpha did not blend with background: "+partial);
            Debug.Log("ToonSimpleTransparencyPortCheck passed: original arrow and fading boulder materials, alpha and albedo");
        } finally { RenderSettings.ambientMode=oldMode; RenderSettings.ambientLight=oldLight; RenderTexture.active=null; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(lightObject); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(fading); UnityEngine.Object.DestroyImmediate(tex); }
    }
}
