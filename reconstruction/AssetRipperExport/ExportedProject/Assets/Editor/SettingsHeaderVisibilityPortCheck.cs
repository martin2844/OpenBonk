using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using UnityEditor;
using UnityEngine;

public static class SettingsHeaderVisibilityPortCheck
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static void Check(bool ok, string message) { if (!ok) throw new Exception("SettingsHeaderVisibilityPortCheck: " + message); }
    static void Call(SettingsHeaderMonoObject component, string method) => typeof(SettingsHeaderMonoObject).GetMethod(method, Private).Invoke(component, null);
    public static void Run()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/=HEADER=.prefab");
        Check(prefab != null && prefab.GetComponent<SettingsHeaderMonoObject>() != null, "original header prefab");
        var previousListeners = CurrentSettings.A_SettingUpdated; CurrentSettings.A_SettingUpdated = null;
        var oldManager = SaveManager.Instance;
        var state = new GameObject("settings header test state"); state.SetActive(false);
        var save = state.AddComponent<SaveManager>(); save.config = new ConfigSaveFile();
        typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { save });
        var advanced = UnityEngine.Object.Instantiate(prefab); var basic = UnityEngine.Object.Instantiate(prefab);
        var a = advanced.GetComponent<SettingsHeaderMonoObject>(); var b = basic.GetComponent<SettingsHeaderMonoObject>();
        try
        {
            a.Set(new SettingHeader(25, "ADVANCED")); b.Set(new SettingHeader(0, "GAME"));
            Call(a, "Awake"); Call(b, "Awake");
            save.config.cfGameSettings.show_advanced_settings = 0; Call(a, "Start"); Call(b, "Start");
            Check(!advanced.activeSelf && basic.activeSelf, "advanced hidden while ordinary header stays visible");
            save.config.cfGameSettings.show_advanced_settings = 1;
            CurrentSettings.A_SettingUpdated?.Invoke("other", 0, 1);
            Check(!advanced.activeSelf, "unrelated setting does not refresh header");
            CurrentSettings.A_SettingUpdated?.Invoke("show_advanced_settings", 0, 1);
            Check(advanced.activeSelf && basic.activeSelf, "advanced header reappears even while inactive");
            save.config.cfGameSettings.show_advanced_settings = 0;
            CurrentSettings.A_SettingUpdated?.Invoke("show_advanced_settings", 1, 0);
            Check(!advanced.activeSelf && basic.activeSelf, "advanced header hides again");
            Call(a, "OnDestroy"); Call(b, "OnDestroy");
            Check(CurrentSettings.A_SettingUpdated == null, "listeners removed");
            Debug.Log("SettingsHeaderVisibilityPortCheck passed: original prefab, advanced setting events, ordinary header and cleanup");
        }
        finally
        {
            CurrentSettings.A_SettingUpdated = previousListeners;
            typeof(SaveManager).GetProperty("Instance").GetSetMethod(true).Invoke(null, new object[] { oldManager });
            UnityEngine.Object.DestroyImmediate(advanced); UnityEngine.Object.DestroyImmediate(basic); UnityEngine.Object.DestroyImmediate(state);
        }
    }
}
