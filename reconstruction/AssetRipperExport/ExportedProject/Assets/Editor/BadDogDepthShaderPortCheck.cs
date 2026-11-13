using System;
using UnityEditor;
using UnityEngine;

public static class BadDogDepthShaderPortCheck
{
    public static void Run()
    {
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/DomeField.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Dome shader unavailable");
        var fieldMaterial=new Material(source); var pattern=new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        pattern.SetPixel(0,0,Color.white); pattern.Apply(); fieldMaterial.SetTexture("_PatternTex",pattern);
        fieldMaterial.SetColor("_EmissionColor",Color.red); fieldMaterial.SetFloat("_Metallic",0); fieldMaterial.SetFloat("_Smoothness",0);
        fieldMaterial.SetFloat("_DepthOffset",0); fieldMaterial.SetFloat("_FresnelPower",1); fieldMaterial.SetFloat("_AlphaStrength",1);
        var field=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=UnityEngine.Object.Instantiate(field.GetComponent<MeshFilter>().sharedMesh);
        mesh.normals=new[] { Vector3.back,Vector3.back,Vector3.back,Vector3.back }; field.GetComponent<MeshFilter>().sharedMesh=mesh;
        field.transform.localScale=Vector3.one*4; field.GetComponent<Renderer>().sharedMaterial=fieldMaterial;
        var back=GameObject.CreatePrimitive(PrimitiveType.Quad); back.transform.localScale=Vector3.one*10;
        var backMat=new Material(Shader.Find("Standard")); backMat.color=Color.blue; backMat.EnableKeyword("_EMISSION"); backMat.SetColor("_EmissionColor",Color.blue);
        back.GetComponent<Renderer>().sharedMaterial=backMat;
        var cameraObject=new GameObject("depth field check camera"); var cam=cameraObject.AddComponent<Camera>();
        cam.transform.position=new Vector3(0,0,-4); cam.transform.LookAt(Vector3.zero); cam.fieldOfView=30; cam.clearFlags=CameraClearFlags.SolidColor;
        cam.backgroundColor=Color.blue; cam.allowHDR=false; cam.allowMSAA=false; cam.depthTextureMode=DepthTextureMode.Depth;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image=new Texture2D(64,64,TextureFormat.RGBA32,false); cam.targetTexture=target;
        try {
            Color Render(float z) { back.transform.position=new Vector3(0,0,z); cam.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
            var far=Render(3f); var near=Render(.1f);
            if(far.r>.12f || far.b<.8f) throw new Exception("far surface should not form an intersection: "+far);
            if(near.r<.6f || near.b>.5f) throw new Exception("near surface should produce a visible depth intersection: "+near);
            Debug.Log($"BadDogDepthShaderPortCheck passed: real camera depth, far={far}, near={near}");
        } finally { cam.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(back); UnityEngine.Object.DestroyImmediate(backMat); UnityEngine.Object.DestroyImmediate(field); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(fieldMaterial); UnityEngine.Object.DestroyImmediate(pattern); }
    }
}
