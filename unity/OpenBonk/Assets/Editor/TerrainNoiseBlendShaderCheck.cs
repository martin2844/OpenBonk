using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrainNoiseBlendShaderCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static Texture2D Make(Color color) { var t=new Texture2D(1,1,TextureFormat.RGBA32,false);t.SetPixel(0,0,color);t.Apply();return t; }
    static Color Render(Camera camera,RenderTexture target,Texture2D pixel)
    {
        camera.Render();var old=RenderTexture.active;RenderTexture.active=target;pixel.ReadPixels(new Rect(32,32,1,1),0,0);pixel.Apply();RenderTexture.active=old;return pixel.GetPixel(0,0);
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var forest=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_ForestFinal.mat");
        var desert=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_DesertFinal.mat");
        Check(forest && desert && forest.shader==desert.shader && forest.shader.isSupported,"real Forest/Desert terrain materials and shader");
        Check(forest.GetTexture("_TopTex2") && forest.GetTexture("_MainTex") && forest.GetTexture("_MainTexSide") && forest.GetTexture("_BelowYTexture") && forest.GetTexture("_Noise"),"real Forest texture bindings");
        var material=new Material(forest);var top=Make(Color.green);var top2=Make(Color.blue);var side=Make(Color.red);var below=Make(Color.blue);var noise=Make(new Color(.5f,.5f,.5f,1));var normal=Make(new Color(.5f,.5f,1,1));var ramp=Make(Color.white);
        material.SetTexture("_MainTex",top);material.SetTexture("_TopTex2",top2);material.SetFloat("_TopTex2Intensity",.7f);material.SetFloat("_TopTex2BlendSize",.2f);material.SetTexture("_MainTexSide",side);material.SetTexture("_BelowYTexture",below);material.SetTexture("_Noise",noise);material.SetTexture("_NormalT",normal);material.SetTexture("_Normal",normal);material.SetTexture("_Ramp",ramp);
        material.SetFloat("_Scale",.1f);material.SetFloat("_SideScale",.2f);material.SetFloat("_NoiseScale",.3f);material.SetFloat("_TopSpread",.5f);material.SetFloat("_EdgeWidth",.4f);material.SetFloat("_BelowYThreshold",0);material.SetFloat("_BelowYBlend",2);material.SetFloat("_OutlineWidth",0);material.SetColor("_Color",Color.white);material.SetColor("_EdgeColor",Color.black);
        var object3d=GameObject.CreatePrimitive(PrimitiveType.Quad);object3d.GetComponent<Renderer>().sharedMaterial=material;var filter=object3d.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);filter.sharedMesh=mesh;
        var lightObj=new GameObject("terrain light");var light=lightObj.AddComponent<Light>();light.type=LightType.Directional;light.color=Color.white;light.intensity=.5f;light.renderMode=LightRenderMode.ForcePixel;
        var cameraObj=new GameObject("terrain camera");var camera=cameraObj.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
        try {
            var normals=new Vector3[mesh.vertexCount];var tangents=new Vector4[mesh.vertexCount];for(int i=0;i<normals.Length;i++){normals[i]=Vector3.up;tangents[i]=new Vector4(1,0,0,1);}mesh.normals=normals;mesh.tangents=tangents;
            object3d.transform.position=new Vector3(0,5,0);camera.transform.position=new Vector3(0,5,-4);Color up=Render(camera,target,pixel);
            material.SetFloat("_TopTex2Intensity",.05f); Color alternate=Render(camera,target,pixel); material.SetFloat("_TopTex2Intensity",.7f);
            Check(alternate.b>.5f && alternate.g<.1f,"alternate top texture: "+alternate);
            for(int i=0;i<normals.Length;i++){normals[i]=Vector3.right;tangents[i]=new Vector4(0,0,1,1);}mesh.normals=normals;mesh.tangents=tangents;Color wall=Render(camera,target,pixel);
            object3d.transform.position=new Vector3(0,-5,0);camera.transform.position=new Vector3(0,-5,-4);Color under=Render(camera,target,pixel);
            Check(up.g>.5f && up.r<.2f && up.b<.1f,"upward surface should show top texture: "+up);
            Check(wall.r>.5f && wall.g<.1f && wall.b<.1f,"vertical surface should show side texture: "+wall);
            Check(under.b>.5f && under.r<.1f && under.g<.1f,"low surface should show below-height texture: "+under);
            Debug.Log("TerrainNoiseBlendShaderCheck passed: actual Forest/Desert material bindings, synthetic top/alternate/side/below textures and normals, Unity GPU");EditorApplication.Exit(0);
        } catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);} finally {camera.targetTexture=null;RenderTexture.active=null;}
    }
}
