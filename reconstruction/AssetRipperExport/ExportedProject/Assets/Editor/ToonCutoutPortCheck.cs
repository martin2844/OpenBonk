using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ToonCutoutPortCheck
{
    static void Check(bool b, string message) { if (!b) throw new Exception("ToonCutoutPortCheck: " + message); }
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_stickman.mat"); Check(source && source.shader.isSupported, "original cutout material");
        var mat = new Material(source); var texture = new Texture2D(2,1,TextureFormat.RGBA32,false); texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp; texture.SetPixels(new[]{new Color(1,1,1,1),new Color(1,1,1,0)}); texture.Apply();
        mat.SetTexture("_AlbedoMap", texture); mat.EnableKeyword("_MK_ALBEDO_MAP"); mat.SetColor("_AlbedoColor", Color.red); mat.SetColor("_GoochBrightColor", Color.white); mat.SetColor("_GoochDarkColor", Color.white); mat.SetFloat("_Specular",0); mat.SetFloat("_Outline",1); mat.SetFloat("_Rim",0); mat.SetFloat("_RenderFace",0); mat.SetFloat("_AlphaClipping",1); mat.SetFloat("_AlphaCutoff",.5f);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); quad.GetComponent<Renderer>().sharedMaterial=mat; quad.transform.localScale = new Vector3(2,2,1);
        var cameraObject=new GameObject("cutout camera"); var camera=cameraObject.AddComponent<Camera>(); camera.transform.position=new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic=true; camera.orthographicSize=1.5f; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
        var lightObject=new GameObject("cutout light"); var light=lightObject.AddComponent<Light>(); light.type=LightType.Directional; light.renderMode=LightRenderMode.ForcePixel; light.transform.rotation=Quaternion.identity;
        var rt=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); camera.targetTexture=rt; var image=new Texture2D(128,128,TextureFormat.RGBA32,false);
        try {
            camera.Render(); RenderTexture.active=rt; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            var left=image.GetPixel(48,64); var right=image.GetPixel(80,64); Check(left.r > .5f && right.r < .05f, "alpha cutout visible="+left+" clipped="+right);
            File.WriteAllBytes("/home/your-user/megabonk/build/android/toon-cutout-check.png",image.EncodeToPNG());
            mat.SetFloat("_AlphaClipping",0); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); Check(image.GetPixel(80,64).r>.5f,"cutout disabled");
            quad.transform.rotation=Quaternion.Euler(0,180,0); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); Check(image.GetPixel(64,64).r>.5f,"two-sided material renders back face");
            mat.SetFloat("_RenderFace",2); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); Check(image.GetPixel(64,64).r<.05f,"back-face culling");
            Debug.Log("ToonCutoutPortCheck passed: original material alpha cutout and configurable back-face culling");
        } finally { RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(mat); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject); }
    }
}
