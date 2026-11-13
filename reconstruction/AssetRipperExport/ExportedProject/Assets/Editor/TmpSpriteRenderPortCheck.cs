using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class TmpSpriteRenderPortCheck
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static void Run()
    {
        var cameraObject = new GameObject("sprite test camera"); var camera = cameraObject.AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black; camera.orthographic = true; camera.orthographicSize = 5; camera.transform.position = new Vector3(0, 0, -10);
        var rt = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
        var canvasObject = new GameObject("sprite test canvas", typeof(RectTransform), typeof(Canvas)); var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
        var textObject = new GameObject("sprite text", typeof(RectTransform), typeof(TextMeshProUGUI)); textObject.transform.SetParent(canvasObject.transform, false); var text = textObject.GetComponent<TextMeshProUGUI>();
        var rect = text.rectTransform; rect.sizeDelta = new Vector2(200, 160); rect.anchoredPosition = Vector2.zero;
        text.spriteAsset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>("Assets/Resources/sprite assets/EmojiOne.asset"); Check(text.spriteAsset != null, "original sprite atlas missing"); text.spriteAsset.UpdateLookupTables(); Debug.Log("sprite index ARROW=" + text.spriteAsset.GetSpriteIndexFromName("ARROW") + " glyphs=" + string.Join(",", text.spriteAsset.spriteGlyphTable.ConvertAll(x => x.index.ToString())) + " characters=" + string.Join(",", text.spriteAsset.spriteCharacterTable.ConvertAll(x => x.name + ":" + x.glyphIndex)));
        text.fontSize = 96; text.alignment = TextAlignmentOptions.Center; text.color = Color.white; text.text = "<sprite name=arrow>";
        Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(); Check(text.textInfo.characterCount > 0 && text.textInfo.characterInfo[0].textElement != null && text.textInfo.characterInfo[0].textElement.glyphIndex == 9, "game's named arrow tag resolves the original glyph");
        text.color = Color.red; text.text = "<sprite index=9 tint=1>";
        Canvas.ForceUpdateCanvases(); text.ForceMeshUpdate(); Debug.Log("sprite diagnostic count=" + text.textInfo.spriteCount + " chars=" + text.textInfo.characterCount + " materials=" + text.textInfo.materialCount + " glyph=" + (text.textInfo.characterCount > 0 && text.textInfo.characterInfo[0].textElement != null ? text.textInfo.characterInfo[0].textElement.glyphIndex.ToString() : "none")); camera.Render(); var image = new Texture2D(256, 256, TextureFormat.RGBA32, false); RenderTexture.active = rt; image.ReadPixels(new Rect(0, 0, 256, 256), 0, 0); image.Apply();
        int red = 0, white = 0; int minX = 256, maxX = -1, minY = 256, maxY = -1;
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++) { var c = image.GetPixel(x, y); if (c.r > .5f && c.g < .2f && c.b < .2f) { red++; minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y); } if (c.r > .7f && c.g > .7f && c.b > .7f) white++; }
        File.WriteAllBytes("/home/your-user/megabonk/build/android/tmp-sprite-render-diagnostic.png", image.EncodeToPNG());
        Check(red > 50 && white < 10, "sprite must respect vertex tint: red=" + red + " white=" + white);
        int blackInside = 0; for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++) { var c = image.GetPixel(x, y); if (c.r < .05f && c.g < .05f && c.b < .05f) blackInside++; }
        Check(blackInside > 25, "sprite transparency should reveal background within glyph bounds, black=" + blackInside);
        File.WriteAllBytes("/home/your-user/megabonk/build/android/tmp-sprite-render.png", image.EncodeToPNG()); RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(canvasObject); UnityEngine.Object.DestroyImmediate(cameraObject);
        Debug.Log("TmpSpriteRenderPortCheck passed: exported arrow sprite preserves transparency and tint; red=" + red + " transparent=" + blackInside);
    }
}
