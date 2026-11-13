#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Localization.Settings;

internal sealed class AndroidResultsGlyphAudit : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_results_glyph_audit", false)) return;
        var go = new GameObject("results glyph audit"); DontDestroyOnLoad(go); go.AddComponent<AndroidResultsGlyphAudit>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 300f;
        TMP_Text label = null;
        using (var sampleUnity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var sampleActivity = sampleUnity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var sampleIntent = sampleActivity.Call<AndroidJavaObject>("getIntent"))
        {
            if (sampleIntent.Call<bool>("getBooleanExtra", "megabonk_results_glyph_sample", false))
            {
                yield return LocalizationSettings.InitializationOperation;
                LocalizationPrewarmer prewarmer = null;
                while (Time.realtimeSinceStartup < deadline && (prewarmer = FindObjectOfType<LocalizationPrewarmer>()) == null) yield return null;
                var locale = LocalizationSettings.AvailableLocales.GetLocale("es-419");
                if (prewarmer == null || locale == null) { Debug.LogError("Results glyph audit INCONCLUSIVE: prewarmer/locale unavailable"); yield break; }
                LocalizationSettings.SelectedLocale = locale;
                prewarmer.PrewarmFontAsset(prewarmer.notoDefault, locale);
                var canvasObject = new GameObject("glyph sample canvas", typeof(RectTransform), typeof(Canvas));
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
                var textObject = new GameObject("glyph sample", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(canvasObject.transform, false);
                var sample = textObject.GetComponent<TextMeshProUGUI>(); sample.font = prewarmer.mainFont; sample.fontSize = 70f; sample.color = Color.white;
                sample.alignment = TextAlignmentOptions.Center; sample.fontStyle = FontStyles.UpperCase; sample.text = "Daño";
                sample.rectTransform.anchorMin = Vector2.zero; sample.rectTransform.anchorMax = Vector2.one; sample.rectTransform.offsetMin = Vector2.zero; sample.rectTransform.offsetMax = Vector2.zero;
                label = sample;
            }
        }
        while (label == null && Time.realtimeSinceStartup < deadline)
        {
            foreach (var candidate in Resources.FindObjectsOfTypeAll<TextMeshProUGUI>())
                if (candidate.gameObject.scene.IsValid() && candidate.gameObject.activeInHierarchy && candidate.text == "Daño" && (candidate.fontStyle & FontStyles.UpperCase) != 0) { label = candidate; break; }
            if (label != null) break;
            yield return new WaitForSecondsRealtime(0.2f);
        }
        if (label == null) { Debug.LogError("Results glyph audit INCONCLUSIVE: damage label not visible"); yield break; }
        label.ForceMeshUpdate();
        Debug.Log($"Results glyph audit: name={label.name} text={label.text} style={label.fontStyle} primary={label.font.name} chars={label.textInfo.characterCount} materials={label.textInfo.materialCount}");
        for (int i = 0; i < label.textInfo.characterCount; i++)
        {
            var c = label.textInfo.characterInfo[i];
            var mesh = label.textInfo.meshInfo[c.materialReferenceIndex];
            var mat = mesh.material;
            var tex = mat != null ? mat.mainTexture : null;
            var color = mesh.colors32 != null && c.vertexIndex < mesh.colors32.Length ? mesh.colors32[c.vertexIndex] : default;
            Debug.Log($"Results glyph audit: i={i} char={(int)c.character:X4} unicode={(c.textElement != null ? c.textElement.unicode.ToString("X4") : "null")} font={(c.fontAsset != null ? c.fontAsset.name : "null")} visible={c.isVisible} material={c.materialReferenceIndex} shader={(mat != null && mat.shader != null ? mat.shader.name : "null")} tex={(tex != null ? tex.name : "null")} alpha={color.a} bounds={c.bottomLeft}/{c.topRight}");
            if (mat != null) Debug.Log($"Results glyph audit: material={mat.name} face={mat.GetColor("_FaceColor")} gradient={mat.GetFloat("_GradientScale")} stencil={mat.GetFloat("_Stencil")} stencilComp={mat.GetFloat("_StencilComp")} width={mat.GetFloat("_TextureWidth")} height={mat.GetFloat("_TextureHeight")}");
            if (i == 2 && tex is Texture2D texture && texture.isReadable && c.textElement != null && c.textElement.glyph != null)
            {
                var rect = c.textElement.glyph.glyphRect; int max = 0, min = 255;
                var pixels = texture.GetPixels32();
                foreach (var pixel in pixels) { if (pixel.a > max) max = pixel.a; if (pixel.a < min) min = pixel.a; }
                int regionMin = 255, regionMax = 0;
                for (int y = rect.y; y < rect.y + rect.height; y++) for (int x = rect.x; x < rect.x + rect.width; x++)
                { var alpha = pixels[y * texture.width + x].a; regionMin = Mathf.Min(regionMin, alpha); regionMax = Mathf.Max(regionMax, alpha); }
                Debug.Log($"Results glyph audit: atlas={texture.name} size={texture.width}x{texture.height} format={texture.format} rect={rect.x},{rect.y},{rect.width},{rect.height} alphaRange={min}-{max}");
                Debug.Log($"Results glyph audit: glyphAlphaRange={regionMin}-{regionMax}");
            }
        }
        foreach (var sub in label.GetComponentsInChildren<TMP_SubMeshUI>(true))
            Debug.Log($"Results glyph audit: submesh={sub.name} active={sub.gameObject.activeInHierarchy} enabled={sub.enabled} culled={sub.canvasRenderer.cull} material={(sub.materialForRendering != null ? sub.materialForRendering.name : "null")}");
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (intent.Call<bool>("getBooleanExtra", "megabonk_results_glyph_fresh", false))
        {
            var oldFont = label.textInfo.characterInfo[2].fontAsset;
            var source = oldFont != null ? oldFont.sourceFontFile : null;
            if (source == null) { Debug.LogError("Results glyph fresh INCONCLUSIVE: source font unavailable"); yield break; }
            var fresh = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fresh == null) { Debug.LogError("Results glyph fresh INCONCLUSIVE: could not create font"); yield break; }
            bool added = fresh.TryAddCharacters("Ñ", out var missing);
            var createdGlyph = fresh.characterLookupTable[0x00D1].glyph;
            var createdAtlas = fresh.atlasTextures[createdGlyph.atlasIndex]; var createdRect = createdGlyph.glyphRect;
            var createdPixels = createdAtlas.GetPixels32(); int createdMax = 0;
            for (int y = createdRect.y; y < createdRect.y + createdRect.height; y++) for (int x = createdRect.x; x < createdRect.x + createdRect.width; x++) createdMax = Mathf.Max(createdMax, createdPixels[y * createdAtlas.width + x].a);
            Debug.Log($"Results glyph fresh atlas: format={createdAtlas.format} glyphAlphaMax={createdMax} rect={createdRect.x},{createdRect.y},{createdRect.width},{createdRect.height}");
            var oldFallbacks = label.font.fallbackFontAssetTable;
            label.font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fresh };
            if (oldFallbacks != null) label.font.fallbackFontAssetTable.AddRange(oldFallbacks);
            if (label.font.characterLookupTable.TryGetValue(0x00D1, out var cached) && cached.textAsset != label.font)
            {
                label.font.characterLookupTable.Remove(0x00D1);
                Debug.Log("Results glyph fresh: removed cached fallback character from primary font");
            }
            label.text = ""; label.ForceMeshUpdate(true, true); label.text = "Daño"; label.SetAllDirty(); label.ForceMeshUpdate(true, true);
            var c = label.textInfo.characterInfo[2]; var glyph = c.textElement.glyph;
            var atlas = c.fontAsset.atlasTextures[glyph.atlasIndex]; var rect = glyph.glyphRect;
            var pixels = atlas.GetPixels32(); int max = 0;
            for (int y = rect.y; y < rect.y + rect.height; y++) for (int x = rect.x; x < rect.x + rect.width; x++) max = Mathf.Max(max, pixels[y * atlas.width + x].a);
            Debug.Log($"Results glyph fresh: added={added} missing={missing} selectedFresh={c.fontAsset == fresh} format={atlas.format} glyphAlphaMax={max} source={source.name}");
        }
    }
}
#endif
