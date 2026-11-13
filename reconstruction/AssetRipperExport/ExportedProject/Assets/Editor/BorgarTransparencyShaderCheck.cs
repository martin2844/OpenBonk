using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BorgarTransparencyShaderCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/Borgar.prefab");
        var source=prefab!=null?prefab.GetComponentInChildren<MeshRenderer>(true):null;
        var filter=source!=null?source.GetComponent<MeshFilter>():null;
        Check(source!=null && filter!=null && filter.sharedMesh!=null && source.sharedMaterial!=null,"original Borgar mesh/material binding");
        Check(source.sharedMaterial.GetTexture("_AlbedoMap")!=null && source.sharedMaterial.shader.isSupported,"original albedo and shader support");
        var obj=new GameObject("Borgar visual"); obj.AddComponent<MeshFilter>().sharedMesh=filter.sharedMesh; obj.AddComponent<MeshRenderer>().sharedMaterial=source.sharedMaterial;
        obj.transform.localScale=new Vector3(2f,2f,2f);
        var lightObj=new GameObject("light");var light=lightObj.AddComponent<Light>(); light.type=LightType.Directional;light.color=Color.white;light.intensity=1f;lightObj.transform.rotation=Quaternion.identity;
        var camObj=new GameObject("camera"); var camera=camObj.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-4);camera.orthographic=true;camera.orthographicSize=1.1f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0f,0f,1f,.35f);
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var image=new Texture2D(128,128,TextureFormat.RGBA32,false);
        bool oldFog=RenderSettings.fog;var oldFogColor=RenderSettings.fogColor;var oldFogMode=RenderSettings.fogMode;float oldFogStart=RenderSettings.fogStartDistance,oldFogEnd=RenderSettings.fogEndDistance;
        try {
            RenderSettings.fog=false;
            camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=null;
            Color corner=image.GetPixel(20,20), center=image.GetPixel(64,64);
            Check(corner.b>.9f && corner.r<.1f && corner.g<.1f,"transparent pixels must preserve background: "+corner);
            Check(center.r>.15f && center.g>.1f && center.b<.5f,"opaque Borgar center should be visible: "+center);
            Check(Mathf.Abs(center.a-corner.a)<.02f,"Borgar should preserve destination alpha: corner="+corner.a+" center="+center.a);
            camera.orthographic=false;camera.fieldOfView=31f;RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=Color.green;RenderSettings.fogStartDistance=0f;RenderSettings.fogEndDistance=1f;
            camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=null;
            Color fogged=image.GetPixel(64,64);Check(fogged.g>.8f && fogged.r<.2f,"distant Borgar must approach green fog: "+fogged);
            Debug.Log("BorgarTransparencyShaderCheck passed: original prefab, mesh, material and texture; transparent exterior, preserved alpha, opaque center, linear fog");
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
        finally {RenderSettings.fog=oldFog;RenderSettings.fogColor=oldFogColor;RenderSettings.fogMode=oldFogMode;RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;camera.targetTexture=null;RenderTexture.active=null;}
    }
}
