#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

// Enabled in the experimental split package only. Test APK can opt in via intent.
internal sealed class AndroidOriginalLocalizationRuntime : MonoBehaviour
{
    internal static int AppliedCount;
    internal static bool Ready;
    private bool started;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        string package = activity.Call<string>("getPackageName");
        bool test = package == "com.megabonk.nativeport.test" && intent.Call<bool>("getBooleanExtra", "megabonk_original_localization_runtime_probe", false);
        if (package != "com.megabonk.nativeport.splittest" && !test &&
            !intent.Call<bool>("getBooleanExtra", "openbonk_split_launcher", false)) return;
        var root = new GameObject("Original localization runtime"); DontDestroyOnLoad(root); root.AddComponent<AndroidOriginalLocalizationRuntime>();
    }
    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        while (SceneManager.GetActiveScene().name != "MainMenu" || !SaveManager.loaded) yield return null;
        LocalizationSettings.SelectedLocaleChanged += Changed; started = true; Ready = true;
        Changed(LocalizationSettings.SelectedLocale);
    }
    private void OnDestroy() { if (started) LocalizationSettings.SelectedLocaleChanged -= Changed; }
    private void Changed(Locale locale)
    {
        if (locale == null || AndroidOriginalLocalizationCatalog.Find(locale.Identifier.Code) == null) return;
        StartCoroutine(ApplyOnNextFrame(locale));
    }
    private IEnumerator ApplyOnNextFrame(Locale locale)
    {
        yield return null;
        if (LocalizationSettings.SelectedLocale != locale) yield break;
        string root = Path.Combine(Application.persistentDataPath, "OriginalLocalization");
        string active = AndroidOriginalLocalizationStore.ActiveDirectory(root, locale.Identifier.Code);
        if (active == null) yield break;
        AssetBundle shared = null, strings = null;
        try
        {
            shared = AssetBundle.LoadFromFile(Path.Combine(active, "shared.bundle"));
            strings = AssetBundle.LoadFromFile(Path.Combine(active, AndroidOriginalLocalizationCatalog.Find(locale.Identifier.Code).StoredFile));
            if (shared == null || strings == null) throw new InvalidDataException("Cannot load stored localization bundles");
            var tables = new List<StringTable>();
            foreach (string name in strings.GetAllAssetNames())
            {
                var table = strings.LoadAsset<StringTable>(name);
                if (table == null) throw new InvalidDataException("Invalid stored localization table");
                tables.Add(table);
            }
            int count = AndroidOriginalLocalizationOverlay.Apply(tables, locale);
            foreach (var component in FindObjectsOfType<LocalizeStringEvent>()) component.RefreshString();
            AppliedCount++;
            Debug.Log($"Stored original localization applied: locale={locale.Identifier.Code} entries={count} count={AppliedCount}");
        }
        catch (Exception e) { Debug.LogWarning("Stored original localization unavailable: " + e.Message); }
        finally { if (strings != null) strings.Unload(true); if (shared != null) shared.Unload(true); }
    }
}
#endif
