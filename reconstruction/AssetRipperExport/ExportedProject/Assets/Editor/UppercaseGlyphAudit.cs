using System;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class UppercaseGlyphAudit
{
    public static void Run()
    {
        LocalizationSettings.InitializationOperation.WaitForCompletion();
        var original = LocalizationSettings.SelectedLocale;
        GameObject root = null;
        try
        {
            var locale = LocalizationSettings.AvailableLocales.GetLocale("es-419");
            if (locale == null) throw new Exception("es-419 unavailable");
            LocalizationSettings.SelectedLocale = locale;
            EditorSceneManager.OpenScene("Assets/ScenesAndroid/MainMenu.unity");
            var prewarmer = UnityEngine.Object.FindObjectOfType<LocalizationPrewarmer>(true);
            if (prewarmer == null || prewarmer.mainFont == null || prewarmer.notoDefault == null) throw new Exception("Font setup unavailable");
            prewarmer.PrewarmFontAsset(prewarmer.notoDefault, locale);
            root = new GameObject("uppercase glyph audit", typeof(RectTransform), typeof(Canvas));
            var go = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(root.transform, false);
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = prewarmer.mainFont;
            text.fontStyle = FontStyles.UpperCase; text.text = "Daño"; text.ForceMeshUpdate();
            Debug.Log($"Uppercase glyph audit: primary={text.font.name} sourceHasUpper={text.font.HasCharacter('Ñ')} fallback={prewarmer.notoDefault.name} fallbackHasUpper={prewarmer.notoDefault.HasCharacter('Ñ')} chars={text.textInfo.characterCount}");
            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                var c = text.textInfo.characterInfo[i];
                Debug.Log($"Uppercase glyph audit: index={i} char={(int)c.character:X4} unicode={(c.textElement != null ? c.textElement.unicode.ToString("X4") : "null")} font={(c.fontAsset != null ? c.fontAsset.name : "null")} visible={c.isVisible} advance={c.xAdvance}");
                if (i == 2 && c.textElement != null && c.textElement.glyph != null)
                {
                    var glyph = c.textElement.glyph;
                    var texture = c.fontAsset.atlasTextures[glyph.atlasIndex];
                    var r = glyph.glyphRect;
                    var pixels = texture.GetPixels32(); int min = 255, max = 0;
                    for (int y = r.y; y < r.y + r.height; y++) for (int x = r.x; x < r.x + r.width; x++)
                    { int a = pixels[y * texture.width + x].a; min = Math.Min(min, a); max = Math.Max(max, a); }
                    Debug.Log($"Uppercase glyph audit: atlas={glyph.atlasIndex} format={texture.format} rect={r.x},{r.y},{r.width},{r.height} alpha={min}-{max}");
                }
            }
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); LocalizationSettings.SelectedLocale = original; }
    }
}
