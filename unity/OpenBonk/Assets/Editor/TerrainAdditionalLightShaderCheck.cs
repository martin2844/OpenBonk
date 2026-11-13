using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrainAdditionalLightShaderCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static Texture2D Make(Color color) { var t=new Texture2D(1,1,TextureFormat.RGBA32,false);t.SetPixel(0,0,color);t.Apply();return t; }
    static Color Render(Camera c,RenderTexture target,Texture2D pixel) { c.Render();var old=RenderTexture.active;RenderTexture.active=target;pixel.ReadPixels(new Rect(32,32,1,1),0,0);pixel.Apply();RenderTexture.active=old;return pixel.GetPixel(0,0); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_TerrainForestEdge.mat");Check(source && source.shader.isSupported,"Forest material");
        var material=new Material(source);foreach(var name in new[]{"_MainTex","_MainTexSide","_BelowYTexture","_Noise","_Ramp"})material.SetTexture(name,Make(Color.white));foreach(var name in new[]{"_NormalT","_Normal"})material.SetTexture(name,Make(new Color(.5f,.5f,1,1)));
        material.SetColor("_Color",Color.white);material.SetColor("_EdgeColor",Color.black);material.SetFloat("_TopSpread",.5f);material.SetFloat("_EdgeWidth",.4f);material.SetFloat("_BelowYThreshold",-100f);material.SetFloat("_BelowYBlend",2);material.SetFloat("_OutlineWidth",0);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.GetComponent<Renderer>().sharedMaterial=material;
        var dirObj=new GameObject("primary directional light");var dir=dirObj.AddComponent<Light>();dir.type=LightType.Directional;dir.intensity=.5f;dir.color=Color.red;dir.renderMode=LightRenderMode.ForcePixel;
        var pointObj=new GameObject("secondary pixel light");pointObj.transform.position=new Vector3(0,0,-2);var point=pointObj.AddComponent<Light>();point.type=LightType.Point;point.intensity=1;point.range=10;point.color=Color.green;point.renderMode=LightRenderMode.ForcePixel;
        var cameraObj=new GameObject("vertex light camera");var camera=cameraObj.AddComponent<Camera>();camera.renderingPath=RenderingPath.Forward;camera.transform.position=new Vector3(0,0,-4);camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
        int oldPixelLights=QualitySettings.pixelLightCount; bool oldFog=RenderSettings.fog; int exitCode=0;
        try {
            QualitySettings.pixelLightCount=2;RenderSettings.fog=false;point.enabled=false;Color baseColor=Render(camera,target,pixel);point.enabled=true;Color withPoint=Render(camera,target,pixel);
            Debug.Log("TerrainAdditionalLightShaderCheck base="+baseColor+" point="+withPoint);
            Check(baseColor.r>.3f && withPoint.g>baseColor.g+.05f,"expected green contribution from secondary vertex light");
            Debug.Log("TerrainAdditionalLightShaderCheck passed: actual Forest material receives secondary pixel point light");
        } catch(Exception e) { Debug.LogException(e);exitCode=1; } finally {QualitySettings.pixelLightCount=oldPixelLights;RenderSettings.fog=oldFog;camera.targetTexture=null;RenderTexture.active=null;}
        EditorApplication.Exit(exitCode);
    }
}
