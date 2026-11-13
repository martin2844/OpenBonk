using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TmpOverlayRenderPortCheck
{
    public static void Run()
    {
        var go = new GameObject("overlay test camera"); var camera = go.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.blue; camera.orthographic = true; camera.orthographicSize = 5f; camera.transform.position = new Vector3(0, 0, -10);
        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
        var root = new GameObject("overlay test canvas", typeof(RectTransform), typeof(Canvas)); var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        var child = new GameObject("text", typeof(RectTransform), typeof(TextMeshProUGUI)); child.transform.SetParent(root.transform, false); var text = child.GetComponent<TextMeshProUGUI>(); text.rectTransform.sizeDelta = new Vector2(220, 160);
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/MonoBehaviour/alagard SDF.asset"); text.fontSharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Material/alagard_AlwaysOnTop.mat");
        if (text.font == null || text.fontSharedMaterial == null) throw new Exception("original overlay font/material missing");
        text.fontSize = 90; text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.text = "X / E";
        Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(); camera.Render();
        var image = new Texture2D(256, 256, TextureFormat.RGBA32, false); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
        int white = 0, blue = 0; for (int y = 48; y < 208; y++) for (int x = 18; x < 238; x++) { var c = image.GetPixel(x,y); if (c.r > .75f && c.g > .75f && c.b > .75f) white++; if (c.b > .7f && c.r < .2f && c.g < .2f) blue++; }
        File.WriteAllBytes("/home/your-user/megabonk/build/android/tmp-overlay-render.png", image.EncodeToPNG());
        if (white < 100 || blue < 1000) throw new Exception("overlay should show white glyphs and transparent background: white=" + white + " blue=" + blue);
        RenderTexture.active = null; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(go);
        Debug.Log("TmpOverlayRenderPortCheck passed: original font and overlay material show white text and transparent background: white=" + white + " blue=" + blue);
    }
}
