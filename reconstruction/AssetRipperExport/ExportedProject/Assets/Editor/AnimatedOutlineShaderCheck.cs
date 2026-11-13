using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AnimatedOutlineShaderCheck
{
    static void Check(bool ok,string msg) { if(!ok) throw new Exception(msg); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var template=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_animatedOutlineChallenge.mat");
        Check(template && template.shader.isSupported && template.shader.name=="UI/AnimatedOutline","original material/shader");
        var material=new Material(template);material.SetFloat("_Thickness",8);material.SetFloat("_TrailLength",.5f);material.SetFloat("_Speed",0);material.SetVector("_RectSize",new Vector4(128,128,0,0));material.SetColor("_Color",Color.green);
        var cameraObj=new GameObject("outline camera");var camera=cameraObj.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-4);camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.blue;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixels=new Texture2D(128,128,TextureFormat.RGBA32,false);
        var canvasObj=new GameObject("outline canvas",typeof(RectTransform),typeof(Canvas));var canvas=canvasObj.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        var canvasRect=canvasObj.GetComponent<RectTransform>();canvasRect.sizeDelta=new Vector2(128,128);canvasRect.localScale=Vector3.one*.01f;
        var imgObj=new GameObject("outline image",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));imgObj.transform.SetParent(canvasObj.transform,false);
        var rect=imgObj.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;var image=imgObj.GetComponent<Image>();image.material=material;
        try {
            Canvas.ForceUpdateCanvases();camera.Render();var prev=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,128,128),0,0);pixels.Apply();RenderTexture.active=prev;
            Color center=pixels.GetPixel(64,64);int green=0, blue=0;foreach(Color32 pixel in pixels.GetPixels32()){if(pixel.g>8 && pixel.b>20)green++;if(pixel.b>230 && pixel.r<10 && pixel.g<10)blue++;}
            Check(center.b>.9f && center.g<.05f,"UI outline center must be transparent: "+center);
            Check(green>100 && blue>1000,"expected visible translucent outline and blue background, got "+green+"/"+blue);
            Debug.Log("AnimatedOutlineShaderCheck passed: actual challenge material on Unity UI Image, transparent interior and blended outline; green pixels="+green);
            EditorApplication.Exit(0);
        }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        finally {camera.targetTexture=null;RenderTexture.active=null;}
    }
}
