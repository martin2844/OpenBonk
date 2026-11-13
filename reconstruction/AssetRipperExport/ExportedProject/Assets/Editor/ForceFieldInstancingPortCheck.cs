using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ForceFieldInstancingPortCheck
{
    public static void Run()
    {
        var original=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/WarningField.mat");
        if (original==null || !original.enableInstancing || !SystemInfo.supportsInstancing || !original.shader.isSupported) throw new Exception("ForceField fixture needs original material and GPU instancing");
        var material=new Material(original); material.SetFloat("_RimPower",0f); material.SetFloat("_IntersectionPower",0f); material.enableInstancing=true;
        var cameraObject=new GameObject("forcefield instance camera"); var camera=cameraObject.AddComponent<Camera>();
        camera.orthographic=true; camera.orthographicSize=1f; camera.transform.position=new Vector3(0f,0f,-5f); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black; camera.allowHDR=false; camera.allowMSAA=false;
        var primitive=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=primitive.GetComponent<MeshFilter>().sharedMesh; primitive.SetActive(false);
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image=new Texture2D(128,128,TextureFormat.RGBA32,false,true); camera.targetTexture=target;
        var props=new MaterialPropertyBlock(); props.SetVectorArray("_MainColor",new[]{new Vector4(1,0,0,1),new Vector4(0,1,0,1)});
        var matrices=new[]{Matrix4x4.Translate(new Vector3(-.5f,0f,0f)),Matrix4x4.Translate(new Vector3(.5f,0f,0f))};
        var commands=new CommandBuffer();
        try
        {
            commands.DrawMeshInstanced(mesh,0,material,0,matrices,2,props);
            camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); camera.Render();
            RenderTexture.active=target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply();
            Color left=image.GetPixel(32,64), right=image.GetPixel(96,64);
            if (!(left.r>.75f && left.g<.1f && right.g>.75f && right.r<.1f)) throw new Exception("Per-instance color or transform mismatch: "+left+" / "+right);
            Debug.Log("ForceFieldInstancingPortCheck passed: actual WarningField material, two instance transforms and independent colors "+left+" / "+right);
        }
        finally
        {
            camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha,commands); commands.Release(); RenderTexture.active=null; camera.targetTexture=null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(primitive); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(material);
        }
    }
}
