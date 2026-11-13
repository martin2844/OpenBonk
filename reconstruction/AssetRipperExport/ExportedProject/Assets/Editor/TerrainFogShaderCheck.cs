using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrainFogShaderCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception(message); }
    static Texture2D Make(Color color) { var t=new Texture2D(1,1,TextureFormat.RGBA32,false);t.SetPixel(0,0,color);t.Apply();return t; }
    static Color Render(Camera c,RenderTexture target,Texture2D pixel) { c.Render();var old=RenderTexture.active;RenderTexture.active=target;pixel.ReadPixels(new Rect(32,32,1,1),0,0);pixel.Apply();RenderTexture.active=old;return pixel.GetPixel(0,0); }
    static void Keywords(string selected) { foreach(string name in new[]{"FOG_LINEAR","FOG_EXP","FOG_EXP2"}) { if(name==selected) Shader.EnableKeyword(name);else Shader.DisableKeyword(name); } }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_TerrainForestEdge.mat");
        Check(source && source.shader.isSupported,"Forest material");
        var material=new Material(source);material.SetTexture("_MainTex",Make(Color.green));material.SetTexture("_MainTexSide",Make(Color.green));material.SetTexture("_BelowYTexture",Make(Color.green));material.SetTexture("_Noise",Make(Color.white));material.SetTexture("_NormalT",Make(new Color(.5f,.5f,1,1)));material.SetTexture("_Normal",Make(new Color(.5f,.5f,1,1)));material.SetTexture("_Ramp",Make(Color.white));material.SetColor("_Color",Color.white);material.SetColor("_EdgeColor",Color.black);material.SetFloat("_TopSpread",.5f);material.SetFloat("_EdgeWidth",.4f);material.SetFloat("_BelowYThreshold",-100f);material.SetFloat("_BelowYBlend",2);material.SetFloat("_OutlineWidth",0);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.GetComponent<Renderer>().sharedMaterial=material;var filter=quad.GetComponent<MeshFilter>();var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);filter.sharedMesh=mesh;var normals=new Vector3[mesh.vertexCount];var tangents=new Vector4[mesh.vertexCount];for(int i=0;i<normals.Length;i++){normals[i]=Vector3.up;tangents[i]=new Vector4(1,0,0,1);}mesh.normals=normals;mesh.tangents=tangents;
        var lightObject=new GameObject("fog light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.5f;light.renderMode=LightRenderMode.ForcePixel;
        var cameraObject=new GameObject("fog camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-4);camera.orthographic=false;camera.fieldOfView=60;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
        try {
            RenderSettings.fog=false;Keywords(null);Color clear=Render(camera,target,pixel);Check(clear.g>.5f && clear.r<.1f,"unfogged reference: "+clear);
            RenderSettings.fog=true;RenderSettings.fogColor=Color.red;RenderSettings.fogStartDistance=0f;RenderSettings.fogEndDistance=1f;RenderSettings.fogDensity=4f;
            foreach(var entry in new[]{(FogMode.Linear,"FOG_LINEAR"),(FogMode.Exponential,"FOG_EXP"),(FogMode.ExponentialSquared,"FOG_EXP2")}) {
                RenderSettings.fogMode=entry.Item1;Keywords(entry.Item2);Color color=Render(camera,target,pixel);
                Debug.Log("TerrainFogShaderCheck "+entry.Item2+" color="+color);
                Check(color.r>.75f && color.g<.25f,"expected strong fog for "+entry.Item2+": "+color);
            }
            Debug.Log("TerrainFogShaderCheck passed: real Forest material with controlled fog modes on Unity GPU");EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); } finally {camera.targetTexture=null;RenderTexture.active=null;}
    }
}
