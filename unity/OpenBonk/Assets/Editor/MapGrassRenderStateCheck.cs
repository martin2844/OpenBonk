using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class MapGrassRenderStateCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static Texture2D Texture(Color color) { var t = new Texture2D(1,1,TextureFormat.RGBA32,false); t.SetPixel(0,0,color); t.Apply(); return t; }
    static Color Read(Camera camera, RenderTexture target, Texture2D pixel)
    {
        camera.Render(); var previous = RenderTexture.active; RenderTexture.active = target;
        pixel.ReadPixels(new Rect(32,32,1,1),0,0); pixel.Apply(); RenderTexture.active = previous; return pixel.GetPixel(0,0);
    }
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cameraObject = new GameObject("state camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0,0,-4); camera.orthographic = true; camera.orthographicSize = 1;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.nearClipPlane=.1f; camera.farClipPlane=20f;
        var target = new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); camera.targetTexture=target;
        var pixel = new Texture2D(1,1,TextureFormat.RGBA32,false);
        var red=Texture(Color.red); var white=Texture(Color.white); var black=Texture(Color.black);
        var semi=Texture(new Color(1,0,0,.65f)); var transparent=Texture(new Color(1,0,0,.3f));
        var mapTemplate=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/MapFogOfWar.mat");
        var heightTemplate=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_meshHeight.mat");
        var grassTemplate=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/GrassMaterialForest1.mat");
        var batchedShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/Resources/GrassBatched.shader");
        Check(mapTemplate && heightTemplate && grassTemplate && batchedShader,"original material bindings");
        var map=new Material(mapTemplate); var height=new Material(heightTemplate); var front=new Material(Shader.Find("Unlit/Color"));
        var grass=new Material(grassTemplate); var batched=new Material(grassTemplate){shader=batchedShader};
        GameObject mapQuad=null, frontQuad=null; ComputeBuffer matrices=null,args=null;
        try
        {
            Check(map.shader.isSupported && height.shader.isSupported && grass.shader.isSupported && batched.shader.isSupported,"shader support");
            Check(map.shader.renderQueue==3050 && map.renderQueue==4100 && height.renderQueue==1000,"original shader and material render queues");
            map.SetTexture("_MainTex",red); map.SetTexture("_FogTex",white); map.SetTexture("_FogNoise",black);
            mapQuad=GameObject.CreatePrimitive(PrimitiveType.Quad); mapQuad.transform.localScale=new Vector3(2,2,1); mapQuad.GetComponent<Renderer>().sharedMaterial=map;
            front.color=Color.green; frontQuad=GameObject.CreatePrimitive(PrimitiveType.Quad); frontQuad.transform.position=new Vector3(0,0,-1); frontQuad.transform.localScale=new Vector3(2,2,1); frontQuad.GetComponent<Renderer>().sharedMaterial=front;
            Color mapPixel=Read(camera,target,pixel); Check(mapPixel.r>.9f && mapPixel.g<.1f,"map must draw over a nearer opaque occluder: "+mapPixel);
            mapQuad.GetComponent<Renderer>().sharedMaterial=height; height.SetColor("_Color",Color.red); height.SetColor("_MaxColor",Color.red); height.SetFloat("_MinDistance",-1); height.SetFloat("_MaxDistance",1);
            frontQuad.transform.position=new Vector3(0,0,1);
            Color heightPixel=Read(camera,target,pixel); Check(heightPixel.g>.9f && heightPixel.r<.1f,"height background must not write depth: "+heightPixel);
            UnityEngine.Object.DestroyImmediate(mapQuad); mapQuad=null; UnityEngine.Object.DestroyImmediate(frontQuad); frontQuad=null;
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=primitive.GetComponent<MeshFilter>().sharedMesh; UnityEngine.Object.DestroyImmediate(primitive);
            matrices=new ComputeBuffer(1,64); matrices.SetData(new[]{Matrix4x4.identity}); grass.SetBuffer("_GrassBuffer",matrices);
            args=new ComputeBuffer(5,sizeof(uint),ComputeBufferType.IndirectArguments); args.SetData(new uint[]{mesh.GetIndexCount(0),1,mesh.GetIndexStart(0),(uint)mesh.GetBaseVertex(0),0});
            foreach(var material in new[]{grass,batched}) {
                material.SetFloat("_WindStrength",0); material.SetFloat("_Cutoff",.5f); material.SetFloat("_RenderDistance",100); material.SetVector("_PlayerPos",Vector4.zero);
                material.SetColor("_BottomColor",Color.white); material.SetColor("_TopColor",Color.white);
                foreach(var texture in new[]{semi,transparent}) {
                    material.SetTexture("_MainTex",texture); var commands=new CommandBuffer();
                    if(material==grass) commands.DrawMeshInstancedIndirect(mesh,0,material,0,args); else commands.DrawMesh(mesh,Matrix4x4.identity,material,0,0);
                    camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque,commands); Color c=Read(camera,target,pixel); camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque,commands); commands.Release();
                    if(texture==semi) Check(c.r>.2f && c.r<.95f && c.b>.1f && c.b<.95f,"grass should blend semi-transparent texels: "+material.shader.name+" "+c);
                    else Check(c.r<.1f && c.b>.9f,"grass cutoff: "+material.shader.name+" "+c);
                }
            }
            Debug.Log("MapGrassRenderStateCheck passed: map overlay depth and queues, map height no depth write, both grass paths alpha blend and cutoff");
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        finally { camera.targetTexture=null; if(matrices!=null)matrices.Release(); if(args!=null)args.Release(); }
    }
}
