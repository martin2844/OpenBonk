#if UNITY_ANDROID && !UNITY_EDITOR && !OPENBONK_BETA_NO_PROBES
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Assets.Scripts.UI.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

internal sealed class AndroidOriginalLocalizationUiProbe : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        using var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity = unity.GetStatic<AndroidJavaObject>("currentActivity");
        using var intent = activity.Call<AndroidJavaObject>("getIntent");
        if (!intent.Call<bool>("getBooleanExtra", "megabonk_localization_ui_probe", false)) return;
        var go = new GameObject("Original localization UI probe"); DontDestroyOnLoad(go); go.AddComponent<AndroidOriginalLocalizationUiProbe>();
    }
    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;
        float deadline = Time.realtimeSinceStartup + 120;
        while (Time.realtimeSinceStartup < deadline && SceneManager.GetActiveScene().name != "MainMenu") yield return null;
        if (SceneManager.GetActiveScene().name != "MainMenu") { Debug.LogError("Original localization UI probe: no main menu"); yield break; }
        var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("es"));
        if (locale == null) { Debug.LogError("Original localization UI probe: no Spanish locale"); yield break; }
        LocalizationSettings.SelectedLocale = locale; yield return null;
        AssetBundle shared = null, strings = null;
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "Probe");
            shared = AssetBundle.LoadFromFile(Path.Combine(dir, "shared.bundle"));
            strings = AssetBundle.LoadFromFile(Path.Combine(dir, "linux.bundle"));
            if (shared == null || strings == null) throw new InvalidDataException("converted bundles could not load");
            var tables = new List<StringTable>();
            foreach (var name in strings.GetAllAssetNames()) { var table = strings.LoadAsset<StringTable>(name); if (table != null) tables.Add(table); }
            var menu = tables.Find(t => t.SharedData.TableCollectionName == "Main Menu");
            var play = menu.GetEntry("PLAY"); string original = play.LocalizedValue; long id = play.KeyId;
            var controls = LocalizationSettings.StringDatabase.GetTable("SettingsControls", locale);
            var before = controls.Values.ToDictionary(x => x.KeyId, x => x.LocalizedValue);
            menu.AddEntry(id, "PRUEBA ZIP");
            int count = AndroidOriginalLocalizationOverlay.Apply(tables, locale);
            string value = LocalizationUtility.GetLocalizedString("Main Menu", "PLAY");
            bool controlsPreserved = controls.Values.Count == before.Count && controls.Values.All(x => before.TryGetValue(x.KeyId, out var v) && v == x.LocalizedValue);
            foreach (var component in FindObjectsOfType<LocalizeStringEvent>()) component.RefreshString();
            Debug.Log($"Original localization UI probe applied={count} value={value} controlsPreserved={controlsPreserved}");
            if (value != "PRUEBA ZIP" || !controlsPreserved) throw new Exception("overlay assertion failed");
            StartCoroutine(VerifyAndRestore(LocalizationSettings.StringDatabase.GetTable("Main Menu", locale), id, original));
        }
        catch (Exception e) { Debug.LogError("Original localization UI probe FAILED " + e); }
        finally { if (strings != null) strings.Unload(true); if (shared != null) shared.Unload(true); }
    }
    private IEnumerator VerifyAndRestore(StringTable menu, long id, string original)
    {
        yield return new WaitForSecondsRealtime(3);
        bool visible = FindObjectsOfType<TMP_Text>().Any(t => t.gameObject.activeInHierarchy && t.text == "PRUEBA ZIP");
        Debug.Log($"Original localization UI probe activeLabel={visible}");
        menu.AddEntry(id, original);
        foreach (var component in FindObjectsOfType<LocalizeStringEvent>()) component.RefreshString();
        Debug.Log($"Original localization UI probe {(visible ? "PASS" : "INCONCLUSIVE")} restored={LocalizationUtility.GetLocalizedString("Main Menu", "PLAY")}");
    }
}
#endif
