using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class SpanishGlyphCoverageAudit
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
            if (prewarmer == null || prewarmer.mainFont == null || prewarmer.notoDefault == null)
                throw new Exception("Font setup unavailable");
            prewarmer.PrewarmFontAsset(prewarmer.notoDefault, locale);
            var letters = new SortedSet<char>(); int tables = 0;
            foreach (var tableName in prewarmer.tableNamesToPreload)
            {
                var table = LocalizationSettings.StringDatabase.GetTable(tableName, locale);
                if (table == null) continue;
                tables++;
                foreach (var entry in table.Values)
                    foreach (char c in entry?.LocalizedValue ?? string.Empty)
                        if (char.IsLetter(c)) { letters.Add(c); letters.Add(char.ToUpperInvariant(c)); }
            }
            root = new GameObject("spanish glyph audit", typeof(RectTransform), typeof(Canvas));
            var go = new GameObject("label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(root.transform, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = prewarmer.mainFont; text.text = new string(new List<char>(letters).ToArray());
            text.ForceMeshUpdate();
            if (text.textInfo.characterCount != letters.Count) throw new Exception("Processed character count differs from input");
            var pixelsByAtlas = new Dictionary<Texture2D, Color32[]>();
            int empty = 0, missing = 0, visible = 0;
            for (int i = 0; i < text.textInfo.characterCount; i++)
            {
                var c = text.textInfo.characterInfo[i];
                char expected = text.text[i];
                if (c.textElement == null || c.textElement.unicode != expected || c.textElement.glyph == null || c.fontAsset == null)
                { missing++; Debug.LogWarning($"Spanish glyph missing: U+{(int)expected:X4}"); continue; }
                var glyph = c.textElement.glyph; var atlas = c.fontAsset.atlasTextures[glyph.atlasIndex];
                if (atlas == null || !atlas.isReadable) throw new Exception("Atlas unavailable for " + c.character);
                if (!pixelsByAtlas.TryGetValue(atlas, out var pixels)) pixelsByAtlas.Add(atlas, pixels = atlas.GetPixels32());
                var rect = glyph.glyphRect; int alpha = 0;
                for (int y = rect.y; y < rect.y + rect.height; y++)
                    for (int x = rect.x; x < rect.x + rect.width; x++) alpha = Math.Max(alpha, pixels[y * atlas.width + x].a);
                if (alpha == 0) { empty++; Debug.LogWarning($"Spanish glyph empty: U+{(int)c.character:X4} '{c.character}' font={c.fontAsset.name} atlas={glyph.atlasIndex}"); }
                else visible++;
            }
            Debug.Log($"Spanish glyph coverage: tables={tables} letters={letters.Count} characters={text.textInfo.characterCount} visible={visible} empty={empty} missing={missing}");
        }
        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); LocalizationSettings.SelectedLocale = original; }
    }
}
