#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

internal sealed class AndroidSpanishGlyphCoverageProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_spanish_glyph_coverage", false)) return;
        var root = new GameObject("spanish glyph coverage probe");
        DontDestroyOnLoad(root);
        root.AddComponent<AndroidSpanishGlyphCoverageProbe>();
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        float deadline = Time.realtimeSinceStartup + 120f;
        LocalizationPrewarmer prewarmer = null;
        while (Time.realtimeSinceStartup < deadline && (prewarmer = FindObjectOfType<LocalizationPrewarmer>()) == null)
            yield return null;
        var locale = LocalizationSettings.AvailableLocales.GetLocale("es-419");
        if (prewarmer == null || locale == null || prewarmer.mainFont == null || prewarmer.notoDefault == null)
        { Debug.LogError("Spanish glyph coverage INCONCLUSIVE: locale or font setup unavailable"); yield break; }
        LocalizationSettings.SelectedLocale = locale;
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
        var canvas = new GameObject("glyph audit canvas", typeof(RectTransform), typeof(Canvas));
        var label = new GameObject("glyph audit text", typeof(RectTransform), typeof(TextMeshProUGUI));
        label.transform.SetParent(canvas.transform, false);
        var text = label.GetComponent<TextMeshProUGUI>();
        text.font = prewarmer.mainFont; text.text = new string(new List<char>(letters).ToArray());
        text.ForceMeshUpdate();
        if (text.textInfo.characterCount != letters.Count)
        { Debug.LogError("Spanish glyph coverage INCONCLUSIVE: processed character count differs"); Destroy(canvas); yield break; }
        var pixelsByAtlas = new Dictionary<Texture2D, Color32[]>();
        int empty = 0, missing = 0, unreadable = 0, populated = 0;
        for (int i = 0; i < text.textInfo.characterCount; i++)
        {
            var c = text.textInfo.characterInfo[i]; char expected = text.text[i];
            if (c.textElement == null || c.textElement.unicode != expected || c.textElement.glyph == null || c.fontAsset == null)
            { missing++; Debug.LogWarning($"Spanish glyph missing: U+{(int)expected:X4}"); continue; }
            var glyph = c.textElement.glyph; var atlas = c.fontAsset.atlasTextures[glyph.atlasIndex];
            if (atlas == null || !atlas.isReadable)
            { unreadable++; Debug.LogWarning($"Spanish glyph unreadable: U+{(int)expected:X4}"); continue; }
            if (!pixelsByAtlas.TryGetValue(atlas, out var pixels)) pixelsByAtlas.Add(atlas, pixels = atlas.GetPixels32());
            var rect = glyph.glyphRect; int alpha = 0;
            for (int y = rect.y; y < rect.y + rect.height; y++)
                for (int x = rect.x; x < rect.x + rect.width; x++) alpha = Math.Max(alpha, pixels[y * atlas.width + x].a);
            if (alpha == 0) { empty++; Debug.LogWarning($"Spanish glyph empty: U+{(int)expected:X4} font={c.fontAsset.name} atlas={glyph.atlasIndex}"); }
            else populated++;
        }
        Debug.Log($"Spanish glyph coverage {(empty == 0 && missing == 0 && unreadable == 0 ? "PASS" : "FAIL")}: tables={tables} letters={letters.Count} populated={populated} empty={empty} missing={missing} unreadable={unreadable}");
        Destroy(canvas);
    }
}
#endif
