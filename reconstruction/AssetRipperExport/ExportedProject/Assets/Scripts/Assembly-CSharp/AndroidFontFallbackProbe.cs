#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Opt-in diagnostic that displays a localized sample with the scene's actual main font.
internal sealed class AndroidFontFallbackProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_font_fallback_probe", false)) return;
        var root = new GameObject("font fallback probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidFontFallbackProbe>();
    }
    IEnumerator Start()
    {
        string code;
        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var activity = unity.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = activity.Call<AndroidJavaObject>("getIntent")) code = intent.Call<string>("getStringExtra", "megabonk_font_locale") ?? "ja";
        yield return LocalizationSettings.InitializationOperation;
        float deadline = Time.realtimeSinceStartup + 100f;
        LocalizationPrewarmer prewarmer = null;
        while (Time.realtimeSinceStartup < deadline && (prewarmer = FindObjectOfType<LocalizationPrewarmer>()) == null) yield return null;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
        if (prewarmer == null || locale == null || prewarmer.mainFont == null) { Debug.LogError("Font fallback probe: scene or locale missing " + code); yield break; }
        LocalizationSettings.SelectedLocale = locale;
        yield return null;
        var panel = new GameObject("font sample canvas", typeof(RectTransform), typeof(Canvas));
        var canvas = panel.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        var background = new GameObject("font sample", typeof(RectTransform), typeof(TextMeshProUGUI));
        background.transform.SetParent(panel.transform, false);
        var label = background.GetComponent<TextMeshProUGUI>(); label.font = prewarmer.mainFont; label.fontSize = 70f; label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center; label.text = code.StartsWith("ja") ? "日本語 文字 表示" : "中文 字体 显示";
        var rect = label.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        label.ForceMeshUpdate();
        int visible = 0, selectedFont = 0, missing = 0;
        TMP_FontAsset expected = code.StartsWith("ja") ? prewarmer.notoJp : prewarmer.notoSc;
        for (int i = 0; i < label.textInfo.characterCount; i++)
        {
            var character = label.textInfo.characterInfo[i];
            if (!character.isVisible) continue;
            visible++;
            if (character.fontAsset == expected) selectedFont++;
            if (character.character == '\u25a1' || character.character == '\ufffd') missing++;
        }
        Debug.Log($"Font fallback probe: locale={code}, text={label.text}, visible={visible}, selectedFont={selectedFont}, missing={missing}, firstFallback={(prewarmer.mainFont.fallbackFontAssetTable.Count > 0 ? prewarmer.mainFont.fallbackFontAssetTable[0].name : "none")}");
    }
}
#endif
