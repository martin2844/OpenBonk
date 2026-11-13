using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoisonPbrBaseShaderCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static Color32[] Render(Camera c,RenderTexture target,Texture2D pixels) { c.Render();var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=old;return pixels.GetPixels32(); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PoisonFlaskProjectile.prefab");
        Check(prefab!=null,"original poison projectile prefab");var outline=prefab.transform.Find("Render/Outline");var filter=outline!=null?outline.GetComponent<MeshFilter>():null;var renderer=outline!=null?outline.GetComponent<MeshRenderer>():null;
        Check(filter!=null && filter.sharedMesh!=null && renderer!=null && renderer.sharedMaterial!=null && renderer.sharedMaterial.shader.isSupported,"original outline mesh and material bindings");
        Check(renderer.sharedMaterial.GetTexture("_AlbedoMap")!=null,"original flask albedo texture");
        var material=new Material(renderer.sharedMaterial);var obj=new GameObject("actual poison mesh");obj.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;obj.transform.rotation=outline.localRotation;obj.transform.localScale=outline.localScale;
        var center=obj.transform.TransformPoint(filter.sharedMesh.bounds.center);float radius=Mathf.Max(.1f,filter.sharedMesh.bounds.extents.magnitude);
        var lightObj=new GameObject("PBR key light");var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1f;light.color=Color.white;light.renderMode=LightRenderMode.ForcePixel;lightObj.transform.rotation=Quaternion.Euler(0,180,0);
        var cameraObj=new GameObject("PBR camera");var camera=cameraObj.AddComponent<Camera>();camera.transform.position=center+Vector3.back*(radius*4+1);camera.transform.LookAt(center);camera.orthographic=true;camera.orthographicSize=radius*1.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixels=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try {
            RenderSettings.fog=false;light.intensity=.1f;var dim=Render(camera,target,pixels);light.intensity=1f;var lit=Render(camera,target,pixels);
            int changed=0,lightened=0;for(int i=0;i<lit.Length;i++){var a=dim[i];var b=lit[i];int d=(b.r+b.g+b.b)-(a.r+a.g+a.b);if(Math.Abs(d)>12)changed++;if(d>12)lightened++;}
            Check(changed>20 && lightened>20,"expected actual material to respond to directional lighting: changed="+changed+" lightened="+lightened);
            System.IO.File.WriteAllBytes("/home/your-user/megabonk/build/android/poison-pbr-base-real-mesh.png",pixels.EncodeToPNG());
            Debug.Log("PoisonPbrBaseShaderCheck passed: actual projectile mesh/material and albedo respond to directional light; changed="+changed+" lightened="+lightened);EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{camera.targetTexture=null;RenderTexture.active=null;}
    }
}
