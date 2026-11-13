using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PlayerRimOutlineCheck
{
    static void Check(bool ok,string message) { if (!ok) throw new Exception(message); }
    static int CountBlue(Camera camera,RenderTexture target,Texture2D image)
    {
        camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=null;
        int n=0;foreach(var c in image.GetPixels32())if(c.b>100 && c.b>c.r+20)n++;return n;
    }
    public static void Run()
    {
        try {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_fox_light.mat");
            Check(source && source.shader.isSupported,"original Fox material missing");
            var a=new Material(source);var b=new Material(source);
            var go=new GameObject("player renderer");go.SetActive(false);var player=go.AddComponent<PlayerRenderer>();
            typeof(PlayerRenderer).GetField("activeMaterials",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,new Material[]{a,null,b});
            player.SetOutlineColor(Color.green);player.SetRim(Color.blue,.7f);
            foreach(var mat in new[]{a,b}) Check(mat.GetColor("_OutlineColor")==Color.green && mat.GetColor("_RimColor")==Color.blue && mat.GetFloat("_Rim")==1f && Mathf.Abs(mat.GetFloat("_RimSize")-.7f)<.001f,"material update");
            player.SetRim(Color.blue,2f);Check(a.GetFloat("_RimSize")==1f,"upper range clamp");
            player.SetRim(Color.blue,-1f);Check(a.GetFloat("_RimSize")==0f,"lower range clamp");player.SetRim(Color.blue,.7f);
            a.SetTexture("_AlbedoMap",Texture2D.whiteTexture);a.SetColor("_AlbedoColor",Color.red);a.SetFloat("_Specular",0);
            var sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere);sphere.GetComponent<Renderer>().sharedMaterial=a;
            var camera=new GameObject("rim camera").AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-4);camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var light=new GameObject("light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;
            var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var image=new Texture2D(128,128,TextureFormat.RGBA32,false);
            int on=CountBlue(camera,target,image);player.SetRimOff();int off=CountBlue(camera,target,image);
            Check(a.GetFloat("_Rim")==0 && b.GetFloat("_Rim")==0 && a.GetColor("_RimColor")==Color.blue,"rim off preserves color");
            Check(on>50 && off==0,"expected blue rim GPU on/off, counts="+on+"/"+off);
            Debug.Log("PlayerRimOutlineCheck passed: two Fox materials, outline/rim values, original range clamp and visible GPU rim on/off="+on+"/"+off);EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
