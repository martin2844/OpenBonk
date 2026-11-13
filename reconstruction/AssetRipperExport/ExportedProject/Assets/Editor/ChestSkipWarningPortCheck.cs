using System;
using System.Reflection;
using Assets.Scripts.Settings___Saves.SaveFiles.ConfigSaves;
using Assets.Scripts.UI.DynamicWindows;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Settings;

public static class ChestSkipWarningPortCheck
{
    static void Require(bool ok, string message) { if (!ok) throw new Exception("ChestSkipWarningPortCheck: " + message); }
    public static void Run()
    {
        LocalizationSettings.Instance = AssetDatabase.LoadAssetAtPath<LocalizationSettings>("Assets/MonoBehaviour/Localization Settings.asset");
        var previousSave = SaveManager.Instance; var previousUi = AlwaysUi.Instance;
        var setSave = typeof(SaveManager).GetProperty("Instance").GetSetMethod(true);
        var root = new GameObject("chest warning fixture", typeof(RectTransform)); root.SetActive(false);
        var save = root.AddComponent<SaveManager>(); save.config = new ConfigSaveFile(); setSave.Invoke(null, new object[] { save });
        var ui = root.AddComponent<AlwaysUi>(); AlwaysUi.Instance = ui;
        var windows = root.AddComponent<DynamicWindows>(); windows.contentParent = root.transform;
        windows.dynamicWindowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/GameObject/DynamicWindow.prefab");
        var settings = root.AddComponent<CurrentSettings>();
        try
        {
            Require(windows.dynamicWindowPrefab != null, "original window prefab");
            settings.BetterUpdateCfSettings("skip_chest_animation", 1, save.config.cfGameSettings);
            Require(!save.config.preferences.hasShownWarningForChestSkip && root.transform.childCount == 0, "wait if window service unavailable");
            ui.dynamicWindows = windows;
            settings.BetterUpdateCfSettings("skip_chest_animation", 0, save.config.cfGameSettings);
            Require(!save.config.preferences.hasShownWarningForChestSkip && root.transform.childCount == 0, "off does not warn");
            settings.BetterUpdateCfSettings("skip_chest_animation", 1, save.config.cfGameSettings);
            Require(save.config.preferences.hasShownWarningForChestSkip && root.transform.childCount == 1, "first enable shows one warning");
            var window = root.transform.GetChild(0).GetComponent<DWindow>();
            Require(window != null && !string.IsNullOrEmpty(window.t_header.text) && !string.IsNullOrEmpty(window.t_content.text), "warning text initialized");
            settings.BetterUpdateCfSettings("skip_chest_animation", 0, save.config.cfGameSettings);
            settings.BetterUpdateCfSettings("skip_chest_animation", 1, save.config.cfGameSettings);
            Require(root.transform.childCount == 1, "warning shown only once");
            Debug.Log("ChestSkipWarningPortCheck passed: setting updates, original window, one-time preference and unavailable service");
        }
        finally { AlwaysUi.Instance = previousUi; setSave.Invoke(null, new object[] { previousSave }); UnityEngine.Object.DestroyImmediate(root); }
    }
}
