using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class ToonShadowPortCheck
{
    static void Check(bool b, string m) { if (!b) throw new Exception("ToonShadowPortCheck: " + m); }
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_ent3.mat"); Check(source && source.shader.isSupported, "original material and supported shader");
        var mat = new Material(source); mat.SetTexture("_AlbedoMap", Texture2D.whiteTexture); mat.SetColor("_AlbedoColor", Color.white); mat.SetColor("_GoochBrightColor", Color.white); mat.SetColor("_GoochDarkColor", Color.black); mat.SetFloat("_LightThreshold", .05f); mat.SetFloat("_DiffuseSmoothness", .1f); mat.SetFloat("_Specular", 0); mat.SetFloat("_Rim", 0); mat.SetFloat("_Outline", 1); mat.SetFloat("_ReceiveShadows", 1);
        var receiver = GameObject.CreatePrimitive(PrimitiveType.Quad); receiver.transform.localScale = new Vector3(3,3,1); receiver.GetComponent<Renderer>().sharedMaterial = mat; receiver.GetComponent<Renderer>().receiveShadows = true;
        var casterMat = new Material(mat); var cutoutTexture = new Texture2D(2,1,TextureFormat.RGBA32,false); cutoutTexture.filterMode = FilterMode.Point; cutoutTexture.wrapMode = TextureWrapMode.Clamp; cutoutTexture.SetPixels(new[]{Color.white,new Color(1,1,1,0)}); cutoutTexture.Apply(); casterMat.SetTexture("_AlbedoMap",cutoutTexture);
        var caster = GameObject.CreatePrimitive(PrimitiveType.Cube); caster.transform.position = new Vector3(.8f,0,-1); caster.transform.localScale = Vector3.one * .6f; var casterRenderer = caster.GetComponent<Renderer>(); casterRenderer.sharedMaterial = casterMat; casterRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        var casterMesh = UnityEngine.Object.Instantiate(caster.GetComponent<MeshFilter>().sharedMesh); caster.GetComponent<MeshFilter>().sharedMesh = casterMesh;
        var cameraObject = new GameObject("shadow camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = 2; camera.renderingPath = RenderingPath.Forward; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var lightObject = new GameObject("shadow light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.renderMode = LightRenderMode.ForcePixel; light.transform.rotation = Quaternion.LookRotation(new Vector3(-.8f,0,1)); light.intensity = 1; light.shadowStrength = 1; light.shadowBias = .01f; light.shadowNormalBias = 0;
        var oldMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight; var oldShadows = QualitySettings.shadows; var oldDistance = QualitySettings.shadowDistance; var oldCascades = QualitySettings.shadowCascades;
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = Color.black; QualitySettings.shadows = UnityEngine.ShadowQuality.All; QualitySettings.shadowDistance = 30; QualitySettings.shadowCascades = 1;
        var rt = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); camera.targetTexture = rt; var image = new Texture2D(128,128,TextureFormat.RGBA32,false);
        try {
            light.shadows = LightShadows.None; camera.Render(); RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var unshadowed = image.GetPixels();
            light.shadows = LightShadows.Hard; camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var shadowed = image.GetPixels(); int darker = 0; float max = 0;
            for (int i=0;i<shadowed.Length;i++) { float difference = unshadowed[i].r-shadowed[i].r; if (difference > .15f) darker++; max = Mathf.Max(max, difference); }
            File.WriteAllBytes("/home/your-user/megabonk/build/android/toon-shadow-check.png", image.EncodeToPNG());
            Check(darker > 20, "caster produces a visible shadow on receiver, darker=" + darker + " max=" + max);
            casterMat.SetFloat("_AlphaClipping",1); casterMat.SetFloat("_AlphaCutoff",.5f); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var cutout=image.GetPixels(); int cutoutDarker=0; for(int i=0;i<cutout.Length;i++) if (unshadowed[i].r-cutout[i].r>.15f) cutoutDarker++;
            Check(cutoutDarker < darker * .9f && cutoutDarker > 0, "alpha cutout changes caster shadow: opaque="+darker+" cutout="+cutoutDarker);
            casterMat.DisableKeyword("_MK_ALBEDO_MAP");
            var colors = new Color[casterMesh.vertexCount]; for (int i=0;i<colors.Length;i++) colors[i] = new Color(1,1,1,0); casterMesh.colors = colors;
            camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var vertexClear=image.GetPixels(); int clearDarker=0; for(int i=0;i<vertexClear.Length;i++) if(unshadowed[i].r-vertexClear[i].r>.15f) clearDarker++;
            for (int i=0;i<colors.Length;i++) colors[i] = Color.white; casterMesh.colors = colors;
            camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var vertexOpaque=image.GetPixels(); int vertexDarker=0; for(int i=0;i<vertexOpaque.Length;i++) if(unshadowed[i].r-vertexOpaque[i].r>.15f) vertexDarker++;
            Check(clearDarker < 10 && vertexDarker > 20, "vertex alpha controls untextured shadow: clear="+clearDarker+" opaque="+vertexDarker);
            mat.SetFloat("_ReceiveShadows", 0); camera.Render(); image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); var ignored=image.GetPixels(); int remaining=0; for(int i=0;i<ignored.Length;i++) if (unshadowed[i].r-ignored[i].r>.15f) remaining++;
            Check(remaining < 10, "receiver can disable shadows, remaining="+remaining);
            Debug.Log("ToonShadowPortCheck passed: directional shadows, alpha-clipped caster and configurable receiver");
        } finally { RenderTexture.active = null; QualitySettings.shadows = oldShadows; QualitySettings.shadowDistance = oldDistance; QualitySettings.shadowCascades = oldCascades; RenderSettings.ambientMode = oldMode; RenderSettings.ambientLight = oldAmbient; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(cutoutTexture); UnityEngine.Object.DestroyImmediate(casterMat); UnityEngine.Object.DestroyImmediate(mat); UnityEngine.Object.DestroyImmediate(casterMesh); UnityEngine.Object.DestroyImmediate(receiver); UnityEngine.Object.DestroyImmediate(caster); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject); }
    }
}
