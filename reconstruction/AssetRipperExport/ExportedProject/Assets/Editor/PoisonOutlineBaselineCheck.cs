using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PoisonOutlineBaselineCheck
{
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/PoisonFlaskProjectile.prefab");var outline=prefab.transform.Find("Render/Outline");var filter=outline.GetComponent<MeshFilter>();var renderer=outline.GetComponent<MeshRenderer>();
        var obj=new GameObject("poison outline baseline");obj.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=renderer.sharedMaterial;obj.transform.rotation=outline.localRotation;obj.transform.localScale=outline.localScale;
        var center=obj.transform.TransformPoint(filter.sharedMesh.bounds.center);float radius=Mathf.Max(.1f,filter.sharedMesh.bounds.extents.magnitude);var cameraObj=new GameObject("outline camera");var camera=cameraObj.AddComponent<Camera>();camera.transform.position=center+Vector3.back*(radius*4+1);camera.transform.LookAt(center);camera.orthographic=true;camera.orthographicSize=radius*1.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;
        var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixels=new Texture2D(256,256,TextureFormat.RGBA32,false);
        try { camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();RenderTexture.active=null;System.IO.File.WriteAllBytes("/home/your-user/megabonk/build/android/poison-outline-baseline.png",pixels.EncodeToPNG());Debug.Log("PoisonOutlineBaselineCheck captured actual mesh/material with previous shader");EditorApplication.Exit(0); }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}finally{camera.targetTexture=null;RenderTexture.active=null;}
    }
}
