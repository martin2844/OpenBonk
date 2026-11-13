using System;
using UnityEditor;
using UnityEngine;

public static class BadDogForceFieldShaderPortCheck
{
    public static void Run()
    {
        var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/DomeField.mat");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/CoffinTile.prefab");
        if (!source || !prefab || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Dome field material, prefab or shader unavailable");
        bool linked=false; foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true)) foreach (var mat in renderer.sharedMaterials) if (mat==source) linked=true;
        if (!linked) throw new Exception("Coffin tile does not reference DomeField");
        var material=new Material(source); var pattern=new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        material.SetTexture("_PatternTex",pattern); material.SetTextureScale("_PatternTex",Vector2.one); material.SetColor("_EmissionColor",Color.red);
        material.SetFloat("_Metallic",0); material.SetFloat("_Smoothness",0); material.SetFloat("_FresnelPower",1); material.SetFloat("_AlphaStrength",1);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
        var oblique = new Vector3(1, 0, -.2f).normalized;
        mesh.normals=new[] { oblique, oblique, oblique, oblique }; quad.GetComponent<MeshFilter>().sharedMesh=mesh;
        quad.transform.localScale=Vector3.one*2; quad.GetComponent<Renderer>().sharedMaterial=material;
        var cameraObject=new GameObject("forcefield check camera"); var cam=cameraObject.AddComponent<Camera>();
        cam.transform.position=new Vector3(0,0,-4); cam.transform.LookAt(Vector3.zero); cam.orthographic=true; cam.orthographicSize=1;
        cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Color.blue; cam.allowHDR=false; cam.allowMSAA=false; cam.depthTextureMode=DepthTextureMode.Depth;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image=new Texture2D(64,64,TextureFormat.RGBA32,false); cam.targetTexture=target;
        try {
            Color Render(Color p) { pattern.SetPixel(0,0,p); pattern.Apply(); cam.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
            var clear=Render(Color.black); var visible=Render(Color.white);
            if (clear.b<.95f || clear.r>.05f) throw new Exception("zero pattern must be transparent: "+clear);
            if (visible.r<.6f || visible.b>.7f) throw new Exception("white pattern and rim must reveal red emission: "+visible);
            mesh.normals=new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            var headOn=Render(Color.white);
            if (headOn.r>.1f || headOn.b<.9f) throw new Exception("head-on surface should have negligible rim: "+headOn);
            Debug.Log("BadDogForceFieldShaderPortCheck passed: original coffin binding, pattern alpha and rim rendering");
        } finally { cam.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(material); UnityEngine.Object.DestroyImmediate(pattern); }
    }
}
