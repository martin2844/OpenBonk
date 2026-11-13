using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class DissolveInstancedShaderCheck
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static Color32[] Render(Camera camera, RenderTexture target, Texture2D image)
    {
        camera.Render(); RenderTexture.active = target;
        image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); RenderTexture.active = null;
        return image.GetPixels32();
    }
    static float Center(Color32[] pixels, int y)
    {
        int count = 0, sum = 0;
        for (int x=0;x<128;x++) if (pixels[y*128+x].r > 128) { count++; sum+=x; }
        Check(count > 4, "missing quad row " + y); return (float)sum/count;
    }
    public static void Run()
    {
        try {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_disintegration.mat");
            Check(original && original.shader.isSupported && !ShaderUtil.ShaderHasError(original.shader), "original material/shader unavailable");
            var material=new Material(original); var noise=new Texture2D(2,1,TextureFormat.RGBA32,false,true);
            noise.filterMode=FilterMode.Point; noise.wrapMode=TextureWrapMode.Clamp; noise.SetPixels(new[]{Color.white,Color.white}); noise.Apply();
            material.SetTexture("_NoiseTexture",noise); material.SetColor("_Color",Color.white);
            var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
            var normals=new Vector3[mesh.vertexCount]; for(int i=0;i<normals.Length;i++) normals[i]=Vector3.right; mesh.normals=normals;
            quad.GetComponent<MeshFilter>().sharedMesh=mesh; quad.GetComponent<Renderer>().sharedMaterial=material;
            var camera=new GameObject("dissolve check camera").AddComponent<Camera>(); camera.transform.position=new Vector3(0,0,-4); camera.orthographic=true;camera.orthographicSize=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);camera.targetTexture=target; var image=new Texture2D(128,128,TextureFormat.RGBA32,false);
            material.SetFloat("_DisplacementStrength",.2f); material.SetFloat("_DissolveAmount",0); var a=Render(camera,target,image);
            material.SetFloat("_DissolveAmount",Mathf.PI/20f); var b=Render(camera,target,image);
            float aBottom=Center(a,40),aTop=Center(a,88),bBottom=Center(b,40),bTop=Center(b,88);
            Check(aBottom>aTop+3 && bTop>bBottom+3, "expected phase-dependent wave: "+aBottom+","+aTop+" / "+bBottom+","+bTop);
            noise.SetPixels(new[]{Color.black,Color.white});noise.Apply();material.SetFloat("_DisplacementStrength",0);material.SetFloat("_DissolveAmount",.5f);
            material.SetTextureScale("_NoiseTexture",new Vector2(.1f,.1f));material.SetTextureOffset("_NoiseTexture",new Vector2(.9f,0));
            var clipped=Render(camera,target,image);
            Check(clipped[64*128+48].r < 10 && clipped[64*128+80].r > 128,"noise cutout must use unmodified UVs");
            noise.SetPixels(new[]{new Color(.5f,0,0,1),new Color(.5f,0,0,1)}); noise.Apply();
            material.enableInstancing=true; material.SetFloat("_DissolveAmount",0); material.SetFloat("_DisplacementStrength",0);
            quad.layer=30; quad.transform.position=new Vector3(0,10000,0); camera.transform.position=new Vector3(0,10000,-4); camera.cullingMask=1<<30; camera.backgroundColor=Color.blue;
            var isolated=Render(camera,target,image); var control=isolated[64*128+64];
            Check(control.r > 240 && control.g > 240 && control.b > 240, "isolated Android-style camera/material control: "+control);
            Debug.Log("DissolveInstancedShaderCheck passed: original material, wave phase and amplitude, cutout and untransformed UVs; centers="+aBottom+","+aTop+" / "+bBottom+","+bTop);
            EditorApplication.Exit(0);
        } catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
