using System;
using UnityEditor;
using UnityEngine;

public static class ToonEmissionShaderCheck
{
    static void Require(bool value, string why) { if (!value) throw new Exception("ToonEmissionShaderCheck: " + why); }
    public static void Run()
    {
        var ghost = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_fox_ghost.mat");
        var pylon = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_pylon.mat");
        var altar = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_energyAltar.mat");
        Require(ghost && pylon && System.IO.File.ReadAllText("Assets/Material/m_fox_ghost.mat").Contains("- _MK_EMISSION") && System.IO.File.ReadAllText("Assets/Material/m_pylon.mat").Contains("- _MK_EMISSION_MAP"), "original material keyword records");
        Require(ghost.IsKeywordEnabled("_MK_EMISSION") && !ghost.IsKeywordEnabled("_MK_EMISSION_MAP") && pylon.IsKeywordEnabled("_MK_EMISSION") && pylon.IsKeywordEnabled("_MK_EMISSION_MAP"), "Unity loaded material keyword variants");
        var obj = GameObject.CreatePrimitive(PrimitiveType.Quad); var renderer = obj.GetComponent<Renderer>();
        var cameraObject = new GameObject("emission camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(0, 0, -3); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
        var oldMode = RenderSettings.ambientMode; var oldAmbient = RenderSettings.ambientLight;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = Color.black;
        var lightObject = new GameObject("emission test light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 0f;
        var rt = new RenderTexture(64, 64, 24, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear); camera.targetTexture = rt;
        var pixels = new Texture2D(64, 64, TextureFormat.RGBAFloat, false, true);
        var mat = new Material(ghost); renderer.sharedMaterial = mat;
        try {
            mat.SetFloat("_Outline", 1); mat.SetFloat("_Rim", 0); mat.SetFloat("_AlphaClipping", 0); mat.SetFloat("_RenderFace", 0);
            mat.SetColor("_AlbedoColor", Color.black); mat.SetColor("_EmissionColor", new Color(.6f, .2f, .1f, 1)); mat.SetFloat("_IndirectFade", 1);
            Color Read() { camera.Render(); RenderTexture.active = rt; pixels.ReadPixels(new Rect(0, 0, 64, 64), 0, 0); pixels.Apply(); return pixels.GetPixel(32, 32); }
            mat.DisableKeyword("_MK_EMISSION"); mat.DisableKeyword("_MK_EMISSION_MAP"); Color disabled = Read();
            mat.EnableKeyword("_MK_EMISSION"); Color constant = Read();
            mat.EnableKeyword("_MK_EMISSION_MAP"); mat.SetTexture("_EmissionMap", Texture2D.blackTexture); Color black = Read();
            mat.SetTexture("_EmissionMap", Texture2D.whiteTexture); Color white = Read();
            Require(disabled.r < .01f && constant.r > .25f && constant.g > .02f, $"constant emission disabled={disabled} enabled={constant}");
            Require(black.r < .03f && Mathf.Abs(white.r - constant.r) < .04f && Mathf.Abs(white.g - constant.g) < .04f, $"emission map black={black} white={white} constant={constant}");
            var actualPylon = new Material(pylon);
            try {
                renderer.sharedMaterial = actualPylon; actualPylon.SetFloat("_Outline", 1); actualPylon.SetFloat("_Rim", 0); actualPylon.SetFloat("_RenderFace", 0); actualPylon.SetColor("_AlbedoColor", Color.black); actualPylon.SetFloat("_IndirectFade", 1);
                actualPylon.DisableKeyword("_MK_EMISSION"); Read(); Color[] unlit = pixels.GetPixels();
                actualPylon.EnableKeyword("_MK_EMISSION"); Read(); Color[] lit = pixels.GetPixels(); int changed = 0;
                for (int i = 0; i < lit.Length; i++) if (lit[i].g > unlit[i].g + .05f || lit[i].b > unlit[i].b + .05f) changed++;
                Require(changed > 0, "actual pylon emission texture renders, changed=" + changed);
                Debug.Log($"ToonEmissionShaderCheck passed: disabled={disabled} constant={constant} blackMap={black} whiteMap={white} pylonPixels={changed}");
            } finally { UnityEngine.Object.DestroyImmediate(actualPylon); }
            Require(altar && altar.IsKeywordEnabled("_MK_EMISSION") && altar.IsKeywordEnabled("_MK_EMISSION_MAP"), "charge shrine emission variants");
            var shrineMaterial = new Material(altar);
            try {
                renderer.sharedMaterial = shrineMaterial; shrineMaterial.SetFloat("_Outline", 1); shrineMaterial.SetFloat("_Rim", 0); shrineMaterial.SetFloat("_RenderFace", 0); shrineMaterial.SetColor("_AlbedoColor", Color.black); shrineMaterial.SetFloat("_IndirectFade", 1);
                var block = new MaterialPropertyBlock();
                block.SetColor("_EmissionColor", Color.black); renderer.SetPropertyBlock(block); Read(); Color[] empty = pixels.GetPixels();
                block.SetColor("_EmissionColor", new Color(1.5f, 1.5f, 1.5f, 1)); renderer.SetPropertyBlock(block); Read(); Color[] charged = pixels.GetPixels();
                int changed = 0; for (int i = 0; i < charged.Length; i++) if (charged[i].r > empty[i].r + .05f || charged[i].g > empty[i].g + .05f || charged[i].b > empty[i].b + .05f) changed++;
                Require(changed > 0, "charge shrine material property block renders emission mask, changed=" + changed);
                Debug.Log($"ToonEmissionShaderCheck shrine passed: propertyBlockEmissionPixels={changed}");
            } finally { renderer.SetPropertyBlock(null); UnityEngine.Object.DestroyImmediate(shrineMaterial); }
        } finally { RenderTexture.active = null; camera.targetTexture = null; RenderSettings.ambientMode = oldMode; RenderSettings.ambientLight = oldAmbient; UnityEngine.Object.DestroyImmediate(mat); UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(obj); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(lightObject); }
    }
}
