using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoisonOutlineShaderCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static Color32[] Render(Camera c,RenderTexture target,Texture2D pixels) { c.Render();var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=old;return pixels.GetPixels32(); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PoisonFlaskProjectile.prefab");
        Check(prefab!=null,"original poison projectile prefab");
        var outline=prefab.transform.Find("Render/Outline");var filter=outline!=null?outline.GetComponent<MeshFilter>():null;var renderer=outline!=null?outline.GetComponent<MeshRenderer>():null;
        Check(filter!=null && filter.sharedMesh!=null && renderer!=null && renderer.sharedMaterial!=null,"original outline mesh and material bindings");
        Check(renderer.sharedMaterial.shader.isSupported && renderer.sharedMaterial.IsKeywordEnabled("_MK_OUTLINE_NOISE"),"shader and noise variant");
        var material=new Material(renderer.sharedMaterial);material.SetColor("_OutlineColor",Color.magenta);
        var obj=new GameObject("poison outline mesh");obj.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;obj.transform.rotation=outline.localRotation;obj.transform.localScale=outline.localScale;
        var center=obj.transform.TransformPoint(filter.sharedMesh.bounds.center);float radius=Mathf.Max(.1f,filter.sharedMesh.bounds.extents.magnitude);
        var cameraObj=new GameObject("outline camera");var camera=cameraObj.AddComponent<Camera>();camera.transform.position=center+Vector3.back*(radius*4+1);camera.transform.LookAt(center);camera.orthographic=true;camera.orthographicSize=radius*1.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixels=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try {
            float originalSize=material.GetFloat("_OutlineSize");material.SetFloat("_OutlineSize",0);var baseline=Render(camera,target,pixels);material.SetFloat("_OutlineSize",originalSize);var result=Render(camera,target,pixels);
            int changed=0,magenta=0;for(int i=0;i<result.Length;i++){var a=baseline[i];var b=result[i];if(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)>30)changed++;if(b.r>80 && b.b>80 && b.g<40)magenta++;}
            Check(changed>4 && magenta>4,"expected expanded magenta outline on real mesh: changed="+changed+" magenta="+magenta);
            material.SetColor("_OutlineColor",renderer.sharedMaterial.GetColor("_OutlineColor"));
            Render(camera,target,pixels);
            System.IO.File.WriteAllBytes("/home/your-user/megabonk/build/android/poison-outline-real-color.png",pixels.EncodeToPNG());
            Debug.Log("PoisonOutlineShaderCheck passed: original prefab mesh/material, controlled magenta outline, changed="+changed+" magenta="+magenta);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{camera.targetTexture=null;RenderTexture.active=null;}
    }
}
