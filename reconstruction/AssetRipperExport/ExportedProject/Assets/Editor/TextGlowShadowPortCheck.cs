using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TextGlowShadowPortCheck
{
    static void Check(bool ok,string message) { if(!ok) throw new Exception("TextGlowShadowPortCheck: "+message); }
    public static void Run() {
        var beam=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/Atlas_EmojisGlow.mat");
        var toon=AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_ent3.mat");
        Check(beam && toon && beam.shader.isSupported && !ShaderUtil.ShaderHasError(beam.shader),"materials or shader unavailable");
        var receiverMaterial=new Material(toon); receiverMaterial.SetTexture("_AlbedoMap",Texture2D.whiteTexture); receiverMaterial.SetColor("_AlbedoColor",Color.white); receiverMaterial.SetColor("_GoochBrightColor",Color.white); receiverMaterial.SetColor("_GoochDarkColor",Color.black); receiverMaterial.SetFloat("_LightThreshold",.05f); receiverMaterial.SetFloat("_DiffuseSmoothness",.1f); receiverMaterial.SetFloat("_Specular",0); receiverMaterial.SetFloat("_Rim",0); receiverMaterial.SetFloat("_Outline",1); receiverMaterial.SetFloat("_ReceiveShadows",1);
        var receiver=GameObject.CreatePrimitive(PrimitiveType.Quad); receiver.transform.localScale=Vector3.one*3; receiver.GetComponent<Renderer>().sharedMaterial=receiverMaterial;
        var casterMaterial=new Material(beam); var texture=new Texture2D(1,1,TextureFormat.RGBA32,false,true); texture.SetPixel(0,0,Color.white);texture.Apply(); casterMaterial.SetTexture("_TextureSample",texture);casterMaterial.SetTextureScale("_TextureSample",Vector2.one);
        var caster=GameObject.CreatePrimitive(PrimitiveType.Cube);caster.transform.position=new Vector3(.8f,0,-1);caster.transform.localScale=Vector3.one*.6f;
        var mesh=UnityEngine.Object.Instantiate(caster.GetComponent<MeshFilter>().sharedMesh);var colors=new Color[mesh.vertexCount];for(int i=0;i<colors.Length;i++) colors[i]=Color.white;mesh.colors=colors;caster.GetComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=caster.GetComponent<Renderer>();renderer.sharedMaterial=casterMaterial;renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
        var cameraObject=new GameObject("TextGlow shadow camera");var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(0,0,-4);camera.transform.LookAt(Vector3.zero);camera.orthographic=true;camera.orthographicSize=2;camera.renderingPath=RenderingPath.Forward;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var lightObject=new GameObject("TextGlow shadow light");var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.renderMode=LightRenderMode.ForcePixel;light.transform.rotation=Quaternion.LookRotation(new Vector3(-.8f,0,1));light.intensity=1;light.shadowStrength=1;light.shadowBias=.01f;light.shadowNormalBias=0;
        var oldMode=RenderSettings.ambientMode;var oldAmbient=RenderSettings.ambientLight;var oldShadows=QualitySettings.shadows;var oldDistance=QualitySettings.shadowDistance;var oldCascades=QualitySettings.shadowCascades;
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;QualitySettings.shadows=UnityEngine.ShadowQuality.All;QualitySettings.shadowDistance=30;QualitySettings.shadowCascades=1;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);var image=new Texture2D(128,128,TextureFormat.RGBA32,false);camera.targetTexture=target;
        try {
            Color[] Capture() { camera.Render();var old=RenderTexture.active;RenderTexture.active=target;image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();RenderTexture.active=old;return image.GetPixels(); }
            light.shadows=LightShadows.None;var reference=Capture();light.shadows=LightShadows.Hard;var opaque=Capture();
            for(int i=0;i<colors.Length;i++) colors[i]=new Color(1,1,1,0); mesh.colors=colors;
            var vertexTransparent=Capture();
            texture.SetPixel(0,0,new Color(1,1,1,0));texture.Apply();var transparent=Capture();
            int opaqueDark=0,vertexDark=0,transparentDark=0;for(int i=0;i<reference.Length;i++){if(reference[i].r-opaque[i].r>.15f)opaqueDark++;if(reference[i].r-vertexTransparent[i].r>.15f)vertexDark++;if(reference[i].r-transparent[i].r>.15f)transparentDark++;}
            Check(opaqueDark>20 && vertexDark>opaqueDark*.8f && transparentDark<opaqueDark*.2f,$"alpha shadow mismatch opaque={opaqueDark} vertexZero={vertexDark} transparent={transparentDark}");
            Debug.Log($"TextGlowShadowPortCheck passed: original TextGlow material with controlled texture; opaque={opaqueDark}, vertexZero={vertexDark}, transparent={transparentDark}");
        } finally {camera.targetTexture=null;RenderTexture.active=null;QualitySettings.shadows=oldShadows;QualitySettings.shadowDistance=oldDistance;QualitySettings.shadowCascades=oldCascades;RenderSettings.ambientMode=oldMode;RenderSettings.ambientLight=oldAmbient;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(lightObject);UnityEngine.Object.DestroyImmediate(caster);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(casterMaterial);UnityEngine.Object.DestroyImmediate(receiver);UnityEngine.Object.DestroyImmediate(receiverMaterial);}
    }
}
