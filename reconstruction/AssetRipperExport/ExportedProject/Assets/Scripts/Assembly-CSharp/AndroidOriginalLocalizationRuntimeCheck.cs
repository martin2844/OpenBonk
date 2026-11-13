#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

internal sealed class AndroidOriginalLocalizationRuntimeCheck : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_original_localization_runtime_probe", false)) return;
        var go = new GameObject("Original localization runtime check"); DontDestroyOnLoad(go); go.AddComponent<AndroidOriginalLocalizationRuntimeCheck>();
    }
    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        while (SceneManager.GetActiveScene().name != "MainMenu" || !SaveManager.loaded) yield return null;
        while (!AndroidOriginalLocalizationRuntime.Ready) yield return null;
        foreach (var language in AndroidOriginalLocalizationCatalog.All)
        {
            string code = language.Code;
            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            var other = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code == "en" ? "es" : "en"));
            LocalizationSettings.SelectedLocale = other; yield return null;
            var controls = LocalizationSettings.StringDatabase.GetTable("SettingsControls", locale);
            var before = controls.Values.ToDictionary(x => x.KeyId, x => x.LocalizedValue);
            int previous = AndroidOriginalLocalizationRuntime.AppliedCount;
            LocalizationSettings.SelectedLocale = locale;
            float deadline = Time.realtimeSinceStartup + 40;
            while (Time.realtimeSinceStartup < deadline && AndroidOriginalLocalizationRuntime.AppliedCount == previous) yield return null;
            bool preserved = before.Count == controls.Values.Count && controls.Values.All(x => before.TryGetValue(x.KeyId, out var v) && x.LocalizedValue == v);
            bool applied = AndroidOriginalLocalizationRuntime.AppliedCount > previous;
            Debug.Log($"Original localization runtime check {(applied && preserved ? "PASS" : "FAIL")} locale={code} applied={applied} controlsPreserved={preserved}");
        }
    }
}
#endif
