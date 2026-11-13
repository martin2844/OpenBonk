#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System.Collections;
using Assets.Scripts.UI.DynamicWindows;
using UnityEngine;
using UnityEngine.Localization.Settings;

internal sealed class AndroidChestSkipWarningProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_chest_skip_warning_probe", false)) return;
        var root = new GameObject("chest skip warning probe"); DontDestroyOnLoad(root); root.AddComponent<AndroidChestSkipWarningProbe>();
    }
    IEnumerator Start()
    {
        float deadline = Time.realtimeSinceStartup + 120f;
        while (Time.realtimeSinceStartup < deadline && (!SaveManager.loaded || SaveManager.Instance?.config?.preferences == null || CurrentSettings.Instance == null || AlwaysUi.Instance == null || AlwaysUi.Instance.dynamicWindows == null)) yield return null;
        if (!SaveManager.loaded || AlwaysUi.Instance == null) { Debug.LogError("Chest skip warning probe: services unavailable"); yield break; }
        yield return LocalizationSettings.InitializationOperation;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new UnityEngine.Localization.LocaleIdentifier("es"));
        if (locale == null) { Debug.LogError("Chest skip warning probe: Spanish locale unavailable"); yield break; }
        LocalizationSettings.SelectedLocale = locale;
        yield return null;
        var config = SaveManager.Instance.config;
        config.preferences.hasShownWarningForChestSkip = false;
        var windows = AlwaysUi.Instance.dynamicWindows;
        int before = windows.contentParent.childCount;
        CurrentSettings.Instance.BetterUpdateCfSettings("skip_chest_animation", 1, config.cfGameSettings);
        yield return new WaitForSecondsRealtime(1f);
        int after = windows.contentParent.childCount;
        var warning = after > before ? windows.contentParent.GetChild(after - 1).GetComponent<DWindow>() : null;
        Debug.Log($"Chest skip warning probe: before={before} after={after} shown={config.preferences.hasShownWarningForChestSkip} title={warning?.t_header.text} content={warning?.t_content.text}");
        CurrentSettings.Instance.BetterUpdateCfSettings("skip_chest_animation", 0, config.cfGameSettings);
        CurrentSettings.Instance.BetterUpdateCfSettings("skip_chest_animation", 1, config.cfGameSettings);
        Debug.Log($"Chest skip warning probe repeat: windows={windows.contentParent.childCount} expected={after}");
    }
}
#endif
