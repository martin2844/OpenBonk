using System;
using UnityEditor;
using UnityEngine;

public static class TextGlowShaderPortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Atlas_EmojisGlow.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("TextGlow shader unavailable or has errors");
        foreach (var name in new[] { "BonkEffect", "ExecuteEffect" }) {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/GameObject/{name}.prefab");
            if (!prefab) throw new Exception("Missing prefab " + name);
            bool linked=false;
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true)) foreach (var material in renderer.sharedMaterials) if (material==source) linked=true;
            if (!linked) throw new Exception("Missing TextGlow material binding on " + name);
        }
        var mat=new Material(source); var tex=new Texture2D(1,1,TextureFormat.RGBA32,false,true); var pan=new Texture2D(1,1,TextureFormat.RGBA32,false,true);
        mat.SetTexture("_TextureSample",tex); mat.SetTexture("_PanningTexture",pan); mat.SetFloat("_CenterGlow",1f); mat.SetVector("_Speed",Vector4.zero);
        var quad=GameObject.CreatePrimitive(PrimitiveType.Quad); var mesh=UnityEngine.Object.Instantiate(quad.GetComponent<MeshFilter>().sharedMesh);
        mesh.colors=new[] { new Color(.5f,.5f,.5f,0), new Color(.5f,.5f,.5f,0), new Color(.5f,.5f,.5f,0), new Color(.5f,.5f,.5f,0) };
        quad.GetComponent<MeshFilter>().sharedMesh=mesh; quad.transform.localScale=Vector3.one*2f; quad.GetComponent<Renderer>().sharedMaterial=mat;
        var camObj=new GameObject("TextGlow test camera"); var cam=camObj.AddComponent<Camera>(); cam.transform.position=new Vector3(0,0,-4); cam.transform.LookAt(Vector3.zero);
        cam.orthographic=false; cam.fieldOfView=60; cam.clearFlags=CameraClearFlags.SolidColor; cam.backgroundColor=Color.blue; cam.allowHDR=false; cam.allowMSAA=false;
        var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGB32); var image=new Texture2D(64,64,TextureFormat.RGBA32,false); cam.targetTexture=target;
        bool oldFog=RenderSettings.fog; var oldMode=RenderSettings.fogMode; var oldColor=RenderSettings.fogColor;
        float oldStart=RenderSettings.fogStartDistance, oldEnd=RenderSettings.fogEndDistance, oldDensity=RenderSettings.fogDensity;
        bool oldLinear=Shader.IsKeywordEnabled("FOG_LINEAR"), oldExp=Shader.IsKeywordEnabled("FOG_EXP"), oldExp2=Shader.IsKeywordEnabled("FOG_EXP2");
        try {
            RenderSettings.fog=false; Shader.DisableKeyword("FOG_LINEAR"); Shader.DisableKeyword("FOG_EXP"); Shader.DisableKeyword("FOG_EXP2");
            Color Render(Color color, Color p) { tex.SetPixel(0,0,color); tex.Apply(); pan.SetPixel(0,0,p); pan.Apply(); cam.Render(); RenderTexture.active=target; image.ReadPixels(new Rect(0,0,64,64),0,0); image.Apply(); return image.GetPixel(32,32); }
            var clear=Render(new Color(.25f,0,0,0),Color.black);
            var opaque=Render(new Color(.25f,0,0,1),Color.black);
            var bright=Render(new Color(.25f,0,0,1),Color.white);
            if (clear.b<.95f || clear.r>.05f) throw new Exception("transparent texel must reveal background: "+clear);
            if (opaque.r<.08f || opaque.b>.05f) throw new Exception("opaque texel must render even with zero vertex alpha: "+opaque);
            if (bright.r<=opaque.r+.02f) throw new Exception("panning texture must brighten red: "+opaque+" / "+bright);
            RenderSettings.fog=true; RenderSettings.fogColor=Color.green;
            RenderSettings.fogStartDistance=0f; RenderSettings.fogEndDistance=1f; RenderSettings.fogDensity=4f;
            foreach (var entry in new[]{(FogMode.Linear,"FOG_LINEAR"),(FogMode.Exponential,"FOG_EXP"),(FogMode.ExponentialSquared,"FOG_EXP2")}) {
                RenderSettings.fogMode=entry.Item1;
                foreach(var keyword in new[]{"FOG_LINEAR","FOG_EXP","FOG_EXP2"}) { if(keyword==entry.Item2) Shader.EnableKeyword(keyword); else Shader.DisableKeyword(keyword); }
                var fogged=Render(new Color(.25f,0,0,1),Color.white);
                if (fogged.g<.8f || fogged.r>.15f) throw new Exception("distant glow must use fog color for "+entry.Item2+": "+fogged);
                Debug.Log("TextGlow fog mode="+entry.Item2+" color="+fogged);
            }
            Debug.Log("TextGlowShaderPortCheck passed: Bonk and Execute bindings, sampled alpha, vertex alpha, glow and three fog modes");
        } finally { if(oldLinear) Shader.EnableKeyword("FOG_LINEAR"); else Shader.DisableKeyword("FOG_LINEAR"); if(oldExp) Shader.EnableKeyword("FOG_EXP"); else Shader.DisableKeyword("FOG_EXP"); if(oldExp2) Shader.EnableKeyword("FOG_EXP2"); else Shader.DisableKeyword("FOG_EXP2"); RenderSettings.fog=oldFog; RenderSettings.fogMode=oldMode; RenderSettings.fogColor=oldColor; RenderSettings.fogStartDistance=oldStart; RenderSettings.fogEndDistance=oldEnd; RenderSettings.fogDensity=oldDensity; cam.targetTexture=null; RenderTexture.active=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(camObj); UnityEngine.Object.DestroyImmediate(quad); UnityEngine.Object.DestroyImmediate(mesh); UnityEngine.Object.DestroyImmediate(mat); UnityEngine.Object.DestroyImmediate(tex); UnityEngine.Object.DestroyImmediate(pan); }
    }
}
