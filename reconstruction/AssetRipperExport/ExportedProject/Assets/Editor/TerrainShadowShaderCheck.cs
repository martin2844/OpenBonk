using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class TerrainShadowShaderCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static Texture2D Make(Color color) { var t=new Texture2D(1,1,TextureFormat.RGBA32,false);t.SetPixel(0,0,color);t.Apply();return t; }
    static Color Render(Camera camera,RenderTexture target,Texture2D pixel) { camera.Render();var old=RenderTexture.active;RenderTexture.active=target;pixel.ReadPixels(new Rect(64,64,1,1),0,0);pixel.Apply();RenderTexture.active=old;return pixel.GetPixel(0,0); }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_TerrainForestEdge.mat");Check(source && source.shader.isSupported,"Forest material");
        var material=new Material(source);material.SetTexture("_MainTex",Make(Color.green));material.SetTexture("_MainTexSide",Make(Color.green));material.SetTexture("_BelowYTexture",Make(Color.green));material.SetTexture("_Noise",Make(Color.white));material.SetTexture("_NormalT",Make(new Color(.5f,.5f,1,1)));material.SetTexture("_Normal",Make(new Color(.5f,.5f,1,1)));material.SetTexture("_Ramp",Make(Color.white));material.SetColor("_Color",Color.white);material.SetColor("_EdgeColor",Color.black);material.SetFloat("_TopSpread",.5f);material.SetFloat("_EdgeWidth",.4f);material.SetFloat("_BelowYThreshold",-100f);material.SetFloat("_BelowYBlend",2);material.SetFloat("_OutlineWidth",0);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.GetComponent<Renderer>().sharedMaterial=material;ground.GetComponent<Renderer>().receiveShadows=true;
        var caster=GameObject.CreatePrimitive(PrimitiveType.Cube);caster.transform.position=new Vector3(0,2,0);caster.transform.localScale=new Vector3(2,1,2);caster.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        var lightObject=new GameObject("shadow light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.5f;light.shadows=LightShadows.Hard;light.shadowStrength=1;light.renderMode=LightRenderMode.ForcePixel;lightObject.transform.rotation=Quaternion.Euler(90,0,0);
        var cameraObject=new GameObject("shadow camera");var camera=cameraObject.AddComponent<Camera>();camera.renderingPath=RenderingPath.Forward;camera.transform.position=new Vector3(0,10,-5);camera.transform.LookAt(Vector3.zero);camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
        bool oldFog=RenderSettings.fog; var oldShadows=QualitySettings.shadows; var oldResolution=QualitySettings.shadowResolution; float oldDistance=QualitySettings.shadowDistance; int exitCode=0;
        try {
            RenderSettings.fog=false;QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowDistance=50;QualitySettings.shadowResolution=UnityEngine.ShadowResolution.High;
            caster.SetActive(false);Color lit=Render(camera,target,pixel);caster.SetActive(true);Color shaded=Render(camera,target,pixel);
            Debug.Log("TerrainShadowShaderCheck lit="+lit+" shaded="+shaded);
            Check(lit.g>.3f && shaded.g<lit.g*.7f,"expected caster to darken actual terrain material");
            Check(material.FindPass("SHADOWCASTER") >= 0, "terrain shadow caster pass missing");
            caster.GetComponent<Renderer>().sharedMaterial=material;
            material.enableInstancing=true;
            Color terrainShadow=Render(camera,target,pixel);
            Debug.Log("TerrainShadowShaderCheck terrain caster="+terrainShadow);
            Check(terrainShadow.g<lit.g*.7f,"expected terrain material to cast directional shadow");
            caster.SetActive(false);
            var mesh=caster.GetComponent<MeshFilter>().sharedMesh;
            var transforms=new[]{Matrix4x4.TRS(new Vector3(0,2,0),Quaternion.identity,new Vector3(2,1,2))};
            Graphics.DrawMeshInstanced(mesh,0,material,transforms,1,null,ShadowCastingMode.ShadowsOnly,true,0,camera);
            Color instancedShadow=Render(camera,target,pixel);
            Debug.Log("TerrainShadowShaderCheck instanced caster="+instancedShadow);
            Check(instancedShadow.g<lit.g*.7f,"expected instanced terrain material to cast directional shadow");
            Debug.Log("TerrainShadowShaderCheck passed: real Forest material receives controlled directional shadow");
        } catch(Exception e) { Debug.LogException(e);exitCode=1; } finally {RenderSettings.fog=oldFog;QualitySettings.shadows=oldShadows;QualitySettings.shadowDistance=oldDistance;QualitySettings.shadowResolution=oldResolution;camera.targetTexture=null;RenderTexture.active=null;}
        EditorApplication.Exit(exitCode);
    }
}
