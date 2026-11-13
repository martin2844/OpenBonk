using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class UnderlineRenderDiagnostic
{
    public static void Run()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MonoBehaviour/alagard SDF.asset");
        var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/alagard SDF Material.mat");
        if (font == null || mat == null || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null) throw new Exception("Missing font/material or graphics device");
        var root = new GameObject("text diagnostic"); var cam = root.AddComponent<Camera>(); cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f,0.2f,0.2f,1); cam.orthographicSize = 100; cam.transform.position = new Vector3(0,0,-10);
        var rt = new RenderTexture(600, 200, 24); cam.targetTexture = rt;
        var canvasObject = new GameObject("canvas", typeof(Canvas)); canvasObject.transform.SetParent(root.transform); var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = cam; canvas.planeDistance = 1;
        var textObject = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI)); textObject.transform.SetParent(canvasObject.transform, false); var text = textObject.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = 56; text.alignment = TextAlignmentOptions.Center; text.text = "PLAY"; text.rectTransform.sizeDelta = new Vector2(500,150); text.color = Color.white;
        try
        {
            foreach (var shaderName in new[] {"TextMeshPro/Distance Field", "TextMeshPro/Mobile/Distance Field", "TextMeshPro/Distance Field Overlay"})
            foreach (bool bold in new[] {false, true})
            foreach (bool underlay in new[] {true, false})
            {
                var shader = Shader.Find(shaderName); if (shader == null || !shader.isSupported) throw new Exception("Unsupported shader: " + shaderName);
                var variant = new Material(mat); variant.shader = shader; if (!underlay) variant.DisableKeyword("UNDERLAY_ON"); text.fontSharedMaterial = variant; text.fontStyle = FontStyles.UpperCase | (bold ? FontStyles.Bold : FontStyles.Normal); text.ForceMeshUpdate(true, true); Canvas.ForceUpdateCanvases();
                int vertices = 0; foreach (var mesh in text.textInfo.meshInfo) vertices += mesh.vertexCount;
                if (vertices != 16) throw new Exception("Unexpected glyph mesh");
                for (int i = 0; i < text.textInfo.characterCount; i++)
                {
                    var c = text.textInfo.characterInfo[i]; var mesh = text.textInfo.meshInfo[c.materialReferenceIndex]; int v = c.vertexIndex; float scale = mesh.uvs0[v].w;
                    if ((bold && scale >= 0) || (!bold && scale <= 0) || mesh.uvs0[v+1].w != scale) throw new Exception("Unexpected signed glyph scale");
                }
                Debug.Log($"UNDERLINE-RENDER shader={shaderName} bold={bold} underlay={underlay} vertices={vertices} signedScale={text.textInfo.meshInfo[0].uvs0[0].w}");
                cam.Render(); RenderTexture.active = rt; var img = new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false); img.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); img.Apply(); string shaderId = shaderName.Contains("Mobile") ? "mobile" : shaderName.Contains("Overlay") ? "overlay" : "standard"; File.WriteAllBytes("/home/your-user/megabonk/build/android/underline-fixed-"+shaderId+"-"+(bold ? "bold" : "normal")+"-"+(underlay ? "on" : "off")+".png", img.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(img); UnityEngine.Object.DestroyImmediate(variant);
            }
            var underlineMaterial = new Material(mat); text.fontSharedMaterial = underlineMaterial; text.fontStyle = FontStyles.Normal; text.text = "<u>PLAY</u>"; text.ForceMeshUpdate(true, true); Canvas.ForceUpdateCanvases();
            int underlineVertices = 0; foreach (var mesh in text.textInfo.meshInfo) underlineVertices += mesh.vertexCount;
            if (underlineVertices != 28) throw new Exception("Intentional underline geometry missing: " + underlineVertices);
            cam.Render(); RenderTexture.active = rt; var underlined = new Texture2D(rt.width,rt.height,TextureFormat.RGBA32,false); underlined.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); underlined.Apply(); File.WriteAllBytes("/home/your-user/megabonk/build/android/underline-fixed-intentional.png", underlined.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(underlined); UnityEngine.Object.DestroyImmediate(underlineMaterial);
            Debug.Log("UnderlineRenderDiagnostic passed: all three shaders, normal/bold signed scale, underlay variants and intentional underline rendered");
        }
        finally { RenderTexture.active = null; cam.targetTexture = null; UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(root); }
    }
}
