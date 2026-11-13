using System;
using UnityEditor;
using UnityEngine;

public static class ToonStatusIridescencePortCheck
{
    public static void Run()
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/m_player_preset.mat");
        if (!source || !source.shader.isSupported || ShaderUtil.ShaderHasError(source.shader)) throw new Exception("Player shader unavailable");
        var material = new Material(source); material.SetTexture("_AlbedoMap", Texture2D.whiteTexture); material.SetColor("_AlbedoColor", Color.white);
        material.SetTexture("_IridescenceRamp", Texture2D.whiteTexture); material.SetFloat("_IridescenceSize", .18f); material.SetFloat("_IridescenceSmoothness", .5f);
        material.SetColor("_IridescenceColor", new Color(1,0,0,1)); material.SetFloat("_AlphaClipping",0); material.SetFloat("_Outline",0); material.SetFloat("_Rim",0);
        var model = GameObject.CreatePrimitive(PrimitiveType.Sphere); model.GetComponent<Renderer>().sharedMaterial = material;
        var cameraObject = new GameObject("iridescence camera"); var camera = cameraObject.AddComponent<Camera>(); camera.transform.position = new Vector3(0,0,-4); camera.transform.LookAt(Vector3.zero); camera.orthographic = true; camera.orthographicSize = .75f; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.allowHDR = false; camera.allowMSAA = false;
        var lightObject = new GameObject("iridescence light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.identity;
        var target = new RenderTexture(128,128,24,RenderTextureFormat.ARGB32); var image = new Texture2D(128,128,TextureFormat.RGBA32,false); camera.targetTexture = target;
        try {
            Color32[] Render(bool enabled) { if (enabled) material.EnableKeyword("_MK_IRIDESCENCE_DEFAULT"); else material.DisableKeyword("_MK_IRIDESCENCE_DEFAULT"); camera.Render(); RenderTexture.active = target; image.ReadPixels(new Rect(0,0,128,128),0,0); image.Apply(); return image.GetPixels32(); }
            var without = Render(false); var with = Render(true); int changed=0, red=0;
            for (int i=0;i<with.Length;i++) { if (Math.Abs(with[i].r-without[i].r)+Math.Abs(with[i].g-without[i].g)+Math.Abs(with[i].b-without[i].b)>20) changed++; if (with[i].r>with[i].g+20 && with[i].r>with[i].b+20) red++; }
            if (changed<200 || red<200) throw new Exception($"Iridescence variant not visible: changed={changed}, red={red}");
            Debug.Log($"ToonStatusIridescencePortCheck passed: shader variant changes {changed} pixels, red pixels={red}");
        } finally { RenderTexture.active=null; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(lightObject); UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(model); UnityEngine.Object.DestroyImmediate(material); }
    }
}
